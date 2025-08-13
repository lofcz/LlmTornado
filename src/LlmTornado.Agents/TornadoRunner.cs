using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses.Events;
using LlmTornado.StateMachines;
using System;
using System.Reflection;
using System.Threading;

namespace LlmTornado.Agents;

public delegate ValueTask RunnerVerboseCallbacks(string runnerAction);
public delegate ValueTask<bool> ToolPermissionRequest(string message);

/// <summary>
/// <c>Runner</c> to run the agent loop
/// </summary>
public class TornadoRunner
{
    /// <summary>
    /// Invoke the agent loop to begin async
    /// </summary>
    /// <param name="agent">Agent to Run</param>
    /// <param name="input">Message to the Agent</param>
    /// <param name="guardRail">Input Guardrail To perform</param>
    /// <param name="singleTurn">Set loop to not loop</param>
    /// <param name="maxTurns">Max loops to perform</param>
    /// <param name="messages"> Input messages to add to response</param>
    /// <param name="computerUseCallback">delegate to send computer actions</param>
    /// <param name="verboseCallback">delegate to send process info</param>
    /// <param name="streaming">Enable streaming</param>
    /// <param name="streamingCallback">delegate to send streaming information (Console.Write)</param>
    /// <param name="responseId">Previous Response ID from response API</param>
    /// <param name="cancellationToken">Cancellation token to cancel the run</param>
    /// <param name="toolPermissionRequest">Delegate to request tool permission from user</param>
    /// <returns>Result of the run</returns>
    /// <exception cref="GuardRailTriggerException">Triggers when Guardrail detects bad input</exception>
    /// <exception cref="Exception"></exception>
    public static async Task<Conversation> RunAsync(
        TornadoAgent agent,
        string input = "",
        GuardRailFunction? guardRail = null,
        bool singleTurn = false,
        int maxTurns = 10,
        List<ChatMessage>? messages = null,
        //ComputerActionCallbacks? computerUseCallback = null,
        RunnerVerboseCallbacks? verboseCallback = null,
        bool streaming = false,
        StreamingCallbacks? streamingCallback = null,
        string responseId = "",
        CancellationToken cancellationToken = default,
        ToolPermissionRequest? toolPermissionRequest = null
    )
    {
        Conversation chat = SetupConversation(agent, input, messages, responseId, cancellationToken);

        //Check if the input triggers a guardrail to stop the agent from continuing
        await CheckInputGuardrail(input, guardRail);

        return await RunAgentLoop(chat,agent,singleTurn,maxTurns,verboseCallback,streaming,streamingCallback,responseId, cancellationToken,toolPermissionRequest);
    }

    private static async Task<Conversation> RunAgentLoop(
        Conversation chat,
        TornadoAgent agent,
        bool singleTurn = false,
        int maxTurns = 10,
        RunnerVerboseCallbacks? verboseCallback = null,
        bool streaming = false,
        StreamingCallbacks? streamingCallback = null,
        string responseId = "",
        CancellationToken cancellationToken = default,
        ToolPermissionRequest? toolPermissionRequest = null
    )
    {
        //Agent loop
        int currentTurn = 0;
        try
        {
            do
            {
                CheckForCancellation(cancellationToken);

                if (currentTurn >= maxTurns) throw new Exception("Max Turns Reached");

                chat = await GetNewResponse(agent, chat, streaming, streamingCallback, verboseCallback, toolPermissionRequest) ?? chat;

                currentTurn++;
            } while (ProcessOutputItems(chat.Messages.Last()) && !singleTurn);
        }
        catch (Exception ex)
        {
            verboseCallback?.Invoke($"Exception during agent run: {ex.Message}");
        }

        return chat;
    }

    private static Conversation SetupConversation(TornadoAgent agent, string input, List<ChatMessage>? messages = null, string responseId = "", CancellationToken cancellationToken = default)
    {
        Conversation chat = agent.Client.Chat.CreateConversation(agent.Options);

        chat.AddSystemMessage(agent.Instructions); //Set the instructions for the agent

        //Set the cancellation token for the agent client
        chat.RequestParameters.CancellationToken = cancellationToken;

        //Setup the messages from previous runs or memory
        chat = AddMessagesToConversation(chat, messages);

        //Set response id
        if (!string.IsNullOrEmpty(responseId) && chat.RequestParameters.ResponseRequestParameters != null)
        {
            chat.RequestParameters.ResponseRequestParameters!.PreviousResponseId = responseId;
        }

        //Add the latest message to the stream
        if (!string.IsNullOrEmpty(input.Trim())) chat.AppendUserInput(input);

        return chat;
    }

