using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Agents;

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
    /// <param name="conversation">Conversation to use, if null a new conversation will be created</param>
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
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
        string responseId = "",
        CancellationToken cancellationToken = default,
        ToolPermissionRequest? toolPermissionRequest = null
    )
    {
        runnerCallback?.Invoke(new AgentRunnerStartedEvent());
        Conversation conversation = SetupConversation(agent, input, messages, responseId, cancellationToken);

        //Check if the input triggers a guardrail to stop the agent from continuing
        await CheckInputGuardrail(input, guardRail);

        return await RunAgentLoop(conversation, agent, singleTurn, maxTurns, runnerCallback, streaming, responseId, cancellationToken, toolPermissionRequest);
    }

    private static async Task<Conversation> RunAgentLoop(
        Conversation chat,
        TornadoAgent agent,
        bool singleTurn = false,
        int maxTurns = 10,
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
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
                CheckForCancellation(runnerCallback, cancellationToken);

                CheckForMaxTurns(currentTurn, maxTurns, runnerCallback);

                currentTurn++;

                chat = await GetNewResponse(agent, chat, streaming, runnerCallback, toolPermissionRequest) ?? chat;

            } while (GotToolCall(chat) && !singleTurn);
        }
        catch (Exception ex)
        {
            runnerCallback?.Invoke(new AgentRunnerErrorEvent(ex.Message, ex));
        }

        runnerCallback?.Invoke(new AgentRunnerCompletedEvent());
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

    private static void CheckForCancellation(Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            runnerCallback?.Invoke(new AgentRunnerCancelledEvent());
            OperationCanceledException ex = new OperationCanceledException("Operation was cancelled by user.");
            runnerCallback?.Invoke(new AgentRunnerErrorEvent(ex.Message, ex));
            throw ex;
        }
    }

    private static void CheckForMaxTurns(int currentTurn, int maxTurns, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null)
    {
        if (currentTurn >= maxTurns)
        {
            Exception error = new Exception("Max Turns Reached");
            runnerCallback?.Invoke(new AgentRunnerMaxTurnsReachedEvent());
            runnerCallback?.Invoke(new AgentRunnerErrorEvent(error.Message, error));
            throw error;
        }
    }

    /// <summary>
    /// Check for tool calls in the last message of the conversation
    /// </summary>
    /// <param name="chat"> Conversation to check last message for tool calls</param>
    /// <returns></returns>
    private static bool GotToolCall(Conversation chat)
    {
        return chat.Messages.Last() is { Role: ChatMessageRoles.Tool };
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
        FunctionResult functionResult = new FunctionResult(toolCall, "No Result", FunctionResultSetContentModes.Passthrough);

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
            functionResult = new FunctionResult(toolCall, "Tool Permission was not granted by user", FunctionResultSetContentModes.Passthrough);
        }

        if (agent.McpTools.ContainsKey(toolCall.Name)) { functionResult = await ToolRunner.CallMcpToolAsync(agent, toolCall); }
        else 
        {
            functionResult = agent.AgentTools.ContainsKey(toolCall.Name)?await ToolRunner.CallAgentToolAsync(agent, toolCall) : await ToolRunner.CallFuncToolAsync(agent, toolCall);
        }

        return functionResult;
    }


    /// <summary>
    /// Get response from the model or If Error delete last message in thread and retry (max agent loops will cap)
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="chat"> Current Conversation</param>
    /// <param name="Streaming"></param>
    /// <param name="streamingCallback"></param>
    /// <returns></returns>
    private static async Task<Conversation> GetNewResponse(TornadoAgent agent, Conversation chat, bool Streaming = false, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, ToolPermissionRequest? toolPermissionRequest = null)
    {
        try
        {
            if (Streaming && runnerCallback != null)
            {
                return await HandleStreaming(agent, chat, runnerCallback);
            }

            RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe(async functions =>
            {
                foreach (FunctionCall fn in functions)
                {
                    runnerCallback?.Invoke(new AgentRunnerToolInvokedEvent(fn));
                    fn.Result = await HandleToolCall(agent, fn, toolPermissionRequest);
                    runnerCallback?.Invoke(new AgentRunnerToolCompletedEvent(fn));
                }
            });
        }
        catch (Exception ex)
        {
            runnerCallback?.Invoke(new AgentRunnerErrorEvent(ex.Message,ex));
        }

        return chat;
    }


    private static async Task<Conversation> HandleStreaming(TornadoAgent agent, Conversation chat, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, ToolPermissionRequest? toolPermissionRequest = null)
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
                runnerCallback?.Invoke(new AgentRunnerStreamingEvent(new ModelStreamingOutputTextDeltaEvent(1, 1, 1, text)));
                return Threading.ValueTaskCompleted;
            },
            ReasoningTokenHandler = (reasoning) =>
            {
                return Threading.ValueTaskCompleted;
            },
            BlockFinishedHandler = (message) =>
            {
                //Call the streaming callback for completion
                runnerCallback?.Invoke(new AgentRunnerStreamingEvent(new ModelStreamingCompletedEvent(1, message.Id.ToString())));
                chat.AppendMessage(message);
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
                        runnerCallback?.Invoke(new AgentRunnerToolInvokedEvent(fn));
                        fn.Result = await HandleToolCall(agent, fn, toolPermissionRequest);
                        runnerCallback?.Invoke(new AgentRunnerToolCompletedEvent(fn));
                    }
                }
            },
            MessageTypeResolvedHandler = (messageType) =>
            {
                return Threading.ValueTaskCompleted;
            },
            MutateChatRequestHandler = (request) =>
            {
                runnerCallback?.Invoke(new AgentRunnerStreamingEvent(new ModelStreamingCreatedEvent(1)));
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