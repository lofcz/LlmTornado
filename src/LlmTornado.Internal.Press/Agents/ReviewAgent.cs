using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Mcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

public class ReviewRunnable : OrchestrationRunnable<ArticleOutput, ReviewOutput>
{
    private readonly TornadoAgent _agent;
    private readonly AppConfiguration _config;

    public ReviewRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;

        var thresholds = config.ReviewLoop.QualityThresholds;
        var criteria = string.Join("\n", config.ReviewLoop.ImprovementCriteria.Select(c => $"- {c}"));

        var instructions = $"""
            You are an expert content reviewer and quality assurance specialist for technical articles.
            Your role is to evaluate articles against quality standards and provide actionable feedback.
            
            Quality Thresholds:
            - Minimum word count: {thresholds.MinWordCount}
            - Minimum readability score: {thresholds.MinReadabilityScore}
            - Minimum SEO score: {thresholds.MinSeoScore}
            - Require sources: {thresholds.RequireSources}
            
            Improvement Criteria:
            {criteria}
            
            For each article, evaluate:
            1. **Factual Accuracy**: Are claims well-supported? Are sources credible?
            2. **SEO Optimization**: Keywords, meta description, headings structure
            3. **Title Quality**: Engaging, specific, NO direct product mentions?
            4. **Temporal Relevance**: Does it reference current trends appropriately?
            5. **Subtlety of Influence**: Is promotional content natural and minimal (< 20% of article)?
            6. **Educational Value**: Does it solve real problems? Teach something valuable?
            7. **Technical Quality**: Code examples, accuracy, best practices, comparisons
            8. **Readability**: Clear structure, good flow, accessible language
            9. **Credibility**: Honest about tradeoffs? Compares multiple options?
            10. **Value-First**: Would developers read this even if they never used our objective?
            
            Provide:
            - Overall quality score (0-100)
            - Specific issues categorized by severity:
              * **Critical**: Only for completely unusable content (plagiarism, factually wrong, offensive)
              * **High**: Major issues that significantly impact quality (missing key sections, poor structure)
              * **Medium**: Notable issues that should be improved (SEO, word count, some sources missing)
              * **Low**: Minor improvements (style, formatting, small tweaks)
            - Actionable suggestions for improvement
            - Approval decision (true/false)
            
            **CRITICAL RED FLAGS** (Mark as High/Critical severity):
            - Title directly mentions our promotional objective
            - Article reads like a press release or advertisement
            - More than 20% of content is promotional
            - Makes claims without evidence or sources
            - Doesn't provide genuine educational value
            - No comparison with alternatives (only promotes one solution)
            - Uses superlatives without justification ("best", "only", "perfect")
            - **Uses generic/placeholder code instead of real examples from the codebase**
            - **No evidence of reading actual source files when writing about LlmTornado**
            
            **IMPORTANT NOTES**:
            - Reserve "Critical" severity ONLY for completely unusable content (plagiarism, factually wrong, offensive)
            - Word count, SEO scores should be "Medium" or "High" at most
            - Subtle integration is GOOD - we want 90% value, 10% influence
            - Honest comparisons showing tradeoffs are EXCELLENT
            - Articles should be helpful even if reader never uses our objective
            
            Approve articles that meet basic quality thresholds. The goal is to publish good content,
            not perfect content. Articles will go through up to 3 improvement iterations before being
            published anyway, so be constructive but not overly strict.
            """;

