using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Internal.Press.Database;
using LlmTornado.Internal.Press.Database.Models;
using LlmTornado.Internal.Press.Export;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

/// <summary>
/// Main orchestration configuration for article generation with guided autonomy and review loop
/// </summary>
public class ArticleOrchestrationConfiguration : OrchestrationRuntimeConfiguration
{
    private readonly TornadoApi _client;
    private readonly AppConfiguration _config;
    private readonly PressDbContext _dbContext;
    private readonly MarkdownExporter _markdownExporter;
    private readonly JsonExporter _jsonExporter;

    // Runnables for the orchestration flow
    private EntryRunnable? _entry;
    private TrendAnalysisRunnable? _trendAnalysis;
    private IdeationRunnable? _ideation;
    private QueueToIdeaRunnable? _queueToIdea;
    private ResearchRunnable? _research;
    private WritingRunnable? _writing;
    private ReviewRunnable? _review;
    private ImprovementRunnable? _improvement;
    private ReviewToArticleRunnable? _reviewToArticle;
    private ImageGenerationRunnable? _imageGeneration;
    private ExportRunnable? _export;
    private SaveArticleRunnable? _saveArticle;
    private CompletionRunnable? _completion;
    private FailureCompletionRunnable? _failureCompletion;

    // Current context
    private ArticleQueue? _currentQueue;
    private int _currentIteration = 0;

    public ArticleOrchestrationConfiguration(
        TornadoApi client,
        AppConfiguration config,
        PressDbContext dbContext)
    {
        _client = client;
        _config = config;
        _dbContext = dbContext;
        _markdownExporter = new MarkdownExporter(config.Output.Directory);
        _jsonExporter = new JsonExporter(config.Output.Directory);

        RecordSteps = true;
        SetupOrchestration();
    }

    public override async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        // Invoke the StartingExecution event
        OnRuntimeEvent?.Invoke(new LlmTornado.Agents.DataModels.ChatRuntimeStartedEvent(Runtime.Id));

        // Add orchestration event listeners for debugging
        OnOrchestrationEvent += (e) =>
        {
            switch (e)
            {
                case OnStartedRunnableEvent startedEvent:
                    Console.WriteLine($"    → Starting runnable: {startedEvent.RunnableBase.GetType().Name}");
                    break;
                case OnFinishedRunnableEvent finishedEvent:
                    Console.WriteLine($"    ✓ Finished runnable: {finishedEvent.Runnable.GetType().Name}");
                    break;
                case OnErrorOrchestrationEvent errorEvent:
                    Console.WriteLine($"    ERROR: {errorEvent.Exception?.Message}; Stack: {errorEvent.Exception?.StackTrace}");
                    break;
            }
        };

        Console.WriteLine("  → Invoking orchestration...");
        
        try
        {
            await InvokeAsync(message);
            
            Console.WriteLine($"  → Orchestration invoked. Results count: {Results?.Length ?? 0}");
            
            // Get result from completion runnable
            if (Results != null && Results.Length > 0)
            {
                var resultMessage = Results.Last();
                Console.WriteLine($"  → Result: {resultMessage.Content}");
                OnRuntimeEvent?.Invoke(new LlmTornado.Agents.DataModels.ChatRuntimeCompletedEvent(Runtime.Id));
                return resultMessage;
            }
            else
            {
                Console.WriteLine("  ✗ No results from orchestration");
                var errorMessage = new ChatMessage(Code.ChatMessageRoles.Assistant, "Orchestration completed but no results were generated");
                OnRuntimeEvent?.Invoke(new LlmTornado.Agents.DataModels.ChatRuntimeCompletedEvent(Runtime.Id));
                return errorMessage;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Orchestration error: {ex.Message}");
            Console.WriteLine($"  Stack: {ex.StackTrace}");
            var errorMessage = new ChatMessage(Code.ChatMessageRoles.Assistant, $"Error: {ex.Message}");
            OnRuntimeEvent?.Invoke(new LlmTornado.Agents.DataModels.ChatRuntimeErrorEvent(ex, Runtime.Id));
            return errorMessage;
        }
    }

