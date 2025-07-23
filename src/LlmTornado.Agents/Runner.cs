using LlmTornado.Chat;
using LlmTornado.Responses;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
    public delegate void ComputerActionCallbacks(ComputerToolAction computerCall);
    public delegate void RunnerVerboseCallbacks(string runnerAction);
    public delegate Task ModelStreamingEvent(ModelStreamingEvents streamEvent);

    public class Runner
    {

        /// <summary>
        /// Invoke the agent loop to begin async
        /// </summary>
        /// <param name="agent">Agent to Run</param>
        /// <param name="input">Message to the Agent</param>
        /// <param name="guard_rail">Input Guardrail To perform</param>
        /// <param name="single_turn">Set loop to not loop</param>
        /// <param name="maxTurns">Max loops to perform</param>
        /// <param name="messages"> Input messages to add to response</param>
        /// <param name="computerUseCallback">delegate to send computer actions</param>
        /// <param name="verboseCallback">delegate to send process info</param>
        /// <param name="streaming">Enable streaming</param>
        /// <param name="streamingCallback">delegate to send streaming information (Console.Write)</param>
        /// <param name="responseID">Previous Response ID from response API</param>
        /// <returns>Result of the run</returns>
        /// <exception cref="GuardRailTriggerException">Triggers when Guardrail detects bad input</exception>
        /// <exception cref="Exception"></exception>
        public static async Task<RunResult> RunAsync(
            Agent agent,
            string input = "",
            GuardRailFunction? guard_rail = null,
            bool single_turn = false,
            int maxTurns = 10,
            List<ModelItem>? messages = null,
            ComputerActionCallbacks? computerUseCallback = null,
            RunnerVerboseCallbacks? verboseCallback = null,
            bool streaming = false,
            StreamingCallbacks? streamingCallback = null,
            string responseID = "",
            CancellationTokenSource? cancellationToken = default
            )
        {
            RunResult runResult = new RunResult();

            //if (cancellationToken != null)
            //{
            //    //Set the cancellation token for the agent client
            //    agent.Client.CancelTokenSource = cancellationToken;
            //}

            //Setup the messages from previous runs or memory
            if (messages != null)
            {
                runResult.Messages.AddRange(messages);
            }

            //Set response id
            if (!string.IsNullOrEmpty(responseID))
            {
                agent.Options.PreviousResponseId = responseID;
            }

            //Add the latest message to the stream
            if (!string.IsNullOrEmpty(input.Trim()))
            {
                runResult.Messages.Add(new ModelMessageItem("msg_" + Guid.NewGuid().ToString().Replace("-", "_"), "USER", [new ModelMessageRequestTextContent(input),], ModelStatus.Completed));
            }

            //Check if the input triggers a guardrail to stop the agent from continuing
            //if (guard_rail != null)
            //{
            //    var guard_railResult = await (Task<GuardRailFunctionOutput>)guard_rail.DynamicInvoke([input])!;
            //    if (guard_railResult != null)
            //    {
            //        GuardRailFunctionOutput grfOutput = guard_railResult;
            //        if (grfOutput.TripwireTriggered) throw new GuardRailTriggerException($"Input Guardrail Stopped the agent from continuing because, {grfOutput.OutputInfo}");
            //    }
            //    else
            //    {
            //        throw new Exception($"GuardRail Failed To Run");
            //    }
            //}

            //Agent loop
            int currentTurn = 0;
            runResult.Response.OutputItems = new List<ModelItem>();

            try
            {
                do
                {
                    CheckForCancellation(cancellationToken);

                    if (currentTurn >= maxTurns) throw new Exception("Max Turns Reached");

                    runResult.Response = await _get_new_response(agent, runResult.Messages, streaming, streamingCallback, verboseCallback)! ?? runResult.Response;

                    currentTurn++;

                } while (await ProcessOutputItems(agent, runResult, verboseCallback, computerUseCallback) && !single_turn);
            }
            catch (Exception ex)
            {
                verboseCallback?.Invoke($"Exception during agent run: {ex.Message}");
            }

            //Add output guardrail eventually

            return runResult;
        }


        private static void CheckForCancellation(CancellationTokenSource? cancellationToken)
        {
            if (cancellationToken != null && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Operation was cancelled by user.");
            }
        }


        /// <summary>
        /// Add output to messages and handle function and tool calls
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="runResult"></param>
        /// <param name="callback"></param>
        /// <param name="computerUseCallback"></param>
        /// <returns></returns>
        private static async Task<bool> ProcessOutputItems(Agent agent, RunResult runResult, RunnerVerboseCallbacks? callback, ComputerActionCallbacks? computerUseCallback)
        {
            bool requiresAction = false;

            List<ModelItem> outputItems = runResult.Response.OutputItems!.ToList();

            foreach (ModelItem item in outputItems)
            {
                runResult.Messages.Add(item);

                await HandleVerboseCallback(item, callback);

                //Process Action Call
                if (item is ModelFunctionCallItem toolCall)
                {
                    runResult.Messages.Add(await HandleToolCall(agent, toolCall));

                    requiresAction = true;
                }
                else if (item is ModelComputerCallItem computerCall)
                {
                    runResult.Messages.Add(await HandleComputerCall(computerCall, computerUseCallback));

                    requiresAction = true;
                }
            }

            return requiresAction;
        }

        /// <summary>
        /// Handle Tool calls from Agent loop
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="toolCall"></param>
        /// <returns></returns>
        private static async Task<ModelFunctionCallOutputItem> HandleToolCall(Agent agent, ModelFunctionCallItem toolCall)
        {
            return agent.agent_tools.ContainsKey(toolCall.FunctionName) ? await ToolRunner.CallAgentToolAsync(agent, toolCall) : await ToolRunner.CallFuncToolAsync(agent, toolCall);
        }

        /// <summary>
        /// Handle verbose responses from Running
        /// </summary>
        /// <param name="item"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private static async Task HandleVerboseCallback(ModelItem item, RunnerVerboseCallbacks? callback = null)
        {
            if (item is ModelWebCallItem webSearchCall)
            {
                callback?.Invoke($"[Web search invoked]({webSearchCall.Status}) {webSearchCall.Id}");
            }
            else if (item is ModelFileSearchCallItem fileSearchCall)
            {
                callback?.Invoke($"[File search invoked]({fileSearchCall.Status}) {fileSearchCall.Id}");
            }
            else if (item is ModelFunctionCallItem toolCall)
            {
                callback?.Invoke($"""
                        Calling tool:{toolCall.FunctionName}
                        using parameters:{JsonDocument.Parse(toolCall.FunctionArguments).RootElement.GetRawText()}
                        """);
            }
            else if (item is ModelComputerCallItem computerCall)
            {
                callback?.Invoke($"[Computer Call invoked]({computerCall.Status}) {computerCall.Action.TypeText}");
            }
            else if (item is ModelMessageItem message)
            {
                callback?.Invoke($"[Message]({message.Role}) {message.Text}");
            }
        }


        /// <summary>
        /// Handle sending data to callbacks from computer calls and send Screenshot after invoke
        /// </summary>
        /// <param name="computerCall"></param>
        /// <param name="computerCallbacks"></param>
        /// <returns></returns>
        public static async Task<ModelComputerCallOutputItem> HandleComputerCall(ModelComputerCallItem computerCall, ComputerActionCallbacks? computerCallbacks = null)
        {
            computerCallbacks?.Invoke(computerCall.Action);

            Thread.Sleep(1000);

            byte[] data = ComputerToolUtility.TakeScreenshotByteArray(ImageFormat.Png);

            GC.Collect();

            return new ModelComputerCallOutputItem("cuo_" + Guid.NewGuid().ToString().Replace("-", "_"), computerCall.CallId, ModelStatus.Completed, new ModelMessageImageFileContent(BinaryData.FromBytes(data), "image/png"));
        }

        /// <summary>
        /// Get response from the model or If Error delete last message in thread and retry (max agent loops will cap)
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="messages"></param>
        /// <param name="Streaming"></param>
        /// <param name="streamingCallback"></param>
        /// <returns></returns>
        public static async Task<ModelResponse>? _get_new_response(Agent agent, List<ModelItem> messages, bool Streaming = false, StreamingCallbacks? streamingCallback = null, RunnerVerboseCallbacks? verboseCallback = null)
        {
            try
            {
                if (Streaming)
                {
                    return await agent.Client._CreateStreamingResponseAsync(messages, agent.Options, streamingCallback);
                }
                return await agent.Client._CreateResponseAsync(messages, agent.Options);
            }
            catch (Exception ex)
            {
                verboseCallback?.Invoke(ex.ToString());
                verboseCallback?.Invoke("Removing Last Message thread");
                RemoveLastMessageThread(messages);
            }

            return null;
        }

        /// <summary>
        /// Try to rerun last message thread if it fails.
        /// </summary>
        /// <param name="messages"></param>
        public static void RemoveLastMessageThread(List<ModelItem> messages)
        {
            //Remove last messages
            if (messages.Count > 1)
            {
                //Remove function call and output item for called items
                if (messages[messages.Count - 1] is ModelFunctionCallOutputItem || messages[messages.Count - 1] is ModelComputerCallOutputItem)
                {
                    messages.RemoveAt(messages.Count - 1); //Remove last input
                    messages.RemoveAt(messages.Count - 1); //Remove last input
                }
                else
                {
                    messages.RemoveAt(messages.Count - 1); //Remove last input
                }
            }
        }
    }
}
