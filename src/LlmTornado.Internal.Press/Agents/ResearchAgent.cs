using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Internal.Press.Tools;
using System;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.ChatFunctions;

namespace LlmTornado.Internal.Press.Agents;

public class ResearchRunnable : OrchestrationRunnable<ArticleIdea, ResearchOutput>
{
    private readonly TornadoAgent _agent;
    private readonly TavilySearchTool _tavilyTool;
    private readonly AppConfiguration _config;

    public ResearchRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;
        _tavilyTool = new TavilySearchTool(config.ApiKeys.Tavily, config.Tavily);

        DateTime now = DateTime.Now;
        int currentYear = now.Year;

        string instructions = $"""
                               You are a thorough research assistant specializing in technology and software development.
                               Your role is to gather accurate, authoritative information to support article creation.

                               Current Date: {now:MMMM dd, yyyy}
                               Objective context: {config.Objective}

                               **CRITICAL RESEARCH WORKFLOW:**

                               1. **ONE search at a time** - DO NOT make multiple parallel searches
                               2. **READ the search results** before making another search
                               3. **Build on previous findings** - use what you learned to inform next searches
                               4. **Stop after 3-5 searches** - quality over quantity

                               **For each search:**
                               - Make ONE targeted search query
                               - Wait for and read the full results
                               - Extract key information: facts, sources, URLs, dates
                               - Decide if you need another search or if you have enough information

                               **When you have enough research (after 3-5 searches):**
                               - Synthesize findings into structured output
                               - Include factual statements with confidence scores
                               - Document all sources with URLs and publication dates
                               - Extract key insights that could form article sections

                               **DO NOT:**
                               - Make the same search twice
                               - Make multiple searches in one turn without reading results
                               - Continue searching after you have sufficient information

                               Prioritize accuracy, source credibility, and efficiency.
                               """;

