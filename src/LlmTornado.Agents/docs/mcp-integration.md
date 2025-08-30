# MCP Integration

Integrate with Model Context Protocol (MCP) servers to extend your agents with powerful external tools and capabilities.

## Overview

Model Context Protocol (MCP) is a standard for connecting AI systems with external tools and data sources. LlmTornado.Agents provides seamless integration with MCP servers, allowing you to:

- Connect to local MCP servers (subprocess-based)
- Connect to remote MCP servers (HTTP/SSE-based)
- Automatically discover and use available tools
- Control access to specific tools

## Setting Up MCP Servers

### Local MCP Server (Subprocess)

Connect to MCP servers that run as local processes:

```csharp
using LlmTornado.Agents.DataModels;

// Basic local MCP server
var fileServer = new MCPServer(
    serverLabel: "file-operations",
    serverUrl: "/path/to/mcp-file-server"
);

// MCP server with specific allowed tools
var weatherServer = new MCPServer(
    serverLabel: "weather-tools",
    serverUrl: "/path/to/weather-server",
    allowedTools: ["get_weather", "get_forecast"] // Only allow specific tools
);
```

### Remote MCP Server (HTTP/SSE)

Connect to MCP servers over HTTP with Server-Sent Events:

```csharp
// Remote MCP server
var webServer = new MCPServer(
    serverLabel: "web-services",
    serverUrl: "https://api.example.com/mcp"
);

// Multiple servers can be used simultaneously
var databaseServer = new MCPServer(
    serverLabel: "database-tools",
    serverUrl: "https://db.example.com/mcp",
    allowedTools: ["query_users", "update_profile"]
);
```

### Agent Configuration with MCP

```csharp
TornadoAgent agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful assistant with access to file operations and web services.",
    mcpServers: [fileServer, webServer, databaseServer]
);

// The agent now has access to all tools from the MCP servers
var result = await agent.RunAsync("Please read the contents of config.json and check the weather for the location specified in the config");
```

## Working with MCP Tools

### Automatic Tool Discovery

MCP tools are automatically discovered and made available to your agent:

```csharp
// Check available MCP tools
foreach (var (toolName, server) in agent.McpTools)
{
    Console.WriteLine($"Tool: {toolName} (from {server.ServerLabel})");
    
    // Access tool metadata
    var tool = server.mcp_tools[toolName];
    Console.WriteLine($"  Description: {tool.Description}");
    Console.WriteLine($"  Parameters: {string.Join(", ", tool.Parameters?.Keys ?? [])}");
}
```

### Tool Execution Monitoring

Monitor MCP tool execution:

```csharp
ValueTask HandleMcpToolEvents(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.ToolCall:
            if (runEvent is AgentRunnerToolCallEvent toolCall)
            {
                // Check if it's an MCP tool
                if (agent.McpTools.ContainsKey(toolCall.ToolCall.Name))
                {
                    var server = agent.McpTools[toolCall.ToolCall.Name];
                    Console.WriteLine($"🔧 Executing MCP tool: {toolCall.ToolCall.Name}");
                    Console.WriteLine($"   Server: {server.ServerLabel}");
                    Console.WriteLine($"   Arguments: {toolCall.ToolCall.Arguments}");
                }
            }
            break;
            
        case AgentRunnerEventTypes.ToolCallResult:
            if (runEvent is AgentRunnerToolCallResultEvent result)
            {
                if (agent.McpTools.ContainsKey(result.ToolResult.ToolCall.Name))
                {
                    Console.WriteLine($"✅ MCP tool completed: {result.ToolResult.ToolCall.Name}");
                    Console.WriteLine($"   Result: {result.ToolResult.Result}");
                }
            }
            break;
    }
    return ValueTask.CompletedTask;
}

var result = await agent.RunAsync(
    "Use the file tools to list directory contents",
    onAgentRunnerEvent: HandleMcpToolEvents
);
```

## Common MCP Server Examples

### File Operations Server

