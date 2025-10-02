using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Embedding.Models;
using LlmTornado.Microsoft.Extensions.AI;
using Microsoft.Extensions.AI;

// This is a sample demonstrating the usage of LlmTornado.Microsoft.Extensions.AI

namespace LlmTornado.Microsoft.Extensions.AI.Sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Initialize the LlmTornado API (you need to provide your API key)
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("Please set OPENAI_API_KEY environment variable");
        
        var api = new TornadoApi(apiKey);

        Console.WriteLine("=== LlmTornado.Microsoft.Extensions.AI Examples ===\n");

        // Example 1: Simple Chat
        await SimpleChatExample(api);
        
        // Example 2: Streaming Chat
        await StreamingChatExample(api);
        
        // Example 3: Multi-modal Chat (with images)
        await MultiModalChatExample(api);
        
        // Example 4: Function/Tool Calling
        await FunctionCallingExample(api);
        
        // Example 5: Embeddings
        await EmbeddingExample(api);
    }

    static async Task SimpleChatExample(TornadoApi api)
    {
        Console.WriteLine("--- Example 1: Simple Chat ---");
        
        // Create a chat client
        IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41Mini);
        
        // Prepare messages
        var messages = new List<global::Microsoft.Extensions.AI.ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "What is the capital of France?")
        };
        
        // Get response
        var response = await chatClient.CompleteAsync(messages);
        
        Console.WriteLine($"Assistant: {response.Message.Text}");
        Console.WriteLine($"Tokens used: {response.Usage?.TotalTokenCount}\n");
    }

    static async Task StreamingChatExample(TornadoApi api)
    {
        Console.WriteLine("--- Example 2: Streaming Chat ---");
        
        var chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41Mini);
        
        var messages = new List<global::Microsoft.Extensions.AI.ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Write a short poem about AI.")
        };
        
        Console.Write("Assistant: ");
        await foreach (var update in chatClient.CompleteStreamingAsync(messages))
        {
            Console.Write(update.Text);
        }
        Console.WriteLine("\n");
    }

    static async Task MultiModalChatExample(TornadoApi api)
    {
        Console.WriteLine("--- Example 3: Multi-modal Chat ---");
        
        var chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41);
        
        var messages = new List<global::Microsoft.Extensions.AI.ChatMessage>
        {
            new(ChatRole.User, new AIContent[]
            {
                new TextContent("What can you tell me about this image?"),
                new ImageContent("https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Cat03.jpg/1200px-Cat03.jpg")
            })
        };
        
        var response = await chatClient.CompleteAsync(messages);
        Console.WriteLine($"Assistant: {response.Message.Text}\n");
    }

    static async Task FunctionCallingExample(TornadoApi api)
    {
        Console.WriteLine("--- Example 4: Function Calling ---");
        
        var chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41Mini);
        
        // Define a tool
        var options = new ChatOptions
        {
            Tools = new List<AITool>
            {
                AIFunctionFactory.Create(
                    GetWeatherTool,
                    name: "get_weather",
                    description: "Gets the current weather for a location")
            }
        };
        
        var messages = new List<global::Microsoft.Extensions.AI.ChatMessage>
        {
            new(ChatRole.User, "What's the weather like in Paris?")
        };
        
        var response = await chatClient.CompleteAsync(messages, options);
        
        // Check if tool was called
        var functionCalls = response.Message.Contents.OfType<FunctionCallContent>().ToList();

        if (functionCalls.Any())
        {
            Console.WriteLine($"Tool called: {functionCalls[0].Name}");
            Console.WriteLine($"Arguments: {System.Text.Json.JsonSerializer.Serialize(functionCalls[0].Arguments)}");
            messages.Add(response.Message);

            messages.Add(new()
            {
                Role = ChatRole.Tool,
                Contents = new AIContent[]
                    {
                        new FunctionResultContent(functionCalls[0].CallId,functionCalls[0].Name,"the weather  in Paris is 31 C")
                    }
            });

            response = await chatClient.CompleteAsync(messages, options);
            // In a real application, you would execute the function and send the result back
            // For this demo, we just show that the function was called
        }
        
        Console.WriteLine($"Assistant: {response.Message.Text}\n");
    }

    /// <summary>
    /// Retrieves a simulated weather report for the specified location.
    /// </summary>
    /// <param name="location">The name of the location for which to retrieve the weather report. This cannot be null or empty.</param>
    /// <param name="unit">The unit of temperature to include in the report. The default is <see langword="celsius"/>. Valid values are
    /// "celsius" or "fahrenheit".</param>
    /// <returns>A string containing the weather report for the specified location, including the temperature and weather
    /// conditions.</returns>
    public static string GetWeatherTool(string location, string unit = "celsius")
    {
        // Simulate getting weather data
        return $"The weather in {location} is 22 degrees {unit} and sunny.";
    }

    static async Task EmbeddingExample(TornadoApi api)
    {
        Console.WriteLine("--- Example 5: Embeddings ---");
        
        var embeddingGenerator = api.AsEmbeddingGenerator(
            EmbeddingModel.OpenAi.Gen3.Small,
            defaultDimensions: 1536);
        
        var texts = new[] 
        { 
            "The quick brown fox jumps over the lazy dog.",
            "A journey of a thousand miles begins with a single step.",
            "To be or not to be, that is the question."
        };
        
        var embeddings = await embeddingGenerator.GenerateAsync(texts);
        
        Console.WriteLine($"Generated {embeddings.Count} embeddings");
        for (int i = 0; i < embeddings.Count; i++)
        {
            var vector = embeddings[i].Vector;
            var firstFive = new float[Math.Min(5, vector.Length)];
            vector.Span.Slice(0, firstFive.Length).CopyTo(firstFive);
            
            Console.WriteLine($"  Text {i + 1}: {vector.Length} dimensions, " +
                            $"first 5 values: [{string.Join(", ", firstFive.Select(v => v.ToString("F4")))}...]");
        }
        
        Console.WriteLine($"Tokens used: {embeddings.Usage?.TotalTokenCount}\n");
    }
}