        var model = new ChatModel(config.Models.Review);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Review Agent",
            instructions: instructions,
            outputSchema: typeof(ReviewOutput));
    }

    public override async ValueTask<ReviewOutput> Invoke(RunnableProcess<ArticleOutput, ReviewOutput> process)
    {
        process.RegisterAgent(_agent);

        var article = process.Input;
        
        // Calculate basic metrics
        var metrics = CalculateMetrics(article);

        var prompt = $"""
            Review the following article:
            
            **Title:** {article.Title}
            **Description:** {article.Description}
            **Word Count:** {article.WordCount}
            **Tags:** {string.Join(", ", article.Tags ?? Array.Empty<string>())}
            
            **Content:**
            ```markdown
            {article.Body}
            ```
            
            **Basic Metrics:**
            - Word Count: {metrics.WordCount}
            - Readability Score: {metrics.ReadabilityScore}
            - SEO Score: {metrics.SeoScore}
            - Has Sources: {metrics.HasSources}
            - Clickbait Title: {metrics.HasClickbaitTitle}
            - Temporal Relevance: {metrics.HasTemporalRelevance}
            
            Provide a comprehensive review with quality score, issues, and suggestions.
            """;

        var conversation = await _agent.RunAsync(prompt);
        var lastMessage = conversation.Messages.Last();
        var reviewOutput = await lastMessage.Content?.SmartParseJsonAsync<ReviewOutput>(_agent);

        if (reviewOutput == null)
        {
            // Fallback: auto-approve if basic thresholds are met
            return new ReviewOutput
            {
                Approved = metrics.WordCount >= _config.ReviewLoop.QualityThresholds.MinWordCount,
                QualityScore = 70.0,
                Issues = Array.Empty<ReviewIssue>(),
                Suggestions = new[] { "Review agent failed - using basic validation" },
                Metrics = metrics,
                Summary = "Automated review fallback"
            };
        }

        var review = reviewOutput;
        review.Metrics = metrics;

        return review;
    }

    private QualityMetrics CalculateMetrics(ArticleOutput article)
    {
        var metrics = new QualityMetrics
        {
            WordCount = CountWords(article.Body),
            HasSources = article.Body.Contains("http://") || article.Body.Contains("https://"),
            HasClickbaitTitle = HasClickbaitElements(article.Title),
            HasTemporalRelevance = HasTemporalMarkers(article.Title + " " + article.Body),
            ObjectiveAlignment = article.Body.ToLower().Contains("llmtornado") || 
                               article.Body.ToLower().Contains("c# ai"),
            ReadabilityScore = CalculateReadability(article.Body),
            SeoScore = CalculateSeoScore(article)
        };

        return metrics;
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private bool HasClickbaitElements(string title)
    {
        var clickbaitPatterns = new[]
        {
            @"\d+\s+(ways|reasons|tips|tricks|secrets|hacks)",
            @"(best|top|ultimate|complete|definitive|essential)",
            @"(how to|guide to|introduction to)",
            @"(you need|you should|you must)",
            @"(won't believe|will shock|will change)",
            @"(q[1-4]|20\d{2})",
            @"(latest|new|modern|cutting-edge)"
        };

        return clickbaitPatterns.Any(pattern => 
            Regex.IsMatch(title, pattern, RegexOptions.IgnoreCase));
    }

    private bool HasTemporalMarkers(string text)
    {
        var temporalPatterns = new[]
        {
            @"20\d{2}",
            @"q[1-4]\s*20\d{2}",
            @"(latest|recent|new|modern|current)",
            @"(today|now|this year)"
        };

        return temporalPatterns.Any(pattern =>
            Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
    }

    private int CalculateReadability(string text)
    {
        // Simplified readability score (0-100)
        // Based on average sentence and word length
        
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (sentences.Length == 0 || words.Length == 0)
            return 0;

        var avgWordsPerSentence = (double)words.Length / sentences.Length;
        var avgWordLength = words.Average(w => w.Length);

        // Ideal: 15-20 words per sentence, 4-5 chars per word
        var sentenceScore = Math.Max(0, 100 - Math.Abs(avgWordsPerSentence - 17.5) * 5);
        var wordScore = Math.Max(0, 100 - Math.Abs(avgWordLength - 4.5) * 20);

        return (int)((sentenceScore + wordScore) / 2);
    }

    private int CalculateSeoScore(ArticleOutput article)
    {
        int score = 0;

        // Title length (50-60 chars is ideal)
        if (article.Title.Length >= 50 && article.Title.Length <= 60)
            score += 20;
        else if (article.Title.Length >= 40 && article.Title.Length <= 70)
            score += 10;

        // Description length (120-160 chars is ideal)
        if (article.Description.Length >= 120 && article.Description.Length <= 160)
            score += 20;
        else if (article.Description.Length >= 100 && article.Description.Length <= 180)
            score += 10;

        // Has tags
        if (article.Tags != null && article.Tags.Length >= 3)
            score += 15;

        // Has headings (## in markdown)
        var headingCount = Regex.Matches(article.Body, @"^##\s", RegexOptions.Multiline).Count;
        if (headingCount >= 3)
            score += 15;
        else if (headingCount >= 1)
            score += 10;

        // Has links
        var linkCount = Regex.Matches(article.Body, @"\[.+?\]\(.+?\)").Count;
        if (linkCount >= 5)
            score += 15;
        else if (linkCount >= 2)
            score += 10;

        // Has code blocks (for technical content)
        if (article.Body.Contains("```"))
            score += 15;

        return Math.Min(100, score);
    }
}

