using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using System;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

/// <summary>
/// Generates a concise summary of the article for use by other agents (image generation, social media, etc.)
/// </summary>
public class SummarizationAgent : OrchestrationRunnable<ArticleOutput, ArticleSummary>
{
    private readonly TornadoApi _client;
    private readonly AppConfiguration _config;

    public SummarizationAgent(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _client = client;
        _config = config;
    }

    public override async ValueTask<ArticleSummary> Invoke(RunnableProcess<ArticleOutput, ArticleSummary> process)
    {
        ArticleOutput article = process.Input;
        
        Console.WriteLine($"  [Summarization] 📝 Generating summary for: {article.Title}");

        try
        {
            string prompt = $"""
                            Analyze the following technical article and generate a comprehensive summary for use by other AI agents.
                            
                            Article Title: {article.Title}
                            Article Body:
                            {article.Body}
                            
                            Generate a structured summary with the following sections:
                            
                            1. **Executive Summary** (2-3 sentences)
                               - The core message and main takeaway
                               - Who should read this and why
                            
                            2. **Key Technical Points** (3-5 bullet points)
                               - Main technical concepts covered
                               - Important technologies/frameworks mentioned
                               - Code patterns or architectures discussed
                            
                            3. **Visual Elements Suggestion** (1-2 sentences)
                               - What kind of image would best represent this article?
                               - What visual metaphor or concept should be depicted?
                            
                            4. **Target Audience** (1 sentence)
                               - Who is this article written for?
                            
                            5. **Emotional Tone** (1 sentence)
                               - What is the overall tone? (e.g., practical, enthusiastic, educational, cautionary)
                            
                            6. **Social Media Hook** (1 sentence, max 100 characters)
                               - A compelling one-liner that captures the essence
                            
                            Output ONLY the summary in a clear, structured format. Be concise but informative.
                            """;

            ChatModel model = new ChatModel(_config.Models.Review); // Use review model for summarization
            
            var conversation = _client.Chat.CreateConversation(new ChatRequest
            {
                Model = model,
                Temperature = 0.3 // Lower temperature for consistent, factual summaries
            });
            
            conversation.AppendSystemMessage("You are an expert at analyzing technical articles and creating concise, actionable summaries for AI agents.");
            conversation.AppendUserInput(prompt);
            
            Console.WriteLine($"  [Summarization] 🤖 Using model: {model.Name}");
            
            string response = await conversation.GetResponse();
            
            // Parse the response into structured format (simple extraction)
            ArticleSummary summary = ParseSummaryResponse(response, article);
            
            Console.WriteLine($"  [Summarization] ✅ Summary generated ({summary.FullSummary.Length} chars)");
            Console.WriteLine($"  [Summarization] 💡 Visual suggestion: {summary.VisualSuggestion}");
            
            // Store in orchestration properties for downstream use
            Orchestrator.RuntimeProperties["ArticleSummary"] = summary;
            
            return summary;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [Summarization] ❌ Error: {ex.Message}");
            
            // Fallback summary
            return new ArticleSummary
            {
                ExecutiveSummary = article.Description ?? "A technical article about software development.",
                KeyPoints = [article.Title],
                VisualSuggestion = "A modern, abstract representation of software development with clean geometric shapes.",
                TargetAudience = "Software developers",
                EmotionalTone = "Educational",
                SocialMediaHook = article.Title.Length > 100 ? article.Title.Substring(0, 97) + "..." : article.Title,
                FullSummary = article.Description ?? article.Title
            };
        }
    }

    private ArticleSummary ParseSummaryResponse(string response, ArticleOutput article)
    {
        // Simple extraction - the AI should provide structured output
        // We'll do basic parsing to extract sections
        
        string[] lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        string executiveSummary = ExtractSection(lines, "Executive Summary", "Key Technical Points");
        string[] keyPoints = ExtractListItems(lines, "Key Technical Points", "Visual Elements");
        string visualSuggestion = ExtractSection(lines, "Visual Elements", "Target Audience");
        string targetAudience = ExtractSection(lines, "Target Audience", "Emotional Tone");
        string emotionalTone = ExtractSection(lines, "Emotional Tone", "Social Media Hook");
        string socialMediaHook = ExtractSection(lines, "Social Media Hook", null);
        
        // Fallbacks if extraction fails
        if (string.IsNullOrEmpty(executiveSummary))
            executiveSummary = article.Description ?? "Technical article summary.";
        
        if (keyPoints.Length == 0)
            keyPoints = [article.Title];
        
        if (string.IsNullOrEmpty(visualSuggestion))
            visualSuggestion = "Modern software development visualization";
        
        if (string.IsNullOrEmpty(targetAudience))
            targetAudience = "Software developers";
        
        if (string.IsNullOrEmpty(emotionalTone))
            emotionalTone = "Educational";
        
        if (string.IsNullOrEmpty(socialMediaHook))
            socialMediaHook = article.Title.Length > 100 ? article.Title.Substring(0, 97) + "..." : article.Title;
        
        return new ArticleSummary
        {
            ExecutiveSummary = executiveSummary.Trim(),
            KeyPoints = keyPoints,
            VisualSuggestion = visualSuggestion.Trim(),
            TargetAudience = targetAudience.Trim(),
            EmotionalTone = emotionalTone.Trim(),
            SocialMediaHook = socialMediaHook.Trim(),
            FullSummary = response.Trim()
        };
    }

    private string ExtractSection(string[] lines, string startMarker, string? endMarker)
    {
        bool inSection = false;
        System.Text.StringBuilder content = new System.Text.StringBuilder();
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            
            // Check if we're entering the section
            if (trimmed.Contains(startMarker, StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                continue;
            }
            
            // Check if we're leaving the section
            if (endMarker != null && trimmed.Contains(endMarker, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            
            // Collect content if in section
            if (inSection && !string.IsNullOrWhiteSpace(trimmed))
            {
                // Skip markdown formatting and bullet points
                string cleaned = trimmed.TrimStart('*', '-', '•', '#', ' ');
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    content.AppendLine(cleaned);
                }
            }
        }
        
        return content.ToString().Trim();
    }

    private string[] ExtractListItems(string[] lines, string startMarker, string? endMarker)
    {
        bool inSection = false;
        List<string> items = new List<string>();
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            
            // Check if we're entering the section
            if (trimmed.Contains(startMarker, StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                continue;
            }
            
            // Check if we're leaving the section
            if (endMarker != null && trimmed.Contains(endMarker, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            
            // Collect list items if in section
            if (inSection && !string.IsNullOrWhiteSpace(trimmed))
            {
                // Look for bullet points or numbered lists
                if (trimmed.StartsWith("-") || trimmed.StartsWith("*") || 
                    trimmed.StartsWith("•") || char.IsDigit(trimmed[0]))
                {
                    string cleaned = trimmed.TrimStart('*', '-', '•', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ' ');
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        items.Add(cleaned.Trim());
                    }
                }
            }
        }
        
        return items.ToArray();
    }
}

