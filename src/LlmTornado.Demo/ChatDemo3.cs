using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Chat.Vendors.Zai;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using PuppeteerSharp;

namespace LlmTornado.Demo;

public partial class ChatDemo : DemoBase
{
    [TornadoTest]
    public static async Task ZaiWebSearch()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Zai.Glm.Glm46,
            Messages = [
                new ChatMessage(ChatMessageRoles.User, "Use web search to find the latest release of NodeJS")
            ],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorZaiExtensions
            {
                BuiltInTools = [
                    new VendorZaiWebSearchTool
                    {
                        WebSearch = new VendorZaiWebSearchObject
                        {
                            Enable = true,
                            SearchEngine = VendorZaiSearchEngine.SearchProJina
                        }
                    }
                ]
            })
        });

        ChatRichResponse response = await chat.GetResponseRich();

        Console.WriteLine("ZAi:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task ZaiGlm()
    {
        await BasicChat(ChatModel.Zai.Glm.Glm46);
    }

    [TornadoTest]
    [Flaky("manual interaction")]
    public static async Task GoogleComputerUse()
    {
        // Download browser if needed
        Console.WriteLine("Downloading browser for Puppeteer...");
        await new BrowserFetcher().DownloadAsync();

        // Set up browser
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = false, // Set to true for headless operation
            DefaultViewport = new ViewPortOptions { Width = 1440, Height = 900 },
            Args = ["--no-sandbox", "--disable-setuid-sandbox"]
        });

        var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 1440, Height = 900 });

        try
        {
            // Create Computer Use API instance
            var api = Program.Connect();

            // Initial screenshot
            var screenshot = await page.ScreenshotDataAsync(new ScreenshotOptions { Type = ScreenshotType.Png });
            string base64Screenshot = Convert.ToBase64String(screenshot);

            Console.WriteLine("🤖 Starting Computer Use automation...");
            Console.WriteLine("Task: Navigate to Google and search for 'AI technology'");
            Console.WriteLine();

            // Create initial request with screenshot
            Conversation conversation = api.Chat.CreateConversation(new ChatRequest
            {
                Model = ChatModel.Google.GeminiPreview.Gemini25ComputerUsePreview102025,
                Messages =
                [
                    new ChatMessage(ChatMessageRoles.User, "Navigate to google.com and search for 'AI technology'")
                    {
                        Parts = [
                            new ChatMessagePart("Navigate to google.com and search for 'AI technology'"),
                            new ChatMessagePart(new ChatImage(base64Screenshot, "image/png"))
                        ]
                    }
                ],
                VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorGoogleExtensions
                {
                    ComputerUse = ChatRequestVendorGoogleComputerUse.Browser
                }),
                MaxTokens = 1000
            });
            
            // Computer Use loop
            int maxTurns = 10;
            for (int turn = 1; turn <= maxTurns; turn++)
            {
                Console.WriteLine($"--- Turn {turn} ---");

                var cc = conversation.Serialize();
                
                try
                {
                    // Send request to Computer Use model
                    ChatRichResponse? response = await conversation.GetResponseRich();

                    Console.WriteLine($"Model response: {response.Text}");

                    // Check if response contains tool calls (UI actions)
                    var toolCalls = response.Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Function).Select(x => x.FunctionCall!).ToList();
                    if (toolCalls == null || toolCalls.Count == 0)
                    {
                        Console.WriteLine("✅ Task completed - no more actions needed");
                        break;
                    }

                    Console.WriteLine($"📋 Executing {toolCalls.Count} UI actions:");

                    ChatMessage? msg = null;

                    // Execute each UI action
                    foreach (var toolCall in toolCalls)
                    {
                        msg = await ExecuteComputerUseAction(page, toolCall);
                    }

                    // Wait a moment for page to update
                    await Task.Delay(2000);

                    // Take new screenshot
                    var newScreenshot = await page.ScreenshotDataAsync(new ScreenshotOptions { Type = ScreenshotType.Png });
                    string newBase64Screenshot = Convert.ToBase64String(newScreenshot);

                    msg ??= new ChatMessage(ChatMessageRoles.User, [
                        new ChatMessagePart("Continue with the automation"),
                        new ChatMessagePart(new ChatImage(newBase64Screenshot, "image/png"))
                    ]);
                    
                    // Create follow-up request with new screenshot
                    conversation.AddMessage(msg);

                    Console.WriteLine($"🔄 Completed turn {turn}, continuing automation...");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in turn {turn}: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine("🎉 Computer Use automation demo completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Demo error: {ex.Message}");
            Console.WriteLine("Note: Computer Use model requires special API access");
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    private static async Task<ChatMessage?> ExecuteComputerUseAction(IPage page, FunctionCall toolCall)
    {
        ChatMessage? toReturn = null;
        
        if (toolCall.Name == null) return toReturn;

        Console.WriteLine($"  🔧 Executing: {toolCall.Name}");

        try
        {
            switch (toolCall.Name.ToLowerInvariant())
            {
                case "open_web_browser":
                    toReturn = new ChatMessage(ChatMessageRoles.User, "Browser opened");
                    // Browser is already open
                    Console.WriteLine("    ✅ Browser already open");
                    break;

                case "navigate":
                    if (toolCall.Get("url", out string? url))
                    {
                        await page.GoToAsync(url);
                        Console.WriteLine($"    ✅ Navigated to: {url}");
                    }
                    break;

                case "click_at":
                    if (toolCall.Get("x", out int? x) &&
                        toolCall.Get("y", out int? y))
                    {
                        // Convert normalized coordinates (0-1000) to actual pixels
                        var actualX = (int)(x / 1000.0 * 1440);
                        var actualY = (int)(y / 1000.0 * 900);

                        await page.Mouse.ClickAsync(actualX, actualY);
                        Console.WriteLine($"    ✅ Clicked at: ({actualX}, {actualY})");
                    }
                    break;

                case "type_text_at":
                    if (toolCall.Get("x", out int? textX) &&
                        toolCall.Get("y", out int? textY) &&
                        toolCall.Get("text", out string? text))
                    {
                        var actualTextX = (int)(textX / 1000.0 * 1440);
                        var actualTextY = (int)(textY / 1000.0 * 900);

                        await page.Mouse.ClickAsync(actualTextX, actualTextY);
                        await page.Keyboard.TypeAsync(text);

                        // Check for press_enter parameter
                        if (toolCall.Get("press_enter", out bool? pressEnter))
                        {
                            await page.Keyboard.PressAsync("Enter");
                            Console.WriteLine($"    ✅ Typed '{text}' and pressed Enter");
                        }
                        else
                        {
                            Console.WriteLine($"    ✅ Typed: {text}");
                        }
                    }
                    break;

                case "scroll_document":
                    if (toolCall.Get("direction", out string? direction))
                    {
                        switch (direction.ToLowerInvariant())
                        {
                            case "down":
                                await page.EvaluateExpressionAsync("window.scrollBy(0, window.innerHeight)");
                                Console.WriteLine("    ✅ Scrolled down");
                                break;
                            case "up":
                                await page.EvaluateExpressionAsync("window.scrollBy(0, -window.innerHeight)");
                                Console.WriteLine("    ✅ Scrolled up");
                                break;
                        }
                    }
                    break;

                case "wait_5_seconds":
                    await Task.Delay(5000);
                    Console.WriteLine("    ✅ Waited 5 seconds");
                    break;

                default:
                    Console.WriteLine($"    ⚠️  Unhandled action: {toolCall.Name}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ❌ Error executing {toolCall.Name}: {ex.Message}");
        }

        return toReturn;
    }

    [TornadoTest]
    public static async Task GoogleComputerUseWithExclusions()
    {
        Console.WriteLine("=== Google Computer Use with Function Exclusions ===");
        Console.WriteLine();

        // Example showing how to exclude certain Computer Use functions
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Google.GeminiPreview.Gemini25ComputerUsePreview102025,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.User,
                    "Open a web browser and navigate to example.com")
            ],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorGoogleExtensions
            {
                ComputerUse = new ChatRequestVendorGoogleComputerUse(
                    ChatRequestVendorGoogleComputerUsePredefinedFunctions.DragAndDrop,
                    ChatRequestVendorGoogleComputerUsePredefinedFunctions.KeyCombination
                )
            })
        };

        Console.WriteLine("Computer Use configuration with exclusions:");
        Console.WriteLine($"- Environment: {request.VendorExtensions.Google.ComputerUse?.Environment}");
        Console.WriteLine($"- Excluded Functions: {string.Join(", ", request.VendorExtensions.Google.ComputerUse?.ExcludedPredefinedFunctions ?? [])}");
        Console.WriteLine();

        Console.WriteLine("Available Computer Use Functions:");
        Console.WriteLine("- OpenWebBrowser, Wait5Seconds, GoBack, GoForward, Search, Navigate");
        Console.WriteLine("- ClickAt, HoverAt, TypeTextAt, KeyCombination, ScrollDocument");
        Console.WriteLine("- ScrollAt, DragAndDrop");
        Console.WriteLine();
        Console.WriteLine("The model will avoid using the excluded functions in its response.");
    }

    [TornadoTest("MCP Anthropic")]
    [Flaky("Requires GITHUB_API_KEY setup in environment variables")]
    public static async Task AnthropicMcpServerUse()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            Messages = [
                new ChatMessage(ChatMessageRoles.User, "Make a new branch on the Agent-Skills Repo")
            ],
            VendorExtensions = new ChatRequestVendorExtensions()
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    McpServers = [
                    new AnthropicMcpServer(){
                            Name = "github",
                            Url = "https://api.githubcopilot.com/mcp/",
                            AuthorizationToken = Environment.GetEnvironmentVariable("GITHUB_API_KEY") ?? "github-api-key"
                        }
                    ]
                }
            }
        });

        ChatRichResponse response = await chat.GetResponseRich();
        Console.WriteLine("Anthropic MCP Server Use:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task QwenMax()
    {
        await BasicChat(ChatModel.Alibaba.Flagship.Qwen3Max);
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionBasic()
    {
        Console.WriteLine("=== Basic Conversation Compression Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        // Set up a system message
        conversation.AddSystemMessage("You are a knowledgeable science tutor.");
        
        // Simulate a long conversation with multiple exchanges
        Console.WriteLine("Building a conversation with 20 messages...");
        for (int i = 0; i < 10; i++)
        {
            conversation.AddUserMessage($"Tell me an interesting fact about the number {i + 1}.");
            conversation.AddAssistantMessage($"The number {i + 1} is interesting because it has unique mathematical properties. [Simulated response for demo purposes]");
        }
        
        Console.WriteLine($"Messages before compression: {conversation.Messages.Count}");
        Console.WriteLine();
        
        // Compress the conversation
        Console.WriteLine("Compressing conversation (keeping last 4 messages, system message preserved)...");
        int compressed = await conversation.CompressMessages(
            chunkSize: 5000,
            preserveRecentCount: 4,
            preserveSystemMessages: true
        );
        
        Console.WriteLine($"Messages after compression: {conversation.Messages.Count}");
        Console.WriteLine($"Compression result: {compressed} net change");
        Console.WriteLine();
        
        // Show the compressed conversation structure
        Console.WriteLine("Compressed conversation structure:");
        foreach (var msg in conversation.Messages)
        {
            string preview = msg.Content?.Length > 80 
                ? msg.Content.Substring(0, 77) + "..." 
                : msg.Content ?? "[No content]";
            Console.WriteLine($"  [{msg.Role}] {preview}");
        }
        Console.WriteLine();
        
        // Continue the conversation with compressed history
        Console.WriteLine("Continuing conversation with compressed history...");
        conversation.AddUserMessage("Can you recap what we've discussed?");
        ChatRichResponse response = await conversation.GetResponseRich();
        Console.WriteLine($"AI Response: {response.Text}");
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionAdvanced()
    {
        Console.WriteLine("=== Advanced Conversation Compression Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo,
            MaxTokens = 500
        });

        // Set up a detailed system message
        conversation.AddSystemMessage("You are an expert software architect helping design a new e-commerce platform.");
        
        // Simulate a technical discussion
        Console.WriteLine("Simulating a long technical discussion...");
        string[] topics = [
            "What database should we use for the product catalog?",
            "How should we handle user authentication?",
            "What's the best approach for payment processing?",
            "How do we implement a shopping cart system?",
            "What caching strategy should we use?",
            "How should we handle product images?",
            "What's the best way to implement search?",
            "How do we manage inventory?",
            "What's the approach for order fulfillment?",
            "How do we handle customer notifications?"
        ];
        
        foreach (string topic in topics)
        {
            conversation.AddUserMessage(topic);
            conversation.AddAssistantMessage($"For {topic.ToLower()}, I recommend using industry best practices with a focus on scalability and reliability. [Detailed technical response would go here]");
        }
        
        Console.WriteLine($"Initial message count: {conversation.Messages.Count}");
        Console.WriteLine();
        
        // Show token usage before compression
        int totalCharsBefore = conversation.Messages.Sum(m => 
            (m.Content?.Length ?? 0) + 
            (m.Parts?.Sum(p => p.Text?.Length ?? 0) ?? 0)
        );
        Console.WriteLine($"Approximate character count before: {totalCharsBefore:N0}");
        Console.WriteLine();
        
        // Compress with custom settings
        Console.WriteLine("Compressing with custom settings:");
        Console.WriteLine("  - Chunk size: 8000 characters");
        Console.WriteLine("  - Preserve recent: 6 messages");
        Console.WriteLine("  - Use GPT-3.5 for summarization (cheaper)");
        Console.WriteLine("  - Custom summary prompt");
        Console.WriteLine();
        
        int compressed = await conversation.CompressMessages(
            chunkSize: 8000,
            preserveRecentCount: 6,
            preserveSystemMessages: true,
            summaryModel: ChatModel.OpenAi.Gpt35.Turbo,
            summaryPrompt: "Create a concise technical summary of this software architecture discussion, focusing on key decisions and recommendations:",
            maxSummaryTokens: 1000
        );
        
        Console.WriteLine($"Messages after compression: {conversation.Messages.Count}");
        Console.WriteLine($"Net change: {compressed}");
        
        int totalCharsAfter = conversation.Messages.Sum(m => 
            (m.Content?.Length ?? 0) + 
            (m.Parts?.Sum(p => p.Text?.Length ?? 0) ?? 0)
        );
        Console.WriteLine($"Approximate character count after: {totalCharsAfter:N0}");
        Console.WriteLine($"Character reduction: {totalCharsBefore - totalCharsAfter:N0} ({((totalCharsBefore - totalCharsAfter) * 100.0 / totalCharsBefore):F1}%)");
        Console.WriteLine();
        
        // Show summary message
        var summaryMsg = conversation.Messages.FirstOrDefault(m => 
            m.Role == ChatMessageRoles.Assistant && 
            m.Content?.Contains("[Previous conversation summary]") == true
        );
        
        if (summaryMsg != null)
        {
            Console.WriteLine("Generated Summary:");
            Console.WriteLine(summaryMsg.Content);
            Console.WriteLine();
        }
        
        // Continue with new question
        Console.WriteLine("Asking a follow-up question with compressed context...");
        conversation.AddUserMessage("Based on our previous discussions, what would be the estimated monthly infrastructure cost?");
        ChatRichResponse response = await conversation.GetResponseRich();
        Console.WriteLine($"AI Response: {response.Text}");
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionAutomatic()
    {
        Console.WriteLine("=== Automatic Periodic Compression Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        conversation.AddSystemMessage("You are a helpful AI assistant.");
        
        // Simulate an interactive conversation with automatic compression
        const int CompressionInterval = 8; // Compress every 8 turns
        int turnCount = 0;
        
        string[] userQueries = [
            "What is artificial intelligence?",
            "How does machine learning work?",
            "Explain neural networks.",
            "What is deep learning?",
            "Tell me about natural language processing.",
            "What are transformers in AI?",
            "Explain GPT models.",
            "What is computer vision?",
            "How does reinforcement learning work?",
            "What are the ethical considerations in AI?",
            "What is the future of AI?",
            "How can AI help in healthcare?",
            "What are the limitations of current AI?",
            "Explain AGI vs narrow AI.",
            "What role does data play in AI?"
        ];
        
        foreach (string query in userQueries)
        {
            turnCount++;
            
            Console.WriteLine($"\n--- Turn {turnCount} ---");
            Console.WriteLine($"User: {query}");
            
            conversation.AddUserMessage(query);
            ChatRichResponse response = await conversation.GetResponseRich();
            
            int textLength = response.Text?.Length ?? 0;
            string preview = textLength > 0 
                ? response.Text!.Substring(0, Math.Min(100, textLength)) 
                : "[No response]";
            Console.WriteLine($"AI: {preview}...");
            Console.WriteLine($"Current messages: {conversation.Messages.Count}");
            
            // Automatic compression every N turns
            if (turnCount % CompressionInterval == 0 && turnCount > 0)
            {
                Console.WriteLine("\n🗜️  Compressing conversation automatically...");
                int messagesBefore = conversation.Messages.Count;
                
                int compressed = await conversation.CompressMessages(
                    chunkSize: 6000,
                    preserveRecentCount: 6,
                    summaryModel: ChatModel.OpenAi.Gpt35.Turbo
                );
                
                Console.WriteLine($"✅ Compressed: {messagesBefore} → {conversation.Messages.Count} messages (net change: {compressed})");
            }
        }
        
        Console.WriteLine("\n=== Final Conversation State ===");
        Console.WriteLine($"Total turns: {turnCount}");
        Console.WriteLine($"Final message count: {conversation.Messages.Count}");
        Console.WriteLine($"Without compression would have: {1 + (turnCount * 2)} messages (system + {turnCount} exchanges)");
        
        // Show final structure
        Console.WriteLine("\nFinal conversation structure:");
        foreach (var msg in conversation.Messages)
        {
            string preview = msg.Content?.Length > 60 
                ? msg.Content.Substring(0, 57) + "..." 
                : msg.Content ?? "[No content]";
            Console.WriteLine($"  [{msg.Role}] {preview}");
        }
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionPerformance()
    {
        Console.WriteLine("=== Compression Performance Demo ===");
        Console.WriteLine("Demonstrating parallel chunk processing for fast compression");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        conversation.AddSystemMessage("You are an AI assistant.");
        
        // Create a very long conversation
        Console.WriteLine("Creating a long conversation (50 exchanges)...");
        for (int i = 0; i < 50; i++)
        {
            conversation.AddUserMessage($"Question {i + 1}: Tell me about topic {i + 1}.");
            conversation.AddAssistantMessage($"Response {i + 1}: Here's detailed information about topic {i + 1}. This response contains multiple sentences to simulate a realistic conversation. The information covers various aspects and provides comprehensive details. [Simulated detailed response for performance testing]");
        }
        
        Console.WriteLine($"Created {conversation.Messages.Count} messages");
        Console.WriteLine();
        
        // Measure compression time
        Console.WriteLine("Starting compression with parallel processing...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        int compressed = await conversation.CompressMessages(
            chunkSize: 10000,
            preserveRecentCount: 10,
            summaryModel: ChatModel.OpenAi.Gpt35.Turbo
        );
        
        stopwatch.Stop();
        
        Console.WriteLine($"✅ Compression completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Messages compressed: {compressed}");
        Console.WriteLine($"Final message count: {conversation.Messages.Count}");
        Console.WriteLine();
        
        Console.WriteLine("Note: Multiple chunks were processed in parallel using Task.WhenAll");
        Console.WriteLine("This significantly reduces total compression time for long conversations.");
    }
}
