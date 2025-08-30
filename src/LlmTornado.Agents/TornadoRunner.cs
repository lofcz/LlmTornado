using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Agents;

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
    /// <param name="messagesToAppend"> Input messages to add to response</param>
    /// <param name="streaming">Enable streaming</param>
    /// <param name="runnerCallback">delegate to send event information </param>
    /// <param name="responseId">Previous Response ID from response API</param>
    /// <param name="cancellationToken">Cancellation token to cancel the run</param>
    /// <param name="toolPermissionHandle">Delegate to request tool permission from user</param>
    /// <returns>Result of the run</returns>
    /// <exception cref="GuardRailTriggerException">Triggers when Guardrail detects bad input</exception>
    /// <exception cref="Exception"> Max Turns Reached or Error</exception>
    /// <exception cref="OperationCanceledException"></exception>
    public static async Task<Conversation> RunAsync(
        TornadoAgent agent,
        string input = "",
        GuardRailFunction? guardRail = null,
        bool singleTurn = false,
        int maxTurns = 10,
        List<ChatMessage>? messagesToAppend = null,
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
        string responseId = "",
        Func<string, ValueTask<bool>>? toolPermissionHandle = null,
        CancellationToken cancellationToken = default
    )
    {
        Conversation conversation = SetupConversation(agent, input, messagesToAppend, responseId, cancellationToken);
        //Check if the input triggers a guardrail to stop the agent from continuing
        await CheckInputGuardrail(input, guardRail);

        return await RunAgentLoop(conversation, agent, singleTurn, maxTurns, runnerCallback, streaming, responseId, cancellationToken, toolPermissionHandle);
    }

    /// <summary>
    /// Invoke the agent loop to begin async without a agent defined
    /// </summary>
    /// <param name="api">Client with api key</param>
    /// <param name="model">Model to use</param> 
    /// <param name="input">Message to the Agent</param>
    /// <param name="guardRail">Input Guardrail To perform</param>
    /// <param name="singleTurn">Set loop to not loop</param>
    /// <param name="maxTurns">Max loops to perform</param>
    /// <param name="messagesToAppend"> Input messages to add to response</param>
    /// <param name="streaming">Enable streaming</param>
    /// <param name="runnerCallback">delegate to send event information </param>
    /// <param name="responseId">Previous Response ID from response API</param>
    /// <param name="cancellationToken">Cancellation token to cancel the run</param>
    /// <param name="toolPermissionHandle">Delegate to request tool permission from user</param>
    /// <returns>Result of the run</returns>
    /// <exception cref="GuardRailTriggerException">Triggers when Guardrail detects bad input</exception>
    /// <exception cref="Exception"> Max Turns Reached or Error</exception>
    /// <exception cref="OperationCanceledException"></exception>
    public static async Task<Conversation> RunAsync(
        TornadoApi api,
        ChatModel model,
        ChatRequest options,
        string input = "",
        string instructions = "You are a useful assistant",
        GuardRailFunction? guardRail = null,
        bool singleTurn = false,
        int maxTurns = 10,
        List<ChatMessage>? messagesToAppend = null,
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
        string responseId = "",
        Func<string, ValueTask<bool>>? toolPermissionHandle = null,
        CancellationToken cancellationToken = default
    )
    {
        TornadoAgent agent = new TornadoAgent(api, model, instructions) { Options = options };

        return await RunAsync(agent, input, guardRail, singleTurn, maxTurns, messagesToAppend, runnerCallback, streaming, responseId, toolPermissionHandle, cancellationToken);
    }

    /// <summary>
    /// Main Agent Loop running the functions calls 
    /// </summary>
    /// <param name="chat"></param>
    /// <param name="agent"></param>
    /// <param name="singleTurn"></param>
    /// <param name="maxTurns"></param>
    /// <param name="runnerCallback"></param>
    /// <param name="streaming"></param>
    /// <param name="responseId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="toolPermissionRequest"></param>
    /// <returns></returns>
    private static async Task<Conversation> RunAgentLoop(
        Conversation chat,
        TornadoAgent agent,
        bool singleTurn = false,
        int maxTurns = 10,
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
        string responseId = "",
        CancellationToken cancellationToken = default,
        Func<string, ValueTask<bool>>? toolPermissionRequest = null
    )
    {
        runnerCallback?.Invoke(new AgentRunnerStartedEvent());
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

        runnerCallback?.Invoke(new AgentRunnerCompletedEvent(chat));
        return chat;
    }

    // [consideration] Feeling very off about using setup here or maintaining the conversation within the agent class
    // Depends if we want to keep the agent stateless or not
    /// <param name="agent"></param>
    /// <param name="input"></param>
    /// <param name="messages"></param>
    /// <param name="responseId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    private static async Task CheckInputGuardrail(string input, GuardRailFunction? guardRail, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null)
    {
        if (guardRail != null)
        {
            GuardRailFunctionOutput? guard_railResult = (GuardRailFunctionOutput?)(await AsyncHelpers.InvokeValueTaskFuncAsync(guardRail, [input]));

            if (guard_railResult != null && guard_railResult.TripwireTriggered)
            {
                runnerCallback?.Invoke(new AgentRunnerGuardrailTriggeredEvent($"Input Guardrail Stopped the agent from continuing because, {guard_railResult.OutputInfo}"));
                GuardRailTriggerException triggerException = new GuardRailTriggerException($"Input Guardrail Stopped the agent from continuing because, {guard_railResult.OutputInfo}");
                runnerCallback?.Invoke(new AgentRunnerErrorEvent(triggerException.Message, triggerException));
                throw triggerException;
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

    private static bool GotToolCall(Conversation chat)
    {
        return chat.Messages.Last() is { Role: ChatMessageRoles.Tool };
    }

    /// <summary>
    /// Handle Tool calls from Agent loop
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="toolCall"></param>
    /// <param name="toolPermissionHandle">Request tool permission handle</param>
    /// <returns></returns>
    private static async Task<FunctionResult> HandleToolCall(TornadoAgent agent, FunctionCall toolCall, Func<string, ValueTask<bool>>? toolPermissionHandle = null)
    {
        bool permissionGranted = true;
        FunctionResult functionResult = new FunctionResult(toolCall, "No Result", FunctionResultSetContentModes.Passthrough);

        if (toolPermissionHandle != null)
        {
            if (toolPermissionHandle?.GetInvocationList().Length > 0 && agent.ToolPermissionRequired[toolCall.Name])
            {
                //If tool permission is required, ask user for permission
                permissionGranted = toolPermissionHandle.Invoke($"Do you want to allow the agent to use the tool: {toolCall.Name}?").Result;
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
    /// <param name="agent">Agent to respond</param>
    /// <param name="chat">Current Conversation</param>
    /// <param name="Streaming">Should we stream the response</param>
    /// <param name="runnerCallback">Callback events</param>
    /// <param name="toolPermissionRequest">Request Tool permissino</param>
    /// <returns></returns>
    private static async Task<Conversation> GetNewResponse(
        TornadoAgent agent, 
        Conversation chat, 
        bool Streaming = false, 
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, 
        Func<string, ValueTask<bool>>? toolPermissionRequest = null)
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
                    fn.Result = await HandleToolCall(agent, fn, toolPermissionRequest); //[consideration]I could go parallel here but not sure if its worth the complexity
                    runnerCallback?.Invoke(new AgentRunnerToolCompletedEvent(fn));
                }
            });

            if(response.Exception != null)
            {
                runnerCallback?.Invoke(new AgentRunnerErrorEvent(response.Exception.Message, response.Exception));
                return chat;
            }

            if (response != null && response.Exception == null)
            {
                runnerCallback?.Invoke(new AgentRunnerUsageReceivedEvent(response.Data.Usage.TotalTokens));
            }
        }
        catch (Exception ex)
        {
            runnerCallback?.Invoke(new AgentRunnerErrorEvent(ex.Message,ex));
        }

        return chat;
    }

    //[consideration] Need to massively improve this to handle all the streaming events
    private static async Task<Conversation> HandleStreaming(TornadoAgent agent, Conversation chat, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, Func<string, ValueTask<bool>>? toolPermissionRequest = null)
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
                        fn.Result = await HandleToolCall(agent, fn, toolPermissionRequest); //I could go parallel here but not sure if its worth the complexity
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
                runnerCallback?.Invoke(new AgentRunnerUsageReceivedEvent(usage.TotalTokens));
                return Threading.ValueTaskCompleted;
            }
        });
           
        return chat;
    }
}