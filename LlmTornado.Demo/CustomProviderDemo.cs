using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Demo;

public class CustomProviderDemo
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
}