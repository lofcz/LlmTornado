# TornadoRunner API Reference

Complete API reference for the `TornadoRunner` class, which provides execution utilities for TornadoAgent instances.

## Class Overview

```csharp
namespace LlmTornado.Agents
{
    public static class TornadoRunner
    {
        // Static methods for agent execution
    }
}
```

The `TornadoRunner` class provides static methods for executing agents with various configuration options.

## Methods

### RunAsync (Basic)

```csharp
public static async Task<Conversation> RunAsync(
    TornadoAgent agent, 
    string input)
```

Executes an agent with basic input.

**Parameters:**
- `agent` (`TornadoAgent`): The agent to execute
- `input` (`string`): The input text to process

**Returns:** `Task<Conversation>` containing the conversation result.

**Example:**
```csharp
var agent = new TornadoAgent(client, model,"Assistant", "You are helpful");
var result = await TornadoRunner.RunAsync(agent, "Hello world");
```

### RunAsync (With Streaming)

```csharp
public static async Task<Conversation> RunAsync(
    TornadoAgent agent, 
    string input, 
    bool streaming = false)
```

Executes an agent with streaming option.

**Parameters:**
- `agent` (`TornadoAgent`): The agent to execute
- `input` (`string`): The input text to process
- `streaming` (`bool`): Whether to enable streaming responses

**Returns:** `Task<Conversation>` containing the conversation result.

**Example:**
```csharp
var result = await TornadoRunner.RunAsync(agent, "Tell a story", streaming: true);
```

### RunAsync (Full Options)

```csharp
public static async Task<Conversation> RunAsync(
    TornadoAgent agent, 
    string input, 
    bool streaming = false,
    Func<AgentRunnerEvents, ValueTask>? onAgentRunnerEvent = null,
    GuardRailFunction? inputGuardRailFunction = null)
```

Executes an agent with full configuration options.

**Parameters:**
- `agent` (`TornadoAgent`): The agent to execute
- `input` (`string`): The input text to process
- `streaming` (`bool`): Whether to enable streaming responses
- `onAgentRunnerEvent` (`Func<AgentRunnerEvents, ValueTask>?`): Event handler for execution events
- `inputGuardRailFunction` (`GuardRailFunction?`): Input validation function

**Returns:** `Task<Conversation>` containing the conversation result.

**Example:**
```csharp
async ValueTask HandleEvents(AgentRunnerEvents eventData)
{
    switch (eventData.EventType)
    {
        case AgentRunnerEventTypes.Streaming:
            // Handle streaming events
            break;
        case AgentRunnerEventTypes.ToolCall:
            // Handle tool call events
            break;
    }
    return ValueTask.CompletedTask;
}

async ValueTask<GuardRailFunctionOutput> GuardRail(string input = "")
{
    // Input validation logic
    return new GuardRailFunctionOutput("Validated", false);
}

var result = await TornadoRunner.RunAsync(
    agent,
    "Process this input",
    streaming: true,
    onAgentRunnerEvent: HandleEvents,
    inputGuardRailFunction: GuardRail
);
```

## Event Handling

The `onAgentRunnerEvent` parameter allows you to handle various events during agent execution.

### AgentRunnerEvents Types

#### AgentRunnerStreamingEvent
Fired during streaming responses.

```csharp
public class AgentRunnerStreamingEvent : AgentRunnerEvents
{
    public ModelStreamingEvents ModelStreamingEvent { get; set; }
}
```

#### AgentRunnerToolCallEvent
Fired when a tool is being called.

```csharp
public class AgentRunnerToolCallEvent : AgentRunnerEvents
{
    public FunctionCall ToolCall { get; set; }
}
```

#### AgentRunnerToolCallResultEvent
Fired when a tool call completes.

```csharp
public class AgentRunnerToolCallResultEvent : AgentRunnerEvents
{
    public FunctionResult ToolResult { get; set; }
}
```

### Event Handler Examples

#### Basic Event Handling
```csharp
async ValueTask HandleBasicEvents(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.ToolCall:
            Console.WriteLine($"🔧 Calling tool: {((AgentRunnerToolCallEvent)runEvent).ToolCall.Name}");
            break;
            
        case AgentRunnerEventTypes.ToolCallResult:
            Console.WriteLine($"✅ Tool completed");
            break;
    }
    return ValueTask.CompletedTask;
}
```

#### Streaming Event Handling
```csharp
async ValueTask HandleStreamingEvents(AgentRunnerEvents runEvent)
{
    if (runEvent.EventType == AgentRunnerEventTypes.Streaming)
    {
        var streamingEvent = (AgentRunnerStreamingEvent)runEvent;
        
        if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
        {
            Console.Write(deltaEvent.DeltaText);
        }
    }
    return ValueTask.CompletedTask;
}
```

