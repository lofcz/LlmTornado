using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Mcp;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Agents;

/// <summary>
/// Class to Invoke the tools during run
/// </summary>
public static class ToolRunner
{
    /// <summary>
    /// Invoke function from FunctionCallItem and return FunctionOutputItem
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="call"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<FunctionResult> CallFuncToolAsync(TornadoAgent agent, FunctionCall call)
    {
        if (!agent.ToolList.TryGetValue(call.Name, out Common.Tool tool))
            throw new Exception($"I don't have a tool called {call.Name}");

        //Need to check if function has required parameters and if so, parse them from the call.FunctionArguments
        if (call.Arguments != null && tool.Delegate != null)
        {
            object[]  arguments = tool.Delegate.ParseFunctionCallArgs(call.Arguments);

            string? result = (string?)await tool.Delegate.InvokeAsync(arguments);

            return new FunctionResult(call, result);
        }

        return new FunctionResult(call, "Error No Delegate found");
    }


    private static string GetInputFromFunctionArgs(string? args)
    {
        if (!string.IsNullOrEmpty(args))
        {
            using JsonDocument argumentsJson = JsonDocument.Parse(args!);
            if (argumentsJson.RootElement.TryGetProperty("input", out JsonElement jValue))
            {
                return jValue.GetString() ?? string.Empty;
            }
        }
        return "Error Could not deserialize json argument Input from last function call";
    }

    /// <summary>
    /// Calls the agent tool and returns the result
    /// </summary>
    /// <param name="agent">The agent invoking the tool</param>
    /// <param name="call">The function call containing the arguments</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<FunctionResult> CallAgentToolAsync(TornadoAgent agent, FunctionCall call)
    {
        if (!agent.AgentTools.TryGetValue(call.Name, out TornadoAgentTool? tool))
            throw new Exception($"I don't have a Agent tool called {call.Name}");

        string agentInput = GetInputFromFunctionArgs(call.Arguments);

        Conversation agentToolResult = await TornadoRunner.RunAsync(tool.ToolAgent, agentInput);

        return new FunctionResult(call, agentToolResult.MostRecentApiResult!.Choices?.Last().Message?.Content);
    }

    /// <summary>
    /// Calls the MCP tool and returns the result
    /// </summary>
    /// <param name="agent">The agent invoking the tool</param>
    /// <param name="call">The function call containing the arguments</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="System.Text.Json.JsonException"></exception>
    public static async Task<FunctionResult> CallMcpToolAsync(TornadoAgent agent, FunctionCall call)
    {

        if (!agent.McpTools.TryGetValue(call.Name, out MCPServer? server))
            throw new Exception($"I don't have a tool called {call.Name}");

        CallToolResult localResult;
        Dictionary<string, object?>? dict = null;

        //Need to check if function has required parameters and if so, parse them from the call.FunctionArguments
        if (call.Arguments != null)
        {
            if (!JsonUtility.IsValidJson(call.Arguments))
                throw new System.Text.Json.JsonException($"Function arguments for {call.Name} are not valid JSON");
            dict = JsonConvert.DeserializeObject<Dictionary<string, object?>>(call.Arguments);
        }

        localResult = await server.McpClient!.CallToolAsync(call.Name, dict);

        if (localResult is not { } callToolResult) return new FunctionResult(call, "Error");

        if (callToolResult.Content.Count <= 0) return new FunctionResult(call, string.Empty);

        ContentBlock firstBlock = callToolResult.Content[0];

        string result = firstBlock switch
        {
            TextContentBlock textBlock => textBlock.Text,
            ImageContentBlock imageBlock => imageBlock.Data,
            AudioContentBlock audioBlock => audioBlock.Data,
            EmbeddedResourceBlock embeddedResourceBlock => embeddedResourceBlock.Resource.Uri,
            ResourceLinkBlock resourceLinkBlock => resourceLinkBlock.Uri,
            _ => string.Empty
        };

        return new FunctionResult(call, result);
    }

   
}