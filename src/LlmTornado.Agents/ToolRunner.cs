using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;
using LlmTornado.Code;
using LlmTornado.StateMachines;

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
        List<object> arguments = [];

        if (!agent.ToolList.TryGetValue(call.Name, out Common.Tool tool))
            throw new Exception($"I don't have a tool called {call.Name}");

        //Need to check if function has required parameters and if so, parse them from the call.FunctionArguments
        if (call.Arguments != null && tool.Delegate != null)
        {
            arguments = tool.Delegate?.ParseFunctionCallArgs(call.Arguments) ?? [];

            string? result = (string?)await CallFuncAsync(tool.Delegate, [.. arguments]);

            return new FunctionResult(call, result);
        }

        return new FunctionResult(call, "Error No Delegate found");
    }
    
    public static async Task<FunctionResult> CallAgentToolAsync(TornadoAgent agent, FunctionCall call)
    {
        if (!agent.AgentTools.TryGetValue(call.Name, out TornadoAgentTool? tool))
            throw new Exception($"I don't have a Agent tool called {call.Name}");

        TornadoAgent newAgent = tool.ToolAgent;

        if (call.Arguments != null)
        {
            using JsonDocument argumentsJson = JsonDocument.Parse(call.Arguments);

            if (argumentsJson.RootElement.TryGetProperty("input", out JsonElement jValue))
            {
                Conversation agentToolResult = await TornadoRunner.RunAsync(newAgent, jValue.GetString());
                return new FunctionResult(call, agentToolResult.MostRecentApiResult!.Choices?.Last().Message?.Content);
            }

            return new FunctionResult(call, "Error Could not deserialize json argument Input from last function call");
        }

        return new FunctionResult(call, "Error");
    }
    
    public static async Task<FunctionResult> CallMcpToolAsync(TornadoAgent agent, FunctionCall call)
    {
        if (!agent.McpTools.TryGetValue(call.Name, out MCPServer? server))
            throw new Exception($"I don't have a tool called {call.Name}");

        CallToolResult localResult;
        
        //Need to check if function has required parameters and if so, parse them from the call.FunctionArguments
        if (call.Arguments != null)
        {
            if (!JsonUtility.IsValidJson(call.Arguments))
                throw new System.Text.Json.JsonException($"Function arguments for {call.Name} are not valid JSON");

            string json = call.Arguments;
            Dictionary<string, object?>? dict = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json);
            localResult = await server.McpClient!.CallToolAsync(call.Name, dict);
        }
        else
        {
            localResult = await server.McpClient!.CallToolAsync(call.Name);
        }

        if (localResult is not { } callToolResult)
        {
            return new FunctionResult(call, "Error");
        }
        
        string result = string.Empty;

        if (callToolResult.Content.Count <= 0)
        {
            return new FunctionResult(call, result);
        }
            
        ContentBlock firstBlock = callToolResult.Content[0];

        result = firstBlock switch
        {
            TextContentBlock textBlock => textBlock.Text,
            ImageContentBlock imageBlock => imageBlock.Data,
            AudioContentBlock audioBlock => audioBlock.Data,
            EmbeddedResourceBlock embeddedResourceBlock => embeddedResourceBlock.Resource.Uri,
            ResourceLinkBlock resourceLinkBlock => resourceLinkBlock.Uri,
            _ => result
        };

        return new FunctionResult(call, result);
    }

    /// <summary>
    /// Handles the actual Method Invoke async/sync and returns the result
    /// </summary>
    /// <param name="function"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    static async Task<object?> CallFuncAsync(Delegate function, object[] args)
    {
        object? returnValue = function.DynamicInvoke(args);
        Type returnType = function.Method.ReturnType;
        object? result = null;
        if (AsyncHelpers.IsGenericTask(returnType, out _))
        {
            var task = (Task?)returnValue;
            if (task is not null)
            {
                await task.ConfigureAwait(false);
                // for Task<T> get Result off the runtime type (safer)
                var resProp = task.GetType().GetProperty("Result");
                result = resProp?.GetValue(task);
            }
        }
        else if (returnType == typeof(Task))
        {
            var task = (Task?)returnValue;
            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
        else if (AsyncHelpers.IsGenericValueTask(returnType, out _))
        {
            // boxed ValueTask<T> -> call AsTask() via reflection -> await Task<T>
            var asTask = returnType.GetMethod("AsTask")!;
            var taskObj = (Task)asTask.Invoke(returnValue!, null)!;

            await taskObj.ConfigureAwait(false);
            var resProp = taskObj.GetType().GetProperty("Result");
            result = resProp?.GetValue(taskObj);
        }
        else if (returnType == typeof(ValueTask))
        {
            // boxed ValueTask -> cast then await (or use AsTask())
            var vt = (ValueTask)returnValue!;
            await vt.ConfigureAwait(false); // or: await vt.AsTask().ConfigureAwait(false);
            result = null;
        }
        else
        {
            // synchronous
            result = returnValue;
        }
        return result;
    }
}