# Model Context Protocol (MCP)

## Overview

The Model Context Protocol (MCP) is an open standard that enables seamless integration between AI models and external tools, data sources, and services. LlmTornado provides first-class support for MCP through the `LlmTornado.Mcp` adapter, allowing you to easily connect your applications to MCP servers and use their tools in your AI workflows.

## Quick Start

Here's a basic example of using MCP with LlmTornado:

```csharp
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using ModelContextProtocol.Client;

// Install the package first:
// dotnet add package LlmTornado.Mcp

// 1. Create MCP client with your transport (e.g., StdioClientTransport)
IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport);

// 2. Fetch available tools from the MCP server
List<Tool> tools = await mcpClient.ListTornadoToolsAsync();

// 3. Create conversation with MCP tools
TornadoApi api = new TornadoApi("your-api-key");
Conversation conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt41.V41,
    Tools = tools
});

// 4. Use the conversation with automatic MCP tool resolution
await conversation
    .AddSystemMessage("You are a helpful assistant")
    .AddUserMessage("What's the weather like?")
    .GetResponseRich(async calls =>
    {
        foreach (FunctionCall call in calls)
        {
            // MCP tools are automatically resolved
            await call.ResolveRemote(call.Arguments);
        }
    });
```

## Prerequisites

- Installation of `LlmTornado.Mcp` package via NuGet
- Basic understanding of LlmTornado chat and function calling
- Familiarity with the Model Context Protocol standard
- An MCP server implementation to connect to

## Detailed Explanation

### What is MCP?

The Model Context Protocol (MCP) is a protocol that standardizes how AI applications communicate with external tools and services. It provides:

- **Standardized Tool Discovery**: Automatically detect available tools and their schemas
- **Seamless Integration**: Connect to any MCP-compliant server
- **Bidirectional Communication**: Enable AI models to call tools and receive results
- **Type Safety**: Strongly-typed tool definitions and parameters
- **Extensibility**: Easy addition of new tools and capabilities

### How LlmTornado Integrates MCP

LlmTornado's MCP adapter provides extension methods that bridge the official .NET MCP SDK with LlmTornado's tool system:

1. **Tool Discovery**: `ListTornadoToolsAsync()` fetches tools from MCP servers
2. **Automatic Conversion**: MCP tool schemas are converted to LlmTornado Tool objects
3. **Remote Resolution**: `ResolveRemote()` executes tools on the MCP server
4. **Result Handling**: MCP responses are automatically parsed and returned

### Key Components

#### IMcpClient
The MCP client interface from the official SDK:

```csharp
public interface IMcpClient : IAsyncDisposable
{
    Task<ListToolsResult> ListToolsAsync();
    Task<CallToolResult> CallToolAsync(string name, IDictionary<string, object?> arguments);
    // ... other methods
}
```

#### Extension Methods
LlmTornado adds convenient extension methods:

```csharp
// Convert MCP tools to LlmTornado tools
List<Tool> tools = await mcpClient.ListTornadoToolsAsync();

// Resolve a tool call remotely on the MCP server
await functionCall.ResolveRemote(arguments);
```

## Basic Usage

### Setting Up an MCP Server Connection

```csharp
using ModelContextProtocol.Client;
using ModelContextProtocol.Transports.Stdio;

// Create stdio transport for local MCP server
StdioClientTransport transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Command = "path/to/mcp-server",
    Arguments = new[] { "--port", "8080" }
});

// Create MCP client
IMcpClient mcpClient = await McpClientFactory.CreateAsync(transport);

// List available tools
List<Tool> tools = await mcpClient.ListTornadoToolsAsync();

Console.WriteLine($"Found {tools.Count} tools:");
foreach (Tool tool in tools)
{
    Console.WriteLine($"  - {tool.Function?.Name}: {tool.Function?.Description}");
}
```

### Using MCP Tools in Conversations

