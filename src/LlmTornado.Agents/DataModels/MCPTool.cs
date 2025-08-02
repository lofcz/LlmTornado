using ModelContextProtocol.Client;


namespace LlmTornado.Agents.DataModels
{
    public class MCPServer
    {
        public string ServerLabel { get; set; }
        public string ServerUrl { get; set; }
        public string[]? AllowedTools { get; set; }
        public List<McpClientTool> Tools { get; set; } = new List<McpClientTool>();

        public Dictionary<string, McpClientTool> mcp_tools = new Dictionary<string, McpClientTool>();
        public IMcpClient? McpClient { get; set; }

        public MCPServer( string serverLabel, string serverUrl,  string[] allowedTools = null)
        {
            ServerLabel = serverLabel;
            ServerUrl = serverUrl;
            AllowedTools = allowedTools;
            Task.Run(async () => Tools = await AsToolkit(this)).Wait();
        }

        public async Task<List<McpClientTool>> AsToolkit(MCPServer server)
        {
            var result = new List<McpClientTool>();

            try
            {
                if (!server.ServerUrl.StartsWith("http"))
                {
                    (string command, string[] arguments) = GetCommandAndArguments([server.ServerUrl]);
                    // Create MCP client to connect to the server
                    McpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
                    {
                        Name = server.ServerLabel,
                        Command = command,
                        Arguments = arguments,
                    }));
                }
                else
                {
                    SseClientTransport sseClientTransport = new SseClientTransport(new SseClientTransportOptions
                    {
                        Name = server.ServerLabel,
                        Endpoint = new Uri(server.ServerUrl)
                    });
                    McpClient = await McpClientFactory.CreateAsync(sseClientTransport);
                }

                // Ping the server to ensure it's reachable
                await McpClient.PingAsync();
                
            }
            catch (Exception ex)
            {
                return result;
            }

            if (McpClient != null)
            {
                // If the server URL is an HTTP endpoint, we can use the MCP client factory directly
                var tools = await McpClient.ListToolsAsync();
                foreach (McpClientTool tool in tools)
                {
                    if (server.AllowedTools != null)
                    {
                        if (!server.AllowedTools.Contains(tool.Name))
                            continue; // Skip tools not in the allowed list
                    }
                    result.Add(tool);
                    mcp_tools.Add(tool.Name, tool);
                }
            }

            return result;
        }


        public (string command, string[] arguments) GetCommandAndArguments(string[] args)
        {
            return args switch
            {
                [var script] when script.EndsWith(".py") => ($"{Path.GetDirectoryName(args[0])}\\.venv\\Scripts\\python.exe", args),
                [var script] when script.EndsWith(".js") => ("node", args),
                [var script] when Directory.Exists(script) || (File.Exists(script) && script.EndsWith(".csproj")) => ("dotnet", ["run", "--project", script]),
                _ => ("echo", ["Failed to Get Project"])
            };
        }
    }


}