#### Comprehensive Event Handling
```csharp
async ValueTask HandleAllEvents(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.Streaming:
            var streamEvent = (AgentRunnerStreamingEvent)runEvent;
            if (streamEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent delta)
            {
                Console.Write(delta.DeltaText);
            }
            break;
            
        case AgentRunnerEventTypes.ToolCall:
            var toolCallEvent = (AgentRunnerToolCallEvent)runEvent;
            Console.WriteLine($"\\n🔧 Calling: {toolCallEvent.ToolCall.Name}");
            Console.WriteLine($"   Args: {toolCallEvent.ToolCall.Arguments}");
            break;
            
        case AgentRunnerEventTypes.ToolCallResult:
            var resultEvent = (AgentRunnerToolCallResultEvent)runEvent;
            Console.WriteLine($"✅ Result: {resultEvent.ToolResult.Result}");
            break;
            
        default:
            Console.WriteLine($"📝 Event: {runEvent.EventType}");
            break;
    }
    return ValueTask.CompletedTask;
}
```

## Guardrail Integration

TornadoRunner supports input validation through guardrail functions.

### Guardrail Function Signature
```csharp
public delegate ValueTask<GuardRailFunctionOutput> GuardRailFunction(string? input = "");
```

### Example Guardrails

#### Content Filter
```csharp
async ValueTask<GuardRailFunctionOutput> ContentFilter(string input = "")
{
    var forbiddenWords = new[] { "spam", "harmful" };
    
    if (forbiddenWords.Any(word => input.Contains(word, StringComparison.OrdinalIgnoreCase)))
    {
        return new GuardRailFunctionOutput(
            "Content contains forbidden words",
            tripwireTriggered: true
        );
    }
    
    return new GuardRailFunctionOutput("Content approved", false);
}
```

#### Length Validator
```csharp
async ValueTask<GuardRailFunctionOutput> LengthValidator(string input = "")
{
    const int maxLength = 1000;
    
    if (input.Length > maxLength)
    {
        return new GuardRailFunctionOutput(
            $"Input too long: {input.Length} characters (max: {maxLength})",
            tripwireTriggered: true
        );
    }
    
    return new GuardRailFunctionOutput("Length is acceptable", false);
}
```

#### AI-Powered Validation
```csharp
async ValueTask<GuardRailFunctionOutput> TopicValidator(string input = "")
{
    var validatorAgent = new TornadoAgent(
        client,
        ChatModel.OpenAi.Gpt41.V41Mini,
        "Assistant",
        "Determine if input is appropriate for a family-friendly assistant"
    );
    
    var result = await validatorAgent.RunAsync($"Is this appropriate: {input}");
    var response = result.Messages.Last().Content.ToLower();
    
    bool isAppropriate = response.Contains("yes") || response.Contains("appropriate");
    
    return new GuardRailFunctionOutput(
        response,
        tripwireTriggered: !isAppropriate
    );
}
```

## Error Handling

### Exception Types

#### GuardRailTriggerException
Thrown when input validation fails.

```csharp
try
{
    var result = await TornadoRunner.RunAsync(agent, input, inputGuardRailFunction: strictValidator);
}
catch (GuardRailTriggerException ex)
{
    Console.WriteLine($"Input validation failed: {ex.Message}");
}
```

#### Network and API Exceptions
```csharp
try
{
    var result = await TornadoRunner.RunAsync(agent, input);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (TaskCanceledException ex)
{
    Console.WriteLine($"Request timeout: {ex.Message}");
}
```

### Retry Logic
```csharp
public static async Task<Conversation> RunWithRetry(
    TornadoAgent agent, 
    string input, 
    int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await TornadoRunner.RunAsync(agent, input);
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
        }
    }
    
    throw new Exception($"Failed after {maxRetries} attempts");
}
```

## Advanced Usage Patterns

### Tool Execution Monitoring
```csharp
var toolCallCount = 0;
var totalExecutionTime = TimeSpan.Zero;
var executionStart = DateTime.UtcNow;

async ValueTask MonitorExecution(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.ToolCall:
            toolCallCount++;
            Console.WriteLine($"Tool call #{toolCallCount}");
            break;
            
        case AgentRunnerEventTypes.ToolCallResult:
            var elapsed = DateTime.UtcNow - executionStart;
            totalExecutionTime += elapsed;
            Console.WriteLine($"Tool completed in {elapsed.TotalMilliseconds}ms");
            break;
    }
    return ValueTask.CompletedTask;
}

var result = await TornadoRunner.RunAsync(
    agent, 
    input, 
    onAgentRunnerEvent: MonitorExecution
);

Console.WriteLine($"Total tools called: {toolCallCount}");
Console.WriteLine($"Total execution time: {totalExecutionTime.TotalSeconds}s");
```