    private void SetupOrchestration()
    {
        // Initialize static runnables (those not dependent on specific article context)
        _entry = new EntryRunnable(this);
        _trendAnalysis = new TrendAnalysisRunnable(_client, _config, this);
        _ideation = new IdeationRunnable(_client, _config, this);
        _queueToIdea = new QueueToIdeaRunnable(this);
        
        // Review, image, export, save, completion runnables
        _review = new ReviewRunnable(_client, _config, this);
        _improvement = new ImprovementRunnable(this);
        _reviewToArticle = new ReviewToArticleRunnable(this);
        _imageGeneration = new ImageGenerationRunnable(_client, _config, this);
        _export = new ExportRunnable(_markdownExporter, _jsonExporter, _config, this);
        _saveArticle = new SaveArticleRunnable(_dbContext, _markdownExporter, _jsonExporter, this);
        _completion = new CompletionRunnable(this);
        _failureCompletion = new FailureCompletionRunnable(this);

        // Setup static orchestration flow
        // Entry → QueueToIdea (wired per article in SetCurrentQueue)
        // Dynamic flow (Research → Writing → Review) is wired in SetCurrentQueue()
        // Static flow: Review → [Improvement Loop OR Approve OR Fail] → Image → Export → Save → Complete
        
        // Entry → QueueToIdea
        _entry!.AddAdvancer((queue) => queue != null && !string.IsNullOrEmpty(queue.Title), _queueToIdea!);
        
        // Review loop with guided autonomy
        _review!.AddAdvancer(ShouldApprove, _reviewToArticle!);
        _review!.AddAdvancer(ShouldImprove, _improvement!);
        _review!.AddAdvancer(ShouldFail, _failureCompletion!);
        
        // Approved article → Image generation
        _reviewToArticle!.AddAdvancer((article) => !string.IsNullOrEmpty(article.Title), _imageGeneration!);
        
        // Image → Export → Save → Complete
        _imageGeneration!.AddAdvancer((img) => true, _export!);
        _export!.AddAdvancer((paths) => paths.MarkdownPath != null, _saveArticle!);
        _saveArticle!.AddAdvancer((article) => article.Id > 0, _completion!);

        // Entry and exit points
        SetEntryRunnable(_entry!);
        SetRunnableWithResult(_completion!); // Success exit
        SetRunnableWithResult(_failureCompletion!); // Failure exit
    }

    public void SetCurrentQueue(ArticleQueue queue)
    {
        _currentQueue = queue;
        _currentIteration = 0;
        
        // Store queue, objective, and article idea in context for access by runnables
        RuntimeProperties["CurrentQueue"] = queue;
        RuntimeProperties["Objective"] = _config.Objective;
        
        var articleIdea = new ArticleIdea
        {
            Title = queue.Title,
            IdeaSummary = queue.IdeaSummary,
            EstimatedRelevance = queue.EstimatedRelevance,
            Tags = JsonConvert.DeserializeObject<string[]>(queue.Tags) ?? Array.Empty<string>(),
            Reasoning = "From queue"
        };
        RuntimeProperties["CurrentArticleIdea"] = articleIdea;
        
        // Create article-specific runnables
        _research = new ResearchRunnable(_client, _config, this);
        _writing = new WritingRunnable(_client, _config, queue.Title, queue.IdeaSummary, this);
        
        // Wire the complete article generation flow:
        // QueueToIdea → Research → Writing → Review → [Improvement back to Research]
        _queueToIdea!.AddAdvancer((idea) => !string.IsNullOrEmpty(idea.Title), _research);
        _research.AddAdvancer((researchOutput) => researchOutput.Facts != null && researchOutput.Facts.Length > 0, _writing);
        _writing.AddAdvancer((articleOutput) => 
        {
            // Store article for later use (review → image)
            RuntimeProperties["CurrentArticle"] = articleOutput;
            return !string.IsNullOrEmpty(articleOutput.Title);
        }, _review!);
        
        // Wire improvement loop back to research
        _improvement!.AddAdvancer((idea) => !string.IsNullOrEmpty(idea.Title), _research);
    }

