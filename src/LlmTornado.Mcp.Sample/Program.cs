using System.Text.RegularExpressions;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using ModelContextProtocol.Client;
using Newtonsoft.Json;

namespace LlmTornado.Mcp.Sample;

class Program
{
    static async Task Main(string[] args)
    {
        ApiKeys apiKeys = JsonConvert.DeserializeObject<ApiKeys>(await File.ReadAllTextAsync("apiKey.json"));
        (string command, string[] arguments) = GetCommandAndArguments(args);
        
        StdioClientTransport clientTransport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "Demo Server",
            Command = command,
            Arguments = arguments,
        });
        
        await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport);
        List<Tool> tools = await mcpClient.ListTornadoToolsAsync();
        
        TornadoApi api = new TornadoApi(LLmProviders.OpenAi, apiKeys.OpenAi);
        Conversation conversation = api.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools = tools,
            ToolChoice = OutboundToolChoice.Required
        });
        
        await conversation
            .AddSystemMessage("You are a helpful assistant")
            .AddUserMessage("What is the weather like in Dallas?")
            .GetResponseRich(async calls =>
            {
                foreach (FunctionCall call in calls)
                {
                    // retrieve arguments inferred by the model
                    double latitude = call.GetOrDefault<double>("latitude");
                    double longitude = call.GetOrDefault<double>("longitude");
                    
                    // call the tool on MCP server, pass args
                    await call.ResolveRemote(new
                    {
                        latitude = latitude,
                        longitude = longitude
                    });

                    // extract tool result and pass it back to the model
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

        // stop forcing the client to call the tool
        conversation.RequestParameters.ToolChoice = null;
        
        // stream final response
        await conversation.StreamResponse(Console.Write);
        Console.ReadKey();
    }
    
    static (string command, string[] arguments) GetCommandAndArguments(string[] args)
    {
        string serverPath = Path.GetFullPath(Path.Join("..", "..", "..", "..", "LlmTornado.Mcp.Sample.Server"));

        if (serverPath.EndsWith(".py"))
        {
            return ("python", args.Length is 0 ? [ serverPath ] : args);
        }

        if (serverPath.EndsWith(".js"))
        {
            return ("node", args.Length is 0 ? [ serverPath ] : args);
        }
        
        return args switch
        {
            [var script] when script.EndsWith(".py") => ("python", args),
            [var script] when script.EndsWith(".js") => ("node", args),
            [var script] when Directory.Exists(script) || (File.Exists(script) && script.EndsWith(".csproj")) => ("dotnet", ["run", "--project", script]),
            _ => ("dotnet", ["run", "--project", serverPath])
        };
    }
    
    class ApiKeys
    {
        public string OpenAi { get; set; }
    }
}