        ChatModel model = new ChatModel(config.Models.Research);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Research Agent",
            instructions: instructions,
            outputSchema: typeof(ResearchOutput),
            tools: [DeepSearchFunc],
            temperature: 1);
    }

    public override async ValueTask<ResearchOutput> Invoke(RunnableProcess<ArticleIdea, ResearchOutput> process)
    {
        Console.WriteLine($"  [ResearchAgent] 🔍 Starting research: {Snippet(process.Input.Title, 60)}");
        process.RegisterAgent(_agent);

        ArticleIdea idea = process.Input;
        
        // Check if this is an improvement iteration
        bool isImprovement = Orchestrator?.RuntimeProperties.ContainsKey("ImprovementFeedback") ?? false;
        bool needsResearch = (bool?)Orchestrator?.RuntimeProperties.GetValueOrDefault("NeedsResearch") ?? true;

        // If this is an improvement and research is sufficient, return cached research
        if (isImprovement && !needsResearch)
        {
            Console.WriteLine($"  [ResearchAgent] ✓ Reusing previous research (sufficient quality)");
            
            if (Orchestrator?.RuntimeProperties.TryGetValue("PreviousResearch", out object? researchObj) == true &&
                researchObj is ResearchOutput cachedResearch)
            {
                return cachedResearch;
            }
            
            Console.WriteLine($"  [ResearchAgent] ⚠ No cached research found, conducting new research");
        }

        // If we need new/additional research, indicate it's accumulative
        string accumulativeNote = isImprovement && needsResearch 
            ? "\n\n**IMPROVEMENT MODE**: Previous research was insufficient. Conduct NEW searches focusing on areas flagged in review."
            : "";
        
        string prompt = $"""
                         Research this specific article topic:

                         **Title:** {idea.Title}
                         **Summary:** {idea.IdeaSummary}
                         **Tags:** {string.Join(", ", idea.Tags ?? [])}
                         {accumulativeNote}

                         **RESEARCH STRATEGY:**

                         1. **Search based on the SPECIFIC title and topic** - not generic queries
                            - Use the exact concepts from the title
                            - Search for specific frameworks, tools, or patterns mentioned
                            - If the title mentions a specific problem/technique, search for that

                         2. **Perform 3-5 DISTINCT searches** (do NOT repeat searches):
                            - First search: The main topic from the title
                            - Second search: Related technical details or comparisons
                            - Third search: Real-world examples or case studies
                            - Optional 4th/5th: Deep dives into specific aspects

                         3. **After each search, use the results** - don't search again unless you need NEW information

                         4. **Focus on:**
                            - Specific information relevant to THIS article's angle
                            - Recent developments (2024-2025) if applicable
                            - Concrete examples and data points
                            - Credible sources with URLs

                         **IMPORTANT:** 
                         - Do NOT make generic searches like "current state of C# AI libraries"
                         - Do NOT repeat the same search multiple times
                         - Each search should target a DIFFERENT aspect of the topic
                         - Use the title's specific keywords in your searches

                         After 3-5 targeted searches, synthesize the information into Facts, Sources, and Insights.
                         """;

        Console.WriteLine($"  [ResearchAgent] 🔎 Running with max turns: 8");
        
        int searchCount = 0;
        
        Conversation conversation;
        try
        {
            conversation = await _agent.Run(prompt, maxTurns: 8, onAgentRunnerEvent: (evt) =>
            {
                Console.WriteLine($"  [ResearchAgent] Event: {evt.EventType} at {evt.Timestamp:HH:mm:ss}");
                
                if (evt.InternalConversation != null)
                {
                    ChatMessage? lastMsg = evt.InternalConversation.Messages.LastOrDefault();
                    if (lastMsg != null)
                    {
                        if (lastMsg.ToolCalls != null && lastMsg.ToolCalls.Count > 0)
                        {
                            foreach (ToolCall toolCall in lastMsg.ToolCalls)
                            {
                                string funcName = toolCall.FunctionCall?.Name ?? toolCall.CustomCall?.Name ?? "unknown";
                                string args = toolCall.FunctionCall?.Arguments ?? toolCall.CustomCall?.Input ?? "";
                                
                                if (funcName.Contains("search", StringComparison.OrdinalIgnoreCase))
                                {
                                    searchCount++;
                                    Console.WriteLine($"    🔎 Search #{searchCount} | {Snippet(args, 80)}");
                                }
                                else
                                {
                                    Console.WriteLine($"    🔧 Tool: {funcName} | {Snippet(args, 60)}");
                                }
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(lastMsg.Content))
                        {
                            Console.WriteLine($"    💬 Message: {Snippet(lastMsg.Content, 150)}");
                        }
                    }
                }
                return ValueTask.CompletedTask;
            });
            
            Console.WriteLine($"  [ResearchAgent] ✓ Complete. Messages: {conversation.Messages.Count}, Searches: {searchCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ResearchAgent] ❌ ERROR: {ex.Message}");
            Console.WriteLine($"  [ResearchAgent] Stack trace: {ex.StackTrace}");
            return new ResearchOutput
            {
                Facts = [],
                Sources = [],
                KeyInsights = [],
                Summary = $"Research failed: {ex.Message}",
                ResearchDate = DateTime.UtcNow
            };
        }
        
        ChatMessage lastMessage = conversation.Messages.Last();
        Console.WriteLine($"  [ResearchAgent] 📝 Parsing research output...");
        
        ResearchOutput? researchOutput = await lastMessage.Content?.SmartParseJsonAsync<ResearchOutput>(_agent);

        if (researchOutput == null)
        {
            Console.WriteLine($"  [ResearchAgent] ❌ Failed to parse research output");
            Console.WriteLine($"  [ResearchAgent] Content length: {lastMessage.Content?.Length ?? 0}");
            Console.WriteLine($"  [ResearchAgent] Content snippet: {Snippet(lastMessage.Content ?? "", 200)}");
            return new ResearchOutput
            {
                Facts = [],
                Sources = [],
                KeyInsights = [],
                Summary = "Research failed to complete",
                ResearchDate = DateTime.UtcNow
            };
        }

        Console.WriteLine($"  [ResearchAgent] ✅ Research complete!");
        Console.WriteLine($"    📊 Facts: {researchOutput.Facts?.Length ?? 0}");
        Console.WriteLine($"    📚 Sources: {researchOutput.Sources?.Length ?? 0}");
        Console.WriteLine($"    💡 Insights: {researchOutput.KeyInsights?.Length ?? 0}");
        
        if (researchOutput.Sources != null && researchOutput.Sources.Length > 0)
        {
            Console.WriteLine($"  [ResearchAgent] 🔗 Top sources:");
            foreach (ResearchSource source in researchOutput.Sources.Take(3))
            {
                Console.WriteLine($"    • {Snippet(source.Title, 60)} ({source.Domain})");
            }
        }
        
        // Store research for potential reuse in improvement iterations
        Orchestrator?.RuntimeProperties["PreviousResearch"] = researchOutput;
        
        return researchOutput;
    }
    
    private string Snippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return "[empty]";
        
        if (text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength) + "...";
    }

    private async Task<string> DeepSearchFunc(string query)
    {
        return await _tavilyTool.SearchAsync(query, maxResults: 5);
    }
}

