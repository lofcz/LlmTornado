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
    private static readonly Random _random = new Random();

    // Diverse article angle hints
    private static readonly string[] ArticleAngleHints =
    [
        "🔍 **Tool Comparison**: Compare 3-5 popular tools/libraries in this space, analyzing pros/cons objectively",
        "📊 **State of the Industry**: \"State of X in {year}\" - survey current landscape, major players, and future direction",
        "⚡ **Performance Benchmarks**: Compare performance/speed/efficiency of different approaches with real metrics",
        "🎯 **Decision Guide**: \"When to Use X vs Y\" - help readers make informed technology choices",
        "🔨 **Hands-On Tutorial**: Step-by-step guide building something practical from scratch",
        "⚠️ **Common Pitfalls**: \"X Mistakes Developers Make with Y\" - learn from others' errors",
        "🏗️ **Architecture Patterns**: Explore design patterns, best practices, and anti-patterns",
        "💡 **Problem-Solution**: Start with a painful problem, explore multiple solutions",
        "📈 **Migration Guide**: \"Moving from X to Y\" - help developers transition between technologies",
        "🔬 **Deep Technical Dive**: Explain how something works under the hood",
        "🎓 **Learning Path**: \"From Zero to Hero\" - comprehensive learning journey",
        "🆚 **Technology Debate**: Present both sides of a controversial technical decision",
        "🚀 **Productivity Tips**: \"10 Ways to Speed Up Your X Workflow\"",
        "🔐 **Security Focus**: Security considerations, best practices, and common vulnerabilities",
        "💰 **Cost Analysis**: Compare costs, pricing models, and ROI of different solutions",
        "🌍 **Real-World Case Study**: How Company X solved Problem Y (can be anonymized)",
        "🔄 **Evolution Story**: \"How X Has Changed\" - historical perspective on technology evolution",
        "🎪 **Behind the Scenes**: How popular frameworks/tools actually work internally",
        "📱 **Platform-Specific**: Focus on specific platform (mobile, web, desktop, cloud)",
        "🧪 **Experimental**: \"I Tested X for 30 Days\" - personal experiment with insights"
    ];

    public IdeationRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;

        DateTime now = DateTime.Now;
        int currentYear = now.Year;
        string currentQuarter = $"Q{(now.Month - 1) / 3 + 1}";
        string currentMonth = now.ToString("MMMM");

        string instructions = $"""
                               You are an expert content strategist specializing in developer marketing through educational content.
                               Your goal is to create article ideas that provide GENUINE VALUE while subtly influencing decisions.

                               Current Date: {now:MMMM dd, yyyy} ({currentQuarter} {currentYear})
                               Background Context (Keep Private): {config.Objective}

                               STRATEGIC APPROACH - The Art of Subtle Influence:

                               1. **Problem-First Thinking**
                                  - Start with real problems developers face TODAY
                                  - Address pain points, frustrations, decision paralysis
                                  - Offer solutions (where our context naturally fits as ONE option)

                               2. **Trend Commentary & Analysis**
                                  - Analyze CURRENT industry shifts, new technologies, changing best practices
                                  - Compare approaches, frameworks, or patterns (tool comparisons are EXCELLENT)
                                  - Position context as a relevant example without forcing it
                                  - Use actual trending topics from research

                               3. **Educational Deep-Dives**
                                  - "How to...", "Understanding...", "Guide to..."
                                  - Technical tutorials, architecture patterns, best practices
                                  - Mention context where it genuinely adds value

                               4. **Temporal Relevance (USE CURRENT DATE)**
                                  - "Best X in {currentQuarter} {currentYear}", "X Trends in {currentYear}", "What's Changed in {currentYear}"
                                  - Month/quarter/year-specific: "{currentMonth} {currentYear}", "{currentQuarter} {currentYear}"
                                  - Current events, new releases, emerging patterns
                                  - "State of X in {currentYear}" surveys

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

        ChatModel model = new ChatModel(config.Models.Ideation);

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
        string trendsContext = BuildTrendsContext(process.Input);
        
        // Get random article angle hints
        string[] selectedAngles = GetRandomArticleAngles(count: 3);
        string anglesText = "";
        
        if (selectedAngles.Length > 0)
        {
            Console.WriteLine($"  [IdeationAgent] 🎯 Selected {selectedAngles.Length} article angle suggestions:");
            foreach (string angle in selectedAngles)
            {
                string angleTitle = angle.Split(':')[0].Trim();
                Console.WriteLine($"    • {angleTitle}");
            }
            
            anglesText = "**🎯 ARTICLE ANGLE SUGGESTIONS (Consider these approaches):**\n\n";
            foreach (string angle in selectedAngles)
            {
                anglesText += $"{angle}\n\n";
            }
            anglesText += "---\n\n";
        }
        
        DateTime now = DateTime.Now;
        int currentYear = now.Year;
        string currentQuarter = $"Q{(now.Month - 1) / 3 + 1}";
        
        string prompt = $"""
                         {anglesText}**CURRENT CONTEXT:**
                         Today's Date: {now:MMMM dd, yyyy}
                         Current Period: {currentQuarter} {currentYear}

                         **TRENDING TOPICS TO LEVERAGE:**
                         {trendsContext}

                         **YOUR MISSION:**
                         Generate 3-5 article ideas that developers will GENUINELY want to read. These ideas should:

                         1. **Latch onto REAL trends** from the research above (not generic AI agent content)
                         2. **Use diverse angles** - comparisons, tutorials, problem-solving, industry analysis, etc.
                         3. **Be temporally relevant** - reference current period ({currentQuarter} {currentYear}) when appropriate
                         4. **Provide educational value first** - teach, compare, analyze, guide
                         5. **Subtly position context** - mention as ONE option among several, not the hero
                         6. **Use engaging titles** - NO direct promotional mentions, use curiosity/specificity/controversy

                         **EXCELLENT ANGLE IDEAS:**
                         - "Top 5 C# AI Libraries in {currentQuarter} {currentYear}: A Developer's Comparison"
                         - "I Spent 30 Days Testing LLM Orchestration Frameworks in .NET - Here's What I Found"
                         - "Why Your C# AI Integration is Probably Too Complex (And How to Simplify)"
                         - "From LangChain to Native .NET: A Migration Story"
                         - "The Hidden Costs of Popular AI SDKs (Performance Benchmark)"

                         Think like a respected developer writing for other developers - what would YOU genuinely want to read?
                         Be creative with angles, be current with dates, be diverse in approach!
                         """;

        Conversation conversation = await _agent.Run(prompt);
        ChatMessage lastMessage = conversation.Messages.Last();
        ArticleIdeaOutput? ideaOutput = await lastMessage.Content?.SmartParseJsonAsync<ArticleIdeaOutput>(_agent);

        if (ideaOutput == null)
        {
            return new ArticleIdeaOutput
            {
                Ideas = []
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

        string context = "Trending Topics:\n";
        foreach (TrendItem trend in trends.Trends.Take(5))
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

    /// <summary>
    /// Selects random article angle hints to diversify ideation
    /// </summary>
    private static string[] GetRandomArticleAngles(int count)
    {
        if (count <= 0 || count > ArticleAngleHints.Length)
            return [];

        // Fisher-Yates shuffle
        List<int> indices = Enumerable.Range(0, ArticleAngleHints.Length).ToList();
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices.Take(count)
            .Select(i => ArticleAngleHints[i])
            .ToArray();
    }
}

