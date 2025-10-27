using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

public class IdeationRunnable : OrchestrationRunnable<TrendAnalysisOutput, ArticleIdeaOutput>
{
    private readonly TornadoAgent _agent;
    private readonly AppConfiguration _config;

    public IdeationRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;

        var instructions = $"""
            You are an expert content strategist specializing in developer marketing through educational content.
            Your goal is to create article ideas that provide GENUINE VALUE while subtly influencing decisions.
            
            Background Context (Keep Private): {config.Objective}
            
            STRATEGIC APPROACH - The Art of Subtle Influence:
            
            1. **Problem-First Thinking**
               - Start with real problems developers face
               - Address pain points, frustrations, decision paralysis
               - Offer solutions (where our context naturally fits as ONE option)
            
            2. **Trend Commentary & Analysis**
               - Analyze industry shifts, new technologies, changing best practices
               - Compare approaches, frameworks, or patterns
               - Position context as a relevant example without forcing it
            
            3. **Educational Deep-Dives**
               - "How to...", "Understanding...", "Guide to..."
               - Technical tutorials, architecture patterns, best practices
               - Mention context where it genuinely adds value
            
            4. **Temporal Relevance**
               - "Best X in Q4 2025", "X Trends in 2025", "What's Changed in 2025"
               - Year-end roundups, quarterly updates, "state of" articles
               - Current events, new releases, emerging patterns
            
            TITLE GUIDELINES:
            - NEVER mention the promotional context directly in titles
            - Use curiosity gaps: "Why...", "The Secret to...", "What Nobody Tells You..."
            - Use specificity: numbers, timeframes, concrete outcomes
            - Be contrarian when appropriate: "Why X is Overrated", "The Problem With..."
            - Focus on reader benefit, not product features
            
            EXAMPLES OF GOOD vs BAD TITLES:
            
            ❌ BAD (Too Direct):
            - "Why You Should Use [Product X]"
            - "10 Reasons [Product X] is Great"
            - "[Product X]: The Best Solution for Y"
            
            ✅ GOOD (Subtle):
            - "Building Production-Ready AI Agents: A C# Developer's Journey"
            - "The Hidden Complexity of LLM API Integration (And How to Tame It)"
            - "What I Learned Managing 100B+ Tokens in a .NET Application"
            - "5 Patterns for Reliable LLM Orchestration in Enterprise C#"
            - "Why Most C# AI Tutorials Get Error Handling Wrong"
            
            CONTENT STRATEGY:
            - Provide 90% genuine value, 10% subtle positioning
            - Lead with problems, trends, or questions
            - Compare multiple approaches (mention context as one)
            - Use case studies, real-world scenarios, lessons learned
            - Be honest about tradeoffs and limitations
            
            For each idea, provide:
            - A subtle, value-driven title (NO direct product mentions)
            - A summary focused on the problem/topic (not the solution)
            - Relevance score based on trend fit and reader value
            - Tags reflecting the actual topic, not promotional keywords
            - Reasoning explaining the subtle influence strategy
            
            Generate 3-5 article ideas that developers will genuinely want to read.
            """;

        var model = new ChatModel(config.Models.Ideation);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Ideation Agent",
            instructions: instructions,
            outputSchema: typeof(ArticleIdeaOutput),
            temperature: 1);
    }
    
    

    public override async ValueTask<ArticleIdeaOutput> Invoke(RunnableProcess<TrendAnalysisOutput, ArticleIdeaOutput> process)
    {
        process.RegisterAgent(_agent);

        // Build context from trends
        var trendsContext = BuildTrendsContext(process.Input);
        
        var prompt = $"""
            Analyze the following trends and generate subtle, value-driven article ideas:
            
            {trendsContext}
            
            Create article ideas that:
            1. Address real developer problems or curiosities related to these trends
            2. Use subtle, engaging titles (NO direct promotional mentions)
            3. Provide educational/analytical value first, influence second
            4. Naturally connect to the background context where relevant
            
            Think like a respected developer writing for other developers - what would YOU want to read?
            """;

        var conversation = await _agent.Run(prompt);
        var lastMessage = conversation.Messages.Last();
        var ideaOutput = await lastMessage.Content?.SmartParseJsonAsync<ArticleIdeaOutput>(_agent);

        if (ideaOutput == null)
        {
            return new ArticleIdeaOutput
            {
                Ideas = Array.Empty<ArticleIdea>()
            };
        }

        return ideaOutput;
    }

    private string BuildTrendsContext(TrendAnalysisOutput trends)
    {
        if (trends.Trends == null || trends.Trends.Length == 0)
        {
            return "No specific trends available. Use general industry knowledge.";
        }

        var context = "Trending Topics:\n";
        foreach (var trend in trends.Trends.Take(5))
        {
            context += $"\n- {trend.Topic} (Relevance: {trend.Relevance:F2})\n";
            context += $"  {trend.Description}\n";
            if (trend.Keywords != null && trend.Keywords.Length > 0)
            {
                context += $"  Keywords: {string.Join(", ", trend.Keywords)}\n";
            }
        }

        return context;
    }
}

