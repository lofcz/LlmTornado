# TornadoAgent API Reference

Complete API reference for the `TornadoAgent` class, the core component of the LlmTornado.Agents framework.

## Class Overview

```csharp
namespace LlmTornado.Agents
{
    public class TornadoAgent
    {
        // Properties, constructors, and methods
    }
}
```

The `TornadoAgent` class encapsulates AI agent behavior, tool integration, and conversation management.

## Constructors

### Primary Constructor

```csharp
public TornadoAgent(
    TornadoApi client, 
    ChatModel model, 
    string instructions = "", 
    Type? outputSchema = null, 
    List<Delegate>? tools = null,
    List<MCPServer>? mcpServers = null)
```

**Parameters:**
- `client` (`TornadoApi`): The API client for LLM communication
- `model` (`ChatModel`): The chat model to use (e.g., `ChatModel.OpenAi.Gpt41.V41Mini`)
- `instructions` (`string`, optional): Instructions defining agent behavior
- `outputSchema` (`Type?`, optional): Type for structured output validation
- `tools` (`List<Delegate>?`, optional): List of tool methods the agent can use
- `mcpServers` (`List<MCPServer>?`, optional): List of MCP servers for tool integration

**Example:**
```csharp
var agent = new TornadoAgent(
    client: apiClient,
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful assistant",
    outputSchema: typeof(MyOutputType),
    tools: [MyToolMethod],
    mcpServers: [mcpServer]
);
```

### Dummy Agent Constructor

```csharp
public static TornadoAgent DummyAgent()
```

Creates a dummy agent for testing purposes with default OpenAI configuration.

**Returns:** A `TornadoAgent` instance configured with basic settings.

**Example:**
```csharp
var dummyAgent = TornadoAgent.DummyAgent();
```

## Properties

### Core Properties

#### Client
```csharp
public TornadoApi Client { get; set; }
```
The API client used for LLM communication.

#### Instructions
```csharp
public string Instructions { get; set; }
```
The instructions that define the agent's behavior and role.

#### Model
```csharp
public ChatModel Model { get; set; }
```
The chat model used by the agent.

#### Name
```csharp
public string Name { get; set; }
```
The name identifier for the agent.

#### Conversation
```csharp
public Conversation Conversation { get; set; }
```
The current conversation context maintained by the agent.

### Tool and Schema Properties

#### Tools
```csharp
public List<Delegate> Tools { get; set; }
```
List of delegate tools available to the agent.

#### AgentTools
```csharp
public Dictionary<string, TornadoAgentTool> AgentTools { get; set; }
```
Dictionary mapping tool names to agent tool instances.

#### McpTools
```csharp
public Dictionary<string, MCPServer> McpTools { get; set; }
```
Dictionary mapping MCP tool names to their servers.

#### McpServers
```csharp
public List<MCPServer> McpServers { get; set; }
```
List of MCP servers configured for the agent.

#### OutputSchema
```csharp
public Type? OutputSchema { get; set; }
```
The type used for structured output validation.

### Configuration Properties

#### Options
```csharp
public ChatRequest Options { get; set; }
```
Chat request options including model parameters, response format, and tool configurations.

## Methods

### Execution Methods

#### RunAsync (Basic)
```csharp
public async Task<Conversation> RunAsync(string input)
```

Executes the agent with a simple text input.

**Parameters:**
- `input` (`string`): The user input to process

**Returns:** `Task<Conversation>` containing the conversation with the agent's response.

**Example:**
```csharp
var result = await agent.RunAsync("Hello, how are you?");
string response = result.Messages.Last().Content;
```

#### RunAsync (Advanced)
```csharp
public async Task<Conversation> RunAsync(
    string input,
    bool streaming = false,
    Func<AgentRunnerEvents, ValueTask>? onAgentRunnerEvent = null,
    GuardRailFunction? inputGuardRailFunction = null)
```

Executes the agent with advanced options.

**Parameters:**
- `input` (`string`): The user input to process
- `streaming` (`bool`, optional): Enable streaming responses
- `onAgentRunnerEvent` (`Func<AgentRunnerEvents, ValueTask>?`, optional): Event handler for agent execution events
- `inputGuardRailFunction` (`GuardRailFunction?`, optional): Input validation function

**Returns:** `Task<Conversation>` containing the conversation result.

**Example:**
```csharp
async ValueTask HandleEvents(AgentRunnerEvents eventData)
{
    if (eventData is AgentRunnerStreamingEvent streamEvent)
    {
        Console.Write(streamEvent.ModelStreamingEvent.DeltaText);
    }
    return ValueTask.CompletedTask;
}

var result = await agent.RunAsync(
    "Tell me a story",
    streaming: true,
    onAgentRunnerEvent: HandleEvents
);
```

### Tool Integration Methods

#### AsTool Property
```csharp
public Delegate AsTool { get; }
```

Converts the agent into a tool that can be used by other agents.

**Example:**
```csharp
var translatorAgent = new TornadoAgent(client, model, "You translate English to Spanish");
var mainAgent = new TornadoAgent(
    client, 
    model, 
    "You are a helpful assistant",
    tools: [translatorAgent.AsTool]
);
```

### Utility Methods

#### SetupTools (Private)
```csharp
private void SetupTools(List<Delegate> tools)
```

Internal method that configures delegate tools for the agent.

## Static Methods

### DummyAgent
```csharp
public static TornadoAgent DummyAgent()
```

Creates a test agent with default configuration.

## Usage Patterns

### Basic Agent Creation and Usage

