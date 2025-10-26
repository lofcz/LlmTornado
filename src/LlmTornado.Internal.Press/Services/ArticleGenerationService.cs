using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Internal.Press.Agents;
using LlmTornado.Code;
using Microsoft.EntityFrameworkCore;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.Database;
using LlmTornado.Internal.Press.Database.Models;
using LlmTornado.Internal.Press.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Services;

public class ArticleGenerationService
{
    private readonly TornadoApi _client;
    private readonly AppConfiguration _config;
    private readonly PressDbContext _dbContext;
    private readonly QueueService _queueService;

    public ArticleGenerationService(
        TornadoApi client,
        AppConfiguration config,
        PressDbContext dbContext)
    {
        _client = client;
        _config = config;
        _dbContext = dbContext;
        _queueService = new QueueService(dbContext);
    }

    /// <summary>
    /// Generate X number of articles from the queue
    /// </summary>
    public async Task<List<Article>> GenerateArticlesAsync(int count)
    {
        var articles = new List<Article>();

        for (int i = 0; i < count; i++)
        {
            var queueItem = await _queueService.GetNextPendingAsync();
            
            if (queueItem == null)
            {
                Console.WriteLine("No more articles in queue. Run seed-queue command first.");
                break;
            }

            Console.WriteLine($"\n[{i + 1}/{count}] Processing: {queueItem.Title}");
            
            var article = await GenerateSingleArticleAsync(queueItem);
            
            if (article != null)
            {
                articles.Add(article);
                await _queueService.LinkArticleAsync(queueItem.Id, article.Id);
                Console.WriteLine($"✓ Article generated successfully: {article.Title}");
            }
            else
            {
                await _queueService.UpdateStatusAsync(queueItem.Id, QueueStatus.Failed, 
                    "Article generation failed");
                Console.WriteLine($"✗ Article generation failed");
            }

            // Delay between articles if configured
            if (i < count - 1 && _config.ArticleGeneration.DelayBetweenArticles > 0)
            {
                await Task.Delay(_config.ArticleGeneration.DelayBetweenArticles);
            }
        }

        return articles;
    }

