# Microsoft.Extensions.AI Integration

## Overview

LlmTornado.Microsoft.Extensions.AI provides integration with Microsoft's AI abstractions, enabling seamless use of LlmTornado in applications using Microsoft.Extensions.AI patterns.

## Quick Start

```csharp
// Install package:
// dotnet add package LlmTornado.Microsoft.Extensions.AI

using Microsoft.Extensions.AI;
using LlmTornado;

// Create LlmTornado API
TornadoApi tornadoApi = new TornadoApi("your-api-key");

// Use with Microsoft.Extensions.AI patterns
IChatClient chatClient = tornadoApi.AsChatClient(ChatModel.OpenAi.Gpt4.O);

// Generate chat completion
ChatCompletion completion = await chatClient.CompleteAsync(
    "Tell me about C#",
    cancellationToken: CancellationToken.None
);

Console.WriteLine(completion.Message.Text);
```

## Integration Patterns

### Chat Client

```csharp
using Microsoft.Extensions.AI;
using LlmTornado;

TornadoApi api = new TornadoApi("your-api-key");

// Convert to IChatClient
IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt41.V41);

// Use standard Microsoft.Extensions.AI patterns
ChatCompletion result = await chatClient.CompleteAsync(
    new ChatMessage(ChatRole.User, "What is async/await?")
);
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using LlmTornado;

// Configure services
IServiceCollection services = new ServiceCollection();

services.AddSingleton<TornadoApi>(sp => 
    new TornadoApi(Configuration["OpenAI:ApiKey"])
);

services.AddSingleton<IChatClient>(sp =>
{
    TornadoApi api = sp.GetRequiredService<TornadoApi>();
    return api.AsChatClient(ChatModel.OpenAi.Gpt4.O);
});

// Use in application
IServiceProvider provider = services.BuildServiceProvider();
IChatClient chatClient = provider.GetRequiredService<IChatClient>();

ChatCompletion result = await chatClient.CompleteAsync("Hello!");
```

### Streaming Support

```csharp
IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt4.O);

await foreach (StreamingChatCompletionUpdate update in 
    chatClient.CompleteStreamingAsync("Tell me a story"))
{
    Console.Write(update.Text);
}
```

## Benefits

### Standardization
- Use consistent patterns across different AI providers
- Switch providers with minimal code changes
- Leverage Microsoft.Extensions.AI ecosystem

### Dependency Injection
- Integrate cleanly with ASP.NET Core
- Use standard DI patterns
- Testability through interfaces

### Middleware Support
- Add logging, caching, retry logic
- Use built-in middleware patterns
- Compose behaviors cleanly

## Use Cases

### ASP.NET Core Integration

```csharp
// Startup.cs / Program.cs
builder.Services.AddSingleton<TornadoApi>(sp =>
    new TornadoApi(builder.Configuration["OpenAI:ApiKey"])
);

builder.Services.AddSingleton<IChatClient>(sp =>
{
    TornadoApi api = sp.GetRequiredService<TornadoApi>();
    return api.AsChatClient(ChatModel.OpenAi.Gpt4.O);
});

// Controller
public class ChatController : ControllerBase
{
    readonly IChatClient chatClient;
    
    public ChatController(IChatClient chatClient)
    {
        this.chatClient = chatClient;
    }
    
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] string message)
    {
        ChatCompletion result = await chatClient.CompleteAsync(message);
        return Ok(result.Message.Text);
    }
}
```

### Testing

```csharp
public class ChatServiceTests
{
    [Fact]
    public async Task TestChatCompletion()
    {
        // Mock IChatClient for testing
        Mock<IChatClient> mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new ChatCompletion(new ChatMessage(ChatRole.Assistant, "Test response")));
        
        ChatService service = new ChatService(mockClient.Object);
        string result = await service.ProcessQuery("Test");
        
        Assert.Equal("Test response", result);
    }
}
```

## Best Practices

- Use dependency injection for better testability
- Implement proper cancellation token support
- Handle exceptions appropriately
- Log AI interactions for monitoring
- Use middleware for cross-cutting concerns

## Related Topics

- [Chat Basics](../1.%20LlmTornado/1.%20Chat/1.%20basics.md)
- [Agent Basics](../2.%20Agents/1.%20Getting-Started.md)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/)