```csharp
TornadoApi api = new TornadoApi("your-api-key");

// Fetch MCP tools
List<Tool> mcpTools = await mcpClient.ListTornadoToolsAsync();

// Create conversation with MCP tools
Conversation conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt41.V41,
    Tools = mcpTools,
    ToolChoice = OutboundToolChoice.Auto // Let model decide when to use tools
});

// Run conversation
await conversation
    .AddSystemMessage("You are a helpful assistant with access to weather data.")
    .AddUserMessage("What's the weather like in Dallas?")
    .GetResponseRich(async calls =>
    {
        foreach (FunctionCall call in calls)
        {
            // Get arguments inferred by the model
            double latitude = call.GetOrDefault<double>("latitude");
            double longitude = call.GetOrDefault<double>("longitude");
            
            // Call the tool on the MCP server
            await call.ResolveRemote(new
            {
                latitude = latitude,
                longitude = longitude
            });
            
            // Extract and use the result
            if (call.Result?.RemoteContent is McpContent mcpContent)
            {
                foreach (IMcpContentBlock block in mcpContent.McpContentBlocks)
                {
                    if (block is McpContentBlockText textBlock)
                    {
                        call.Result.Content = textBlock.Text;
                    }
                }
            }
        }
    });

// Get final response from model
conversation.RequestParameters.ToolChoice = null;
string? response = await conversation.GetResponse();
Console.WriteLine(response);
```

### Creating an MCP Server

Define tools on your MCP server using the official SDK:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class WeatherTools
{
    [McpServerTool, Description("Get weather forecast for a location.")]
    public static async Task<string> GetForecast(
        HttpClient client,
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude)
    {
        string pointUrl = string.Create(
            CultureInfo.InvariantCulture, 
            $"/points/{latitude},{longitude}"
        );
        
        JsonDocument jsonDocument = await client.ReadJsonDocumentAsync(pointUrl);
        string? forecastUrl = jsonDocument.RootElement
            .GetProperty("properties")
            .GetProperty("forecast")
            .GetString();
            
        if (forecastUrl == null)
        {
            throw new Exception("No forecast URL provided");
        }
        
        JsonDocument forecastDoc = await client.ReadJsonDocumentAsync(forecastUrl);
        JsonElement.ArrayEnumerator periods = forecastDoc.RootElement
            .GetProperty("properties")
            .GetProperty("periods")
            .EnumerateArray();
        
        return string.Join("\n---\n", periods.Select(period => $"""
            {period.GetProperty("name").GetString()}
            Temperature: {period.GetProperty("temperature").GetInt32()}°F
            Wind: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
            Forecast: {period.GetProperty("detailedForecast").GetString()}
            """));
    }
}
```

## Advanced Usage

### Using MCP Tools with Agents

```csharp
using LlmTornado.Agents;

// Fetch MCP tools
List<Tool> mcpTools = await mcpClient.ListTornadoToolsAsync();

// Create MCP server wrapper for agent
MCPServer mcpServer = new MCPServer(mcpClient);

// Create agent with MCP tools
TornadoAgent agent = new TornadoAgent(
    client: api,
    model: ChatModel.OpenAi.Gpt41.V41,
    instructions: "You are a helpful assistant with access to weather information.",
    mcpServers: new List<MCPServer> { mcpServer }
);

// Run agent - it will automatically use MCP tools when needed
Conversation result = await agent.RunAsync("What's the forecast for Seattle this week?");
Console.WriteLine(result.Messages.Last().Content);
```

### Multiple MCP Servers

You can connect to multiple MCP servers and use their tools together:

```csharp
// Create multiple MCP clients
IMcpClient weatherClient = await McpClientFactory.CreateAsync(weatherTransport);
IMcpClient databaseClient = await McpClientFactory.CreateAsync(databaseTransport);
IMcpClient fileClient = await McpClientFactory.CreateAsync(fileTransport);

// Fetch tools from all servers
List<Tool> allTools = new List<Tool>();
allTools.AddRange(await weatherClient.ListTornadoToolsAsync());
allTools.AddRange(await databaseClient.ListTornadoToolsAsync());
allTools.AddRange(await fileClient.ListTornadoToolsAsync());

// Use all tools in one conversation
Conversation conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt41.V41,
    Tools = allTools
});

// Model can now use any available tool from any server
await conversation
    .AddUserMessage("Get weather for New York and save it to the database")
    .GetResponseRich(async calls =>
    {
        foreach (FunctionCall call in calls)
        {
            await call.ResolveRemote(call.Arguments);
        }
    });
```

### Handling MCP Resources

MCP also supports resources (files, data sources, etc.):

```csharp
// List available resources
ListResourcesResult resources = await mcpClient.ListResourcesAsync();

