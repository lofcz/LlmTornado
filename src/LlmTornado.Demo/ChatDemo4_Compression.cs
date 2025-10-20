using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Demo;

public partial class ChatDemo : DemoBase
{
    [TornadoTest]
    public static async Task ConversationCompressionHookable()
    {
        Console.WriteLine("=== Hookable Compression Strategy Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        
        // Create conversation with message count strategy
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        // Configure automatic compression strategy
        conversation.CompressionStrategy = new MessageCountCompressionStrategy(
            messageThreshold: 15,
            options: new ConversationCompressionOptions
            {
                PreserveRecentCount = 6,
                ChunkSize = 8000,
                SummaryModel = ChatModel.OpenAi.Gpt35.Turbo
            }
        );

        conversation.AddSystemMessage("You are a helpful assistant.");
        
        Console.WriteLine("Adding messages with automatic compression...");
        
        for (int i = 0; i < 20; i++)
        {
            bool compressed = await conversation.AddUserMessageSmart($"Tell me fact #{i + 1}");
            
            if (compressed)
            {
                Console.WriteLine($"???  Auto-compressed at message {i + 1} (total: {conversation.Messages.Count})");
            }
            else
            {
                Console.WriteLine($"   Added message {i + 1} (total: {conversation.Messages.Count})");
            }
            
            // Simulate assistant response
            conversation.AddAssistantMessage($"Fact #{i + 1}: [Simulated response]");
        }
        
        Console.WriteLine();
        Console.WriteLine($"Final message count: {conversation.Messages.Count}");
        Console.WriteLine("Without compression would have: " + (1 + 40) + " messages");
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionCustomSummarizer()
    {
        Console.WriteLine("=== Custom Summarizer Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        // Use custom summarizer that creates structured summaries
        conversation.Summarizer = new CustomStructuredSummarizer(api.Chat, conversation.RequestParameters);
        
        conversation.AddSystemMessage("You are a technical consultant.");
        
        Console.WriteLine("Adding technical discussion messages...");
        
        string[] topics = [
            "Explain microservices architecture",
            "What is event-driven design?",
            "How do message queues work?",
            "What is CQRS pattern?",
            "Explain domain-driven design",
            "What are saga patterns?",
            "How does eventual consistency work?",
            "What is circuit breaker pattern?",
            "Explain API gateway pattern",
            "What is service mesh?"
        ];

        foreach (string topic in topics)
        {
            conversation.AddUserMessage(topic);
            conversation.AddAssistantMessage($"[Detailed response about: {topic}]");
        }
        
        Console.WriteLine($"Messages before compression: {conversation.Messages.Count}");
        
        // Manually trigger compression with custom summarizer
        int compressed = await conversation.CompressMessages(new ConversationCompressionOptions
        {
            ChunkSize = 6000,
            PreserveRecentCount = 4,
            PreserveSystemMessages = true,
            SummaryModel = ChatModel.OpenAi.Gpt35.Turbo,
            SummaryPrompt = "Create a structured technical summary with key topics and decisions:"
        });
        
        Console.WriteLine($"Messages after compression: {conversation.Messages.Count}");
        Console.WriteLine();
        
        // Show summary structure
        var summaryMsg = conversation.Messages.FirstOrDefault(m => 
            m.Role == ChatMessageRoles.Assistant && 
            m.Content?.Contains("[Previous conversation summary]") == true
        );
        
        if (summaryMsg != null)
        {
            Console.WriteLine("Custom Structured Summary:");
            Console.WriteLine(summaryMsg.Content);
        }
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionAdaptive()
    {
        Console.WriteLine("=== Adaptive Compression Strategy Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        // Configure adaptive strategy that considers multiple factors
        conversation.CompressionStrategy = new AdaptiveCompressionStrategy(
            messageThreshold: 15,
            characterThreshold: 30000,
            options: new ConversationCompressionOptions
            {
                PreserveRecentCount = 5,
                SummaryModel = ChatModel.OpenAi.Gpt35.Turbo
            }
        );

        conversation.AddSystemMessage("You are a helpful assistant.");
        
        Console.WriteLine("Testing adaptive compression with varying message lengths...");
        Console.WriteLine();
        
        // Add short messages
        for (int i = 0; i < 10; i++)
        {
            bool compressed = await conversation.AddUserMessageSmart($"Short question {i + 1}");
            conversation.AddAssistantMessage($"Short answer {i + 1}");
            
            if (compressed)
            {
                Console.WriteLine($"???  Compressed (messages: {conversation.Messages.Count})");
            }
        }
        
        Console.WriteLine($"After 10 short exchanges: {conversation.Messages.Count} messages");
        Console.WriteLine();
        
        // Add long messages to trigger character threshold
        for (int i = 0; i < 5; i++)
        {
            string longMessage = $"Very long detailed question {i + 1}. " + new string('x', 3000);
            bool compressed = await conversation.AddUserMessageSmart(longMessage);
            conversation.AddAssistantMessage($"Detailed response {i + 1}. " + new string('y', 3000));
            
            if (compressed)
            {
                Console.WriteLine($"???  Compressed due to character threshold (messages: {conversation.Messages.Count})");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"Final message count: {conversation.Messages.Count}");
        int totalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));
        Console.WriteLine($"Total characters: {totalChars:N0}");
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionPeriodic()
    {
        Console.WriteLine("=== Periodic Compression Strategy Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        // Compress every 8 messages
        conversation.CompressionStrategy = new PeriodicCompressionStrategy(
            interval: 8,
            options: new ConversationCompressionOptions
            {
                PreserveRecentCount = 4,
                SummaryModel = ChatModel.OpenAi.Gpt35.Turbo
            }
        );

        conversation.AddSystemMessage("You are a helpful assistant.");
        
        Console.WriteLine("Adding messages with periodic compression (every 8 messages)...");
        Console.WriteLine();
        
        for (int i = 0; i < 25; i++)
        {
            bool compressed = await conversation.AddUserMessageSmart($"Message {i + 1}");
            conversation.AddAssistantMessage($"Response {i + 1}");
            
            if (compressed)
            {
                Console.WriteLine($"   Turn {i + 1}: ???  Periodic compression triggered (messages: {conversation.Messages.Count})");
            }
            else
            {
                Console.WriteLine($"   Turn {i + 1}: Added (messages: {conversation.Messages.Count})");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"Final message count: {conversation.Messages.Count}");
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionSmartInsertWithResponse()
    {
        Console.WriteLine("=== Smart Insert with GetResponseRichSmart Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        // Configure compression
        conversation.CompressionStrategy = new MessageCountCompressionStrategy(
            messageThreshold: 12,
            options: new ConversationCompressionOptions
            {
                PreserveRecentCount = 6,
                SummaryModel = ChatModel.OpenAi.Gpt35.Turbo
            }
        );

        conversation.AddSystemMessage("You are a helpful AI assistant.");
        
        Console.WriteLine("Interactive conversation with smart compression...");
        Console.WriteLine();
        
        string[] queries = [
            "What is artificial intelligence?",
            "How does machine learning work?",
            "Explain neural networks",
            "What is deep learning?",
            "Tell me about transformers",
            "What is GPT?",
            "Explain attention mechanism",
            "What is reinforcement learning?",
            "How do LLMs work?",
            "What is prompt engineering?"
        ];

        foreach (string query in queries)
        {
            Console.WriteLine($"User: {query}");
            
            await conversation.AddUserMessageSmart(query);
            ChatRichResponse response = await conversation.GetResponseRichSmart();
            
            string preview = response.Text?.Length > 100 
                ? response.Text.Substring(0, 97) + "..." 
                : response.Text ?? "[No response]";
            
            Console.WriteLine($"AI: {preview}");
            Console.WriteLine($"   (Total messages: {conversation.Messages.Count})");
            Console.WriteLine();
        }
        
        Console.WriteLine($"Conversation completed with {conversation.Messages.Count} messages");
    }
    
    [TornadoTest]
    public static async Task ConversationCompressionCharacterThreshold()
    {
        Console.WriteLine("=== Character-Based Compression Strategy Demo ===");
        Console.WriteLine();
        
        var api = Program.Connect();
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });

        // Compress when character count exceeds threshold
        conversation.CompressionStrategy = new CharacterCountCompressionStrategy(
            characterThreshold: 25000,
            options: new ConversationCompressionOptions
            {
                PreserveRecentCount = 5,
                ChunkSize: 10000,
                SummaryModel = ChatModel.OpenAi.Gpt35.Turbo
            }
        );

        conversation.AddSystemMessage("You are a helpful assistant.");
        
        Console.WriteLine("Adding messages until character threshold is reached...");
        Console.WriteLine();
        
        int turnCount = 0;
        
        while (turnCount < 20)
        {
            turnCount++;
            
            // Vary message length
            int msgLength = turnCount % 2 == 0 ? 2000 : 500;
            string message = $"Question {turnCount}: " + new string('x', msgLength);
            string response = $"Answer {turnCount}: " + new string('y', msgLength);
            
            bool compressed = await conversation.AddUserMessageSmart(message);
            conversation.AddAssistantMessage(response);
            
            int totalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));
            
            if (compressed)
            {
                Console.WriteLine($"Turn {turnCount}: ???  Compressed at {totalChars:N0} chars (messages: {conversation.Messages.Count})");
            }
            else
            {
                Console.WriteLine($"Turn {turnCount}: {totalChars:N0} chars (messages: {conversation.Messages.Count})");
            }
        }
        
        Console.WriteLine();
        int finalChars = conversation.Messages.Sum(m => Conversation.GetMessageLength(m));
        Console.WriteLine($"Final state: {conversation.Messages.Count} messages, {finalChars:N0} characters");
    }
}

/// <summary>
///     Custom summarizer that creates structured summaries
/// </summary>
internal class CustomStructuredSummarizer : IConversationSummarizer
{
    private readonly ChatEndpoint endpoint;
    private readonly ChatRequest requestParameters;

    public CustomStructuredSummarizer(ChatEndpoint endpoint, ChatRequest requestParameters)
    {
        this.endpoint = endpoint;
        this.requestParameters = requestParameters;
    }

    public async Task<List<ChatMessage>> SummarizeMessages(List<ChatMessage> messages, ConversationCompressionOptions options, CancellationToken token = default)
    {
        // Group messages by topic (simplified - in production you'd use more sophisticated grouping)
        StringBuilder summaryBuilder = new StringBuilder();
        summaryBuilder.AppendLine("## Technical Discussion Summary");
        summaryBuilder.AppendLine();
        summaryBuilder.AppendLine("### Topics Covered:");
        
        int topicNum = 1;
        foreach (ChatMessage msg in messages.Where(m => m.Role == ChatMessageRoles.User))
        {
            string content = Conversation.GetMessageContent(msg);
            if (content.Length > 50)
            {
                content = content.Substring(0, 47) + "...";
            }
            summaryBuilder.AppendLine($"{topicNum}. {content}");
            topicNum++;
        }
        
        summaryBuilder.AppendLine();
        summaryBuilder.AppendLine($"### Discussion Length: {messages.Count} exchanges");

        // Create single structured summary message
        return new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRoles.Assistant, $"[Previous conversation summary]: {summaryBuilder}")
        };
    }
}