    private static Conversation AddMessagesToConversation(Conversation chat, List<ChatMessage>? messages = null)
    {
        if (messages == null) return chat;

        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatMessageRoles.System) continue; //Skip system messages if any to avoid Instruction overlap
            chat.AppendMessage(message);
        }

        return chat;
    }

    private static async Task CheckInputGuardrail(string input, GuardRailFunction? guardRail)
    {
        if (guardRail != null)
        {
            GuardRailFunctionOutput? guard_railResult = (GuardRailFunctionOutput?)(await AsyncHelpers.InvokeValueTaskFuncAsync(guardRail, [input]));

            if (guard_railResult != null && guard_railResult.TripwireTriggered)
            {
                throw new GuardRailTriggerException($"Input Guardrail Stopped the agent from continuing because, {guard_railResult.OutputInfo}");
            }
        }
    }

    private static void CheckForCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Operation was cancelled by user.");
        }
    }

    /// <summary>
    /// Check for tool calls in the last message of the conversation
    /// </summary>
    /// <param name="chat"> Conversation to check last message for tool calls</param>
    /// <returns></returns>
    private static bool ProcessOutputItems(ChatMessage lastResult)
    {
        bool requiresAction = false;

        if (lastResult is { Role: ChatMessageRoles.Tool }) 
        {
            requiresAction = true;
        }

        return requiresAction;
    }

    /// <summary>
    /// Handle Tool calls from Agent loop
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="toolCall"></param>
    /// <returns></returns>
    private static async Task<FunctionResult> HandleToolCall(TornadoAgent agent, FunctionCall toolCall, ToolPermissionRequest? toolPermissionRequest = null)
    {
        bool permissionGranted = true;
        if (toolPermissionRequest != null)
        {
            if (toolPermissionRequest?.GetInvocationList().Length > 0 && agent.ToolPermissionRequired[toolCall.Name])
            {
                //If tool permission is required, ask user for permission
                permissionGranted = toolPermissionRequest.Invoke($"Do you want to allow the agent to use the tool: {toolCall.Name}?").Result;
            }
        }

        if (!permissionGranted)
        {
            //If permission is not granted, remove the tool call from the request
            return new FunctionResult(toolCall, "Tool Permission was not granted by user", FunctionResultSetContentModes.Passthrough);
        }

        if (agent.McpTools.ContainsKey(toolCall.Name)) return await ToolRunner.CallMcpToolAsync(agent, toolCall);
        return agent.AgentTools.ContainsKey(toolCall.Name) ? await ToolRunner.CallAgentToolAsync(agent, toolCall) : await ToolRunner.CallFuncToolAsync(agent, toolCall);
    }


    /// <summary>
    /// Get response from the model or If Error delete last message in thread and retry (max agent loops will cap)
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="chat"> Current Conversation</param>
    /// <param name="Streaming"></param>
    /// <param name="streamingCallback"></param>
    /// <returns></returns>
    private static async Task<Conversation> GetNewResponse(TornadoAgent agent, Conversation chat, bool Streaming = false, StreamingCallbacks? streamingCallback = null, RunnerVerboseCallbacks? verboseCallback = null, ToolPermissionRequest? toolPermissionRequest = null)
    {
        try
        {
            if (Streaming && streamingCallback != null)
            {
                return await HandleStreaming(agent, chat, streamingCallback);
            }

            RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe(async functions =>
            {
                foreach (FunctionCall fn in functions)
                {
                    fn.Result = await HandleToolCall(agent, fn, toolPermissionRequest);
                }
            });
        }
        catch (Exception ex)
        {
            verboseCallback?.Invoke(ex.ToString());
        }

        return chat;
    }


    private static async Task<Conversation> HandleStreaming(TornadoAgent agent, Conversation chat, StreamingCallbacks? streamingCallback = null, ToolPermissionRequest? toolPermissionRequest = null)
    {
        //Create Open response
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenExHandler = (exText) =>
            {
                //Call the streaming callback for text
                return Threading.ValueTaskCompleted;
            },
            MessageTokenHandler = (text) =>
            {
                streamingCallback?.Invoke(new ModelStreamingOutputTextDeltaEvent(1, 1, 1, text));
                return Threading.ValueTaskCompleted;
            },
            ReasoningTokenHandler = (reasoning) =>
            {
                return Threading.ValueTaskCompleted;
            },
            BlockFinishedHandler = (message) =>
            {
                //Call the streaming callback for completion
                streamingCallback?.Invoke(new ModelStreamingCompletedEvent(1, message.Id.ToString()));
                return Threading.ValueTaskCompleted;
            },
            MessagePartHandler = (part) =>
            {
                return Threading.ValueTaskCompleted;
            },
            FunctionCallHandler = async (toolCall) =>
            {
                foreach (FunctionCall call in toolCall)
                {
                    //Add the tool call to the response output
                    foreach (FunctionCall fn in toolCall)
                    {
                        fn.Result = await HandleToolCall(agent, fn, toolPermissionRequest);
                    }
                }
            },
            MessageTypeResolvedHandler = (messageType) =>
            {
                return Threading.ValueTaskCompleted;
            },
            MutateChatRequestHandler = (request) =>
            {
                streamingCallback?.Invoke(new ModelStreamingCreatedEvent(1));
                //Mutate the request if needed
                return Threading.FromResult(request);
            },
            HttpExceptionHandler = (exception) =>
            {
                //Handle any exceptions that occur during streaming
                return Threading.ValueTaskCompleted;
            },
            OnUsageReceived = (usage) =>
            {
                return Threading.ValueTaskCompleted;
            }
        });
           
        return chat;
    }
}