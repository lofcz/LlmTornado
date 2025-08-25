using LlmTornado.Agents.API.Models;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Code;
using Microsoft.AspNetCore.Mvc;

namespace LlmTornado.Agents.API.Controllers;

public partial class ChatRuntimeController
{
    private bool _streamingComplete = false;
    private bool _streamingError = false;
    private string _errorMessage = "";
    private int _eventsReceived = 0;

    /// <summary>
    /// Streaming message endpoint. Clients should subscribe to SignalR hub /hub/chatruntime and group runtime-{runtimeId}.
    /// This endpoint triggers processing and returns acknowledgement plus (optionally) final response when available.
    /// </summary>
    [HttpPost("{runtimeId}/stream")] // streaming path
    public async Task StreamMessage(string runtimeId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var runtime = _runtimeService.GetRuntime(runtimeId);
            if (runtime is null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsync("Agent not found");
                return;
            }

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

            // Disable response buffering for real-time streaming
            var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            if (feature != null)
            {
                feature.DisableBuffering();
            }

            // streaming handler to handle different event types
            async ValueTask StreamHandler(ChatRuntimeEvents runnerEvent)
            {
                try
                {
                    _eventsReceived++;
                    await ProcessRuntimeEvents(runnerEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[STREAMING DEBUG] Error queuing streaming event: {ex.Message}");
                }
            }

            runtime.OnRuntimeEvent = async (evt) => await StreamHandler(evt);

            ChatMessage message = new ChatMessage(ChatMessageRoles.User, request.Content);

            // Invoke runtime; streaming deltas will be delivered via the event handlers we just set up
            ChatMessage final = await _runtimeService.SendMessageAsync(runtimeId, message);
        }
        catch (ArgumentException ex)
        {
            Response.StatusCode = 404;
            await Response.WriteAsync($"Runtime not found: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream message to runtime {RuntimeId}", runtimeId);
            Console.WriteLine($"[STREAMING DEBUG] Agent error: {ex.Message}");
            Response.StatusCode = 500;
            await Response.WriteAsync($"Internal error: {ex.Message}");
            return;
        }
    }

    // Helper method to process different streaming event types
    async Task ProcessRuntimeEvents(ChatRuntimeEvents runtimeEvent)
    {
        await ProcessRuntimeEvent(runtimeEvent);
        await Response.Body.FlushAsync();
    }

