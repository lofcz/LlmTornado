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
using System.Threading.Tasks;
using LlmTornado.Agents.DataModels;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;

namespace LlmTornado.Internal.Press.Agents;

public class ArticleMetadata
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public string Slug { get; set; } = "";
}

public class WritingRunnable : OrchestrationRunnable<ResearchOutput, ArticleOutput>
{
    private readonly TornadoAgent _agent;
    private readonly AppConfiguration _config;
    private bool initialized = false;
    private static readonly Random _random = new Random();

    // Diverse writing style hints to randomly inject
    private static readonly string[] WritingStyleHints =
    [
        "📊 **Use Comparison Tables**: When comparing options/features, use markdown tables for clarity. Example:\n| Feature | Option A | Option B |\n|---------|----------|----------|",
        "🎯 **Include Decision Trees**: Help readers decide with \"When to use X vs Y\" sections based on specific criteria",
        "📈 **Add Performance Metrics**: Include concrete numbers, benchmarks, or timing comparisons when relevant",
        "🔍 **Show Before/After Examples**: Demonstrate improvement with side-by-side code comparisons",
        "⚠️ **Call Out Common Pitfalls**: Add a \"Common Mistakes\" or \"What Not to Do\" section with anti-patterns",
        "🎨 **Use ASCII Diagrams**: For architecture or flow, simple ASCII diagrams can clarify concepts:\n```\nClient → API → Service\n          ↓\n       Database\n```",
        "💡 **Include Quick Wins**: Add a \"Quick Start\" or \"TL;DR\" section at the top for busy readers",
        "🔬 **Add Reproducible Examples**: Include complete, copy-pasteable examples readers can run immediately",
        "📝 **Use Numbered Steps**: For processes or tutorials, use clear numbered steps instead of paragraphs",
        "🧪 **Include Real-World Case Study**: Reference or create a mini case study showing practical application",
        "🎓 **Add a Glossary Section**: For complex topics, include a brief glossary of key terms",
        "🔗 **Create Resource Links Section**: Add a \"Further Reading\" or \"Useful Resources\" section at the end",
        "💬 **Use Callout Boxes**: Highlight important notes with markdown blockquotes for tips/warnings/notes",
        "📋 **Add Checklists**: Include actionable checklists (\"✓ Prerequisites\", \"✓ Testing Steps\")",
        "🎯 **Show Decision Matrix**: When multiple options exist, show a simple decision matrix or flowchart",
        "🔄 **Demonstrate Progressive Enhancement**: Show basic → intermediate → advanced variations",
        "⚡ **Include Troubleshooting Section**: Add common errors and their solutions",
        "🎪 **Use Analogies**: Explain complex technical concepts with relatable analogies",
        "📊 **Visualize Data Structures**: Show object/data structures in a visual format when explaining APIs",
        "🔧 **Show Configuration Examples**: Include complete config file examples, not just snippets"
    ];