    /// <summary>
    /// Generate a single article from a queue item
    /// </summary>
    private async Task<Article?> GenerateSingleArticleAsync(ArticleQueue queueItem)
    {
        // Update status to InProgress and increment attempt count
        await _queueService.UpdateStatusAsync(queueItem.Id, QueueStatus.InProgress);
        
        // Reload to get updated attempt count
        var updatedQueue = await _dbContext.ArticleQueue.FindAsync(queueItem.Id);
        int attemptCount = updatedQueue?.AttemptCount ?? queueItem.AttemptCount;

        try
        {
            // Create orchestration configuration
            var orchestrationConfig = new ArticleOrchestrationConfiguration(_client, _config, _dbContext);
            orchestrationConfig.SetCurrentQueue(queueItem);

            // Create runtime
            var runtime = new ChatRuntime(orchestrationConfig);

            Console.WriteLine($"  → Starting orchestration (attempt {attemptCount}/3)...");
            
            // Run the orchestration - pass queue item to start processing
            var initialMessage = new Chat.ChatMessage(Code.ChatMessageRoles.User, $"Generate article: {queueItem.Title}");
            var result = await runtime.InvokeAsync(initialMessage);

            Console.WriteLine($"  → Orchestration completed: {result.Content}");

            // Extract article from runtime properties
            if (orchestrationConfig.RuntimeProperties.TryGetValue("FinalArticle", out var articleObj) &&
                articleObj is Article article)
            {
                Console.WriteLine($"  ✓ Article extracted successfully");
                return article;
            }

            Console.WriteLine("  ✗ Article not found in runtime properties");
            
            // Check if there were any results at all
            var props = orchestrationConfig.RuntimeProperties.Keys.ToList();
            Console.WriteLine($"  Available properties: {string.Join(", ", props)}");
            
            // Reset to Pending if we can retry (attempt count < 3)
            if (attemptCount < 3)
            {
                await _queueService.UpdateStatusAsync(queueItem.Id, QueueStatus.Pending, 
                    "Orchestration completed but no article was generated");
                Console.WriteLine($"  → Queue item reset to Pending for retry");
            }
            else
            {
                await _queueService.UpdateStatusAsync(queueItem.Id, QueueStatus.Failed, 
                    "Max retry attempts reached - orchestration did not produce article");
                Console.WriteLine($"  → Max retries reached, marking as Failed");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error generating article: {ex.Message}");
            Console.WriteLine($"  Stack trace: {ex.StackTrace}");
            
            await LogWorkHistory(null, WorkAction.ErrorOccurred, ex.Message);
            
            // Reset to Pending if we can retry
            if (attemptCount < 3)
            {
                await _queueService.UpdateStatusAsync(queueItem.Id, QueueStatus.Pending, 
                    $"Exception: {ex.Message}");
                Console.WriteLine($"  → Queue item reset to Pending for retry");
            }
            else
            {
                await _queueService.UpdateStatusAsync(queueItem.Id, QueueStatus.Failed, 
                    $"Max retry attempts reached - Exception: {ex.Message}");
                Console.WriteLine($"  → Max retries reached, marking as Failed");
            }
            
            return null;
        }
    }

    /// <summary>
    /// Seed the article queue with ideas
    /// </summary>
    public async Task<int> SeedQueueAsync(int count)
    {
        Console.WriteLine($"Generating {count} article ideas...\n");

        // Create simple orchestration config for standalone use
        var tempConfig = new ArticleOrchestrationConfiguration(_client, _config, _dbContext);
        
        // Run trend analysis
        var trendRunnable = new TrendAnalysisRunnable(_client, _config, tempConfig);
        var trendProcess = new RunnableProcess<string, TrendAnalysisOutput>(trendRunnable, _config.Objective, Guid.NewGuid().ToString());
        
        var trends = await trendRunnable.Invoke(trendProcess);

        Console.WriteLine($"Found {trends.Trends?.Length ?? 0} trending topics");

        // Run ideation
        var ideationRunnable = new IdeationRunnable(_client, _config, tempConfig);
        var ideationProcess = new RunnableProcess<TrendAnalysisOutput, ArticleIdeaOutput>(ideationRunnable, trends, Guid.NewGuid().ToString());
        
        var ideas = await ideationRunnable.Invoke(ideationProcess);

        if (ideas.Ideas == null || ideas.Ideas.Length == 0)
        {
            Console.WriteLine("No ideas generated");
            return 0;
        }

        // Add ideas to queue
        int added = 0;
        foreach (var idea in ideas.Ideas.Take(count))
        {
            await _queueService.AddToQueueAsync(
                idea.Title,
                idea.IdeaSummary,
                idea.Tags ?? Array.Empty<string>(),
                idea.EstimatedRelevance,
                priority: (int)(idea.EstimatedRelevance * 100));

            Console.WriteLine($"✓ Added to queue: {idea.Title}");
            added++;
        }

        return added;
    }

    /// <summary>
    /// Get current queue statistics
    /// </summary>
    public async Task<QueueStats> GetQueueStatsAsync()
    {
        return new QueueStats
        {
            PendingCount = await _queueService.GetPendingCountAsync(),
            CompletedCount = await _queueService.GetCompletedCountAsync(),
            FailedCount = await _queueService.GetFailedCountAsync(),
            TotalArticles = await _dbContext.Articles.CountAsync()
        };
    }


    private async Task LogWorkHistory(int? articleId, string action, string? details)
    {
        var history = new WorkHistory
        {
            ArticleId = articleId,
            Action = action,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.WorkHistory.Add(history);
        await _dbContext.SaveChangesAsync();
    }
}

public class QueueStats
{
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalArticles { get; set; }
}

