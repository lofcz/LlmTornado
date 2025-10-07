# Streaming Responses

## Overview

Streaming allows you to receive AI responses incrementally as they're generated, rather than waiting for the complete response. This is essential for real-time applications like chatbots, live translation, and any interface where users need immediate feedback. LlmTornado provides robust streaming capabilities across all supported AI providers.

## Quick Start

Here's a basic example of how to use streaming with LlmTornado:

```csharp
using LlmTornado;
using LlmTornado.Chat;

var api = new TornadoApi("your-api-key");

var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo
});

conversation.AddUserMessage("Tell me a story about a brave knight.");

// Stream the response token by token
await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        Console.Write(token); // Print each token as it arrives
        await Task.CompletedTask;
    }
});
```

## Prerequisites

- Understanding of async/await patterns in C#
- Familiarity with basic chat functionality
- API access to a model that supports streaming (all modern models do)
- Knowledge of event-driven programming concepts

## Detailed Explanation

### How Streaming Works

Streaming follows this flow:

1. **Request Streaming**: Enable streaming in the ChatRequest
2. **Connection**: Establish a persistent connection to the AI service
3. **Token Stream**: Receive tokens (pieces of words) as they're generated
4. **Event Handling**: Process tokens through event handlers
5. **Completion**: Handle the end of the stream

### Key Components

#### ChatStreamEventHandler
Handles streaming events:

```csharp
public class ChatStreamEventHandler
{
    public Func<string, ValueTask> MessageTokenHandler { get; set; }
    public Func<FunctionCall[], ValueTask> ToolCallsHandler { get; set; }
    public Func<ChatStreamResponse, ValueTask> ResponseHandler { get; set; }
    public Func<Exception, ValueTask> ErrorHandler { get; set; }
    public Func<ValueTask> StreamEndHandler { get; set; }
}
```

#### ChatStreamResponse
Contains streaming response data:

```csharp
public class ChatStreamResponse
{
    public string? Content { get; set; }
    public FunctionCall[]? ToolCalls { get; set; }
    public string? Role { get; set; }
    public string? FinishReason { get; set; }
}
```

## Basic Usage

### Simple Text Streaming

```csharp
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo
});

conversation.AddUserMessage("Explain quantum computing in simple terms.");

await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        Console.Write(token);
        await Task.Delay(10); // Simulate processing time
    },
    
    StreamEndHandler = async () =>
    {
        Console.WriteLine("\nStreaming completed!");
    }
});
```

### Streaming with Progress

```csharp
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo
});

conversation.AddUserMessage("Write a comprehensive guide to C# programming.");

int tokenCount = 0;
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        tokenCount++;
        Console.Write(token);
        
        // Show progress every 50 tokens
        if (tokenCount % 50 == 0)
        {
            Console.WriteLine($"\n[Progress: {tokenCount} tokens, {stopwatch.ElapsedMilliseconds}ms]");
        }
    },
    
    StreamEndHandler = async () =>
    {
        stopwatch.Stop();
        Console.WriteLine($"\nFinal stats: {tokenCount} tokens in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average speed: {tokenCount / (stopwatch.ElapsedMilliseconds / 1000.0):F1} tokens/second");
    }
});
```

### Handling Different Response Types

```csharp
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((string city, ToolArguments args) =>
        {
            return $"Weather in {city}: Sunny, 22°C";
        }, "get_weather", "Gets weather for a city")
    ]
});

conversation.AddUserMessage("What's the weather like in Paris and London?");

await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        Console.Write(token);
    },
    
    ToolCallsHandler = async (toolCalls) =>
    {
        Console.WriteLine("\n[AI wants to call functions]");
        foreach (var call in toolCalls)
        {
            Console.WriteLine($"- {call.Name} with args: {call.Arguments}");
        }
    },
    
    ResponseHandler = async (response) =>
    {
        Console.WriteLine($"\n[Full response received: {response.Content?.Substring(0, Math.Min(100, response.Content?.Length ?? 0))}...]");
    },
    
    StreamEndHandler = async () =>
    {
        Console.WriteLine("\nStream ended");
    }
});
```

## Advanced Usage

### Streaming with Error Handling

