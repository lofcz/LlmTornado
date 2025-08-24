using LlmTornado.Agents.API.Models;
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

            // Use a thread-safe queue for streaming messages
            var messageQueue = new System.Collections.Concurrent.ConcurrentQueue<ModelStreamingEvents>();


            // Enhanced streaming handler to handle different event types
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

            // CRITICAL FIX: Set the OnRuntimeEvent BEFORE creating the message and invoking
            // This ensures that the event handler is wired up properly to the runtime configuration
            var existingHandler = runtime.OnRuntimeEvent;
            runtime.OnRuntimeEvent = async (evt) =>
            {
                // Call existing handler first (from ChatRuntimeService)
                if (existingHandler != null)
                {
                    await existingHandler(evt);
                }
                // Then call our streaming handler
                await StreamHandler(evt);
            };

            // Also ensure the runtime configuration has the event handler
            if (runtime.RuntimeConfiguration != null)
            {
                runtime.RuntimeConfiguration.OnRuntimeEvent = runtime.OnRuntimeEvent;
            }

            ChatMessage message = new ChatMessage(
                request.Role == "user" ? ChatMessageRoles.User : ChatMessageRoles.Assistant,
                request.Content);

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
        if (runtimeEvent is ChatRuntimeAgentRunnerEvents rEvent)
        {
            await ProcessAgentRunnerEvents(rEvent.AgentRunnerEvent);
        }
        else if (runtimeEvent is ChatRuntimeOrchestrationEvent orchestrationEvent)
        {
            await ProcessOrchestrationEvents(orchestrationEvent.OrchestrationEventData);
        }
        else
        {
            await ProcessRuntimeEvent(runtimeEvent);
        }

        await Response.Body.FlushAsync();
    }

    private async Task ProcessRuntimeEvent(ChatRuntimeEvents runtimeEvents)
    {

        
    }

    private async Task ProcessOrchestrationEvents(OrchestrationEvent orchestrationEvent)
    {
        
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
            default: break;
        };
        
    }

    private async Task ProcessModelStreamingEvent(ModelStreamingEvents streamingEvent)
    {
        switch (streamingEvent.EventType)
        {
            case ModelStreamingEventType.Created:
                await Response.WriteAsync($"event: created\n");
                break;
            case ModelStreamingEventType.OutputTextDelta:
                if (streamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                {
                    Console.Write(deltaEvent.DeltaText);
                    await Response.WriteAsync($"event: delta_text\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"Text\": \"{EscapeJsonString(deltaEvent.DeltaText ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;

            case ModelStreamingEventType.Completed:
                await Response.WriteAsync($"event: stream_complete\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
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
                    await Response.WriteAsync($"event: reasoning\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {reasoningEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {reasoningEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(reasoningEvent.DeltaText ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"itemId\": \"{reasoningEvent.ItemId}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                }
                break;

            default:
                // For any other event types, send a generic event
                await Response.WriteAsync($"event: {streamingEvent.EventType.ToString().ToLowerInvariant()}\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"status\": \"{streamingEvent.Status}\"}}\n\n");
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
