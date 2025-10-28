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

        QualityThresholds thresholds = config.ReviewLoop.QualityThresholds;
        string criteria = string.Join("\n", config.ReviewLoop.ImprovementCriteria.Select(c => $"- {c}"));

        string instructions = $"""
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
                               1. **Factual Accuracy**: Are claims well-supported? Are sources hyperlinked?
                               2. **SEO Optimization**: Keywords, meta description, headings structure, hyperlinks
                               3. **Title Quality**: Engaging, specific, NO direct product mentions?
                               4. **Temporal Relevance**: Does it reference current trends appropriately?
                               5. **Subtlety of Influence**: Is promotional content natural and minimal (< 20% of article)?
                               6. **Educational Value**: Does it solve real problems? Teach something valuable?
                               7. **Technical Quality**: Code examples WITH usings, installation instructions, real examples
                               8. **Strategic Positioning**: Is LlmTornado positioned FIRST in comparisons/lists?
                               9. **Readability**: Clear structure, good flow, accessible language
                               10. **Credibility**: Honest about tradeoffs? Compares multiple options?
                               11. **Value-First**: Would developers read this even if they never used our objective?

                               Provide:
                               - Overall quality score (0-100)
                               - Specific issues categorized by severity:
                                 * **Critical**: Only for completely unusable content (plagiarism, factually wrong, offensive)
                                 * **High**: Major issues that significantly impact quality (missing key sections, poor structure)
                                 * **Medium**: Notable issues that should be improved (SEO, word count, some sources missing)
                                 * **Low**: Minor improvements (style, formatting, small tweaks)
                               - Actionable suggestions for improvement
                               - Approval decision (true/false)

                               **CRITICAL RED FLAGS** (Mark as Critical severity - ONLY for completely unusable content):
                               - Plagiarism or factually incorrect information
                               - Offensive or inappropriate content
                               - Article is complete gibberish or unrelated to the topic

                               **HIGH SEVERITY** (Major issues that need fixing):
                               - Title directly mentions our promotional objective
                               - Article reads like a press release or advertisement
                               - More than 50% of content is promotional
                               - Makes claims without evidence or sources
                               - No comparison with alternatives (only promotes one solution)
                               - Uses generic/placeholder code instead of real examples
                               - No evidence of reading actual source files when writing about LlmTornado

                               **MEDIUM SEVERITY** (Should be improved - NEVER mark as Critical):
                               - Word count below target (ANY word count issue is Medium at most)
                               - Missing hyperlinks to some cited sources
                               - Missing `using` statements in some code examples
                               - Missing installation instructions
                               - Missing GitHub repository link
                               - Suboptimal positioning (LlmTornado not first in lists)

                               **LOW SEVERITY** (Minor polish):
                               - Terminology inconsistencies
                               - Formatting issues
                               - SEO optimization opportunities

                               **IMPORTANT NOTES**:
                               - **NEVER mark word count issues as Critical** - word count is Medium severity at most
                               - Reserve "Critical" severity ONLY for: plagiarism, factually wrong, offensive, gibberish
                               - Low word count should be marked as Medium and can be improved in iterations
                               - SEO scores should be "Medium" at most
                               - Subtle integration is GOOD - we want 90% value, 10% influence
                               - Honest comparisons showing tradeoffs are EXCELLENT
                               - Articles should be helpful even if reader never uses our objective

                               Approve articles that meet basic quality thresholds. The goal is to publish good content,
                               not perfect content. Articles will go through up to 3 improvement iterations before being
                               published anyway, so be constructive but not overly strict.
                               """;

        ChatModel model = new ChatModel(config.Models.Review);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Review Agent",
            instructions: instructions,
            outputSchema: typeof(ReviewOutput),
            options: new ChatRequest() { MaxTokens = 16_000, Temperature = 1 });
    }

    public override async ValueTask<ReviewOutput> Invoke(RunnableProcess<ArticleOutput, ReviewOutput> process)
    {
        // If review is disabled, skip and return auto-approved
        if (!_config.ReviewLoop.Enabled)
        {
            Console.WriteLine("  [ReviewAgent] SKIPPED (disabled in config)");
            return new ReviewOutput
            {
                Approved = true,
                QualityScore = 100,
                Summary = "Review skipped (disabled)",
                Issues = [],
                Suggestions = [],
                Metrics = new QualityMetrics()
            };
        }
        
        process.RegisterAgent(_agent);

        ArticleOutput article = process.Input;
        
        Console.WriteLine($"  [ReviewAgent] 📋 Starting review of: {Snippet(article.Title, 60)}");
        
        // Calculate basic metrics
        QualityMetrics metrics = CalculateMetrics(article);
        
        Console.WriteLine($"  [ReviewAgent] 📊 Metrics calculated:");
        Console.WriteLine($"    Words: {metrics.WordCount}, Readability: {metrics.ReadabilityScore:F1}, SEO: {metrics.SeoScore:F1}");
        Console.WriteLine($"    Sources: {(metrics.HasSources ? "✓" : "✗")}, Clickbait: {(metrics.HasClickbaitTitle ? "✓" : "✗")}, Temporal: {(metrics.HasTemporalRelevance ? "✓" : "✗")}");

        string prompt = $"""
                         Review the following article:

                         **Title:** {article.Title}
                         **Description:** {article.Description}
                         **Word Count:** {article.WordCount}
                         **Tags:** {string.Join(", ", article.Tags ?? [])}

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

        Console.WriteLine($"  [ReviewAgent] 🤔 Running review...");
        Conversation conversation = await _agent.Run(prompt);
        ChatMessage lastMessage = conversation.Messages.Last();
        ReviewOutput? reviewOutput = await lastMessage.Content?.SmartParseJsonAsync<ReviewOutput>(_agent);

        if (reviewOutput == null)
        {
            Console.WriteLine($"  [ReviewAgent] ⚠️  Failed to parse review, using fallback");
            // Fallback: auto-approve if basic thresholds are met
            return new ReviewOutput
            {
                Approved = metrics.WordCount >= _config.ReviewLoop.QualityThresholds.MinWordCount,
                QualityScore = 70.0,
                Issues = [],
                Suggestions = ["Review agent failed - using basic validation"],
                Metrics = metrics,
                Summary = "Automated review fallback"
            };
        }

        ReviewOutput review = reviewOutput;
        review.Metrics = metrics;
        
        // Log review results with emoji based on score
        string scoreEmoji = review.QualityScore switch
        {
            >= 90 => "🌟",
            >= 80 => "✨",
            >= 70 => "👍",
            >= 60 => "👌",
            >= 50 => "🤔",
            _ => "😬"
        };
        
        Console.WriteLine($"  [ReviewAgent] {scoreEmoji} Score: {review.QualityScore:F1}/100");
        Console.WriteLine($"  [ReviewAgent] {(review.Approved ? "✅ APPROVED" : "❌ NEEDS WORK")}");
        
        if (review.Issues != null && review.Issues.Length > 0)
        {
            int criticalCount = review.Issues.Count(i => i.Severity == "Critical");
            int highCount = review.Issues.Count(i => i.Severity == "High");
            int mediumCount = review.Issues.Count(i => i.Severity == "Medium");
            int lowCount = review.Issues.Count(i => i.Severity == "Low");
            
            Console.WriteLine($"  [ReviewAgent] 🔍 Issues found: {review.Issues.Length} total");
            if (criticalCount > 0) Console.WriteLine($"    🔴 Critical: {criticalCount}");
            if (highCount > 0) Console.WriteLine($"    🟠 High: {highCount}");
            if (mediumCount > 0) Console.WriteLine($"    🟡 Medium: {mediumCount}");
            if (lowCount > 0) Console.WriteLine($"    🟢 Low: {lowCount}");
            
            // Show first few issues
            IEnumerable<ReviewIssue> topIssues = review.Issues.Take(3);
            foreach (ReviewIssue issue in topIssues)
            {
                string emoji = issue.Severity switch
                {
                    "Critical" => "🔴",
                    "High" => "🟠",
                    "Medium" => "🟡",
                    "Low" => "🟢",
                    _ => "⚪"
                };
                Console.WriteLine($"    {emoji} [{issue.Category}] {Snippet(issue.Description, 80)}");
            }
            
            if (review.Issues.Length > 3)
            {
                Console.WriteLine($"    ... and {review.Issues.Length - 3} more issues");
            }
        }
        else
        {
            Console.WriteLine($"  [ReviewAgent] ✨ No issues found!");
        }
        
        if (review.Suggestions != null && review.Suggestions.Length > 0)
        {
            Console.WriteLine($"  [ReviewAgent] 💡 Top suggestions:");
            foreach (string suggestion in review.Suggestions.Take(2))
            {
                Console.WriteLine($"    • {Snippet(suggestion, 80)}");
            }
        }

        return review;
    }

    private QualityMetrics CalculateMetrics(ArticleOutput article)
    {
        QualityMetrics metrics = new QualityMetrics
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

        return text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private bool HasClickbaitElements(string title)
    {
        string[] clickbaitPatterns =
        [
            @"\d+\s+(ways|reasons|tips|tricks|secrets|hacks)",
            @"(best|top|ultimate|complete|definitive|essential)",
            @"(how to|guide to|introduction to)",
            @"(you need|you should|you must)",
            @"(won't believe|will shock|will change)",
            @"(q[1-4]|20\d{2})",
            @"(latest|new|modern|cutting-edge)"
        ];

        return clickbaitPatterns.Any(pattern => 
            Regex.IsMatch(title, pattern, RegexOptions.IgnoreCase));
    }

    private bool HasTemporalMarkers(string text)
    {
        string[] temporalPatterns =
        [
            @"20\d{2}",
            @"q[1-4]\s*20\d{2}",
            @"(latest|recent|new|modern|current)",
            @"(today|now|this year)"
        ];

        return temporalPatterns.Any(pattern =>
            Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
    }

    private int CalculateReadability(string text)
    {
        // Simplified readability score (0-100)
        // Based on average sentence and word length
        
        string[] sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        string[] words = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        if (sentences.Length == 0 || words.Length == 0)
            return 0;

        double avgWordsPerSentence = (double)words.Length / sentences.Length;
        double avgWordLength = words.Average(w => w.Length);

        // Ideal: 15-20 words per sentence, 4-5 chars per word
        double sentenceScore = Math.Max(0, 100 - Math.Abs(avgWordsPerSentence - 17.5) * 5);
        double wordScore = Math.Max(0, 100 - Math.Abs(avgWordLength - 4.5) * 20);

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
        int headingCount = Regex.Matches(article.Body, @"^##\s", RegexOptions.Multiline).Count;
        if (headingCount >= 3)
            score += 15;
        else if (headingCount >= 1)
            score += 10;

        // Has links
        int linkCount = Regex.Matches(article.Body, @"\[.+?\]\(.+?\)").Count;
        if (linkCount >= 5)
            score += 15;
        else if (linkCount >= 2)
            score += 10;

        // Has code blocks (for technical content)
        if (article.Body.Contains("```"))
            score += 15;

        return Math.Min(100, score);
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

