# Model Context Protocol (MCP)

## Overview

The Model Context Protocol (MCP) is an open standard that enables seamless integration between AI models and external tools, data sources, and services. LlmTornado provides first-class support for MCP through the `LlmTornado.Mcp` adapter, allowing you to easily connect your applications to MCP servers and use their tools in your AI workflows.

## Quick Start

 Please see [https://github.com/GongRzhe/Gmail-MCP-Server](gmail-mcp-server) for more details on setting up OAuth

```csharp

    MCPServer gmailServer = new MCPServer(
        serverLabel:"gmail",
        command: "npx", 
        arguments: new[] { "@gongrzhe/server-gmail-autoauth-mcp" },
        allowedTools: [
            "read_email", 
            "draft_email", 
            "search_emails"]);

    await gmailServer.InitializeAsync(); // This will handle MCP Client connection to setup tools

    TornadoAgent agent = new TornadoAgent(
        client,
        model: ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: "You are a useful assistant for managing Gmail."
            );


    agent.AddMcpTools(gmailServer.AllowedTornadoTools.ToArray()); // Register MCP tools to the agent

    Conversation result = await agent.RunAsync("Did mom respond?");

    Console.WriteLine(result.Messages.Last().Content);
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