    // Decision functions for review loop (guided autonomy)
    private bool ShouldApprove(ReviewOutput review)
    {
        // Approve if:
        // 1. Review says approved, OR
        // 2. We've hit max iterations (force completion after trying to improve)
        // 3. AND no critical issues that would make the article unusable
        bool result = (review.Approved || _currentIteration >= _config.ReviewLoop.MaxIterations) 
                      && !HasCriticalIssues(review);
        
        Console.WriteLine($"  [Review Decision] ShouldApprove: {result} (Approved={review.Approved}, Iteration={_currentIteration}/{_config.ReviewLoop.MaxIterations}, Critical={HasCriticalIssues(review)})");
        return result;
    }

    private bool ShouldImprove(ReviewOutput review)
    {
        // Continue improving if:
        // 1. Not approved
        // 2. Has issues but not critical
        // 3. Haven't exceeded max iterations yet
        bool shouldImprove = !review.Approved && 
                            !HasCriticalIssues(review) && 
                            _currentIteration < _config.ReviewLoop.MaxIterations;
        
        Console.WriteLine($"  [Review Decision] ShouldImprove: {shouldImprove} (Approved={review.Approved}, Iteration={_currentIteration}/{_config.ReviewLoop.MaxIterations}, Critical={HasCriticalIssues(review)})");
        
        // Only increment if we're actually going to improve
        if (shouldImprove)
        {
            _currentIteration++;
            Console.WriteLine($"  [Review Decision] Incremented iteration to {_currentIteration}");
        }
        
        return shouldImprove;
    }

    private bool ShouldFail(ReviewOutput review)
    {
        // Only fail if there are CRITICAL issues that make the article unusable
        // Otherwise, after max iterations we just approve and publish what we have
        bool result = HasCriticalIssues(review);
        Console.WriteLine($"  [Review Decision] ShouldFail: {result} (Critical={result})");
        return result;
    }

    private bool HasCriticalIssues(ReviewOutput review)
    {
        return review.Issues != null && 
               review.Issues.Any(i => i.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase));
    }
}

// Helper runnables

public class EntryRunnable : OrchestrationRunnable<ChatMessage, ArticleQueue>
{
    public EntryRunnable(Orchestration orchestrator) : base(orchestrator) { }

    public override ValueTask<ArticleQueue> Invoke(RunnableProcess<ChatMessage, ArticleQueue> process)
    {
        Console.WriteLine($"  [EntryRunnable] Invoked with message: {process.Input?.Content}");
        
        // Get the queue item from orchestration context (set by SetCurrentQueue)
        if (Orchestrator?.RuntimeProperties.TryGetValue("CurrentQueue", out var queueObj) == true &&
            queueObj is ArticleQueue queue)
        {
            Console.WriteLine($"  [EntryRunnable] Found queue: {queue.Title}");
            return ValueTask.FromResult(queue);
        }

        // Fallback: shouldn't happen
        Console.WriteLine($"  [EntryRunnable] ERROR: Queue not found in RuntimeProperties");
        var props = Orchestrator?.RuntimeProperties.Keys.ToList() ?? new List<string>();
        Console.WriteLine($"  [EntryRunnable] Available properties: {string.Join(", ", props)}");
        return ValueTask.FromResult(new ArticleQueue
        {
            Title = "Error",
            IdeaSummary = "Queue not set",
            Status = QueueStatus.Failed
        });
    }
}

public class QueueToIdeaRunnable : OrchestrationRunnable<ArticleQueue, ArticleIdea>
{
    public QueueToIdeaRunnable(Orchestration orchestrator) : base(orchestrator) { }

    public override ValueTask<ArticleIdea> Invoke(RunnableProcess<ArticleQueue, ArticleIdea> process)
    {
        var queue = process.Input;
        var tags = JsonConvert.DeserializeObject<string[]>(queue.Tags) ?? Array.Empty<string>();
        
        return ValueTask.FromResult(new ArticleIdea
        {
            Title = queue.Title,
            IdeaSummary = queue.IdeaSummary,
            EstimatedRelevance = queue.EstimatedRelevance,
            Tags = tags,
            Reasoning = "From queue"
        });
    }
}

public class ImprovementRunnable : OrchestrationRunnable<ReviewOutput, ArticleIdea>
{
    public ImprovementRunnable(Orchestration orchestrator) : base(orchestrator) { }

