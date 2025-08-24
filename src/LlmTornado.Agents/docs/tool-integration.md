# Tool Integration

Extend your agents with powerful tools to interact with external systems, APIs, and other agents.

## Overview

LlmTornado.Agents supports three types of tools:

1. **Delegate Tools** - Convert C# methods into agent tools
2. **MCP Tools** - Integrate with Model Context Protocol servers
3. **Agent Tools** - Use other agents as tools

## Delegate Tools

Convert any C# method into a tool that your agent can use.

### Basic Tool Creation

```csharp
using System.ComponentModel;

[Description("Get the current weather in a given location")]
public static string GetCurrentWeather(
    [Description("The city and state, e.g. Boston, MA")] string location)
{
    // Your weather API integration here
    return $"The weather in {location} is sunny, 72°F";
}

// Create agent with tools
TornadoAgent agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful weather assistant.",
    tools: [GetCurrentWeather]
);

// Use the tool
var result = await agent.RunAsync("What's the weather like in Boston?");
```

### Tool with Multiple Parameters

```csharp
[Description("Calculate the distance between two cities")]
public static string CalculateDistance(
    [Description("Starting city")] string fromCity,
    [Description("Destination city")] string toCity,
    [Description("Unit of measurement")] DistanceUnit unit = DistanceUnit.Miles)
{
    // Distance calculation logic
    return $"Distance from {fromCity} to {toCity}: 500 {unit}";
}

public enum DistanceUnit
{
    Miles,
    Kilometers
}
```

### Async Tools

Tools can be asynchronous:

```csharp
[Description("Search the web for information")]
public static async Task<string> WebSearch(
    [Description("Search query")] string query,
    [Description("Number of results to return")] int maxResults = 5)
{
    using var httpClient = new HttpClient();
    // Implement web search logic
    await Task.Delay(1000); // Simulate API call
    return $"Found {maxResults} results for '{query}'";
}
```

### Complex Return Types

Return structured data from tools:

```csharp
public class SearchResult
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string Summary { get; set; }
}

[Description("Search for articles on a topic")]
public static async Task<List<SearchResult>> SearchArticles(
    [Description("Topic to search for")] string topic)
{
    // Return structured search results
    return new List<SearchResult>
    {
        new() { Title = "Article 1", Url = "http://example.com", Summary = "Summary 1" },
        new() { Title = "Article 2", Url = "http://example.com", Summary = "Summary 2" }
    };
}
```

### Parameter Validation

Use attributes to control parameter behavior:

```csharp
using LlmTornado.Code;

[Description("Send an email")]
public static string SendEmail(
    [Description("Recipient email address")] string to,
    [Description("Email subject")] string subject,
    [Description("Email body")] string body,
    [SchemaIgnore] bool sendActualEmail = false) // Hidden from AI
{
    if (sendActualEmail)
    {
        // Send real email
    }
    return $"Email sent to {to} with subject '{subject}'";
}
```

## MCP Tools

Integrate with Model Context Protocol servers for advanced tool capabilities.

### Setting up MCP Servers

```csharp
using LlmTornado.Agents.DataModels;

// Local MCP server (subprocess)
var mcpServer = new MCPServer(
    serverLabel: "file-tools",
    serverUrl: "path/to/mcp-server-executable",
    allowedTools: ["read_file", "write_file"] // Optional: restrict tools
);

// Remote MCP server (HTTP/SSE)
var remoteMcpServer = new MCPServer(
    serverLabel: "web-tools",
    serverUrl: "https://api.example.com/mcp",
    allowedTools: null // Allow all tools
);

// Create agent with MCP servers
TornadoAgent agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful assistant with file and web access.",
    mcpServers: [mcpServer, remoteMcpServer]
);
```

### Using MCP Tools

MCP tools are automatically available to your agent:

```csharp
// The agent can now use file operations
var result = await agent.RunAsync("Please read the contents of config.json and summarize it");

// Or web operations if the MCP server provides them
var result2 = await agent.RunAsync("Search for the latest news about AI");
```

### Custom MCP Server Integration

For advanced scenarios, you can work with MCP tools directly:

```csharp
// Access MCP tools programmatically
var mcpTools = agent.McpTools;
foreach (var (toolName, server) in mcpTools)
{
    Console.WriteLine($"Available tool: {toolName} from server: {server.ServerLabel}");
}
```

## Agent Tools

Use other agents as tools for complex workflows and specialization.

### Creating Agent Tools

