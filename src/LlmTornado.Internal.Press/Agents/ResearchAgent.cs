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

        var instructions = $"""
            You are a thorough research assistant specializing in technology and software development.
            Your role is to gather accurate, authoritative information to support article creation.
            
            Objective context: {config.Objective}
            
            When researching:
            1. Use Tavily search to find authoritative sources
            2. Verify facts with multiple sources when possible
            3. Extract key statistics, quotes, and data points
            4. Document all sources with URLs and publication dates
            5. Identify expert opinions and industry trends
            6. Focus on recent information (2024-2025) when relevant
            
            Organize your research into:
            - Factual statements with confidence scores
            - Source citations with URLs
            - Key insights that could form article sections
            - Supporting data and statistics
            
            Prioritize accuracy and source credibility over quantity.
            """;

        var model = new ChatModel(config.Models.Research);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Research Agent",
            instructions: instructions,
            outputSchema: typeof(ResearchOutput),
            tools: [DeepSearchFunc]);
    }

    public override async ValueTask<ResearchOutput> Invoke(RunnableProcess<ArticleIdea, ResearchOutput> process)
    {
        Console.WriteLine($"  [ResearchRunnable] Starting research for: {process.Input.Title}");
        process.RegisterAgent(_agent);

        var idea = process.Input;
        
        var prompt = $"""
            Research the following article topic thoroughly:
            
            Title: {idea.Title}
            Summary: {idea.IdeaSummary}
            Tags: {string.Join(", ", idea.Tags ?? Array.Empty<string>())}
            
            Conduct comprehensive research using the search tool. Focus on:
            1. Current state of C# AI libraries and frameworks
            2. Recent developments and announcements
            3. Developer experiences and community sentiment
            4. Technical capabilities and comparisons
            5. Use cases and success stories
            
            Gather enough information to write a detailed, well-sourced article.
            """;

        Console.WriteLine($"  [ResearchRunnable] Running agent with {15} max turns...");
        
        Chat.Conversation conversation;
        try
        {
            conversation = await _agent.RunAsync(prompt, maxTurns: 15);
            Console.WriteLine($"  [ResearchRunnable] Agent completed. Message count: {conversation.Messages.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ResearchRunnable] ERROR during agent run: {ex.Message}");
            Console.WriteLine($"  [ResearchRunnable] Stack trace: {ex.StackTrace}");
            return new ResearchOutput
            {
                Facts = Array.Empty<ResearchFact>(),
                Sources = Array.Empty<ResearchSource>(),
                KeyInsights = Array.Empty<string>(),
                Summary = $"Research failed: {ex.Message}",
                ResearchDate = DateTime.UtcNow
            };
        }
        
        var lastMessage = conversation.Messages.Last();
        Console.WriteLine($"  [ResearchRunnable] Last message content length: {lastMessage.Content?.Length ?? 0}");
        Console.WriteLine($"  [ResearchRunnable] Last message content:\n{lastMessage.Content}");
        
        var researchOutput = await lastMessage.Content?.SmartParseJsonAsync<ResearchOutput>(_agent);

        if (researchOutput == null)
        {
            Console.WriteLine($"  [ResearchRunnable] ERROR: Failed to parse research output");
            Console.WriteLine($"  [ResearchRunnable] Attempted to parse: {lastMessage.Content}");
            return new ResearchOutput
            {
                Facts = Array.Empty<ResearchFact>(),
                Sources = Array.Empty<ResearchSource>(),
                KeyInsights = Array.Empty<string>(),
                Summary = "Research failed to complete",
                ResearchDate = DateTime.UtcNow
            };
        }

        Console.WriteLine($"  [ResearchRunnable] Research complete. Facts: {researchOutput.Facts.Length}, Sources: {researchOutput.Sources.Length}");
        return researchOutput;
    }

    private async Task<string> DeepSearchFunc(string query)
    {
        return await _tavilyTool.SearchAsync(query, maxResults: 5);
    }
}

