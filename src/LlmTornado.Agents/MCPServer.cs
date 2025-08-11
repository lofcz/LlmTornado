using ModelContextProtocol.Client;


namespace LlmTornado.Agents;

public class MCPServer
{
    public string ServerLabel { get; set; }
    public string ServerUrl { get; set; }
    public string[]? DisableTools { get; set; }
    public List<McpClientTool> Tools { get; set; } = [];

    public Dictionary<string, McpClientTool> mcp_tools = new Dictionary<string, McpClientTool>();
    public IMcpClient? McpClient { get; set; }

    public MCPServer( string serverLabel, string serverUrl,  string[]? disableTools = null)
    {
        ServerLabel = serverLabel;
        ServerUrl = serverUrl;
        DisableTools = disableTools;
        Task.Run(async () => Tools = await AsToolkit(this)).Wait();
    }

    private async Task<bool> TryGetMcpClient()
    {
        try
        {
            if (!this.ServerUrl.StartsWith("http"))
            {
                (string command, string[] arguments) = GetCommandAndArguments([this.ServerUrl]);
                // Create MCP client to connect to the server
                McpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = this.ServerLabel,
                    Command = command,
                    Arguments = arguments,
                }));
            }
            else
            {
                SseClientTransport sseClientTransport = new SseClientTransport(new SseClientTransportOptions
                {
                    Name = this.ServerLabel,
                    Endpoint = new Uri(this.ServerUrl)
                });
                McpClient = await McpClientFactory.CreateAsync(sseClientTransport);
            }

            // Ping the server to ensure it's reachable
            await McpClient.PingAsync();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// Helper method to attempt to create and return an MCP client for the given server.
    /// </summary>
    /// <param name="server">Server you wish to get tools from</param>
    /// <returns></returns>
    public static async Task<IMcpClient>? TryGetMcpClient(string serverUrl, string serverLabel)
    {
        IMcpClient? mcpClient = null;
        try
        {
            if (!serverUrl.StartsWith("http"))
            {
                (string command, string[] arguments) = GetCommandAndArguments([serverUrl]);
                // Create MCP client to connect to the server
                mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = serverLabel,
                    Command = command,
                    Arguments = arguments,
                }));
            }
            else
            {
                SseClientTransport sseClientTransport = new SseClientTransport(new SseClientTransportOptions
                {
                    Name = serverLabel,
                    Endpoint = new Uri(serverUrl)
                });
                mcpClient = await McpClientFactory.CreateAsync(sseClientTransport);
            }

            // Ping the server to ensure it's reachable
            await mcpClient.PingAsync();

            return mcpClient;
        }
        catch (Exception ex)
        {
            return mcpClient;
        }
    }

    public async Task<List<McpClientTool>> AsToolkit(MCPServer server)
    {
        List<McpClientTool> result = new List<McpClientTool>();

        if(!(await TryGetMcpClient()))
        {
            // If we cannot connect to the server, return an empty list
            return result;
        }

        if (McpClient != null)
        {
            // If the server URL is an HTTP endpoint, we can use the MCP client factory directly
            IList<McpClientTool> tools = await McpClient.ListToolsAsync();
            foreach (McpClientTool tool in tools)
            {
                if (server.DisableTools != null)
                {
                    if (server.DisableTools.Contains(tool.Name)) continue; // Skip tools not in the allowed list
                }
                result.Add(tool);
                mcp_tools.Add(tool.Name, tool);
            }
        }

        return result;
    }
    
    public static (string command, string[] arguments) GetCommandAndArguments(string[] args)
    {
        return args switch
        {
            { Length: 1 } when args[0].EndsWith(".py") => ($"{Path.GetDirectoryName(args[0])}\\.venv\\Scripts\\python.exe", args),
            { Length: 1 } when args[0].EndsWith(".js") => ("node", args),
            { Length: 1 } when (Directory.Exists(args[0]) || (File.Exists(args[0]) && args[0].EndsWith(".csproj"))) => ("dotnet", new[] { "run", "--project", args[0] }),
            _ => ("echo", ["Failed to Get Project"])
        };
    }

}