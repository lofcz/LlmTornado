using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;

namespace LlmTornado.Agents
{
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
            List<object> arguments = new();

            if (!agent.ToolList.TryGetValue(call.Name, out FunctionTool? tool))
                throw new Exception($"I don't have a tool called {call.Name}");

            //Need to check if function has required parameters and if so, parse them from the call.FunctionArguments
            if (call.Arguments != null)
            {
                arguments = tool.Function.ParseFunctionCallArgs(BinaryData.FromString(call.Arguments)) ?? new();
            }

            string? result = (string?)await CallFuncAsync(tool.Function, [.. arguments]);

            return new FunctionResult(call,result);
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
                else
                {
                    return new FunctionResult(call, "Error Could not deserialize json argument Input from last function call");
                }
            }

            return new FunctionResult(call, "Error");
        }
        public static async Task<FunctionResult> CallMcpToolAsync(TornadoAgent agent, FunctionCall call)
        {
            List<object> arguments = new();

            if (!agent.McpTools.TryGetValue(call.Name, out MCPServer? server))
                throw new Exception($"I don't have a tool called {call.Name}");

            CallToolResult _result;
            //Need to check if function has required parameters and if so, parse them from the call.FunctionArguments
            if (call.Arguments != null)
            {
                if (!JsonUtility.IsValidJson(call.Arguments.ToString()))
                    throw new System.Text.Json.JsonException($"Function arguments for {call.Name} are not valid JSON");

                var json = call.Arguments.ToString();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json);
                _result = await server.McpClient!.CallToolAsync(call.Name, dict);
            }
            else
            {
                _result = await server.McpClient!.CallToolAsync(call.Name);
            }

            if (_result is CallToolResult callToolResult)
            {
                string? result = "";

                if (callToolResult.Content.Count > 0)
                {
                    ContentBlock firstBlock = callToolResult.Content[0];

                    switch (firstBlock)
                    {
                        case TextContentBlock textBlock:
                            {
                                result = textBlock.Text;
                                break;
                            }
                        case ImageContentBlock imageBlock:
                            {
                                result = imageBlock.Data;
                                break;
                            }
                        case AudioContentBlock audioBlock:
                            {
                                result = audioBlock.Data;
                                break;
                            }
                        case EmbeddedResourceBlock embeddedResourceBlock:
                            {
                                result = embeddedResourceBlock.Resource.Uri;
                                break;
                            }
                        case ResourceLinkBlock resourceLinkBlock:
                            {
                                result = resourceLinkBlock.Uri;
                                break;
                            }
                    }
                }
                return new FunctionResult(call, result);
            }


            return new FunctionResult(call, "Error");
        }
        /// <summary>
        /// Handles the actual Method Invoke async/syncr and returns the result
        /// </summary>
        /// <param name="function"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static async Task<object?> CallFuncAsync(Delegate function, object[] args)
        {
            object? result;
            MethodInfo method = function.Method;
            if (AsyncHelpers.IsGenericTask(method.ReturnType, out Type taskResultType))
            {
                // Method is async, invoke and await
                var task = (Task)function.DynamicInvoke(args);
                await task.ConfigureAwait(false);
                // Get the Result property from the Task
                result = taskResultType.GetProperty("Result").GetValue(task);
            }
            else
            {
                // Method is synchronous
                result = function.DynamicInvoke( args);
            }

            return result ?? null;
        }

    }
}
