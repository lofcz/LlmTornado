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

        DateTime now = DateTime.Now;
        int currentYear = now.Year;
        string currentQuarter = $"Q{(now.Month - 1) / 3 + 1}";
        string currentMonth = now.ToString("MMMM");

        string instructions = $"""
                               You are a trend analysis expert. Your role is to discover and analyze trending topics in the tech industry,
                               particularly focusing on AI, machine learning, and software development trends.

                               Current Date: {now:MMMM dd, yyyy} ({currentQuarter} {currentYear})
                               The current objective is: {config.Objective}

                               Use the Tavily search tool to:
                               1. Find trending topics in {currentMonth} {currentYear} ({currentQuarter}) related to AI, machine learning, and .NET/C#
                               2. Discover what developers are currently interested in RIGHT NOW
                               3. Identify emerging patterns, new releases, and hot topics in AI development
                               4. Find newsworthy events, announcements, and discussions happening THIS month/quarter

                               SEARCH BROADLY - don't limit yourself to just "C# AI agents":
                               - General AI/ML trends (LLMs, RAG, agents, vector databases, etc.)
                               - Popular frameworks and tools (ANY language, we'll compare)
                               - Cloud AI services and platforms
                               - Developer productivity tools
                               - Industry shifts and debates

                               Analyze the relevance of each trend to our objective and provide a relevance score (0.0 to 1.0).
                               Cast a WIDE net - we want diverse, current trends that developers care about.
                               """;

        ChatModel model = new ChatModel(config.Models.TrendAnalysis);

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

        DateTime now = DateTime.Now;
        int currentYear = now.Year;
        string currentQuarter = $"Q{(now.Month - 1) / 3 + 1}";
        string currentMonth = now.ToString("MMMM");

        string searchContext = $"""
                                Current Date: {now:MMMM dd, yyyy}

                                Search for DIVERSE, CURRENT trends in AI and software development.
                                Focus on {currentMonth} {currentYear} ({currentQuarter} {currentYear}) trends.

                                Cast a wide net:
                                - General AI/ML developments (not just C# specific)
                                - Popular frameworks, tools, and libraries (multi-language)
                                - Hot topics developers are discussing NOW
                                - Recent releases, announcements, and industry shifts

                                Goal: {process.Input}
                                """;
        
        Console.WriteLine($"  [TrendAnalysis] 📅 Searching for trends in {currentMonth} {currentYear} ({currentQuarter})");
        Conversation conversation = await _agent.Run(searchContext);

        ChatMessage lastMessage = conversation.Messages.Last();
        TrendAnalysisOutput? trendOutput = await lastMessage.Content?.SmartParseJsonAsync<TrendAnalysisOutput>(_agent);

        if (trendOutput == null)
        {
            // Return empty result if parsing fails
            return new TrendAnalysisOutput
            {
                Trends = [],
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

