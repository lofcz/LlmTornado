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
    /// Command to start the MCP server if using stdio transport.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the command when starting the MCP server if using stdio transport.
    /// </summary>
    public string[] Arguments { get; set; } = [];

    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();

    public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

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
    public MCPServer( 
        string serverLabel, 
        string serverUrl,  
        string[]? disableTools = null, 
        Dictionary<string, string>? additionalConnectionHeaders = null, 
        ClientOAuthOptions? oAuthOptions = null
        )
    {
        ServerLabel = serverLabel;
        ServerUrl = serverUrl;
        DisableTools = disableTools;
        AdditionalConnectionHeaders = additionalConnectionHeaders;
        OAuthOptions = oAuthOptions;
    }

    public MCPServer(
       string serverLabel,
       string command,
       string[]? arguments,
       string workingDirectory = "",
       Dictionary<string, string>? environmentVariables = null,
       string[]? disableTools = null
       )
    {
        ServerLabel = serverLabel;
        DisableTools = disableTools;
        Command = command;
        Arguments = arguments ?? [];
        WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory;
        EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
    }

    private async Task<bool> TryGetMcpClientAsync()
    {
        try
        {
            IClientTransport clientTransport;

            if (!string.IsNullOrEmpty(ServerUrl))
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
               if(string.IsNullOrEmpty(Command) || Arguments == null || Arguments.Length == 0)
                {
                    (Command, Arguments) = TryGetCommandAndArguments([this.ServerUrl]);
                }

                // Create MCP client to connect to the server

                clientTransport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = ServerLabel,
                    Command = Command,
                    Arguments = Arguments,
                    WorkingDirectory = WorkingDirectory,
                    EnvironmentVariables = EnvironmentVariables
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
   /// Run this to get tools for the MCP server
   /// </summary>
   /// <returns></returns>
    public async Task InitializeAsync()
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

    internal static (string command, string[] arguments) TryGetCommandAndArguments(string[] args)
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