foreach (Resource resource in resources.Resources)
{
    Console.WriteLine($"Resource: {resource.Name}");
    Console.WriteLine($"  URI: {resource.Uri}");
    Console.WriteLine($"  Type: {resource.MimeType}");
}

// Read a specific resource
ReadResourceResult result = await mcpClient.ReadResourceAsync(
    new Uri("mcp://server/path/to/resource")
);

foreach (IMcpContentBlock content in result.Contents)
{
    if (content is McpContentBlockText textContent)
    {
        Console.WriteLine(textContent.Text);
    }
}
```

### Error Handling

```csharp
try
{
    List<Tool> tools = await mcpClient.ListTornadoToolsAsync();
    
    await conversation.GetResponseRich(async calls =>
    {
        foreach (FunctionCall call in calls)
        {
            try
            {
                await call.ResolveRemote(call.Arguments);
                
                if (call.Result?.RemoteContent == null)
                {
                    call.Result = new FunctionResult(
                        call, 
                        "Tool execution failed - no result returned"
                    );
                }
            }
            catch (Exception ex)
            {
                call.Result = new FunctionResult(
                    call,
                    $"Error executing tool: {ex.Message}"
                );
            }
        }
    });
}
catch (Exception ex)
{
    Console.WriteLine($"MCP error: {ex.Message}");
}
```

## Best Practices

### Connection Management
- Properly dispose MCP clients using `await using` or try-finally blocks
- Reuse MCP clients across multiple conversations when possible
- Handle connection failures gracefully with retry logic
- Monitor MCP server health and availability

### Tool Usage
- Cache tool lists to avoid repeated discovery calls
- Validate tool arguments before sending to MCP server
- Provide clear error messages when tools fail
- Log tool calls for debugging and monitoring

### Security
- Validate MCP server endpoints before connecting
- Use secure transports (TLS/SSL) for remote servers
- Implement authentication when required
- Sanitize tool inputs and outputs

### Performance
- Use streaming for long-running MCP operations
- Implement timeouts for tool calls
- Consider batching multiple tool calls when possible
- Monitor and optimize tool execution times

## Common Issues

### Issue: MCP Client Connection Fails
**Problem**: Cannot connect to MCP server
**Solutions**:
- Verify server is running and accessible
- Check transport configuration (command, port, etc.)
- Ensure correct protocol version compatibility
- Review server logs for connection errors

### Issue: Tools Not Appearing
**Problem**: `ListTornadoToolsAsync()` returns empty list
**Solutions**:
- Verify MCP server has tools registered
- Check server is properly initialized
- Ensure tool schemas are valid
- Review server tool configuration

### Issue: Tool Execution Fails
**Problem**: `ResolveRemote()` throws exceptions
**Solutions**:
- Validate tool arguments match schema
- Check MCP server logs for errors
- Ensure required dependencies are available
- Implement proper error handling

### Issue: Result Parsing Problems
**Problem**: Cannot extract tool results from MCP content
**Solutions**:
- Check content block types (text, resource, etc.)
- Handle all possible content block variants
- Validate result structure matches expectations
- Log raw MCP responses for debugging

## API Reference

### Extension Methods

#### ListTornadoToolsAsync
```csharp
public static async Task<List<Tool>> ListTornadoToolsAsync(this IMcpClient client)
```
Fetches all tools from the MCP server and converts them to LlmTornado Tool objects.

#### ResolveRemote
```csharp
public static async Task ResolveRemote(this FunctionCall call, object arguments)
```
Executes a function call on the remote MCP server.

### MCP Server Configuration

#### MCPServer Class
```csharp
public class MCPServer
{
    public MCPServer(IMcpClient client)
    public IMcpClient Client { get; }
}
```

### MCP Content Types

- `McpContent` - Container for MCP content blocks
- `McpContentBlockText` - Text content block
- `McpContentBlockResource` - Resource reference block
- `McpContentBlockImage` - Image content block

## Related Topics

- [Function Calling](../1.%20LlmTornado/1.%20Chat/4.%20functions.md) - Learn about LlmTornado's function calling system
- [Tornado Agent Tools](../2.%20Agents/2.%20Tornado-Agent/4.%20Tools/3.%20MCP%20Tools.md) - Using MCP tools with agents
- [Official MCP Documentation](https://modelcontextprotocol.io/) - Learn more about the MCP standard
- [MCP .NET SDK](https://github.com/modelcontextprotocol/dotnet-sdk) - Official .NET SDK for MCP