```csharp
// Specialized translator agent
TornadoAgent translatorAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You translate English input to Spanish output. Only translate, don't answer questions."
);

// Main agent that can use translator
TornadoAgent mainAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful assistant. Use the translator tool when asked to translate to Spanish.",
    tools: [translatorAgent.AsTool]
);

// Use the agent tool
var result = await mainAgent.RunAsync("What is 2+2? Please provide the answer in Spanish.");
Console.WriteLine(result.Messages.Last().Content);
```

### Complex Agent Tool Workflows

```csharp
// Research agent
TornadoAgent researchAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a research specialist. Provide detailed, accurate information on any topic."
);

// Writing agent
TornadoAgent writingAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a professional writer. Create engaging, well-structured content."
);

// Coordinator agent
TornadoAgent coordinator = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: """
        You coordinate between research and writing specialists.
        First gather information using research, then create content using writing.
        """,
    tools: [researchAgent.AsTool, writingAgent.AsTool]
);

var result = await coordinator.RunAsync("Create a blog post about renewable energy trends in 2024");
```

## Tool Execution Flow

Understanding how tools are invoked:

```csharp
ValueTask HandleToolEvents(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.ToolCall:
            if (runEvent is AgentRunnerToolCallEvent toolCallEvent)
            {
                Console.WriteLine($"🔧 Calling tool: {toolCallEvent.ToolCall.Name}");
                Console.WriteLine($"   Arguments: {toolCallEvent.ToolCall.Arguments}");
            }
            break;
            
        case AgentRunnerEventTypes.ToolCallResult:
            if (runEvent is AgentRunnerToolCallResultEvent resultEvent)
            {
                Console.WriteLine($"✅ Tool result: {resultEvent.ToolResult.Result}");
            }
            break;
    }
    return ValueTask.CompletedTask;
}

// Monitor tool execution
var result = await agent.RunAsync(
    "What's the weather in New York?",
    onAgentRunnerEvent: HandleToolEvents
);
```

## Advanced Tool Patterns

### Conditional Tool Access

Control when tools are available:

```csharp
public class ConditionalTool
{
    private readonly bool _isAuthorized;
    
    public ConditionalTool(bool isAuthorized)
    {
        _isAuthorized = isAuthorized;
    }
    
    [Description("Access sensitive data")]
    public string AccessSensitiveData(string query)
    {
        if (!_isAuthorized)
        {
            return "Access denied: insufficient permissions";
        }
        
        return "Sensitive data response";
    }
}

var conditionalTool = new ConditionalTool(isAuthorized: true);
var agent = new TornadoAgent(
    client,
    model,
    instructions,
    tools: [conditionalTool.AccessSensitiveData]
);
```

### Tool Result Caching

Implement caching for expensive operations:

```csharp
private static readonly Dictionary<string, string> _cache = new();

[Description("Expensive computation that benefits from caching")]
public static string ExpensiveComputation(string input)
{
    if (_cache.TryGetValue(input, out string cachedResult))
    {
        return $"[Cached] {cachedResult}";
    }
    
    // Simulate expensive operation
    Thread.Sleep(2000);
    string result = $"Computed result for: {input}";
    
    _cache[input] = result;
    return result;
}
```

### Error Handling in Tools

```csharp
[Description("Tool that might fail")]
public static string RiskyOperation(string input)
{
    try
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be empty");
        }
        
        // Risky operation here
        return "Operation completed successfully";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}";
    }
}
```

## Best Practices

### Tool Design Guidelines

1. **Clear Descriptions**: Use detailed descriptions for tools and parameters
2. **Error Handling**: Always handle errors gracefully
3. **Performance**: Consider caching and async operations for slow tools
4. **Security**: Validate inputs and control access to sensitive operations
5. **Atomicity**: Keep tools focused on single responsibilities

### Tool Naming Conventions

```csharp
// Good: Descriptive, action-oriented names
[Description("Get weather forecast for a specific location")]
public static string GetWeatherForecast(string location) { }

[Description("Send notification via email")]
public static string SendEmailNotification(string recipient, string message) { }

// Avoid: Vague or overly generic names
public static string DoStuff(string input) { } // Bad
public static string Process(object data) { } // Bad
```

## Troubleshooting

### Common Issues

**Tool Not Found**
```
Error: I don't have a tool called 'ToolName'
```
Solution: Ensure the tool is included in the agent's tools list.

**Invalid Tool Arguments**
```
Error: Function arguments are not valid JSON
```
Solution: Check parameter types and descriptions. Avoid complex nested objects.

**Tool Execution Timeout**
```
Error: Tool execution timed out
```
Solution: Implement async operations and consider breaking down complex tools.

## Next Steps

- Explore [Structured Output](structured-output.md) for consistent responses
- Learn about [MCP Integration](mcp-integration.md) in depth
- Discover [Chat Runtime](chat-runtime.md) for complex workflows
- Check [Examples](examples/) for working implementations