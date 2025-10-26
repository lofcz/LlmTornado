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

namespace LlmTornado.Internal.Press.Agents;

public class WritingRunnable : OrchestrationRunnable<ResearchOutput, ArticleOutput>
{
    private readonly TornadoAgent _agent;
    private readonly AppConfiguration _config;
    private bool initialized = false;

    public WritingRunnable(
        TornadoApi client,
        AppConfiguration config,
        string articleTitle,
        string articleIdea,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;

        string codebaseInstructions = $"""
                                       CRITICAL: CODEBASE ACCESS AVAILABLE

                                       You have direct access to the LlmTornado C# repository via MCP filesystem tools.
                                       Repository location: {config.CodebaseAccess.RepositoryPath}

                                       **PATH FORMAT RULES:**
                                       The repository is mounted in a Docker container at `/projects/workspace`.
                                       ALL paths must be relative to this mount point:
                                       - ✅ USE: "/projects/workspace/src/LlmTornado.Agents/TornadoAgent.cs"
                                       - ✅ USE: "/projects/workspace/src/LlmTornado/Chat"
                                       - ❌ NEVER: Absolute Windows paths like "C:\\Users\\..."
                                       - ❌ NEVER: Paths starting with "/app/" or "/src"

                                       **YOU MUST USE THE FILESYSTEM TOOLS TO READ ACTUAL CODE.**

                                       MANDATORY WORKFLOW FOR WRITING ABOUT LLMTORNADO:

                                       1. **FIRST: Explore the Codebase Structure**
                                          - Use `list_directory` with path: "/projects/workspace/src"
                                          - Browse subdirectories: "/projects/workspace/src/LlmTornado.Agents", "/projects/workspace/src/LlmTornado/Chat", etc.
                                          - Identify relevant directories for your article topic

                                       2. **THEN: Read Relevant Source Files**
                                          - Use `read_file` to examine actual implementation
                                          - Look in:
                                            * /projects/workspace/src/LlmTornado - Core library
                                            * /projects/workspace/src/LlmTornado.Agents - Agents framework
                                            * /projects/workspace/src/LlmTornado/Chat - Chat and conversation APIs
                                            * /projects/workspace/src/LlmTornado.Demo - Real usage examples
                                            * /projects/workspace/src/LlmTornado.Mcp - Model Context Protocol (MCP)
                                            * /projects/workspace/src/LlmTornado.A2A - Agent-to-Agent communication (A2A)

                                       3. **USE REAL CODE SNIPPETS**
                                          - Copy actual code from the files you read
                                          - DO NOT make up examples - use real implementation
                                          - Show actual class/method signatures
                                          - Reference real file paths (e.g., "from src/LlmTornado.Agents/TornadoAgent.cs")

                                       4. **Verify Technical Claims**
                                          - If you mention a feature, READ THE CODE that implements it
                                          - Check actual method names, parameters, return types
                                          - Look at real examples in Demo projects

                                       WHY THIS MATTERS:
                                       - Generic examples are obvious marketing fluff
                                       - Real code from the actual repository is credible and valuable
                                       - Developers can verify your claims by looking at the same files
                                       - Shows you actually understand the library, not just talking about it

                                       EXAMPLES OF WHAT TO DO:
                                       ✅ "Let's look at how TornadoAgent handles tool calls. Here's the actual implementation from src/LlmTornado.Agents/TornadoAgent.cs..."
                                       ✅ "The orchestration system uses a graph-based approach. In src/LlmTornado.Agents/ChatRuntime/Orchestration/..."
                                       ✅ "Here's a real example from the Demo project showing multi-agent workflow..."

                                       WHAT NOT TO DO:
                                       ❌ "TornadoAgent agent = new TornadoAgent(...);" // Generic placeholder code
                                       ❌ "You can do X with Y..." // Without showing actual code
                                       ❌ Writing about features you haven't verified in the codebase

                                       TRACKING:
                                       - Maximum {config.CodebaseAccess.MaxFilesPerSession} files per article
                                       - Track accessed files via RuntimeProperties["AccessedFiles"]
                                       - Share memory across all agents in orchestration

                                       **REMEMBER: If you're writing about LlmTornado, YOU MUST READ THE ACTUAL CODE FIRST.**
                                       **This is not optional - it's the core value proposition of this article.**
                                       """;
        
        var instructions = $"""
                            You are an expert technical writer and journalist specializing in developer education.
                            Your mission: Create content that developers GENUINELY want to read, while subtly influencing their decisions.

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

                            2. **Educate First, Influence Second**
                               - Teach concepts, patterns, best practices
                               - Compare multiple approaches objectively
                               - Show tradeoffs, limitations, real-world considerations
                               - Mention background context as ONE viable option (not THE option)

                            3. **Show, Don't Tell**
                               - Use code examples, case studies, benchmarks
                               - Share specific numbers, metrics, real experiences
                               - Let results speak louder than claims
                               - "Here's what happened when..." > "This is the best..."

                            4. **Natural Integration (NEVER Force It)**
                               - Mention the background context where it GENUINELY fits
                               - Examples: "tools like X, Y, and [context]", "I used [context] for this..."
                               - If it doesn't fit naturally, DON'T force it
                               - The reader should learn something valuable even if they never use our context

                            STYLE GUIDELINES:
                            - Write in **Markdown** format with proper structure
                            - Conversational but professional (like a senior dev sharing knowledge)
                            - Use "I" and "we" for relatability (share experiences, mistakes, learnings)
                            - Include code examples with C# syntax where relevant
                            - Cite sources naturally, build credibility
                            - Target: {config.ReviewLoop.QualityThresholds.MinWordCount}+ words of VALUABLE content

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

                            **Conclusion (Actionable)**
                            - Recap key insights
                            - Provide next steps
                            - Soft CTA if natural (try X, explore Y, etc.)

                            ANTI-PATTERNS TO AVOID:
                            ❌ "Product X is the best solution for..."
                            ❌ Listicles that are just feature lists in disguise
                            ❌ Mentioning the context in every section
                            ❌ Making claims without evidence
                            ❌ Writing like a press release or ad copy

                            GOOD EXAMPLES:
                            ✅ "When I was building a production chatbot, I hit a wall with API rate limits..."
                            ✅ "There are several approaches here - you can use LangChain, Semantic Kernel, LlmTornado, or even roll your own..."
                            ✅ "After processing 100B+ tokens, here's what I learned about error handling..."
                            ✅ "The real challenge isn't choosing a library, it's understanding X..."

                            Remember: Your credibility comes from being HONEST and HELPFUL, not promotional.
                            Write the article YOU would want to read as a developer.
                            """;

        var model = new ChatModel(config.Models.Writing);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Writing Agent",
            instructions: instructions,
            outputSchema: typeof(ArticleOutput));
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
            foreach (var server in mcpServers)
            {
                Console.WriteLine($"  [WritingAgent] Initializing MCP server: {server.ServerLabel}");
                await server.InitializeAsync();
                
                var tools = server.AllowedTornadoTools.ToArray();
                Console.WriteLine($"  [WritingAgent] Adding {tools.Length} MCP tools to agent");
                foreach (var tool in tools)
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

        var research = process.Input;
        
        Console.WriteLine($"  [WritingAgent] Agent has {_agent.ToolList.Count} tools available");
        Console.WriteLine($"  [WritingAgent] MCP tools: {_agent.McpTools.Count}");
        
        // PHASE 1: Exploration & Writing (NO structured output - allows tool calls)
        Console.WriteLine($"  [WritingAgent] PHASE 1: Exploring codebase and writing draft...");
        var explorationPrompt = BuildWritingPrompt(research);
        explorationPrompt += """
            
            
            Write the complete article in MARKDOWN format.
            Include all sections, code examples, and explanations.
            
            When you're done writing, output ONLY the article content in markdown.
            DO NOT output JSON yet - we'll format it in the next step.
            """;
        
        // Temporarily remove output schema to allow tool calls
        var originalSchema = _agent.OutputSchema;
        _agent.UpdateOutputSchema(null);
        
        var conversation = await _agent.RunAsync(explorationPrompt, maxTurns: 20, onAgentRunnerEvent: (evt) =>
        {
            Console.WriteLine($"  [WritingAgent] AgentRunner Event: {evt.EventType} at {evt.Timestamp:HH:mm:ss}");
            
            if (evt.InternalConversation != null)
            {
                var lastMsg = evt.InternalConversation.Messages.LastOrDefault();
                if (lastMsg != null)
                {
                    if (lastMsg.ToolCalls != null && lastMsg.ToolCalls.Count > 0)
                    {
                        foreach (var toolCall in lastMsg.ToolCalls)
                        {
                            var funcName = toolCall.FunctionCall?.Name ?? toolCall.CustomCall?.Name ?? "unknown";
                            Console.WriteLine($"    🔧 Tool: {funcName}");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(lastMsg.Content))
                    {
                        Console.WriteLine($"    💬 Message: {lastMsg.Content}");
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
        foreach (var msg in conversation.Messages)
        {
            if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                toolCallCount += msg.ToolCalls.Count;
                foreach (var toolCall in msg.ToolCalls)
                {
                    var funcName = toolCall.FunctionCall?.Name ?? toolCall.CustomCall?.Name ?? "unknown";
                    Console.WriteLine($"    🔧 Tool called: {funcName}");
                }
            }
        }
        Console.WriteLine($"  [WritingAgent] Total tool calls made: {toolCallCount}");
        
        var draftArticle = conversation.Messages.Last().Content;
        
        if (string.IsNullOrEmpty(draftArticle))
        {
            Console.WriteLine($"  [WritingAgent] ERROR: No article content generated");
            return new ArticleOutput
            {
                Title = "Article Generation Failed",
                Body = "Unable to generate article content",
                Description = "Error occurred during writing",
                Tags = Array.Empty<string>(),
                WordCount = 0,
                Slug = "error"
            };
        }
        
        // PHASE 2: Format into structured JSON
        Console.WriteLine($"  [WritingAgent] PHASE 2: Formatting article into structured output...");
        var formattingPrompt = $"""
            You wrote the following article in markdown format:
            
            {draftArticle}
            
            Now, format this article into the required JSON structure:
            - Extract the title (first heading)
            - The body is the full markdown content
            - Create a compelling description (1-2 sentences)
            - Generate 3-5 relevant tags
            - Create a URL-friendly slug
            - Count the words
            
            Output the article in the required JSON format.
            """;
        
        var formattingConversation = await _agent.RunAsync(formattingPrompt, maxTurns: 1);
        var lastMessage = formattingConversation.Messages.Last();
        var articleOutput = await lastMessage.Content?.SmartParseJsonAsync<ArticleOutput>(_agent);

        if (articleOutput == null)
        {
            Console.WriteLine($"  [WritingAgent] ERROR: Failed to parse structured output, using fallback");
            // Fallback: parse manually from markdown
            return ParseArticleFromMarkdown(draftArticle);
        }

        Console.WriteLine($"  [WritingAgent] ✓ Article formatted successfully");
        
        // Generate slug if not provided
        var article = articleOutput;
        if (string.IsNullOrEmpty(article.Slug))
        {
            article.Slug = GenerateSlug(article.Title);
        }

        // Calculate word count if not provided
        if (article.WordCount == 0)
        {
            article.WordCount = CountWords(article.Body);
        }

        return article;
    }

    private string BuildWritingPrompt(ResearchOutput research)
    {
        var prompt = "";
        
        // Add codebase access reminder FIRST if enabled - before any research data
        if (_config.CodebaseAccess.Enabled)
        {
            var accessedFiles = Orchestrator.RuntimeProperties.TryGetValue("AccessedFiles", out object? property) 
                ? property as List<string> 
                : new List<string>();
            
            var filesLeft = _config.CodebaseAccess.MaxFilesPerSession - (accessedFiles?.Count ?? 0);
            
            prompt += $$"""
                        ⚠️ **MANDATORY STEP: ACCESS THE LLMTORNADO CODEBASE** ⚠️

                        Files accessed: {{accessedFiles?.Count ?? 0}}/{{_config.CodebaseAccess.MaxFilesPerSession}}
                        Files remaining: {{filesLeft}}

                        **YOU HAVE FILESYSTEM TOOLS AVAILABLE. USE THEM NOW.**

                        **IMPORTANT PATH FORMAT:**
                        The repository is mounted at `/projects/workspace` in the filesystem.
                        Use paths relative to that mount point:
                        - ✅ CORRECT: "src/LlmTornado.Agents"
                        - ✅ CORRECT: "src/LlmTornado/Chat"
                        - ❌ WRONG: "C:\\Users\\..." (absolute Windows paths)
                        - ❌ WRONG: "/app/src" (wrong mount point)

                        STEP-BY-STEP PROCESS (DO THIS FIRST):

                        Step 1: Explore the repository structure
                        → list_directory { "path": "src" }
                        → This shows you what's available (LlmTornado/, LlmTornado.Agents/, LlmTornado.Demo/, etc.)

                        Step 2: Find relevant files for your article
                        → If writing about agents: list_directory { "path": "src/LlmTornado.Agents" }
                        → If writing about chat: list_directory { "path": "src/LlmTornado/Chat" }
                        → If writing about demos: list_directory { "path": "src/LlmTornado.Demo" }

                        Step 3: Read the actual implementation
                        → Call read_file on 2-3 relevant files
                        → Example: read_file { "path": "src/LlmTornado.Agents/TornadoAgent.cs" }
                        → Example: read_file { "path": "src/LlmTornado.Demo/Demos/01 hello world.cs" }

                        Step 4: Use the REAL code you just read in your article
                        → Copy actual method signatures
                        → Show real implementation patterns
                        → Reference the file paths

                        **THIS IS NOT OPTIONAL. START BY CALLING THE TOOLS.**
                        Articles without real code from the repository will be REJECTED.
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
            foreach (var insight in research.KeyInsights)
            {
                prompt += $"- {insight}\n";
            }
            prompt += "\n";
        }

        if (research.Facts != null && research.Facts.Length > 0)
        {
            prompt += "**Research Facts:**\n";
            foreach (var fact in research.Facts.Take(10))
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
            prompt += "**Sources to Reference:**\n";
            foreach (var source in research.Sources.Take(5))
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

        prompt += """
            
            Write the complete article now. Remember:
            - Use Markdown formatting
            - Include code examples where appropriate
            - Cite sources naturally
            - Write {config.ReviewLoop.QualityThresholds.MinWordCount}+ words
            - Make it engaging and actionable
            - Subtly integrate LlmTornado's advantages
            """;

        return prompt;
    }

    private ArticleOutput ParseArticleFromMarkdown(string markdown)
    {
        // Extract title (first # heading)
        var lines = markdown.Split('\n');
        var title = "Untitled Article";
        var bodyLines = new List<string>();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("# ") && title == "Untitled Article")
            {
                title = line.Substring(2).Trim();
            }
            else
            {
                bodyLines.Add(line);
            }
        }
        
        var body = string.Join("\n", bodyLines).Trim();
        var wordCount = CountWords(body);
        
        return new ArticleOutput
        {
            Title = title,
            Body = body,
            Description = title.Length > 150 ? title.Substring(0, 147) + "..." : title,
            Tags = new[] { "AI", ".NET", "C#", "Development" },
            WordCount = wordCount,
            Slug = GenerateSlug(title)
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

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