    public override ValueTask<ArticleIdea> Invoke(RunnableProcess<ReviewOutput, ArticleIdea> process)
    {
        var review = process.Input;
        
        // Build improvement feedback for the next iteration
        var feedback = "The article needs improvement. Review feedback:\n\n";
        feedback += $"Quality Score: {review.QualityScore}/100\n\n";

        if (review.Issues != null && review.Issues.Length > 0)
        {
            feedback += "Issues to address:\n";
            foreach (var issue in review.Issues)
            {
                feedback += $"- [{issue.Severity}] {issue.Category}: {issue.Description}\n";
                if (!string.IsNullOrEmpty(issue.Suggestion))
                {
                    feedback += $"  Suggestion: {issue.Suggestion}\n";
                }
            }
            feedback += "\n";
        }

        if (review.Suggestions != null && review.Suggestions.Length > 0)
        {
            feedback += "Suggestions:\n";
            foreach (var suggestion in review.Suggestions)
            {
                feedback += $"- {suggestion}\n";
            }
        }

        // Store feedback in orchestration properties for next iteration
        Orchestrator?.RuntimeProperties["ImprovementFeedback"] = feedback;
        
        // Return the original article idea from context to re-research
        if (Orchestrator?.RuntimeProperties.TryGetValue("CurrentArticleIdea", out var ideaObj) == true &&
            ideaObj is ArticleIdea idea)
        {
            return ValueTask.FromResult(idea);
        }

        // Fallback: create basic idea from review context
        return ValueTask.FromResult(new ArticleIdea
        {
            Title = "Article Improvement",
            IdeaSummary = feedback,
            Tags = Array.Empty<string>(),
            EstimatedRelevance = 0.5,
            Reasoning = "Improvement iteration"
        });
    }
}

public class ReviewToArticleRunnable : OrchestrationRunnable<ReviewOutput, ArticleOutput>
{
    public ReviewToArticleRunnable(Orchestration orchestrator) : base(orchestrator) { }

    public override ValueTask<ArticleOutput> Invoke(RunnableProcess<ReviewOutput, ArticleOutput> process)
    {
        // Get the approved article from orchestration context
        if (Orchestrator?.RuntimeProperties.TryGetValue("CurrentArticle", out var articleObj) == true &&
            articleObj is ArticleOutput article)
        {
            return ValueTask.FromResult(article);
        }

        // Fallback: shouldn't happen, but handle gracefully
        return ValueTask.FromResult(new ArticleOutput
        {
            Title = "Error",
            Body = "Article not found in context",
            Description = "Error",
            Tags = Array.Empty<string>()
        });
    }
}

public class ExportRunnable : OrchestrationRunnable<ImageOutput, ExportPaths>
{
    private readonly MarkdownExporter _markdownExporter;
    private readonly JsonExporter _jsonExporter;
    private readonly AppConfiguration _config;

    public ExportRunnable(
        MarkdownExporter markdownExporter,
        JsonExporter jsonExporter,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _markdownExporter = markdownExporter;
        _jsonExporter = jsonExporter;
        _config = config;
    }

    public override async ValueTask<ExportPaths> Invoke(RunnableProcess<ImageOutput, ExportPaths> process)
    {
        var imageOutput = process.Input;
        
        // Store image URL for later use
        if (!string.IsNullOrEmpty(imageOutput.Url))
        {
            Orchestrator.RuntimeProperties["ImageUrl"] = imageOutput.Url;
        }

        // Export will happen after saving to database
        // For now, just pass through (actual export happens in SaveArticleRunnable or after)
        Console.WriteLine($"✓ Image generated: {imageOutput.Url}");

        return new ExportPaths
        {
            MarkdownPath = "pending",
            JsonPath = "pending"
        };
    }
}

public class SaveArticleRunnable : OrchestrationRunnable<ExportPaths, Article>
{
    private readonly PressDbContext _dbContext;
    private readonly MarkdownExporter _markdownExporter;
    private readonly JsonExporter _jsonExporter;

    public SaveArticleRunnable(PressDbContext dbContext, MarkdownExporter markdownExporter, JsonExporter jsonExporter, Orchestration orchestrator) : base(orchestrator)
    {
        _dbContext = dbContext;
        _markdownExporter = markdownExporter;
        _jsonExporter = jsonExporter;
    }

