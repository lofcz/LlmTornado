using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses.Events;
using System.Drawing.Imaging;
using System.Net;
using System.Text.Json;

namespace LlmTornado.Agents
{
    public delegate void TornadoStreamingCallbacks(IResponseEvent streamingResult);
    /// <summary>
    /// <c>Runner</c> to run the agent loop
    /// </summary>
    public class TornadoRunner
    {
        public delegate void RunnerVerboseCallbacks(string runnerAction);
        public delegate bool ToolPermissionRequest(string message);
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
            CancellationTokenSource? cancellationToken = default,
            ToolPermissionRequest? toolPermissionRequest = null
            )
        {
            Conversation chat;
            if (agent.Options != null)
            {
                
                chat = agent.Client.Chat.CreateConversation(agent.Options);
            }
            else
            {
                chat = agent.Client.Chat.CreateConversation(agent.Model);
            }


            if (agent.ResponseOptions != null)
            {
                chat.RequestParameters.ResponseRequestParameters = agent.ResponseOptions;
            }
            
            chat.AddSystemMessage(agent.Instructions);

            if (cancellationToken != null)
            {
                //Set the cancellation token for the agent client
                chat.RequestParameters.CancellationToken = cancellationToken.Token;
            }

            //Setup the messages from previous runs or memory
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    chat.AppendMessage(message);
                }
            }

            //Set response id
            if (!string.IsNullOrEmpty(responseId))
            {
                if(chat.RequestParameters.ResponseRequestParameters != null)
                {
                    chat.RequestParameters.ResponseRequestParameters!.PreviousResponseId = responseId;
                }
            }

            //Add the latest message to the stream
            if (!string.IsNullOrEmpty(input.Trim()))
            {
                chat.AppendUserInput(input);
            }

            //Check if the input triggers a guardrail to stop the agent from continuing
            if (guardRail != null)
            {
                var guard_railResult = await (Task<GuardRailFunctionOutput>)guardRail.DynamicInvoke([input])!;
                if (guard_railResult != null)
                {
                    GuardRailFunctionOutput grfOutput = guard_railResult;
                    if (grfOutput.TripwireTriggered) throw new GuardRailTriggerException($"Input Guardrail Stopped the agent from continuing because, {grfOutput.OutputInfo}");
                }
                else
                {
                    throw new Exception($"GuardRail Failed To Run");
                }
            }

            //Agent loop
            int currentTurn = 0;
            ProcessResult processResult;
            try
            {
                do
                {
                    CheckForCancellation(cancellationToken);

                    if (currentTurn >= maxTurns) throw new Exception("Max Turns Reached");

                    chat = await GetNewResponse(agent, chat, streaming, streamingCallback, verboseCallback) ?? chat;

                    currentTurn++;
                    processResult = await ProcessOutputItems(agent, chat, verboseCallback, toolPermissionRequest);
                    chat = processResult.Chat;
                } while (processResult.RequiresAction && !singleTurn);
            }
            catch (Exception ex)
            {
                verboseCallback?.Invoke($"Exception during agent run: {ex.Message}");
            }

            //Add output guardrail eventually

            return chat;
        }


        private static void CheckForCancellation(CancellationTokenSource? cancellationToken)
        {
            if (cancellationToken != null && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Operation was cancelled by user.");
            }
        }

        public class ProcessResult
        {
            public bool RequiresAction { get; set; }
            public Conversation Chat { get; set; }
            public ProcessResult(bool requiresAction, Conversation chat)
            {
                RequiresAction = requiresAction;
                Chat = chat;
            }
        }

        /// <summary>
        /// Add output to messages and handle function and tool calls
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="runResult"></param>
        /// <param name="callback"></param>
        /// <param name="computerUseCallback"></param>
        /// <param name="toolPermissionRequest"
        /// <returns></returns>
        private static async Task<ProcessResult> ProcessOutputItems(TornadoAgent agent,  Conversation chat, RunnerVerboseCallbacks? callback, ToolPermissionRequest? toolPermissionRequest)
        {
            bool requiresAction = false;
            var lastResult = chat.Messages.Last();
            if(lastResult == null) 
            { 
                return new ProcessResult(requiresAction, chat);
            }
            if(lastResult.ToolCalls != null)
            {
                foreach (var call in lastResult.ToolCalls)
                {
                    if (call.FunctionCall != null)
                    {
                        bool permissionGranted = true;
                        if (toolPermissionRequest != null)
                        {
                            if (toolPermissionRequest?.GetInvocationList().Length > 0 && agent.ToolPermissionRequired[call.FunctionCall.Name])
                            {
                                //If tool permission is required, ask user for permission
                                permissionGranted = toolPermissionRequest.Invoke($"Do you want to allow the agent to use the tool: {call.FunctionCall.Name}?");
                            }
                        }

                        if (permissionGranted)
                        {
                            var toolOutput = await HandleToolCall(agent, call.FunctionCall);
                            ChatMessage chatMessage = new ChatMessage();
                            chatMessage.Role = ChatMessageRoles.Tool;
                            chatMessage.ToolCallId = call.Id;
                            chatMessage.Content = toolOutput.Content;

                            chat.AppendMessage(chatMessage);

                            requiresAction = true;
                        }
                        else
                        {
                            var toolOutput = await HandleToolCall(agent, call.FunctionCall);
                            ChatMessage chatMessage = new ChatMessage();
                            chatMessage.Role = ChatMessageRoles.Tool;
                            chatMessage.ToolCallId = call.Id;
                            chatMessage.Content = "Tool Permission was not granted by user";

                            chat.AppendMessage(chatMessage);

                            requiresAction = true;
                        }
                    }
                }
            }
            //else if (item is ModelComputerCallItem computerCall)
            //{
            //    var computerOutput = await HandleComputerCall(computerCall, computerUseCallback);
            //    //chat.AppendMessage();  need to verify if we can get this to work
            //    requiresAction = true;
            //}
            
            return new ProcessResult(requiresAction, chat);
        }

        /// <summary>
        /// Handle Tool calls from Agent loop
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="toolCall"></param>
        /// <returns></returns>
        private static async Task<FunctionResult> HandleToolCall(TornadoAgent agent, FunctionCall toolCall)
        {
            if (agent.McpTools.ContainsKey(toolCall.Name)) return await ToolRunner.CallMcpToolAsync(agent, toolCall);
            return agent.AgentTools.ContainsKey(toolCall.Name) ? await ToolRunner.CallAgentToolAsync(agent, toolCall) : await ToolRunner.CallFuncToolAsync(agent, toolCall);
        }

        /// <summary>
        /// Handle verbose responses from Running
        /// </summary>
        /// <param name="item"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private static async Task HandleVerboseCallback(ChatMessage item, RunnerVerboseCallbacks? callback = null)
        {
           //handle verbose responses
        }


        /// <summary>
        /// Get response from the model or If Error delete last message in thread and retry (max agent loops will cap)
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="messages"></param>
        /// <param name="Streaming"></param>
        /// <param name="streamingCallback"></param>
        /// <returns></returns>
        public static async Task<Conversation>? GetNewResponse(TornadoAgent agent, Conversation chat, bool Streaming = false, StreamingCallbacks? streamingCallback = null, RunnerVerboseCallbacks? verboseCallback = null)
        {
            try
            {
                if (Streaming && streamingCallback != null)
                {
                    return await HandleStreaming(agent, chat, streamingCallback);
                }
                RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe();
                return chat;
            }
            catch (Exception ex)
            {
                verboseCallback?.Invoke(ex.ToString());
                verboseCallback?.Invoke("Removing Last Message thread");
                //RemoveLastMessageThread(messages); //Removed due to parallel tool calling moving input to InputItems so messages has no bearing on input
            }

            return null;
        }


        public static async Task<Conversation> HandleStreaming(TornadoAgent agent, Conversation chat, StreamingCallbacks? streamingCallback = null)
        {
            //Create Open response
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenExHandler = (exText) =>
                {
                    //Call the streaming callback for text
                    return ValueTask.CompletedTask;
                },
                ImageTokenHandler = (image) =>
                {
                    //Call the streaming callback for image
                    return ValueTask.CompletedTask;
                },
                MessageTokenHandler = (text) =>
                {
                    streamingCallback?.Invoke(new ModelStreamingOutputTextDeltaEvent(1, 1, 1, text));
                    return ValueTask.CompletedTask;
                },
                ReasoningTokenHandler = (reasoning) =>
                {
                    return ValueTask.CompletedTask;
                },
                BlockFinishedHandler = (message) =>
                {
                    //Call the streaming callback for completion
                    streamingCallback?.Invoke(new ModelStreamingCompletedEvent(1, message.Id.ToString()));
                    return ValueTask.CompletedTask;
                },
                MessagePartHandler = (part) =>
                {
                    if (part.Type == ChatMessageTypes.Text)
                    {
                    }
                    return ValueTask.CompletedTask;
                },
                FunctionCallHandler = (toolCall) =>
                {
                    foreach (FunctionCall call in toolCall)
                    {
                        //Add the tool call to the response output
                        foreach (FunctionCall fn in toolCall)
                        {
                            Task.Run(async () => fn.Result = await HandleToolCall(agent, fn)).Wait();
                        }
                    }
                    return ValueTask.CompletedTask;
                },
                MessageTypeResolvedHandler = (messageType) =>
                {
                    return ValueTask.CompletedTask;
                },
                MutateChatRequestHandler = (request) =>
                {
                    streamingCallback?.Invoke(new ModelStreamingCreatedEvent(1));
                    //Mutate the request if needed
                    return ValueTask.FromResult(request);
                },
                HttpExceptionHandler = (exception) =>
                {
                    //Handle any exceptions that occur during streaming
                    return ValueTask.CompletedTask;
                },
                OnUsageReceived = (usage) =>
                {
                    return ValueTask.CompletedTask;
                }
            });
           
            return chat;
        }
    }
}