### Streaming with Progress Tracking
```csharp
var streamedContent = new StringBuilder();
var wordCount = 0;

async ValueTask TrackProgress(AgentRunnerEvents runEvent)
{
    if (runEvent.EventType == AgentRunnerEventTypes.Streaming)
    {
        var streamEvent = (AgentRunnerStreamingEvent)runEvent;
        
        if (streamEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent delta)
        {
            streamedContent.Append(delta.DeltaText);
            
            // Count words
            if (delta.DeltaText.Contains(' '))
            {
                wordCount += delta.DeltaText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                Console.WriteLine($"\\rWords streamed: {wordCount}");
            }
        }
    }
    return ValueTask.CompletedTask;
}

var result = await TornadoRunner.RunAsync(
    agent, 
    "Write a detailed essay",
    streaming: true,
    onAgentRunnerEvent: TrackProgress
);
```

### Conditional Processing
```csharp
async ValueTask<GuardRailFunctionOutput> ConditionalValidator(string input = "")
{
    // Different validation based on input characteristics
    if (input.StartsWith("URGENT:", StringComparison.OrdinalIgnoreCase))
    {
        // Expedited processing for urgent requests
        return new GuardRailFunctionOutput("Urgent request approved", false);
    }
    
    if (input.Length > 500)
    {
        // Require additional validation for long inputs
        var detailedValidator = new TornadoAgent(client, model, "Validate complex requests");
        var validation = await detailedValidator.RunAsync($"Validate: {input}");
        
        bool isValid = validation.Messages.Last().Content.Contains("valid");
        return new GuardRailFunctionOutput(
            validation.Messages.Last().Content,
            tripwireTriggered: !isValid
        );
    }
    
    // Standard validation for normal inputs
    return new GuardRailFunctionOutput("Standard validation passed", false);
}
```

## Performance Considerations

### Optimize Event Handlers
```csharp
// Efficient event handler
async ValueTask EfficientHandler(AgentRunnerEvents runEvent)
{
    // Use pattern matching for performance
    switch (runEvent)
    {
        case AgentRunnerStreamingEvent streamEvent:
            // Handle streaming efficiently
            ProcessStreamingEvent(streamEvent);
            break;
            
        case AgentRunnerToolCallEvent toolEvent:
            // Handle tool calls
            ProcessToolEvent(toolEvent);
            break;
    }
    
    return ValueTask.CompletedTask;
}

void ProcessStreamingEvent(AgentRunnerStreamingEvent streamEvent)
{
    // Avoid async work in hot path when possible
    if (streamEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent delta)
    {
        Console.Write(delta.DeltaText);
    }
}
```

### Resource Management
```csharp
// Properly manage resources in long-running applications
public class AgentManager : IDisposable
{
    private readonly TornadoAgent _agent;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public AgentManager(TornadoAgent agent)
    {
        _agent = agent;
        _cancellationTokenSource = new CancellationTokenSource();
    }
    
    public async Task<Conversation> ExecuteAsync(string input)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _cancellationTokenSource.Token, 
            timeoutCts.Token
        );
        
        return await TornadoRunner.RunAsync(_agent, input);
    }
    
    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
```

## Best Practices

1. **Event Handler Efficiency**: Keep event handlers lightweight and fast
2. **Error Handling**: Always handle potential exceptions appropriately
3. **Resource Management**: Dispose of resources properly in long-running applications
4. **Guardrail Design**: Design guardrails to be fast and accurate
5. **Monitoring**: Use event handlers to monitor performance and behavior

## Thread Safety

TornadoRunner methods are thread-safe, but the underlying TornadoAgent instances are not. For concurrent usage:

```csharp
// Safe: Different agents for different threads
var task1 = TornadoRunner.RunAsync(agent1, input1);
var task2 = TornadoRunner.RunAsync(agent2, input2);
await Task.WhenAll(task1, task2);

// Unsafe: Same agent in multiple threads
// Don't do this without proper synchronization
```

## See Also

- [TornadoAgent API Reference](tornado-agent.md)
- [ToolRunner API Reference](tool-runner.md)
- [Data Models API Reference](data-models.md)
- [Guardrails Guide](../guardrails.md)
- [Basic Agent Usage Guide](../basic-agent-usage.md)