    public override async ValueTask<Article> Invoke(RunnableProcess<ExportPaths, Article> process)
    {
        // Get article from orchestration context
        if (!Orchestrator.RuntimeProperties.TryGetValue("CurrentArticle", out var articleOutputObj) ||
            articleOutputObj is not ArticleOutput articleOutput)
        {
            return new Article { Id = 0, Title = "Error: Article not found" };
        }

        try
        {
            // Get the objective from configuration
            string objective = "";
            if (Orchestrator.RuntimeProperties.TryGetValue("Objective", out var objObj) && objObj is string obj)
            {
                objective = obj;
            }

            // Create Article entity from ArticleOutput
            var article = new Article
            {
                Title = articleOutput.Title,
                Body = articleOutput.Body,
                Description = articleOutput.Description,
                Tags = JsonConvert.SerializeObject(articleOutput.Tags ?? Array.Empty<string>()),
                CreatedDate = DateTime.UtcNow,
                PublishedDate = DateTime.UtcNow,
                Status = ArticleStatus.Published,
                Objective = objective,
                WordCount = articleOutput.WordCount,
                Slug = articleOutput.Slug
            };

            // Get image URL if available
            if (Orchestrator.RuntimeProperties.TryGetValue("ImageUrl", out var imageUrlObj) && imageUrlObj is string imageUrl)
            {
                article.ImageUrl = imageUrl;
            }
            
            // Save to database first to get the ID
            _dbContext.Articles.Add(article);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"✓ Saved article to database: {article.Title} (ID: {article.Id})");
            
            // Now export to files
            try
            {
                var markdownPath = await _markdownExporter.ExportArticleAsync(article);
                var jsonPath = await _jsonExporter.ExportArticleAsync(article);
                
                // Convert to absolute paths
                var absoluteMarkdownPath = Path.GetFullPath(markdownPath);
                var absoluteJsonPath = Path.GetFullPath(jsonPath);
                
                Console.WriteLine($"\n📄 Article exported:");
                Console.WriteLine($"   Markdown: {absoluteMarkdownPath}");
                Console.WriteLine($"   JSON:     {absoluteJsonPath}");
                Console.WriteLine($"   Image:    {article.ImageUrl ?? "None"}");
            }
            catch (Exception exportEx)
            {
                Console.WriteLine($"⚠ Export warning: {exportEx.Message}");
                // Continue - article is saved in DB even if export fails
            }
            
            // Store in properties for service to extract
            Orchestrator.RuntimeProperties["FinalArticle"] = article;
            
            return article;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Database save error: {ex.Message}");
            return new Article { Id = 0, Title = "Error" };
        }
    }
}

public class CompletionRunnable : OrchestrationRunnable<Article, ChatMessage>
{
    public CompletionRunnable(Orchestration orchestrator) : base(orchestrator) 
    { 
        AllowDeadEnd = true; // This is a terminal node for successful articles
    }

    public override ValueTask<ChatMessage> Invoke(RunnableProcess<Article, ChatMessage> process)
    {
        Orchestrator?.HasCompletedSuccessfully();
        
        var article = process.Input;
        var message = $"Article generation completed successfully: {article.Title}";

        return ValueTask.FromResult(new ChatMessage(ChatMessageRoles.Assistant, message));
    }
}

public class FailureCompletionRunnable : OrchestrationRunnable<ReviewOutput, ChatMessage>
{
    public FailureCompletionRunnable(Orchestration orchestrator) : base(orchestrator) 
    { 
        AllowDeadEnd = true; // This is a terminal node for failed articles
    }

    public override ValueTask<ChatMessage> Invoke(RunnableProcess<ReviewOutput, ChatMessage> process)
    {
        Orchestrator?.HasCompletedSuccessfully();
        
        var review = process.Input;
        var message = $"Article generation failed after review: {review.Summary}";

        return ValueTask.FromResult(new ChatMessage(ChatMessageRoles.Assistant, message));
    }
}

public struct ExportPaths
{
    public string? MarkdownPath { get; set; }
    public string? JsonPath { get; set; }
}