```csharp
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo
});

try
{
    await conversation.StreamResponseRich(new ChatStreamEventHandler
    {
        MessageTokenHandler = async (token) =>
        {
            Console.Write(token);
        },
        
        ErrorHandler = async (error) =>
        {
            Console.WriteLine($"\n[Error occurred: {error.Message}]");
            // Implement retry logic or fallback here
        },
        
        StreamEndHandler = async () =>
        {
            Console.WriteLine("\nStream completed successfully");
        }
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Stream failed: {ex.Message}");
}
```

### Building a Complete Response

```csharp
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo
});

conversation.AddUserMessage("Write a detailed explanation of machine learning.");

StringBuilder completeResponse = new();
ListFunctionCall> toolCalls = new();

await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        completeResponse.Append(token);
        Console.Write(token);
    },
    
    ToolCallsHandler = async (calls) =>
    {
        toolCalls.AddRange(calls);
        Console.WriteLine($"\n[{calls.Length} tool calls detected]");
    },
    
    ResponseHandler = async (response) =>
    {
        Console.WriteLine($"\n[Response chunk: {response.Content?.Length ?? 0} chars]");
    },
    
    StreamEndHandler = async () =>
    {
        Console.WriteLine($"\nComplete response ({completeResponse.Length} characters):");
        Console.WriteLine(completeResponse.ToString());
        
        if (toolCalls.Any())
        {
            Console.WriteLine($"Tool calls made: {toolCalls.Count}");
        }
    }
});
```

### Caching Streamed Responses

```csharp
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo
});

conversation.AddUserMessage("Explain the benefits of exercise.");

string streamedContent = "";
bool isComplete = false;

await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        streamedContent += token;
        Console.Write(token);
    },
    
    StreamEndHandler = async () =>
    {
        isComplete = true;
        
        // Cache the complete response
        CacheResponse("exercise_benefits", streamedContent);
        
        Console.WriteLine($"\nResponse cached for future use");
    }
});

// Helper method
void CacheResponse(string key, string content)
{
    // Implement your caching strategy
    Console.WriteLine($"Caching '{key}' with {content.Length} characters");
}
```

### Multi-Turn Streaming Conversations

```csharp
async Task StreamMultiTurnConversation()
{
    var conversation = api.Chat.CreateConversation(new ChatRequest
    {
        Model = ChatModel.OpenAi.Gpt35Turbo
    });

    // First turn
    conversation.AddUserMessage("What is artificial intelligence?");
    await StreamTurn(conversation, "AI Explanation");

    // Second turn
    conversation.AddUserMessage("Can you give me some real-world examples?");
    await StreamTurn(conversation, "Real-world Examples");

    // Third turn
    conversation.AddUserMessage("What are the ethical considerations?");
    await StreamTurn(conversation, "Ethical Considerations");
}

async Task StreamTurn(Conversation conversation, string topic)
{
    Console.WriteLine($"\n--- {topic} ---");
    
    await conversation.StreamResponseRich(new ChatStreamEventHandler
    {
        MessageTokenHandler = async (token) =>
        {
            Console.Write(token);
        },
        
        StreamEndHandler = async () =>
        {
            Console.WriteLine($"\n--- End of {topic} ---");
        }
    });
}

// Usage
await StreamMultiTurnConversation();
```

## Best Practices

### 1. Handle Stream Completion
- Always implement the `StreamEndHandler`
- Use it to clean up resources and update UI state
- Consider it a signal that the response is complete

```csharp
await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) => { /* ... */ },
    
    StreamEndHandler = async () =>
    {
        // Update UI to show response is complete
        IsStreaming = false;
        ShowTypingIndicator = false;
        
        // Enable user input
        UserInputEnabled = true;
    }
});
```

### 2. Error Handling
- Implement comprehensive error handling
- Handle network interruptions gracefully
- Provide fallback behavior for streaming failures

```csharp
await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) => { /* ... */ },
    
    ErrorHandler = async (error) =>
    {
        // Log the error
        Logger.LogError(error, "Streaming error");
        
        // Update UI
        ErrorMessage = "Failed to stream response. Please try again.";
        
        // Fallback to non-streaming
        try
        {
            var fallbackResponse = await conversation.GetResponseRich();
            Console.WriteLine($"Fallback response: {fallbackResponse}");
        }
        catch
        {
            // Handle fallback failure
        }
    }
});
```