```csharp
// Create client
var client = new TornadoApi("your-api-key");

// Create basic agent
var agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You are a helpful assistant"
);

// Use the agent
var result = await agent.RunAsync("What is machine learning?");
Console.WriteLine(result.Messages.Last().Content);
```

### Agent with Tools

```csharp
[Description("Get current weather for a location")]
public static string GetWeather(string location)
{
    return $"Weather in {location}: Sunny, 72°F";
}

var agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You are a weather assistant",
    tools: [GetWeather]
);

var result = await agent.RunAsync("What's the weather in New York?");
```

### Agent with Structured Output

```csharp
public struct WeatherInfo
{
    [Description("Location name")]
    public string Location { get; set; }
    
    [Description("Temperature in Fahrenheit")]
    public int Temperature { get; set; }
    
    [Description("Weather condition")]
    public string Condition { get; set; }
}

var agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "Provide weather information",
    outputSchema: typeof(WeatherInfo)
);

var result = await agent.RunAsync("Weather in Miami");
var weather = result.Messages.Last().Content.ParseJson<WeatherInfo>();
```

### Agent with MCP Servers

```csharp
var mcpServer = new MCPServer("file-ops", "/path/to/mcp-server");

var agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You can read and write files",
    mcpServers: [mcpServer]
);

var result = await agent.RunAsync("Read the config.json file");
```

### Streaming Agent

```csharp
async ValueTask HandleStreaming(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.Streaming:
            if (runEvent is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                {
                    Console.Write(deltaEvent.DeltaText);
                }
            }
            break;
    }
    return ValueTask.CompletedTask;
}

var result = await agent.RunAsync(
    "Write a short story",
    streaming: true,
    onAgentRunnerEvent: HandleStreaming
);
```

### Agent with Guardrails

```csharp
async ValueTask<GuardRailFunctionOutput> ContentFilter(string input = "")
{
    if (input.Contains("inappropriate"))
    {
        return new GuardRailFunctionOutput(
            "Content contains inappropriate material",
            tripwireTriggered: true
        );
    }
    
    return new GuardRailFunctionOutput(
        "Content is appropriate",
        tripwireTriggered: false
    );
}

try
{
    var result = await agent.RunAsync(
        "Tell me a joke",
        inputGuardRailFunction: ContentFilter
    );
}
catch (GuardRailTriggerException ex)
{
    Console.WriteLine($"Guardrail triggered: {ex.Message}");
}
```

## Configuration Options

### Chat Request Options

The `Options` property provides access to advanced configuration:

```csharp
agent.Options.MaxTokens = 1000;
agent.Options.Temperature = 0.7;
agent.Options.TopP = 0.9;
agent.Options.PresencePenalty = 0.1;
agent.Options.FrequencyPenalty = 0.1;
```

### Response Format Configuration

When using structured output, the response format is automatically configured:

```csharp
// This is handled automatically when outputSchema is provided
agent.Options.ResponseFormat = ChatRequestResponseFormats.StructuredJson(
    schemaName, 
    jsonSchema
);
```

## Error Handling

### Common Exceptions

- `ArgumentNullException`: Thrown when required parameters are null
- `GuardRailTriggerException`: Thrown when input guardrails are triggered
- `HttpRequestException`: Thrown for network-related errors
- `JsonException`: Thrown when structured output parsing fails

### Error Handling Patterns

```csharp
try
{
    var result = await agent.RunAsync("Your input here");
    // Process result
}
catch (GuardRailTriggerException ex)
{
    // Handle guardrail violations
    Console.WriteLine($"Input blocked: {ex.Message}");
}
catch (HttpRequestException ex)
{
    // Handle network errors
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (JsonException ex)
{
    // Handle parsing errors
    Console.WriteLine($"Parsing error: {ex.Message}");
}
```

## Performance Considerations

### Resource Management

```csharp
// Agents can be reused for multiple conversations
var agent = new TornadoAgent(client, model, instructions);

// Multiple calls with the same agent maintain conversation context
var result1 = await agent.RunAsync("First question");
var result2 = await agent.RunAsync("Follow-up question");

// Clear conversation history if needed
agent.Conversation = client.Chat.CreateConversation();
```

### Optimization Tips

1. **Reuse Agents**: Create agents once and reuse for multiple conversations
2. **Manage Context**: Clear conversation history when it becomes too long
3. **Streaming**: Use streaming for long responses to improve perceived performance
4. **Tool Caching**: Cache expensive tool operations when appropriate

## Thread Safety

The `TornadoAgent` class is **not thread-safe**. For concurrent usage:

```csharp
// Create separate agents for each thread
var agent1 = new TornadoAgent(client, model, instructions);
var agent2 = new TornadoAgent(client, model, instructions);

// Or use locks if sharing is necessary
private readonly object _agentLock = new object();

lock (_agentLock)
{
    var result = await agent.RunAsync(input);
}
```

## Best Practices

1. **Single Purpose**: Design agents with specific, well-defined purposes
2. **Clear Instructions**: Provide detailed, unambiguous instructions
3. **Tool Design**: Keep tools focused and atomic
4. **Error Handling**: Always handle potential exceptions
5. **Resource Management**: Properly manage conversation context and memory usage

## Migration Notes

When upgrading between versions, check for:
- Changes in constructor parameters
- New configuration options
- Updated method signatures
- Deprecated features

## See Also

- [TornadoRunner API Reference](tornado-runner.md)
- [ToolRunner API Reference](tool-runner.md)
- [Data Models API Reference](data-models.md)
- [Basic Agent Usage Guide](../basic-agent-usage.md)
- [Tool Integration Guide](../tool-integration.md)