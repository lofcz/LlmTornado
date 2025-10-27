using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Internal.Press.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

public class TrendAnalysisRunnable : OrchestrationRunnable<string, TrendAnalysisOutput>
{
    private readonly TornadoAgent _agent;
    private readonly TavilySearchTool _tavilyTool;
    private readonly AppConfiguration _config;

    public TrendAnalysisRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;
        _tavilyTool = new TavilySearchTool(config.ApiKeys.Tavily, config.Tavily);

        var instructions = $"""
            You are a trend analysis expert. Your role is to discover and analyze trending topics in the tech industry,
            particularly focusing on AI, machine learning, and software development trends.
            
            The current objective is: {config.Objective}
            
            Use the Tavily search tool to:
            1. Find trending topics in Q4 2025 related to C# AI libraries and agent frameworks
            2. Discover what developers are currently interested in
            3. Identify emerging patterns in AI development
            4. Find newsworthy events and announcements
            
            Analyze the relevance of each trend to our objective and provide a relevance score (0.0 to 1.0).
            Focus on trends that can be naturally integrated with content about C# AI libraries.
            """;

        var model = new ChatModel(config.Models.TrendAnalysis);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Trend Analyzer",
            instructions: instructions,
            outputSchema: typeof(TrendAnalysisOutput),
            tools: [SearchTrendsFunc]);
    }

    public override async ValueTask<TrendAnalysisOutput> Invoke(RunnableProcess<string, TrendAnalysisOutput> process)
    {
        process.RegisterAgent(_agent);

        var searchQueries = new List<string>
        {
            "latest AI development trends Q4 2025",
            "C# AI libraries trending 2025",
            "machine learning frameworks news",
            "AI agent frameworks popularity",
            "developer tools trending 2025"
        };

        var searchContext = $"Search for trends related to: {process.Input}";
        var conversation = await _agent.Run(searchContext);

        var lastMessage = conversation.Messages.Last();
        var trendOutput = await lastMessage.Content?.SmartParseJsonAsync<TrendAnalysisOutput>(_agent);

        if (trendOutput == null)
        {
            // Return empty result if parsing fails
            return new TrendAnalysisOutput
            {
                Trends = Array.Empty<TrendItem>(),
                Source = "Tavily",
                Timestamp = DateTime.UtcNow,
                Summary = "No trends found"
            };
        }

        return trendOutput;
    }

    private async Task<string> SearchTrendsFunc(string query)
    {
        return await _tavilyTool.SearchAsync(query, maxResults: _config.TrendAnalysis.MaxTopics);
    }
}