```csharp
// MCP server for file operations
var fileServer = new MCPServer(
    serverLabel: "file-ops",
    serverUrl: "/usr/local/bin/mcp-file-server",
    allowedTools: [
        "read_file",
        "write_file", 
        "list_directory",
        "delete_file",
        "create_directory"
    ]
);

TornadoAgent fileAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: """
        You are a file management assistant. You can help users:
        - Read and analyze file contents
        - Create and modify files
        - Organize directories
        - Find and manage files
        
        Always ask for confirmation before deleting files.
        """,
    mcpServers: [fileServer]
);

// Example usage
var result = await fileAgent.RunAsync("Please read the README.md file and summarize its contents");
```

### Web Search Server

```csharp
// MCP server for web search capabilities
var searchServer = new MCPServer(
    serverLabel: "web-search",
    serverUrl: "https://search-api.example.com/mcp",
    allowedTools: [
        "web_search",
        "get_webpage_content",
        "search_news",
        "search_images"
    ]
);

TornadoAgent researchAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: """
        You are a research assistant with web search capabilities.
        Help users find information on any topic by searching the web.
        Provide accurate, up-to-date information with source citations.
        """,
    mcpServers: [searchServer]
);

var result = await researchAgent.RunAsync("Research the latest developments in quantum computing and provide a summary");
```

### Database Operations Server

```csharp
// MCP server for database operations
var dbServer = new MCPServer(
    serverLabel: "database",
    serverUrl: "/path/to/database-mcp-server",
    allowedTools: [
        "execute_query",
        "get_table_schema",
        "list_tables",
        "insert_record",
        "update_record"
    ]
);

TornadoAgent dataAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: """
        You are a database assistant. Help users interact with databases by:
        - Writing and executing SQL queries
        - Explaining query results
        - Suggesting database optimizations
        - Ensuring data integrity
        
        Always validate queries before execution to prevent data loss.
        """,
    mcpServers: [dbServer]
);
```

## Advanced MCP Patterns

### Multi-Server Workflows

Coordinate between multiple MCP servers:

```csharp
var fileServer = new MCPServer("files", "/path/to/file-server");
var emailServer = new MCPServer("email", "https://email.example.com/mcp");
var analyticsServer = new MCPServer("analytics", "/path/to/analytics-server");

TornadoAgent reportAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: """
        You are an automated reporting assistant. Your workflow:
        1. Read data files using file operations
        2. Analyze data using analytics tools
        3. Generate reports
        4. Email reports to stakeholders
        """,
    mcpServers: [fileServer, emailServer, analyticsServer]
);

var result = await reportAgent.RunAsync("""
    Please create a weekly sales report:
    1. Read the sales data from /data/weekly_sales.csv
    2. Analyze trends and key metrics
    3. Generate a summary report
    4. Email the report to sales@company.com
    """);
```

### Conditional MCP Server Access

Control MCP server access based on conditions:

```csharp
public class ConditionalMcpAgent
{
    private readonly TornadoApi _client;
    private readonly List<MCPServer> _baseServers;
    private readonly List<MCPServer> _adminServers;
    
    public ConditionalMcpAgent(TornadoApi client)
    {
        _client = client;
        
        _baseServers = [
            new MCPServer("files", "/path/to/file-server", ["read_file", "list_directory"]),
            new MCPServer("web", "https://web.example.com/mcp", ["web_search"])
        ];
        
        _adminServers = [
            new MCPServer("admin-files", "/path/to/file-server", ["write_file", "delete_file"]),
            new MCPServer("database", "/path/to/db-server")
        ];
    }
    
    public TornadoAgent CreateAgent(bool isAdmin = false)
    {
        var servers = isAdmin ? 
            _baseServers.Concat(_adminServers).ToList() : 
            _baseServers;
            
        return new TornadoAgent(
            _client,
            ChatModel.OpenAi.Gpt41.V41Mini,
            isAdmin ? "You have administrative access to all systems." : "You have read-only access.",
            mcpServers: servers
        );
    }
}
```

### Error Handling with MCP

Handle MCP server errors gracefully:

