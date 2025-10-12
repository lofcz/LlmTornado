using System;
using System.IO;
using LlmTornado.Chat.Models;
using LlmTornado.Embedding.Models;
using LlmTornado.Images.Models;
using LlmTornado.Microsoft.Extensions.AI;
using Microsoft.Extensions.AI;

namespace LlmTornado.Demo;

public class MicrosoftExtensionsAiDemo
{
    [TornadoTest]
    public static async Task SimpleChatExample()
    {
        // Create a chat client
        TornadoApi api = Program.Connect();
        IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41Mini);
        
        // Prepare messages
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "What is the capital of France?")
        ];
        
        // Get response
        ChatResponse response = await chatClient.GetResponseAsync(messages);
        
        Console.WriteLine($"Assistant: {response.Messages.FirstOrDefault().Text}");
        Console.WriteLine($"Tokens used: {response.Usage?.TotalTokenCount}\n");
    }

    [TornadoTest]
    public static async Task StreamingChatExample()
    {
        TornadoApi api = Program.Connect();
        IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41Mini);
        
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Write a short poem about AI.")
        ];
        
        Console.Write("Assistant: ");
        await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(messages))
        {
            Console.Write(update.Text);
        }
        Console.WriteLine("\n");
    }

    [TornadoTest]
    public static async Task MultiModalChatExample()
    {
        TornadoApi api = Program.Connect();
        IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41);
        
        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, [
                new TextContent("What can you tell me about this image?"),
                new UriContent("https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Cat03.jpg/1200px-Cat03.jpg", "image/jpeg")
            ])
        ];
        
        ChatResponse response = await chatClient.GetResponseAsync(messages);
        Console.WriteLine($"Assistant: {response.Messages.FirstOrDefault().Text}\n");
    }

    [TornadoTest]
    public static async Task FunctionCallingExample()
    {
        TornadoApi api = Program.Connect();
        IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41Mini);
        
        // Define a tool
        ChatOptions options = new ChatOptions
        {
            Tools = new List<AITool>
            {
                AIFunctionFactory.Create(
                    GetWeatherTool,
                    name: "get_weather",
                    description: "Gets the current weather for a location")
            }
        };
        
        List<ChatMessage> messages = [new ChatMessage(ChatRole.User, "What's the weather like in Paris?")];
        
        ChatResponse? response = await chatClient.GetResponseAsync(messages, options);
        
        // Check if tool was called
        List<FunctionCallContent> functionCalls = response.Messages.FirstOrDefault().Contents.OfType<FunctionCallContent>().ToList();

        if (functionCalls.Any())
        {
            Console.WriteLine($"Tool called: {functionCalls[0].Name}");
            Console.WriteLine($"Arguments: {System.Text.Json.JsonSerializer.Serialize(functionCalls[0].Arguments)}");
            messages.Add(response.Messages.FirstOrDefault());

            messages.Add(new ChatMessage
            {
                Role = ChatRole.Tool,
                Contents =
                [
                    new FunctionResultContent(functionCalls[0].CallId, "the weather  in Paris is 31 C")
                ]
            });

            response = await chatClient.GetResponseAsync(messages, options);
            // In a real application, you would execute the function and send the result back
            // For this demo, we just show that the function was called
        }
        
        Console.WriteLine($"Assistant: {response.Messages.FirstOrDefault().Text}\n");
    }
    
    static string GetWeatherTool(string location, string unit = "celsius")
    {
        // Simulate getting weather data
        return $"The weather in {location} is 22 degrees {unit} and sunny.";
    }

    [TornadoTest]
    public static async Task EmbeddingExample()
    {
        TornadoApi api = Program.Connect();
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = api.AsEmbeddingGenerator(
            EmbeddingModel.OpenAi.Gen3.Small,
            defaultDimensions: 1536);
        
        string[] texts =
        [
            "The quick brown fox jumps over the lazy dog.",
            "A journey of a thousand miles begins with a single step.",
            "To be or not to be, that is the question."
        ];
        
        GeneratedEmbeddings<Embedding<float>> embeddings = await embeddingGenerator.GenerateAsync(texts);
        
        Console.WriteLine($"Generated {embeddings.Count} embeddings");
        for (int i = 0; i < embeddings.Count; i++)
        {
            ReadOnlyMemory<float> vector = embeddings[i].Vector;
            float[] firstFive = new float[Math.Min(5, vector.Length)];
            vector.Span.Slice(0, firstFive.Length).CopyTo(firstFive);
            
            Console.WriteLine($"  Text {i + 1}: {vector.Length} dimensions, " +
                            $"first 5 values: [{string.Join(", ", firstFive.Select(v => v.ToString("F4")))}...]");
        }
        
        Console.WriteLine($"Tokens used: {embeddings.Usage?.TotalTokenCount}\n");
    }

    [TornadoTest]
    public static async Task ImageGenerationOpenAiExample()
    {
        TornadoApi api = Program.Connect();
        IImageGenerator imageGenerator = api.AsImageGenerator(ImageModel.OpenAi.Dalle.V3);
        
        ImageGenerationRequest request = new ImageGenerationRequest("A serene mountain landscape at sunset with a crystal clear lake reflecting the mountains");
        ImageGenerationResponse response = await imageGenerator.GenerateAsync(request);
        
        Console.WriteLine($"Generated {response.Contents.Count} image(s)");
        
        if (response.RawRepresentation is Images.ImageGenerationResult tornadoResult)
        {
            await ImagesDemo.DisplayImage(tornadoResult);
        }
        
        Console.WriteLine();
    }

    [TornadoTest]
    public static async Task ImageGenerationGoogleImagenExample()
    {
        TornadoApi api = Program.Connect();
        IImageGenerator imageGenerator = api.AsImageGenerator(ImageModel.Google.Imagen.V4FastGenerate001);
        
        ImageGenerationRequest request = new ImageGenerationRequest("A futuristic cityscape with flying cars and neon lights at night, cyberpunk style");
        ImageGenerationOptions options = new ImageGenerationOptions
        {
            ImageSize = new System.Drawing.Size(1536, 1024),
            ResponseFormat = ImageGenerationResponseFormat.Data
        };
        
        ImageGenerationResponse response = await imageGenerator.GenerateAsync(request, options);
        
        Console.WriteLine($"Generated {response.Contents.Count} image(s)");
        
        if (response.RawRepresentation is Images.ImageGenerationResult tornadoResult)
        {
            await ImagesDemo.DisplayImage(tornadoResult);
        }
        
        Console.WriteLine();
    }
}