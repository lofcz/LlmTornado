using LlmTornado.Common;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;


namespace LlmTornado.Mcp;

/// <summary>
/// MCP Server Class for managing connections to MCP server and their tools.
/// </summary>
public class MCPServer
{
    /// <summary>
    /// Gets the label associated with the server.
    /// </summary>
    public string ServerLabel { get; private set; }
    /// <summary>
    /// Get the URL of the MCP server.
    /// </summary>
    public string ServerUrl { get; private set; }
    /// <summary>
    /// Select tools to disable from the server.
    /// </summary>
    public string[]? DisableTools { get; set; }

    /// <summary>
    /// Authentication options for the MCP server, if required.
    /// </summary>
    public ClientOAuthOptions? OAuthOptions { get; set; }

    /// <summary>
    /// Additional Headers to include in the connection to the MCP server (Authentication).
    /// </summary>
    public Dictionary<string, string>? AdditionalConnectionHeaders { get; set; } 

    /// <summary>
    /// Tools available from the MCP server.
    /// </summary>
    public List<McpClientTool> Tools { get; set; } = [];

    public McpClient? McpClient { get; set; }

    /// <summary>
    /// Setup the MCP server connection and auto-load tools.
    /// </summary>
    /// <param name="serverLabel"> Label of the MCP Server</param>
    /// <param name="serverUrl">URL of the MCP Server</param>
    /// <param name="disableTools">List of tools to not use</param>
    public MCPServer( string serverLabel, string serverUrl,  string[]? disableTools = null, Dictionary<string, string>? additionalConnectionHeaders = null, ClientOAuthOptions? oAuthOptions = null)
    {
        ServerLabel = serverLabel;
        ServerUrl = serverUrl;
        DisableTools = disableTools;
        AdditionalConnectionHeaders = additionalConnectionHeaders;
        OAuthOptions = oAuthOptions;
        Task.Run(async () => await AutoSetupToolsAsync()).Wait();
    }

    private async Task<bool> TryGetMcpClientAsync()
    {
        try
        {
            IClientTransport clientTransport;
            if (this.ServerUrl.StartsWith("http"))
            {
                clientTransport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Name = this.ServerLabel,
                    Endpoint = new Uri(this.ServerUrl),
                    AdditionalHeaders = AdditionalConnectionHeaders
                }); 

                Console.WriteLine($"Connecting to MCP server at {this.ServerUrl} via HTTP...");
            }
            else
            {
               
                (string command, string[] arguments) = GetCommandAndArguments([this.ServerUrl]);
                // Create MCP client to connect to the server

                clientTransport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = this.ServerLabel,
                    Command = command,
                    Arguments = arguments,
                });
            }

            McpClient = await McpClient.CreateAsync(clientTransport);
            // Ping the server to ensure it's reachable
            await McpClient.PingAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to MCP server at {this.ServerUrl}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Helper method to attempt to create and return an MCP client for the given server.
    /// </summary>
    /// <param name="serverUrl">Server you wish to get tools from</param>
    /// <param name="serverLabel">Label of the server</param>
    /// <returns>Returns the required IMcpClient type for the following MCP server (SSE vs Stdio).</returns>
    public static async Task<McpClient>? TryGetMcpClientAsync(string serverUrl, string serverLabel, Dictionary<string, string>? additionalConnectionHeaders = null, ClientOAuthOptions? oAuthOptions = null)
    {
        McpClient? mcpClient = null;
        try
        {
            IClientTransport clientTransport;
            if (serverUrl.StartsWith("http"))
            {
                clientTransport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Name = serverLabel,
                    Endpoint = new Uri(serverUrl),
                    AdditionalHeaders = additionalConnectionHeaders,
                    OAuth = oAuthOptions,
                });

            }
            else
            {

                (string command, string[] arguments) = GetCommandAndArguments([serverUrl]);
                // Create MCP client to connect to the server

                clientTransport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = serverLabel,
                    Command = command,
                    Arguments = arguments,
                });
            }

            mcpClient = await McpClient.CreateAsync(clientTransport);
            // Ping the server to ensure it's reachable
            await mcpClient.PingAsync();

            return mcpClient;
        }
        catch (Exception ex)
        {
            return mcpClient;
        }
    }

    private async Task AutoSetupToolsAsync()
    {
        // If we cannot connect to the server, return an empty list
        if (!(await TryGetMcpClientAsync())) return; 

        if (McpClient != null)
        {
            // If the server URL is an HTTP endpoint, we can use the MCP client factory directly
            IList<McpClientTool> tools = await McpClient.ListToolsAsync();

            foreach (McpClientTool tool in tools)
            {
                if (DisableTools != null)
                {
                    if (DisableTools.Contains(tool.Name)) continue; // Skip tools not in the allowed list
                }

                Tools.Add(tool);
            }
        }
    }

    public McpClientTool? GetToolByName(string toolName)
    {
        return Tools.DefaultIfEmpty(null).FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

    }

    internal static (string command, string[] arguments) GetCommandAndArguments(string[] args)
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