    public WritingRunnable(
        TornadoApi client,
        AppConfiguration config,
        string articleTitle,
        string articleIdea,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;

        string codebaseInstructions = $$"""
                                       CRITICAL: CODEBASE ACCESS AVAILABLE

                                       You have direct access to the LlmTornado C# repository via MCP filesystem tools.
                                       Repository location: {{config.CodebaseAccess.RepositoryPath}}

                                       **PATH FORMAT:** Paths are relative to `/projects/workspace`:
                                       - ✅ USE: "/projects/workspace/src/LlmTornado.Demo"
                                       - ❌ NEVER: Absolute Windows paths like "C:\\Users\\..."

                                       **RESEARCH PHASE - READ CODE TO LEARN:**

                                       1. **Start with Demo Files (Most Important)**
                                          - List: "/projects/workspace/src/LlmTornado.Demo"
                                          - Read 3-4 demo files that match your article topic
                                          - These show REAL usage patterns developers will use
                                          - Focus on complete, working examples

                                       2. **Understand Public APIs (Not Internals)**
                                          - Read "/projects/workspace/src/LlmTornado.Agents/TornadoAgent.cs"
                                          - Read "/projects/workspace/src/LlmTornado/Chat/Conversation.cs"
                                          - Focus on PUBLIC constructors, methods, properties
                                          - IGNORE private/internal implementation details

                                       3. **Build Understanding, Not Just Copy**
                                          - Learn HOW developers use the library
                                          - Understand patterns, idioms, best practices
                                          - Extract CONCEPTS, not just syntax

                                       **WRITING PHASE - USE KNOWLEDGE NATURALLY:**

                                       4. **Show Complete, Realistic Examples**
                                          - Include 4-6 substantial code examples (15-40 lines each)
                                          - Use actual code from Demo files, but explain it
                                          - Show initialization, configuration, error handling
                                          - Demonstrate real-world scenarios

                                       5. **Write Like an Experienced User (Don't Flex)**
                                          - DON'T say: "from /projects/workspace/src/..."
                                          - DON'T say: "Looking at the TornadoAgent class definition..."
                                          - DON'T show internal class fields or private methods
                                          - DO write naturally as if you've been using it for months
                                          - DO explain WHAT code does and WHY it matters

                                       6. **Focus on Developer Value**
                                          - Show COMPLETE workflows, not snippets
                                          - Include realistic use cases and patterns
                                          - Compare with alternatives where appropriate
                                          - Explain tradeoffs and best practices

                                       **QUALITY STANDARDS:**

                                       ✅ EXCELLENT Example:
                                       ```csharp
                                       // Create a research assistant with custom behavior
                                       var agent = new TornadoAgent(
                                           client: api,
                                           model: ChatModel.OpenAi.Gpt4,
                                           name: "ResearchAssistant",
                                           instructions: "Provide detailed, cited answers with sources."
                                       );

                                       // Add tools for web search and calculations
                                       agent.AddTool(new WebSearchTool());
                                       agent.AddTool(new CalculatorTool());

                                       // Stream responses for better UX
                                       await foreach (var chunk in agent.StreamAsync("Analyze recent AI trends"))
                                       {
                                           Console.Write(chunk.Delta);
                                       }
                                       ```
                                       ☝️ Complete, realistic, explains the pattern

                                       ❌ BAD Example:
                                       ```csharp
                                       TornadoAgent agent = new TornadoAgent(...);
                                       var result = await agent.Run("query");
                                       ```
                                       ☝️ Too minimal, doesn't show real usage

                                       **AVOID:**
                                       ❌ Showing internal class fields (public string Instructions { get; set; })
                                       ❌ Referencing file paths in article ("from TornadoAgent.cs")
                                       ❌ Minimal 3-line snippets that don't demonstrate real use
                                       ❌ Code without explanation of purpose/value

                                       **DO:**
                                       ✅ Show 4-6 complete, runnable examples throughout article
                                       ✅ Explain the problem each example solves
                                       ✅ Include setup, configuration, error handling
                                       ✅ Write naturally, not like a code review

                                       GOAL: Use code to LEARN deeply, then write as an experienced practitioner.
                                       NOT: Show off that you read the source files.
                                       """;
        
        DateTime now = DateTime.Now;
        int currentYear = now.Year;
        string currentMonth = now.ToString("MMMM");
        
        string instructions = $"""
                               You are an expert technical writer and journalist specializing in developer education.
                               Your mission: Create content that developers GENUINELY want to read, while subtly influencing their decisions.

                               Current Date: {now:MMMM dd, yyyy}
                               Background Context (Use Subtly): {config.Objective}

                               Article Assignment:
                               Title: {articleTitle}
                               Concept: {articleIdea}

                               {(config.CodebaseAccess.Enabled ? codebaseInstructions : "")}

                               WRITING PHILOSOPHY - The 90/10 Rule:
                               - 90% genuine educational value, problem-solving, insights
                               - 10% subtle positioning of the background context

                               CONTENT STRATEGY:

                               1. **Lead with the Problem/Question**
                                  - Hook readers with a relatable pain point or curiosity
                                  - Establish credibility through understanding
                                  - Make them WANT the solution you're about to provide

                               2. **Educate First, Influence Second (STRATEGIC POSITIONING)**
                                  - Teach concepts, patterns, best practices
                                  - Compare multiple approaches objectively
                                  - Show tradeoffs, limitations, real-world considerations
                                  - **SMART JOURNALISM: Position LlmTornado strategically**
                                    * In comparison tables/lists: Place LlmTornado FIRST (people pick the first option)
                                    * In code examples: Show LlmTornado SDK in the FIRST major snippet
                                    * In feature comparisons: Lead with LlmTornado's strengths, then others
                                    * Example: "LlmTornado, LangChain, and Semantic Kernel all support X..."
                                    * NOT: "There's LangChain, Semantic Kernel, and also LlmTornado..."

                               3. **Show, Don't Tell (WITH CITATIONS)**
                                  - Use code examples, case studies, benchmarks
                                  - Share specific numbers, metrics, real experiences
                                  - Let results speak louder than claims
                                  - "Here's what happened when..." > "This is the best..."
                                  - **ALWAYS hyperlink citations and sources**
                                    * Link to research papers, documentation, blog posts
                                    * Example: "[According to recent studies](https://example.com/study)..."
                                    * Example: "As noted in the [official documentation](https://link.to/docs)..."
                                    * Increases SEO value and builds credibility

                               4. **Natural Integration (NEVER Force It)**
                                  - Mention the background context where it GENUINELY fits
                                  - Examples: "tools like X, Y, and [context]", "I used [context] for this..."
                                  - If it doesn't fit naturally, DON'T force it
                                  - The reader should learn something valuable even if they never use our context

                               TONE & VOICE (CRITICAL):
                               - Write like a developer SHARING experiences with peers, not TEACHING from above
                               - Use "I've found..." / "In my experience..." / "Here's what worked..." instead of "You should..." / "You need to..." / "Best practice is..."
                               - Share war stories, mistakes, discoveries - not prescriptions
                               - Example GOOD: "After spending 3 days debugging, I realized..."
                               - Example BAD: "The right way to do this is..."
                               - Example GOOD: "This pattern saved me hours when..."
                               - Example BAD: "You must follow these steps..."
                               - Be conversational, humble, and collaborative - like pairing with a colleague
                               - Include code examples with C# syntax where relevant
                               - Cite sources naturally with HYPERLINKS
                               - Target: {config.ReviewLoop.QualityThresholds.MinWordCount}+ words of VALUABLE content

                               **CRITICAL CODE SNIPPET REQUIREMENTS:**

                               1. **ALWAYS Include `using` Statements**
                                  - Every code example MUST start with necessary using statements
                                  - Example: `using LlmTornado.Chat;`, `using LlmTornado.Agents;`

                               2. **ALWAYS Include Installation Instructions BEFORE First Code**
                                  - Place installation section immediately before the first code snippet
                                  - Use `dotnet add package` format:
                                  ```bash
                                  dotnet add package LlmTornado
                                  dotnet add package LlmTornado.Agents
                                  ```

                               3. **Terminology for LlmTornado**
                                  - Call it an"SDK, library or framework
                                  - Example: "LlmTornado", "this .NET SDK"

                               4. **ALWAYS Link once to Repository**
                                  - Include link to GitHub: https://github.com/lofcz/LlmTornado
                                  - Place naturally in context, e.g., "For more examples, check the [LlmTornado repository](https://github.com/lofcz/LlmTornado)"

                               STRUCTURE TEMPLATE:

                               **Introduction (Hook Hard)**
                               - Start with a problem, surprising fact, or provocative question
                               - Show you understand the reader's struggle
                               - Promise specific value (what they'll learn)

                               **Main Content (Deliver Value)**
                               - Clear headings for scannability
                               - Progressive disclosure (simple → complex)
                               - Real examples, not just theory
                               - Honest about tradeoffs and limitations
                               - Background context mentioned naturally (1-3 times max)

                               **Conclusion (Reflective, Not Prescriptive)**
                               - Share what you've learned from the exploration
                               - Offer thoughts on next steps (not commands)
                               - Example GOOD: "I'm planning to try X next..."
                               - Example BAD: "You should do X, Y, Z..."
                               - Keep it conversational and open-ended

                               ANTI-PATTERNS TO AVOID:
                               ❌ "Product X is the best solution for..."
                               ❌ Listicles that are just feature lists in disguise
                               ❌ Mentioning the context in every section
                               ❌ Making claims without evidence
                               ❌ Writing like a press release or ad copy

                               GOOD EXAMPLES:

                               ✅ STRATEGIC POSITIONING:
                               "For C# developers, the main options are the LlmTornado, Semantic Kernel, and LangChain..."
                               "I tested LlmTornado, LangChain, and Semantic Kernel against this use case..."

                               ✅ CODE WITH INSTALLATION & USINGS:
                               ```bash
                               dotnet add package LlmTornado.Agents
                               ```
                               ```csharp
                               using LlmTornado.Agents;
                               using LlmTornado.Chat;

                               var agent = new TornadoAgent(client, model, ...);
                               ```

                               ✅ NATURAL NARRATIVE:
                               "When I was building a production chatbot, I hit a wall with API rate limits..."
                               "After processing 100B+ tokens with the [LlmTornado](https://github.com/lofcz/LlmTornado), here's what I learned..."
                               "The real challenge isn't choosing an SDK, it's understanding X..."

                               ✅ HYPERLINKED CITATIONS:
                               "According to [Microsoft's AI documentation](https://docs.microsoft.com/...)..."
                               "[Recent benchmarks](https://example.com/benchmark) show that..."

                               Remember: Your credibility comes from being HONEST and HELPFUL, not promotional.
                               Write the article YOU would want to read as a developer.
                               Strategic positioning is about being FIRST in comparisons, not being the ONLY option.
                               """;

        ChatModel model = new ChatModel(config.Models.Writing);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Writing Agent",
            instructions: instructions,
            outputSchema: typeof(ArticleOutput),
            maxTokens: 16_000,
            temperature: 1);
    }

    public override async ValueTask InitializeRunnable()
    {
        if (initialized)
        {
            Console.WriteLine("  [WritingAgent] Already initialized, skipping MCP setup");
            return;
        }

        initialized = true;
        
        Console.WriteLine("  [WritingAgent] Initializing MCP filesystem access...");
        
        // Setup MCP filesystem access if enabled
        List<MCPServer>? mcpServers = null;
        if (_config.CodebaseAccess.Enabled && !string.IsNullOrEmpty(_config.CodebaseAccess.RepositoryPath))
        {
            Console.WriteLine($"  [WritingAgent] MCP enabled for repository: {_config.CodebaseAccess.RepositoryPath}");
            Console.WriteLine($"  [WritingAgent] Allowed tools: {string.Join(", ", _config.CodebaseAccess.AllowedTools)}");
            
            mcpServers =
            [
                MCPToolkits.FileSystemToolkit(
                    _config.CodebaseAccess.RepositoryPath,
                    _config.CodebaseAccess.AllowedTools.ToArray())
            ];
        }
        else
        {
            Console.WriteLine("  [WritingAgent] MCP codebase access is disabled");
            return;
        }
                
        if (mcpServers != null && mcpServers.Count > 0)
        {
            foreach (MCPServer server in mcpServers)
            {
                Console.WriteLine($"  [WritingAgent] Initializing MCP server: {server.ServerLabel}");
                await server.InitializeAsync();
                
                Tool[] tools = server.AllowedTornadoTools.ToArray();
                Console.WriteLine($"  [WritingAgent] Adding {tools.Length} MCP tools to agent");
                foreach (Tool tool in tools)
                {
                    Console.WriteLine($"    - {tool.Function?.Name ?? "unknown"}");
                }
                
                _agent.AddMcpTools(tools);
            }
            
            Console.WriteLine($"  [WritingAgent] ✓ MCP initialization complete. Total tools available: {_agent.ToolList.Count}");
        }
    }
    
    public override async ValueTask<ArticleOutput> Invoke(RunnableProcess<ResearchOutput, ArticleOutput> process)
    {
        process.RegisterAgent(_agent);

        // Initialize shared memory for file access tracking if not exists
        if (!Orchestrator.RuntimeProperties.ContainsKey("AccessedFiles"))
        {
            Orchestrator.RuntimeProperties["AccessedFiles"] = new List<string>();
        }

        ResearchOutput research = process.Input;
        
        // Check if this is an improvement iteration
        bool isImprovement = Orchestrator?.RuntimeProperties.ContainsKey("ImprovementFeedback") ?? false;
        string? improvementFeedback = null;
        ArticleOutput? previousDraft = null;

        if (isImprovement)
        {
            improvementFeedback = Orchestrator?.RuntimeProperties.GetValueOrDefault("ImprovementFeedback") as string;
            previousDraft = Orchestrator?.RuntimeProperties.GetValueOrDefault("PreviousArticleDraft") as ArticleOutput;
            
            Console.WriteLine($"  [WritingAgent] 🔄 IMPROVEMENT MODE - Revising previous draft");
            Console.WriteLine($"  [WritingAgent] Previous draft word count: {previousDraft?.WordCount ?? 0}");
        }
        
        Console.WriteLine($"  [WritingAgent] Agent has {_agent.ToolList.Count} tools available");
        Console.WriteLine($"  [WritingAgent] MCP tools: {_agent.McpTools.Count}");
        
        // PHASE 1: Exploration & Writing (NO structured output - allows tool calls)
        Console.WriteLine($"  [WritingAgent] PHASE 1: Exploring codebase and writing draft...");
        
        string explorationPrompt;
        if (isImprovement && previousDraft != null && !string.IsNullOrEmpty(improvementFeedback))
        {
            explorationPrompt = BuildImprovementPrompt(previousDraft, improvementFeedback, research);
        }
        else
        {
            explorationPrompt = BuildWritingPrompt(research);
        }
        explorationPrompt += """
            
            
            Write the complete article in MARKDOWN format.
            Include all sections, code examples, and explanations.
            
            When you're done writing, output ONLY the article body in markdown.
            DO NOT include YAML frontmatter (no --- headers).
            DO NOT output JSON yet - we'll format it in the next step.
            Start directly with the article content (title as # heading, then content).
            """;
        
        // Temporarily remove output schema to allow tool calls
        Type? originalSchema = _agent.OutputSchema;
        _agent.UpdateOutputSchema(null);
        
        Conversation conversation = await _agent.Run(explorationPrompt, maxTurns: 20, onAgentRunnerEvent: (evt) =>
        {
            Console.WriteLine($"  [WritingAgent] Event: {evt.EventType} at {evt.Timestamp:HH:mm:ss}");
            
            if (evt is AgentRunnerUsageReceivedEvent usage)
            {
                Console.WriteLine($"[WriterAgent] Usage received: input: {usage.InputTokens}, output: {usage.OutputTokens}, total: {usage.TokenUsageAmount}");
            }
            
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
                            Console.WriteLine($"    🔧 Tool: {funcName} | Args: {Snippet(args, 60)}");
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
        
        // Restore schema for phase 2
        _agent.UpdateOutputSchema(originalSchema);
        
        Console.WriteLine($"  [WritingAgent] Phase 1 complete. Total messages: {conversation.Messages.Count}");
        
        // Count tool calls
        int toolCallCount = 0;
        foreach (ChatMessage msg in conversation.Messages)
        {
            if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                toolCallCount += msg.ToolCalls.Count;
                foreach (ToolCall toolCall in msg.ToolCalls)
                {
                    string funcName = toolCall.FunctionCall?.Name ?? toolCall.CustomCall?.Name ?? "unknown";
                    string args = toolCall.FunctionCall?.Arguments ?? toolCall.CustomCall?.Input ?? "";
                    Console.WriteLine($"    🔧 Tool: {funcName} | {Snippet(args, 500)}");
                }
            }
        }
        Console.WriteLine($"  [WritingAgent] Total tool calls made: {toolCallCount}");
        
        string? draftArticle = conversation.Messages.Last().Content;
        
        if (string.IsNullOrEmpty(draftArticle))
        {
            Console.WriteLine($"  [WritingAgent] ERROR: No article content generated");
            return new ArticleOutput
            {
                Title = "Article Generation Failed",
                Body = "Unable to generate article content",
                Description = "Error occurred during writing",
                Tags = [],
                WordCount = 0,
                Slug = "error"
            };
        }
        
        // Strip any YAML frontmatter if present (should not be there, but just in case)
        draftArticle = StripFrontmatter(draftArticle);
        
        // PHASE 2: Extract metadata (without content - keep it pure)
        Console.WriteLine($"  [WritingAgent] PHASE 2: Extracting article metadata...");
        
        string metadataPrompt = $"""
                                 You wrote the following article:

                                 {Snippet(draftArticle, 500)}

                                 Extract metadata for this article:
                                 - Title: The main title (from first # heading)
                                 - Description: Compelling 1-2 sentence summary for SEO (max 200 chars)
                                 - Tags: 3-5 relevant tags (e.g., ["C#", "AI", "Agents", ".NET"])
                                 - Slug: URL-friendly slug (lowercase, hyphens, no special chars)

                                 Output ONLY the metadata as JSON.
                                 """;
        
        // Use structured output for metadata only
        TornadoAgent metadataAgent = new TornadoAgent(
            _agent.Client,
            _agent.Model,
            name: "MetadataExtractor",
            instructions: "Extract article metadata accurately. Output must match ArticleMetadata schema.",
            outputSchema: typeof(ArticleMetadata)
        );
        
        Conversation metadataConversation = await metadataAgent.Run(metadataPrompt, maxTurns: 1);
        ChatMessage lastMetadataMessage = metadataConversation.Messages.Last();
        
        // Parse metadata using SmartParseJsonAsync
        ArticleMetadata? metadata = await lastMetadataMessage.Content?.SmartParseJsonAsync<ArticleMetadata>(metadataAgent);
        
        if (metadata == null)
        {
            Console.WriteLine($"  [WritingAgent] WARNING: Failed to parse metadata, using fallback extraction");
        }
        
        // Build final article output
        ArticleOutput article = new ArticleOutput
        {
            Title = metadata?.Title ?? ExtractTitle(draftArticle),
            Body = draftArticle, // Keep the pure markdown content
            Description = metadata?.Description ?? ExtractDescription(draftArticle),
            Tags = metadata?.Tags ?? ["AI", ".NET", "C#", "Development"],
            Slug = metadata?.Slug ?? GenerateSlug(ExtractTitle(draftArticle)),
            WordCount = CountWords(draftArticle)
        };
        
        Console.WriteLine($"  [WritingAgent] ✓ Article complete: {article.Title}");
        Console.WriteLine($"  [WritingAgent]   Words: {article.WordCount}, Tags: {string.Join(", ", article.Tags)}");

        return article;
    }

    private string BuildWritingPrompt(ResearchOutput research)
    {
        string prompt = "";
        
        // Add random writing style hints for diversity (configurable count)
        int hintCount = _config.ArticleGeneration.WritingStyleHints;
        if (hintCount > 0)
        {
            string[] selectedHints = GetRandomWritingHints(count: hintCount);
            if (selectedHints.Length > 0)
            {
                Console.WriteLine($"  [WritingAgent] 🎨 Selected {selectedHints.Length} writing style hints:");
                foreach (string hint in selectedHints)
                {
                    // Extract just the hint title for logging (first part before colon)
                    string hintTitle = hint.Split(':')[0].Trim();
                    Console.WriteLine($"    • {hintTitle}");
                }
                
                prompt += "**✨ WRITING STYLE SUGGESTIONS FOR THIS ARTICLE:**\n\n";
                foreach (string hint in selectedHints)
                {
                    prompt += $"{hint}\n\n";
                }
                prompt += "---\n\n";
            }
        }
        
        // Add codebase access reminder FIRST if enabled - before any research data
        if (_config.CodebaseAccess.Enabled)
        {
            List<string>? accessedFiles = Orchestrator.RuntimeProperties.TryGetValue("AccessedFiles", out object? property) 
                ? property as List<string> 
                : [];
            
            int filesLeft = _config.CodebaseAccess.MaxFilesPerSession - (accessedFiles?.Count ?? 0);
            
            prompt += $$"""
                        ⚠️ **MANDATORY: ACCESS CODEBASE FOR REAL EXAMPLES** ⚠️

                        Files remaining: {{filesLeft}}/{{_config.CodebaseAccess.MaxFilesPerSession}}

                        **RESEARCH WORKFLOW (Do This First):**

                        1. **Read Demo Files** (PRIORITY)
                           → list_directory { "path": "/projects/workspace/src/LlmTornado.Demo" }
                           → Read 3-4 relevant demos to understand real usage patterns
                           → Focus on complete, working examples

                        2. **Check Public APIs**
                           → read_file { "path": "/projects/workspace/src/LlmTornado.Agents/TornadoAgent.cs" }
                           → Note public constructors/methods (ignore internals)

                        3. **Build Deep Understanding**
                           → Learn how developers actually use the library
                           → Extract patterns, idioms, best practices

                        **WRITING REQUIREMENTS:**

                        - Include 4-6 COMPLETE code examples (15-40 lines each)
                        - Show realistic scenarios with setup + error handling
                        - Use actual code from demos, but explain it naturally
                        - DON'T reference file paths ("from TornadoAgent.cs")
                        - DON'T show internal class structure
                        - DO write as an experienced user
                        - DO explain WHAT and WHY, not just HOW

                        **Write naturally. Don't "flex" that you read the source.**
                        """;
        }
        
        // Now add the research context
        prompt += "\n\nBased on the following research, write a comprehensive article:\n\n";

        if (research.Summary != null && !string.IsNullOrEmpty(research.Summary))
        {
            prompt += $"**Research Summary:**\n{research.Summary}\n\n";
        }

        if (research.KeyInsights != null && research.KeyInsights.Length > 0)
        {
            prompt += "**Key Insights:**\n";
            foreach (string insight in research.KeyInsights)
            {
                prompt += $"- {insight}\n";
            }
            prompt += "\n";
        }

        if (research.Facts != null && research.Facts.Length > 0)
        {
            prompt += "**Research Facts:**\n";
            foreach (ResearchFact fact in research.Facts.Take(10))
            {
                prompt += $"- {fact.Fact}";
                if (!string.IsNullOrEmpty(fact.SourceUrl))
                {
                    prompt += $" ([source]({fact.SourceUrl}))";
                }
                prompt += "\n";
            }
            prompt += "\n";
        }

        if (research.Sources != null && research.Sources.Length > 0)
        {
            prompt += "**Sources to Reference (MUST USE AS HYPERLINKS):**\n";
            foreach (ResearchSource source in research.Sources.Take(5))
            {
                prompt += $"- [{source.Title}]({source.Url})";
                if (!string.IsNullOrEmpty(source.Excerpt))
                {
                    prompt += $"\n  {source.Excerpt}";
                }
                prompt += "\n";
            }
            prompt += "\n";
        }

        prompt += $"""
            
            Write the complete article now. Remember:
            - **CRITICAL: HYPERLINK ALL CITATIONS** - Use **at least some** of the provided source URLs as markdown links
            - Example: "According to [recent research]({(research.Sources?.FirstOrDefault()?.Url ?? "url")})..."
            - Example: "[Studies show](url) that..."
            - Use Markdown formatting
            - Include code examples where appropriate
            - Cite sources naturally throughout the article (not just at the end)
            - Write {_config.ReviewLoop.QualityThresholds.MinWordCount}+ words
            - Make it engaging and actionable
            - Subtly integrate LlmTornado's advantages
            """;

        return prompt;
    }

    private string ExtractTitle(string markdown)
    {
        string[] lines = markdown.Split('\n');
        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith("# "))
            {
                return line.TrimStart().Substring(2).Trim();
            }
        }
        return "Untitled Article";
    }
    
    private string ExtractDescription(string markdown)
    {
        // Find first paragraph after title
        string[] lines = markdown.Split('\n');
        bool foundTitle = false;
        List<string> descriptionLines = [];
        
        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith("# "))
            {
                foundTitle = true;
                continue;
            }
            
            if (foundTitle && !string.IsNullOrWhiteSpace(line))
            {
                descriptionLines.Add(line.Trim());
                if (descriptionLines.Count >= 2) break; // Get first 1-2 sentences
            }
        }
        
        string description = string.Join(" ", descriptionLines);
        if (description.Length > 200)
        {
            description = description.Substring(0, 197) + "...";
        }
        
        return string.IsNullOrEmpty(description) ? "An article about AI and software development." : description;
    }
    
    private string BuildImprovementPrompt(ArticleOutput previousDraft, string feedback, ResearchOutput research)
    {
        string researchContext = FormatResearch(research);
        
        return $"""
                IMPROVEMENT MODE: Revise Existing Article
                
                You are revising an article based on reviewer feedback. Your goal is to IMPROVE the existing article, not rewrite it from scratch.
                
                ═══════════════════════════════════════════════════════════════════
                PREVIOUS ARTICLE DRAFT:
                ═══════════════════════════════════════════════════════════════════
                
                Title: {previousDraft.Title}
                
                {previousDraft.Body}
                
                ═══════════════════════════════════════════════════════════════════
                REVIEWER FEEDBACK:
                ═══════════════════════════════════════════════════════════════════
                
                {feedback}
                
                ═══════════════════════════════════════════════════════════════════
                RESEARCH CONTEXT (for reference):
                ═══════════════════════════════════════════════════════════════════
                
                {researchContext}
                
                ═══════════════════════════════════════════════════════════════════
                YOUR TASK:
                ═══════════════════════════════════════════════════════════════════
                
                1. Read the previous draft carefully
                2. Address EACH issue mentioned in the reviewer feedback
                3. Keep what works well - don't change good sections unnecessarily
                4. Improve specific problem areas:
                   - Fix factual inaccuracies
                   - Add missing sources/citations with hyperlinks
                   - Improve unclear explanations
                   - Strengthen weak arguments
                   - Remove clickbait or promotional language
                   - Add missing code examples if needed
                   - Improve SEO and readability
                5. Maintain the article's voice and structure unless feedback requires changes
                
                OUTPUT: The improved article in markdown format with the same structure as before but with all issues addressed.
                
                DO NOT start from scratch - build on the existing draft and fix the specific problems identified.
                """;
    }
    
    private string FormatResearch(ResearchOutput research)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        if (research.Facts != null && research.Facts.Length > 0)
        {
            sb.AppendLine("Key Facts:");
            foreach (ResearchFact fact in research.Facts.Take(10))
            {
                sb.AppendLine($"- {fact.Fact} (confidence: {fact.Confidence:F2})");
            }
            sb.AppendLine();
        }
        
        if (research.Sources != null && research.Sources.Length > 0)
        {
            sb.AppendLine("Sources:");
            foreach (ResearchSource source in research.Sources.Take(5))
            {
                sb.AppendLine($"- {source.Title} ({source.Url})");
            }
            sb.AppendLine();
        }
        
        if (research.KeyInsights != null && research.KeyInsights.Length > 0)
        {
            sb.AppendLine("Key Insights:");
            foreach (string insight in research.KeyInsights.Take(5))
            {
                sb.AppendLine($"- {insight}");
            }
        }
        
        return sb.ToString();
    }
    
    private ArticleOutput ParseArticleFromMarkdown(string markdown)
    {
        return new ArticleOutput
        {
            Title = ExtractTitle(markdown),
            Body = markdown,
            Description = ExtractDescription(markdown),
            Tags = ["AI", ".NET", "C#", "Development"],
            WordCount = CountWords(markdown),
            Slug = GenerateSlug(ExtractTitle(markdown))
        };
    }

    private string GenerateSlug(string title)
    {
        return title
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(":", "")
            .Replace(",", "")
            .Replace(".", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace("&", "and")
            .Trim('-')
            .Substring(0, Math.Min(100, title.Length));
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private string Snippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return "[empty]";
        
        if (text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength) + "...";
    }
    
    private string StripFrontmatter(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return markdown;
        
        string trimmed = markdown.TrimStart();
        if (!trimmed.StartsWith("---"))
            return markdown; // No frontmatter to strip
        
        // Find the closing ---
        string[] lines = trimmed.Split('\n');
        int closingIndex = -1;
        
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                closingIndex = i;
                break;
            }
        }
        
        if (closingIndex == -1)
            return markdown; // No closing frontmatter, return as is
        
        // Return everything after the closing ---
        string result = string.Join('\n', lines.Skip(closingIndex + 1));
        return result.TrimStart('\n', '\r', ' ');
    }

    /// <summary>
    /// Selects random writing style hints to add diversity to article generation
    /// </summary>
    private static string[] GetRandomWritingHints(int count)
    {
        if (count <= 0 || count > WritingStyleHints.Length)
            return [];

        // Create a copy of indices and shuffle
        List<int> indices = Enumerable.Range(0, WritingStyleHints.Length).ToList();
        
        // Fisher-Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Take the first 'count' indices and return corresponding hints
        return indices.Take(count)
            .Select(i => WritingStyleHints[i])
            .ToArray();
    }
}