    private async Task ProcessRuntimeEvent(ChatRuntimeEvents runtimeEvents)
    {
        switch (runtimeEvents.EventType)
        {
            case ChatRuntimeEventTypes.Started:
                await Response.WriteAsync($"event: runtime_started\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"runtimeId\": \"{EscapeJsonString(runtimeEvents.RuntimeId)}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                break;
                
            case ChatRuntimeEventTypes.Completed:
                await Response.WriteAsync($"event: runtime_completed\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"runtimeId\": \"{EscapeJsonString(runtimeEvents.RuntimeId)}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                break;
                
            case ChatRuntimeEventTypes.Error:
                if (runtimeEvents is ChatRuntimeErrorEvent errorEvent)
                {
                    await Response.WriteAsync($"event: runtime_error\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"runtimeId\": \"{EscapeJsonString(runtimeEvents.RuntimeId)}\",\n");
                    await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(errorEvent.Exception?.Message ?? "Unknown error")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ChatRuntimeEventTypes.Cancelled:
                await Response.WriteAsync($"event: runtime_cancelled\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"runtimeId\": \"{EscapeJsonString(runtimeEvents.RuntimeId)}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                break;
                
            case ChatRuntimeEventTypes.Invoked:
                if (runtimeEvents is ChatRuntimeInvokedEvent invokedEvent)
                {
                    await Response.WriteAsync($"event: runtime_invoked\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"runtimeId\": \"{EscapeJsonString(runtimeEvents.RuntimeId)}\",\n");
                    await Response.WriteAsync($"data: \"messageRole\": \"{EscapeJsonString(invokedEvent.Message.Role?.ToString() ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"messageContent\": \"{EscapeJsonString(invokedEvent.Message.Content ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ChatRuntimeEventTypes.Orchestration:
                if (runtimeEvents is ChatRuntimeOrchestrationEvent orchestrationEvent)
                {
                    await ProcessOrchestrationEvents(orchestrationEvent.OrchestrationEventData);
                }
                break;
            case ChatRuntimeEventTypes.AgentRunner:
                if (runtimeEvents is ChatRuntimeAgentRunnerEvents rEvent)
                {
                    await ProcessAgentRunnerEvents(rEvent.AgentRunnerEvent);
                }
                break;
            default:
                await Response.WriteAsync($"event: runtime_unknown\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"runtimeId\": \"{EscapeJsonString(runtimeEvents.RuntimeId)}\",\n");
                await Response.WriteAsync($"data: \"eventType\": \"{runtimeEvents.EventType}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                break;
        }
    }

    private async Task ProcessOrchestrationEvents(OrchestrationEvent orchestrationEvent)
    {
        switch (orchestrationEvent.Type)
        {
            case "begin":
                await Response.WriteAsync($"event: orchestration_begin\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
                
            case "finished":
                await Response.WriteAsync($"event: orchestration_finished\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
                
            case "canceled":
                await Response.WriteAsync($"event: orchestration_cancelled\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
                
            case "error":
                if (orchestrationEvent is OnErrorOrchestrationEvent errorEvent)
                {
                    await Response.WriteAsync($"event: orchestration_error\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(errorEvent.Exception?.Message ?? "Unknown orchestration error")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                else
                {
                    await Response.WriteAsync($"event: orchestration_error\n");
                    await Response.WriteAsync($"data: {{}}\n\n");
                }
                break;
                
            case "tick":
                await Response.WriteAsync($"event: orchestration_tick\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
                
            case "verbose":
                if (orchestrationEvent is OnVerboseOrchestrationEvent verboseEvent)
                {
                    await Response.WriteAsync($"event: orchestration_verbose\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"message\": \"{EscapeJsonString(verboseEvent.Message ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case "started":
                if (orchestrationEvent is OnStartedRunnableEvent startedEvent)
                {
                    await Response.WriteAsync($"event: orchestration_started_runnable\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"processId\": \"{EscapeJsonString(startedEvent.RunnableProcess?.Id ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"runnerId\": \"{EscapeJsonString(startedEvent.RunnableProcess?.Runner?.Id ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case "exited":
                if (orchestrationEvent is OnFinishedRunnableEvent finishedEvent)
                {
                    await Response.WriteAsync($"event: orchestration_finished_runnable\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"runnableId\": \"{EscapeJsonString(finishedEvent.Runnable?.Id ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case "invoked":
                if (orchestrationEvent is OnInvokedRunnableEvent invokedEvent)
                {
                    await Response.WriteAsync($"event: orchestration_invoked_runnable\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"processId\": \"{EscapeJsonString(invokedEvent.RunnableProcess?.Id ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"runnerId\": \"{EscapeJsonString(invokedEvent.RunnableProcess?.Runner?.Id ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            default:
                await Response.WriteAsync($"event: orchestration_unknown\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"type\": \"{EscapeJsonString(orchestrationEvent.Type)}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                break;
        }
    }

    private void CheckIfStreamingComplete(AgentRunnerEvents runnerEvent)
    {
        if (runnerEvent is AgentRunnerStreamingEvent arEvent)
        {
            var streamingEvent = arEvent.ModelStreamingEvent;
            if (streamingEvent.EventType == ModelStreamingEventType.Completed)
            {
                _streamingComplete = true;
            }
            else if (streamingEvent.EventType == ModelStreamingEventType.Error && streamingEvent is ModelStreamingErrorEvent errorEvent)
            {
                _streamingError = true;
                _errorMessage = errorEvent.ErrorMessage ?? "Unknown error";
            }
        }
    }

    private async Task ProcessAgentRunnerEvents(AgentRunnerEvents runnerEvent)
    {
        CheckIfStreamingComplete(runnerEvent);

        switch (runnerEvent.EventType)
        {
            case AgentRunnerEventTypes.Streaming:
                if (runnerEvent is AgentRunnerStreamingEvent arEvent)
                {
                    await ProcessModelStreamingEvent(arEvent.ModelStreamingEvent);
                }
                break;
            case AgentRunnerEventTypes.Started:
                await Response.WriteAsync($"event: runner_started\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
            case AgentRunnerEventTypes.Completed:
                await Response.WriteAsync($"event: runner_completed\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
            case AgentRunnerEventTypes.Error:
                if (runnerEvent is AgentRunnerErrorEvent errorEvent)
                {
                    await Response.WriteAsync($"event: runner_error\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(errorEvent.ErrorMessage ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
            case AgentRunnerEventTypes.Cancelled:
                await Response.WriteAsync($"event: runner_cancelled\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
            case AgentRunnerEventTypes.ToolInvoked:
                if (runnerEvent is AgentRunnerToolInvokedEvent toolInvokedEvent)
                {
                    await Response.WriteAsync($"event: tool_invoked\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"toolName\": \"{EscapeJsonString(toolInvokedEvent.ToolCalled.Name ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"parameters\": \"{EscapeJsonString(toolInvokedEvent.ToolCalled.Arguments ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
            case AgentRunnerEventTypes.ToolCompleted:
                if (runnerEvent is AgentRunnerToolCompletedEvent toolCompletedEvent)
                {
                    await Response.WriteAsync($"event: tool_completed\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"toolName\": \"{EscapeJsonString(toolCompletedEvent.ToolCall.Name ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"result\": \"{EscapeJsonString(toolCompletedEvent.ToolCall.Result.Content ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
            case AgentRunnerEventTypes.MaxTurnsReached:
                await Response.WriteAsync($"event: runner_max_turns_reached\n");
                await Response.WriteAsync($"data: {{}}\n\n");
                break;
            case AgentRunnerEventTypes.GuardRailTriggered:
                if (runnerEvent is AgentRunnerGuardrailTriggeredEvent guardrailEvent)
                {
                    await Response.WriteAsync($"event: runner_guardrail_triggered\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"reason\": \"{EscapeJsonString(guardrailEvent.Reason ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
            default: break;
        };
        
    }

    private async Task ProcessModelStreamingEvent(ModelStreamingEvents streamingEvent)
    {
        switch (streamingEvent.EventType)
        {
            case ModelStreamingEventType.Created:
                await Response.WriteAsync($"event: created\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.InProgress:
                await Response.WriteAsync($"event: in_progress\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.Failed:
                if (streamingEvent is ModelStreamingFailedEvent failedEvent)
                {
                    await Response.WriteAsync($"event: failed\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {failedEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"responseId\": \"{failedEvent.ResponseId}\",\n");
                    await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(failedEvent.ErrorMessage ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"code\": \"{EscapeJsonString(failedEvent.ErrorCode ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ModelStreamingEventType.Incomplete:
                if (streamingEvent is ModelStreamingIncompleteEvent incompleteEvent)
                {
                    await Response.WriteAsync($"event: incomplete\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {incompleteEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"responseId\": \"{incompleteEvent.ResponseId}\",\n");
                    await Response.WriteAsync($"data: \"reason\": \"{EscapeJsonString(incompleteEvent.Reason ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ModelStreamingEventType.OutputTextDelta:
                if (streamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                {
                    Console.Write(deltaEvent.DeltaText);
                    await Response.WriteAsync($"event: output_text_delta\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {deltaEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {deltaEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"contentPartIndex\": {deltaEvent.ContentPartIndex},\n");
                    await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(deltaEvent.DeltaText ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"itemId\": \"{EscapeJsonString(deltaEvent.ItemId ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;

            case ModelStreamingEventType.Completed:
                await Response.WriteAsync($"event: stream_complete\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.OutputItemAdded:
                if (streamingEvent is ModelStreamingOutputItemAddedEvent outputItemEvent)
                {
                    await Response.WriteAsync($"event: output_item_added\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {outputItemEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {outputItemEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"responseId\": \"{outputItemEvent.ResponseId}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ModelStreamingEventType.OutputItemDone:
                if (streamingEvent is ModelStreamingOutputItemDoneEvent outputDoneEvent)
                {
                    await Response.WriteAsync($"event: output_item_done\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {outputDoneEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {outputDoneEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"responseId\": \"{outputDoneEvent.ResponseId}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ModelStreamingEventType.ContentPartAdded:
                if (streamingEvent is ModelStreamingContentPartAddEvent contentPartEvent)
                {
                    await Response.WriteAsync($"event: content_part_added\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {contentPartEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {contentPartEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"contentPartIndex\": {contentPartEvent.ContentPartIndex},\n");
                    await Response.WriteAsync($"data: \"contentPartType\": \"{contentPartEvent.ContentPartType}\",\n");
                    await Response.WriteAsync($"data: \"contentPartText\": \"{EscapeJsonString(contentPartEvent.ContentPartText ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ModelStreamingEventType.ContentPartDone:
                if (streamingEvent is ModelStreamingContentPartDoneEvent contentDoneEvent)
                {
                    await Response.WriteAsync($"event: content_part_done\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {contentDoneEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {contentDoneEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"contentPartIndex\": {contentDoneEvent.ContentPartIndex},\n");
                    await Response.WriteAsync($"data: \"contentPartType\": \"{contentDoneEvent.ContentPartType}\",\n");
                    await Response.WriteAsync($"data: \"contentPartText\": \"{EscapeJsonString(contentDoneEvent.ContentPartText ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ModelStreamingEventType.TextDone:
                if (streamingEvent is ModelStreamingOutputTextDoneEvent textDoneEvent)
                {
                    await Response.WriteAsync($"event: text_done\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {textDoneEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {textDoneEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"contentPartIndex\": {textDoneEvent.ContentPartIndex},\n");
                    await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(textDoneEvent.DeltaText ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"itemId\": \"{EscapeJsonString(textDoneEvent.ItemId ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;

            case ModelStreamingEventType.Error:
                if (streamingEvent is ModelStreamingErrorEvent errorEvent)
                {
                    await Response.WriteAsync($"event: stream_error\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {errorEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(errorEvent.ErrorMessage ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"code\": \"{EscapeJsonString(errorEvent.ErrorCode ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;

            case ModelStreamingEventType.ReasoningPartAdded:
                if (streamingEvent is ModelStreamingReasoningPartAddedEvent reasoningEvent)
                {
                    await Response.WriteAsync($"event: reasoning_part_added\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {reasoningEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {reasoningEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"summaryPartIndex\": {reasoningEvent.SummaryPartIndex},\n");
                    await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(reasoningEvent.DeltaText ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"itemId\": \"{reasoningEvent.ItemId}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;
                
            case ModelStreamingEventType.ReasoningPartDone:
                if (streamingEvent is ModelStreamingReasoningPartDoneEvent reasoningDoneEvent)
                {
                    await Response.WriteAsync($"event: reasoning_part_done\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {reasoningDoneEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {reasoningDoneEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"summaryPartIndex\": {reasoningDoneEvent.SummaryPartIndex},\n");
                    await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(reasoningDoneEvent.DeltaText ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"itemId\": \"{reasoningDoneEvent.ItemId}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;

            // Additional event types with generic handling for now - can be expanded with specific event classes
            case ModelStreamingEventType.OutputTextAnnotationAdded:
                await Response.WriteAsync($"event: output_text_annotation_added\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.RefusalDelta:
                await Response.WriteAsync($"event: refusal_delta\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.RefusalDone:
                await Response.WriteAsync($"event: refusal_done\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.FunctionCallDelta:
                await Response.WriteAsync($"event: function_call_delta\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.FunctionCallDone:
                await Response.WriteAsync($"event: function_call_done\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.FileSearchInProgress:
                await Response.WriteAsync($"event: file_search_in_progress\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.FileSearchSearching:
                await Response.WriteAsync($"event: file_search_searching\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.FileSearchDone:
                await Response.WriteAsync($"event: file_search_done\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.CodeInterpreterCodeDelta:
                await Response.WriteAsync($"event: code_interpreter_code_delta\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.CodeInterpreterCodeDone:
                await Response.WriteAsync($"event: code_interpreter_code_done\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.CodeInterpreterIntepreting:
                await Response.WriteAsync($"event: code_interpreter_interpreting\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
                
            case ModelStreamingEventType.CodeInterpreterCompleted:
                await Response.WriteAsync($"event: code_interpreter_completed\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;

            default:
                // For any other event types, send a generic event
                await Response.WriteAsync($"event: {streamingEvent.EventType.ToString().ToLowerInvariant()}\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\", \"status\": \"{streamingEvent.Status}\"}}\n\n");
                break;
        }
    }

    private static string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        return s.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

}
