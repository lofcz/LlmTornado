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
        string? input = null,
        GuardRailFunction? guardRail = null,
        bool singleTurn = false,
        int maxTurns = 10,
        List<ChatMessage>? messagesToAppend = null,
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
        string? responseId = null,
        Func<string, ValueTask<bool>>? toolPermissionHandle = null,
        TornadoRunnerOptions? runnerOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        return await RunAsyncInternal(agent, input is null ? null : [ new ChatMessagePart(input) ], guardRail, singleTurn, maxTurns, messagesToAppend, runnerCallback, streaming, responseId, toolPermissionHandle, runnerOptions, cancellationToken);
    }
    
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
        List<ChatMessagePart>? input = null,
        GuardRailFunction? guardRail = null,
        bool singleTurn = false,
        int maxTurns = 10,
        List<ChatMessage>? messagesToAppend = null,
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
        string? responseId = null,
        Func<string, ValueTask<bool>>? toolPermissionHandle = null,
        TornadoRunnerOptions? runnerOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        return await RunAsyncInternal(agent, input, guardRail, singleTurn, maxTurns, messagesToAppend, runnerCallback, streaming, responseId, toolPermissionHandle, runnerOptions, cancellationToken);
    }
    
    private static async Task<Conversation> RunAsyncInternal(
        TornadoAgent agent,
        List<ChatMessagePart>? input = null,
        GuardRailFunction? guardRail = null,
        bool singleTurn = false,
        int maxTurns = 10,
        List<ChatMessage>? messagesToAppend = null,
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null,
        bool streaming = false,
        string? responseId = null,
        Func<string, ValueTask<bool>>? toolPermissionHandle = null,
        TornadoRunnerOptions? runnerOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        agent.Running = true;
        agent.Cancelled = false;

        try
        {
            runnerOptions ??= new TornadoRunnerOptions();
            Conversation conversation = SetupConversation(agent, input, messagesToAppend, responseId, cancellationToken);
        
            // check if the input triggers a guardrail to stop the agent from continuing
            await CheckInputGuardrail(conversation, input, guardRail);

            return await RunAgentLoop(conversation, agent, singleTurn, maxTurns, runnerCallback, streaming, responseId, toolPermissionHandle, runnerOptions, cancellationToken);
        }
        finally
        {
            agent.Running = false;
            agent.Cancelled = false;
        }
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
        TornadoRunnerOptions? runnerOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        TornadoAgent agent = new TornadoAgent(api, model, instructions) { Options = options };

        return await RunAsync(agent, input, guardRail, singleTurn, maxTurns, messagesToAppend, runnerCallback, streaming, responseId, toolPermissionHandle, runnerOptions: runnerOptions, cancellationToken: cancellationToken);
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
        string? responseId = null,
        Func<string, ValueTask<bool>>? toolPermissionRequest = null,
        TornadoRunnerOptions? runnerOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        if (runnerCallback is not null)
        {
            await runnerCallback.Invoke(new AgentRunnerStartedEvent(chat));   
        }
        
        // agentic loop
        int turns = 0;
        
        try
        {
            do
            {
                if (await CheckForCancellation(agent, chat, runnerCallback, runnerOptions, cancellationToken))
                {
                    break;
                }

                if (await CheckForMaxTurns(chat, turns, maxTurns, runnerCallback, runnerOptions))
                {
                    break;
                }

                if (await CheckForMaxTokens(chat, chat.Messages.Sum(TokenEstimator.EstimateTokens), runnerCallback, runnerOptions))
                {
                    break;
                }

                turns++;
                chat = await GetNewResponse(agent, chat, streaming, runnerCallback, toolPermissionRequest);
            } while (GotToolCall(chat) && !singleTurn);
        }
        catch (Exception ex)
        {
            if (runnerCallback is not null)
            {
                await runnerCallback.Invoke(new AgentRunnerErrorEvent(ex.Message, chat, ex));
            }
            else
            {
                throw;
            }
        }

        if (runnerCallback is not null)
        {
            await runnerCallback.Invoke(new AgentRunnerCompletedEvent(chat));    
        }
        
        return chat;
    }

    // [consideration] Feeling very off about using setup here or maintaining the conversation within the agent class
    // Depends on if we want to keep the agent stateless or not
    /// <param name="agent"></param>
    /// <param name="input"></param>
    /// <param name="messages"></param>
    /// <param name="responseId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static Conversation SetupConversation(TornadoAgent agent, List<ChatMessagePart>? input = null, List<ChatMessage>? messages = null, string? responseId = null, CancellationToken cancellationToken = default)
    {
        Conversation chat = agent.Client.Chat.CreateConversation(agent.Options);

        //Set response id
        if (!string.IsNullOrEmpty(responseId))
        {
            chat.RequestParameters.ResponseRequestParameters!.PreviousResponseId = responseId;
        }
        else
        {
            chat.AddSystemMessage(agent.Instructions); //Set the instructions for the agent
        }


        //Set the cancellation token for the agent client
        chat.RequestParameters.CancellationToken = cancellationToken;

        //Setup the messages from previous runs or memory
        chat = AddMessagesToConversation(chat, messages);

        
        //Add the latest message to the stream
        if (input?.Count > 0)
        {
            chat.AppendUserInput(input);
        }

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

    private static async Task CheckInputGuardrail(Conversation chat, List<ChatMessagePart>? input, GuardRailFunction? guardRail, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null)
    {
        if (guardRail != null)
        {
            GuardRailFunctionOutput? result = (GuardRailFunctionOutput?)(await AsyncHelpers.InvokeValueTaskFuncAsync(guardRail, [input]));

            if (result is { TripwireTriggered: true })
            {
                if (runnerCallback is not null)
                {
                    await runnerCallback.Invoke(new AgentRunnerGuardrailTriggeredEvent(chat, $"Input Guardrail Stopped the agent from continuing because, {result.OutputInfo}"));
                    GuardRailTriggerException triggerException = new GuardRailTriggerException($"Input Guardrail Stopped the agent from continuing because, {result.OutputInfo}");
                    await runnerCallback.Invoke(new AgentRunnerErrorEvent(triggerException.Message, chat, triggerException));
                    throw triggerException;   
                }
            }
        }
    }

    private static async ValueTask<bool> CheckForCancellation(TornadoAgent agent, Conversation chat, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, TornadoRunnerOptions? runnerOptions = null, CancellationToken cancellationToken = default)
    {
        if (agent.Cancelled)
        {
            return true;
        }
        
        if (cancellationToken.IsCancellationRequested && runnerCallback is not null)
        {
            await runnerCallback.Invoke(new AgentRunnerCancelledEvent(chat));
            OperationCanceledException ex = new OperationCanceledException("Operation was cancelled by user.");
            await runnerCallback.Invoke(new AgentRunnerErrorEvent(ex.Message, chat, ex));

            if (runnerOptions?.ThrowOnCancelled ?? false)
            {
                throw ex;
            }
            
            return true;   
        }
        
        return false;
    }

    private static async ValueTask<bool> CheckForMaxTurns(Conversation chat, int currentTurn, int maxTurns, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, TornadoRunnerOptions? runnerOptions = null)
    {
        if (currentTurn < maxTurns)
        {
            return false;
        }
        
        if (runnerCallback is not null)
        {
            Exception error = new Exception("Max Turns Reached");
            await runnerCallback.Invoke(new AgentRunnerMaxTurnsReachedEvent(chat));
            await runnerCallback.Invoke(new AgentRunnerErrorEvent(error.Message, chat, error));
                
            if (runnerOptions?.ThrowOnMaxTurnsExceeded ?? false)
            {
                throw error;
            }
        }
            
        return true;
    }

    private static async ValueTask<bool> CheckForMaxTokens(Conversation chat, int currentTokens, Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, TornadoRunnerOptions? runnerOptions = null)
    {
        if (currentTokens < runnerOptions?.TokenLimit)
        {
            return false;
        }
        
        Exception error = new Exception("Max Tokens Reached");

        if (runnerCallback is not null)
        {
            await runnerCallback.Invoke(new AgentRunnerMaxTokensReachedEvent(chat));
            await runnerCallback.Invoke(new AgentRunnerErrorEvent(error.Message, chat, error));    
        }
            
        return runnerOptions?.ThrowOnTokenLimitExceeded ?? true ? throw error : true;
    }

    private static bool GotToolCall(Conversation chat)
    {
        return CheckForChatToolCall(chat);
    }

    static ChatMessage? LastMessage(Conversation chat)
    {
        return chat.Messages.Count > 0 ? chat.Messages[^1] : null;
    }

    private static bool CheckForChatToolCall(Conversation chat)
    {
        return LastMessage(chat) is { Role: ChatMessageRoles.Tool };
    }

    private static bool CheckForResponseToolCall(Conversation chat)
    {
        ChatMessage? lastMsg = LastMessage(chat);
        List<ToolCall>? calls = lastMsg?.ToolCalls?.Where(x => x.BuiltInToolCall?.ResponseExpected ?? false).ToList();
        return calls is { Count: > 0 };
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
            if (agent.ToolPermissionRequired[toolCall.Name])
            {
                //If tool permission is required, ask user for permission
                permissionGranted = await toolPermissionHandle.Invoke($"Do you want to allow the agent to use the tool: {toolCall.Name}?");
            }
        }

        if (!permissionGranted)
        {
            //If permission is not granted, remove the tool call from the request
            functionResult = new FunctionResult(toolCall, "Tool Permission was not granted by user", FunctionResultSetContentModes.Passthrough);
        }
        else
        {
            if (agent.McpTools.ContainsKey(toolCall.Name))
            {
                functionResult = await ToolRunner.CallMcpToolAsync(agent, toolCall);
            }
            else
            {
                functionResult = agent.AgentTools.ContainsKey(toolCall.Name) ? await ToolRunner.CallAgentToolAsync(agent, toolCall) : await ToolRunner.CallFuncToolAsync(agent, toolCall);
            }
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
    /// <param name="toolPermissionRequest">Request Tool permissions</param>
    /// <returns></returns>
    private static async Task<Conversation> GetNewResponse(
        TornadoAgent agent, 
        Conversation chat, 
        bool Streaming = false, 
        Func<AgentRunnerEvents, ValueTask>? runnerCallback = null, 
        Func<string, ValueTask<bool>>? toolPermissionRequest = null,
        TornadoRunnerOptions? runnerOptions = null)
    {
        // we handle this ourselves
        chat.RequestParameters.InvokeClrToolsAutomatically = false;
        
        try
        {
            try
            {
                //dynamically set the tools for the request
                if (agent.Options.Tools != null)
                {
                    chat.RequestParameters.Tools = agent.Options.Tools;
                }

                if (agent.ResponseOptions?.Tools != null)
                {
                    chat.RequestParameters.ResponseRequestParameters?.Tools = agent.ResponseOptions.Tools;
                }
            }
            catch
            {
            }

            // Handle any tool calls from previous response
            List<ToolCall>? calls = chat.Messages.Count is 0 ? null : chat.Messages[^1].ToolCalls;
            
            if (calls is not null)
            {
                foreach (ToolCall tc in calls)
                {
                    if (tc.FunctionCall is null)
                    {
                        continue;
                    }
                    
                    FunctionCall fn = tc.FunctionCall;

                    if (runnerCallback is not null)
                    {
                        await runnerCallback.Invoke(new AgentRunnerToolInvokedEvent(fn, chat));    
                    }
                    
                    fn.Result = await HandleToolCall(agent, fn, toolPermissionRequest); //[consideration]I could go parallel here but not sure if its worth the complexity

                    if (runnerCallback is not null)
                    {
                        await runnerCallback.Invoke(new AgentRunnerToolCompletedEvent(fn, chat));
                    }
                }
            }

            if (Streaming && runnerCallback != null)
            {
                return await HandleStreaming(agent, chat, runnerCallback, toolPermissionRequest);
            }
            
            RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe(async functions =>
            {
                List<Task> tasks = [];
                
                foreach (FunctionCall fn in functions)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        if (runnerCallback is not null)
                        {
                            await runnerCallback.Invoke(new AgentRunnerToolInvokedEvent(fn, chat));
                        }
                        
                        // guard against double execution
                        fn.Result ??= await HandleToolCall(agent, fn, toolPermissionRequest);

                        if (runnerCallback is not null)
                        {
                            await runnerCallback.Invoke(new AgentRunnerToolCompletedEvent(fn, chat));
                        }
                    }));
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);   
                }
            });

            if (response.Exception != null && runnerCallback is not null)
            {
                await runnerCallback.Invoke(new AgentRunnerErrorEvent(response.Exception.Message, chat, response.Exception));
                return chat;
            }

            if (runnerCallback is not null && response is { Exception: null })
            {
                await runnerCallback.Invoke(new AgentRunnerUsageReceivedEvent(response.Data?.Usage?.PromptTokens ?? 0, response.Data?.Usage?.CompletionTokens ?? 0, response.Data?.Usage?.TotalTokens ?? 0, chat));
            }
        }
        catch (Exception ex)
        {
            if (runnerCallback is not null)
            {
                await runnerCallback.Invoke(new AgentRunnerErrorEvent(ex.Message, chat, ex));
                return chat;
            }

            throw;
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
            MessageTokenHandler = async (text) =>
            {
                if (runnerCallback is not null)
                {
                    await runnerCallback.Invoke(new AgentRunnerStreamingEvent(new ModelStreamingOutputTextDeltaEvent(1, 1, 1, text ?? string.Empty), chat));   
                }
            },
            ReasoningTokenHandler = async (reasoning) =>
            {
                if (runnerCallback is not null)
                {
                    await runnerCallback.Invoke(new AgentRunnerStreamingEvent(new ModelStreamingReasoningPartAddedEvent(1, 1, 1, reasoning.Content ?? string.Empty), chat));
                }
            },
            BlockFinishedHandler = (message) =>
            {
                //Call the streaming callback for completion
                return Threading.ValueTaskCompleted;
            },
            MessagePartHandler = (part) =>
            {
                //Need to handle other modalities here like images/audio don't have classes for them yet
                return Threading.ValueTaskCompleted;
            },
            OnResponseEvent = async (response) =>
            {
                if (runnerCallback is not null)
                {
                    await runnerCallback.Invoke(new AgentRunnerResponseApiEvent(response, chat));
                }
            },
            FunctionCallHandler = async (toolCall) =>
            {
                //Add the tool call to the response output
                foreach (FunctionCall fn in toolCall)
                {
                    if (runnerCallback is not null)
                    {
                        await runnerCallback.Invoke(new AgentRunnerToolInvokedEvent(fn, chat));
                        fn.Result ??= await HandleToolCall(agent, fn, toolPermissionRequest); //I could go parallel here but not sure if its worth the complexity
                        await runnerCallback.Invoke(new AgentRunnerToolCompletedEvent(fn, chat));   
                    }
                }
            },
            MessageTypeResolvedHandler = (messageType) =>
            {
                return Threading.ValueTaskCompleted;
            },
            MutateChatRequestHandler = async (request) =>
            {
                if (runnerCallback is not null)
                {
                    await runnerCallback.Invoke(new AgentRunnerStreamingEvent(new ModelStreamingCreatedEvent(1), chat));
                }
                
                return request;
            },
            HttpExceptionHandler = async (exception) =>
            {
                if (runnerCallback is not null)
                {
                    await runnerCallback.Invoke(new AgentRunnerErrorEvent(exception.Exception.Message, chat, exception.Exception));
                }
            },
            OnUsageReceived = async (usage) =>
            {
                if (runnerCallback is not null)
                {
                    await runnerCallback.Invoke(new AgentRunnerUsageReceivedEvent(usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens, chat));   
                }
            },
            OutboundHttpRequestHandler = (http) =>
            {
                return Threading.ValueTaskCompleted;
            },
            OnFinished = (finishedData) =>
            {
                return Threading.ValueTaskCompleted;
            }
        });

        if (runnerCallback is not null)
        {
            await runnerCallback.Invoke(new AgentRunnerStreamingEvent(new ModelStreamingCompletedEvent(1), chat));
        }
        
        return chat;
    }
}