### 3. Performance Optimization
- Buffer tokens efficiently to avoid excessive UI updates
- Debounce rapid token updates for better performance
- Consider virtual scrolling for long responses

```csharp
// Debounce token updates for better performance
private DateTime _lastTokenUpdate = DateTime.MinValue;
private const int TokenUpdateInterval = 50; // ms

await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        var now = DateTime.UtcNow;
        if ((now - _lastTokenUpdate).TotalMilliseconds < TokenUpdateInterval)
        {
            // Buffer the token
            _tokenBuffer.Append(token);
            return;
        }
        
        // Flush buffered tokens
        if (_tokenBuffer.Length > 0)
        {
            Console.Write(_tokenBuffer.ToString());
            _tokenBuffer.Clear();
        }
        
        Console.Write(token);
        _lastTokenUpdate = now;
    }
});
```

### 4. User Experience
- Provide visual feedback during streaming
- Show typing indicators while waiting
- Allow users to interrupt streaming if needed

```csharp
// UI-friendly streaming with indicators
bool _isStreaming = false;
string _currentResponse = "";
CancellationTokenSource _cancellationTokenSource = new();

await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        _currentResponse += token;
        // Update UI thread
        await UpdateUiAsync();
    },
    
    StreamEndHandler = async () =>
    {
        _isStreaming = false;
        await UpdateUiAsync();
    }
});

// Allow user to interrupt
void OnStopStreamingClicked()
{
    _cancellationTokenSource.Cancel();
    _isStreaming = false;
    UpdateUi();
}
```

### 5. Memory Management
- Be careful with large streaming responses
- Implement proper cleanup of resources
- Consider memory limits for very long responses

```csharp
// Memory-efficient streaming for large responses
await conversation.StreamResponseRich(new ChatStreamEventHandler
{
    MessageTokenHandler = async (token) =>
    {
        // Process token immediately to avoid memory buildup
        ProcessToken(token);
        
        // Write to disk if response gets too large
        if (_totalTokens > 10000)
        {
            await WriteToDisk(token);
        }
    },
    
    StreamEndHandler = async () =>
    {
        // Final cleanup
        await FinalizeStream();
    }
});
```

## Common Issues

### Connection Interruptions
- **Issue**: Network disconnects during streaming
- **Solution**: Implement reconnection logic with exponential backoff
- **Prevention**: Monitor connection health and implement heartbeats

### Memory Usage
- **Issue**: High memory usage with long streaming responses
- **Solution**: Process tokens immediately and avoid storing entire responses
- **Prevention**: Implement streaming to disk for very long responses

### UI Performance
- **Issue**: UI becomes unresponsive with rapid token updates
- **Solution**: Debounce token updates and use efficient rendering
- **Prevention**: Implement virtual scrolling and progressive rendering

### Error Recovery
- **Issue**: Streaming fails partway through
- **Solution**: Implement fallback to non-streaming responses
- **Prevention**: Add retry logic with proper error handling

## API Reference

### Conversation.StreamResponseRich
```csharp
Task StreamResponseRich(ChatStreamEventHandler eventHandler)
```

### ChatStreamEventHandler
- `Func<string, ValueTask> MessageTokenHandler` - Handles individual tokens
- `Func<FunctionCall[], ValueTask> ToolCallsHandler` - Handles function calls
- `Func<ChatStreamResponse, ValueTask> ResponseHandler` - Handles response chunks
- `Func<Exception, ValueTask> ErrorHandler` - Handles streaming errors
- `Func<ValueTask> StreamEndHandler` - Handles stream completion

### ChatStreamResponse
- `string? Content` - Response content
- `FunctionCall[]? ToolCalls` - Function calls in the response
- `string? Role` - Message role
- `string? FinishReason` - Reason for stream termination

## Related Topics

- [Chat Basics](/chat/basics) - Fundamental chat functionality
- [Function Calling](/chat/functions) - Using functions with streaming
- [Error Handling](/chat/error-handling) - Handling streaming errors
- [Multiturn Conversations](/chat/multiturn) - Managing streaming in conversations
- [Performance Optimization](/chat/performance) - Optimizing streaming performance
