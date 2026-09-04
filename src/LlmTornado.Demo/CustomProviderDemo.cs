using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;


namespace LlmTornado.Demo;

public class CustomProviderDemo : DemoBase
{
    [Flaky("requires ollama")]
    [TornadoTest]
    public static async Task Ollama()
    {
        TornadoApi api = new TornadoApi(new Uri("http://localhost:11434"));
        
        string? response = await api.Chat.CreateConversation(new ChatModel("falcon3:1b"))
            .AppendUserInput("Why is the sky blue?")
            .GetResponse();
        
        Console.WriteLine(response);
    }
    
    [Flaky("requires ollama")]
    [TornadoTest]
    public static async Task OllamaStreaming()
    {
        TornadoApi api = new TornadoApi(new Uri("http://localhost:11434"));
        
        await api.Chat.CreateConversation(new ChatModel("falcon3:1b"))
            .AppendUserInput("Why is the sky blue?")
            .StreamResponse(Console.Write);
    }
    
    /// <summary>
    /// llmman (https://github.com/llmmanorg/llmman) serves the Ollama API on port 17434, so only the URI differs.
    /// </summary>
    [Flaky("requires llmman")]
    [TornadoTest]
    public static async Task LlmmanStreaming()
    {
        TornadoApi api = new TornadoApi(new Uri("http://localhost:17434"));
        
        await api.Chat.CreateConversation(new ChatModel("gemma4"))
            .AppendUserInput("Why is the sky blue?")
            .StreamResponse(Console.Write);
    }
}