using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Internal.Press.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

/// <summary>
/// Inserts memes into article markdown using diff-based approach
/// </summary>
public class MemeInsertionRunnable : OrchestrationRunnable<MemeCollectionOutput, ArticleOutput>
{
    private readonly TornadoAgent _agent;
    private readonly AppConfiguration _config;

    public MemeInsertionRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;

        var instructions = $$"""
                             You are a content editor specializing in meme placement within technical articles.

                             Your role is to select the BEST insertion points for memes within markdown articles.

                             Guidelines for placement:
                             1. **After section headings**: Place memes after H2/H3 headings to introduce sections
                             2. **Between paragraphs**: Break up long text blocks with relevant memes
                             3. **Contextual relevance**: Match meme topic to surrounding content
                             4. **Even distribution**: Space memes throughout the article, not clustered
                             5. **Flow preservation**: Don't interrupt critical explanations or code examples

                             Given:
                             - Article markdown
                             - List of available insertion points (with line numbers and context)
                             - Memes to insert (with topics)

                             Your task:
                             Select the BEST insertion point for each meme based on contextual relevance and flow.
                             Match meme topics to nearby content.

                             Output a JSON array mapping each meme to its best insertion point line number.
                             """;

        var model = new ChatModel(config.MemeGeneration.MemeGenerationModel);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Meme Insertion Agent",
            instructions: instructions,
            outputSchema: typeof(MemeInsertionDecision),
            temperature: 0.5);
    }

    public override async ValueTask<ArticleOutput> Invoke(RunnableProcess<MemeCollectionOutput, ArticleOutput> process)
    {
        var memeCollection = process.Input;

        // Get the article from orchestration context
        if (!Orchestrator.RuntimeProperties.TryGetValue("CurrentArticle", out var articleObj) ||
            articleObj is not ArticleOutput article)
        {
            Console.WriteLine("  [MemeInsertionAgent] ✗ Article not found in context");
            return new ArticleOutput
            {
                Title = "Error",
                Body = "Article not found",
                Description = "Error",
                Tags = Array.Empty<string>()
            };
        }

        // If no memes to insert, return original article
        if (memeCollection.Memes == null || memeCollection.Memes.Length == 0)
        {
            Console.WriteLine("  [MemeInsertionAgent] No memes to insert, returning original article");
            return article;
        }

        Console.WriteLine($"  [MemeInsertionAgent] Inserting {memeCollection.Memes.Length} meme(s) into article");
        Console.WriteLine($"  [MemeInsertionAgent] Article: {article.Title} ({article.WordCount} words)");

        try
        {
            // Phase 1: Identify potential insertion points
            var insertionPoints = MemeService.IdentifyInsertionPoints(article.Body);
            Console.WriteLine($"  [MemeInsertionAgent] Found {insertionPoints.Count} potential insertion points");

            if (insertionPoints.Count == 0)
            {
                Console.WriteLine("  [MemeInsertionAgent] ⚠ No insertion points found, returning original article");
                return article;
            }

            // Phase 2: Use LLM to select best insertion points for each meme
            var insertionPlan = await SelectInsertionPointsAsync(article, memeCollection.Memes, insertionPoints);

            if (insertionPlan == null || insertionPlan.Placements == null || insertionPlan.Placements.Length == 0)
            {
                Console.WriteLine("  [MemeInsertionAgent] ⚠ Failed to create insertion plan, using simple distribution");
                insertionPlan = CreateSimpleInsertionPlan(memeCollection.Memes, insertionPoints);
            }

            // Phase 3: Insert memes into article markdown
            var modifiedBody = InsertMemesIntoArticle(article.Body, insertionPlan, memeCollection.Memes);

            Console.WriteLine($"  [MemeInsertionAgent] ✓ Successfully inserted {insertionPlan.Placements.Length} meme(s)");

            // Return updated article
            return new ArticleOutput
            {
                Title = article.Title,
                Body = modifiedBody,
                Description = article.Description,
                Tags = article.Tags,
                WordCount = article.WordCount, // Keep original word count
                Slug = article.Slug
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeInsertionAgent] ✗ Error during insertion: {ex.Message}");
            return article; // Return original on error
        }
    }

    /// <summary>
    /// Use LLM to select best insertion points
    /// </summary>
    private async Task<MemeInsertionDecision?> SelectInsertionPointsAsync(
        ArticleOutput article,
        MemeGenerationOutput[] memes,
        List<MemeInsertionPoint> availablePoints)
    {
        try
        {

            // Build prompt with article context and insertion options
            var prompt = $$"""
                           Select the best insertion points for {{memes.Length}} meme(s) in this article.

                           **Article Title:** {{article.Title}}}
                           **Article Length:** {{article.WordCount}} words

                           **Memes to Insert:**
                           {{string.Join("\n", memes.Select((m, i) => $"{i + 1}. Topic: {m.Topic}, Caption: {m.Caption}"))}}

                           **Available Insertion Points:**
                           {{string.Join("\n", availablePoints.Take(20).Select((p, i) => $"{i + 1}. Line {p.LineNumber}: {p.Context}\n   Context: {Snippet(p.SurroundingText, 80)}"))}}

                           **Task:**
                           For each meme, select the insertion point (by number 1-{{Math.Min(20, availablePoints.Count)}}) that:
                           1. Has contextually relevant surrounding content
                           2. Maintains good article flow
                           3. Is well-distributed throughout the article

                           Output JSON array with meme index and selected insertion point number.
                           Example: [{"memeIndex": 0, "insertionPointIndex": 3}, {"memeIndex": 1, "insertionPointIndex": 12}]
                           """;

            var conversation = await _agent.Run(prompt, maxTurns: 1);
            var lastMessage = conversation.Messages.Last();

            var decision = await lastMessage.Content?.SmartParseJsonAsync<MemeInsertionDecision>(_agent);
            return decision;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeInsertionAgent]   LLM selection error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create simple evenly-distributed insertion plan
    /// </summary>
    private MemeInsertionDecision CreateSimpleInsertionPlan(
        MemeGenerationOutput[] memes,
        List<MemeInsertionPoint> points)
    {
        var placements = new List<MemePlacement>();

        // Distribute memes evenly across available points
        int pointsPerMeme = Math.Max(1, points.Count / memes.Length);

        for (int i = 0; i < memes.Length && i * pointsPerMeme < points.Count; i++)
        {
            placements.Add(new MemePlacement
            {
                MemeIndex = i,
                InsertionPointIndex = i * pointsPerMeme
            });
        }

        return new MemeInsertionDecision
        {
            Placements = placements.ToArray()
        };
    }

    /// <summary>
    /// Insert memes into article Markdown at specified points
    /// </summary>
    private string InsertMemesIntoArticle(
        string markdown,
        MemeInsertionDecision plan,
        MemeGenerationOutput[] memes)
    {
        var insertionPoints = MemeService.IdentifyInsertionPoints(markdown);
        var modifiedMarkdown = markdown;

        // Sort placements by line number in reverse order to maintain line numbers during insertion
        var sortedPlacements = plan.Placements
            .OrderByDescending(p => p.InsertionPointIndex)
            .ToArray();

        foreach (var placement in sortedPlacements)
        {
            if (placement.MemeIndex >= memes.Length || placement.InsertionPointIndex >= insertionPoints.Count)
            {
                Console.WriteLine($"  [MemeInsertionAgent]   Invalid placement: meme {placement.MemeIndex}, point {placement.InsertionPointIndex}");
                continue;
            }

            var meme = memes[placement.MemeIndex];
            var point = insertionPoints[placement.InsertionPointIndex];

            // Use URL if available (uploaded meme), otherwise use local path
            var memeMarkdown = !string.IsNullOrEmpty(meme.Url) && 
                              (meme.Url.StartsWith("http://") || meme.Url.StartsWith("https://"))
                ? MemeService.CreateMemeMarkdownFromUrl(meme.Url, meme.Caption)
                : MemeService.CreateMemeMarkdown(meme.LocalPath, meme.Caption);

            Console.WriteLine($"  [MemeInsertionAgent]   Inserting meme {placement.MemeIndex + 1} at line {point.LineNumber}: {meme.Topic}");

            modifiedMarkdown = MemeService.InsertMemeAtLine(modifiedMarkdown, point.LineNumber, memeMarkdown);
        }

        return modifiedMarkdown;
    }

    private string Snippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return "[empty]";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}

/// <summary>
/// Decision about where to insert memes
/// </summary>
public class MemeInsertionDecision
{
    public MemePlacement[] Placements { get; set; } = Array.Empty<MemePlacement>();
}

/// <summary>
/// Maps a meme to an insertion point
/// </summary>
public class MemePlacement
{
    public int MemeIndex { get; set; }
    public int InsertionPointIndex { get; set; }
}