```csharp
ValueTask HandleMcpErrors(AgentRunnerEvents runEvent)
{
    if (runEvent.EventType == AgentRunnerEventTypes.ToolCallResult &&
        runEvent is AgentRunnerToolCallResultEvent resultEvent)
    {
        // Check for MCP tool errors
        if (agent.McpTools.ContainsKey(resultEvent.ToolResult.ToolCall.Name))
        {
            var result = resultEvent.ToolResult.Result;
            
            // Check for common error patterns
            if (result.Contains("Error:") || result.Contains("Failed:"))
            {
                Console.WriteLine($"⚠️ MCP tool error: {resultEvent.ToolResult.ToolCall.Name}");
                Console.WriteLine($"   Error: {result}");
                
                // Could implement retry logic or fallback here
            }
        }
    }
    return ValueTask.CompletedTask;
}
```

## Building Custom MCP Servers

While not part of LlmTornado.Agents, you can build custom MCP servers that integrate with your agents:

### Basic MCP Server Structure

```javascript
// Example Node.js MCP server
const { McpServer } = require('@modelcontextprotocol/server');

const server = new McpServer({
    name: 'custom-tools',
    version: '1.0.0'
});

// Register tools
server.registerTool({
    name: 'custom_calculation',
    description: 'Perform custom calculations',
    parameters: {
        type: 'object',
        properties: {
            operation: { type: 'string', description: 'The operation to perform' },
            values: { type: 'array', items: { type: 'number' }, description: 'Input values' }
        },
        required: ['operation', 'values']
    }
}, async (params) => {
    // Implementation here
    return `Calculation result: ${result}`;
});

server.start();
```

### Integrating Custom Server

```csharp
// Use your custom MCP server
var customServer = new MCPServer(
    serverLabel: "custom-tools",
    serverUrl: "/path/to/your/custom-server"
);

TornadoAgent agent = new TornadoAgent(
    client,
    model,
    instructions: "You have access to custom calculation tools.",
    mcpServers: [customServer]
);
```

## Best Practices

### Security Considerations

1. **Tool Allowlisting**: Always specify `allowedTools` to limit access
2. **Server Validation**: Verify MCP server endpoints and certificates
3. **Input Sanitization**: Ensure MCP servers validate inputs properly
4. **Access Control**: Use conditional server access for sensitive operations

```csharp
// Secure MCP configuration
var secureServer = new MCPServer(
    serverLabel: "secure-operations",
    serverUrl: "https://secure.example.com/mcp",
    allowedTools: ["safe_operation_1", "safe_operation_2"] // Explicitly allow only safe tools
);
```

### Performance Optimization

1. **Server Caching**: Cache MCP server connections when possible
2. **Tool Batching**: Group related operations when supported
3. **Timeout Handling**: Implement timeouts for slow MCP operations
4. **Connection Pooling**: Reuse connections for remote servers

### Error Recovery

```csharp
public async Task<string> ExecuteWithRetry(TornadoAgent agent, string prompt, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var result = await agent.RunAsync(prompt);
            return result.Messages.Last().Content;
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

## Troubleshooting

### Common Issues

**MCP Server Connection Failed**
```
Error: Unable to connect to MCP server
```
Solution: Check server URL, ensure server is running, verify network connectivity.

**Tool Not Found**
```
Error: I don't have a tool called 'tool_name'
```
Solution: Check `allowedTools` list, verify tool is available on the server.

**Tool Execution Timeout**
```
Error: Tool execution timed out
```
Solution: Implement timeout handling, consider asynchronous operations.

### Debugging MCP Integration

```csharp
// Enable detailed MCP logging
ValueTask DebugMcpEvents(AgentRunnerEvents runEvent)
{
    Console.WriteLine($"[DEBUG] Event: {runEvent.EventType}");
    
    if (runEvent is AgentRunnerToolCallEvent toolCall)
    {
        Console.WriteLine($"[DEBUG] Tool: {toolCall.ToolCall.Name}");
        Console.WriteLine($"[DEBUG] Args: {toolCall.ToolCall.Arguments}");
    }
    
    return ValueTask.CompletedTask;
}
```

## Next Steps

- Learn about [Chat Runtime](chat-runtime.md) for complex workflows
- Explore [Orchestration](orchestration.md) for state machine patterns
- Check [Examples](examples/) for complete MCP implementations
- Review [Best Practices](best-practices.md) for production deployments