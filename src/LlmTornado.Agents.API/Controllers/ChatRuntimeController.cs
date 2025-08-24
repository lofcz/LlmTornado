using Microsoft.AspNetCore.Mvc;
using LlmTornado.Agents.API.Models;
using LlmTornado.Agents.API.Services;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.ChatRuntime.Orchestration;

namespace LlmTornado.Agents.API.Controllers;

/// <summary>
/// Controller for managing ChatRuntime instances and communication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatRuntimeController : ControllerBase
{
    private readonly IChatRuntimeService _runtimeService;
    private readonly ILogger<ChatRuntimeController> _logger;

    public ChatRuntimeController(
        IChatRuntimeService runtimeService,
        ILogger<ChatRuntimeController> logger)
    {
        _runtimeService = runtimeService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new ChatRuntime instance
    /// </summary>
    /// <param name="request">Runtime creation request</param>
    /// <returns>Runtime creation response with ID</returns>
    [HttpPost("create")]
    public async Task<ActionResult<CreateChatRuntimeResponse>> CreateRuntime([FromBody] CreateChatRuntimeRequest request)
    {
        try
        {
            string runtimeId = await _runtimeService.CreateRuntimeAsync(
                request.ConfigurationType,
                request.AgentName,
                request.Instructions,
                request.EnableStreaming);

            return Ok(new CreateChatRuntimeResponse { RuntimeId = runtimeId, Status = "created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create runtime");
            return StatusCode(500, new { error = "Failed to create runtime", details = ex.Message });
        }
    }

    /// <summary>
    /// Non-streaming message endpoint. Returns only the final assistant response.
    /// </summary>
    [HttpPost("{runtimeId}/message")] // legacy non-streaming path
    public async Task<ActionResult<SendMessageResponse>> SendMessage(string runtimeId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var runtime = _runtimeService.GetRuntime(runtimeId);
            if (runtime is null)
            {
                return NotFound(new { error = $"Runtime {runtimeId} not found" });
            }

            ChatMessage message = new ChatMessage(
                request.Role == "user" ? ChatMessageRoles.User : ChatMessageRoles.Assistant,
                request.Content);

            ChatMessage responseMessage = await _runtimeService.SendMessageAsync(runtimeId, message);

            return Ok(new SendMessageResponse
            {
                Content = responseMessage.Content ?? string.Empty,
                Role = responseMessage.Role?.ToString() ?? "assistant",
                RequestId = Guid.NewGuid().ToString(),
                IsStreamed = false
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to runtime {RuntimeId}", runtimeId);
            return StatusCode(500, new { error = "Failed to send message", details = ex.Message });
        }
    }

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
            var streamingComplete = false;
            var streamingError = false;
            var errorMessage = "";
            var eventsReceived = 0;

            // Enhanced streaming handler to handle different event types
            async ValueTask StreamHandler(ChatRuntimeEvents runnerEvent)
            {
                try
                {
                    eventsReceived++;
                    Console.WriteLine($"[STREAMING DEBUG] Event #{eventsReceived}: Type='{runnerEvent.EventType}''");
                    if(runnerEvent is ChatRuntimeAgentRunnerEvents rEvent)
                    {
                        if (rEvent.AgentRunnerEvent is AgentRunnerStreamingEvent arEvent)
                        {
                            var streamingEvent = arEvent.ModelStreamingEvent;
                            Console.WriteLine($"[STREAMING DEBUG] Streaming Event: Type='{streamingEvent.EventType}'");
                            if (streamingEvent.EventType == ModelStreamingEventType.Completed)
                            {
                                streamingComplete = true;
                            }
                            else if (streamingEvent.EventType == ModelStreamingEventType.Error && streamingEvent is ModelStreamingErrorEvent errorEvent)
                            {
                                streamingError = true;
                                errorMessage = errorEvent.ErrorMessage ?? "Unknown error";
                            }
                            // Queue the event for async processing
                            //messageQueue.Enqueue(streamingEvent);
                            await ProcessStreamingEvent(streamingEvent);
                        }
                    }
                    else if (runnerEvent is ChatRuntimeOrchestrationEvent orchestrationEvent)
                    {
                        if (orchestrationEvent.OrchestrationEventData is OrchestrationEvent arEvent)
                        {
                            Console.WriteLine($"[STREAMING DEBUG] Streaming Event: Type='{arEvent.Type}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[STREAMING DEBUG] Error queuing streaming event: {ex.Message}");
                }
            }

            runtime.OnRuntimeEvent = StreamHandler;

            ChatMessage message = new ChatMessage(
                request.Role == "user" ? ChatMessageRoles.User : ChatMessageRoles.Assistant,
                request.Content);

            // Invoke runtime; streaming deltas will be delivered via SignalR events already wired in the service.
            ChatMessage final = await _runtimeService.SendMessageAsync(runtimeId, message);
        }
        catch (ArgumentException ex)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream message to runtime {RuntimeId}", runtimeId);
             Console.WriteLine($"[STREAMING DEBUG] Agent error: {ex.Message}");
            return;
        }
    }
    // Helper method to process different streaming event types
    async Task ProcessStreamingEvent(ModelStreamingEvents streamingEvent)
    {
        switch (streamingEvent.EventType)
        {
            case ModelStreamingEventType.Created:
                await Response.WriteAsync($"event: created\n");
                await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
                break;

            case ModelStreamingEventType.OutputTextDelta:
                if (streamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                {
                    await Response.WriteAsync($"event: delta\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"sequenceId\": {deltaEvent.SequenceId},\n");
                    await Response.WriteAsync($"data: \"outputIndex\": {deltaEvent.OutputIndex},\n");
                    await Response.WriteAsync($"data: \"contentIndex\": {deltaEvent.ContentPartIndex},\n");
                    await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(deltaEvent.DeltaText ?? "")}\",\n");
                    await Response.WriteAsync($"data: \"itemId\": \"{deltaEvent.ItemId ?? ""}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                    Console.WriteLine($"[STREAMING DEBUG] Sent delta: '{deltaEvent.DeltaText}'");
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

        await Response.Body.FlushAsync();
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

    /// <summary>
    /// Gets the status of a specific ChatRuntime
    /// </summary>
    /// <param name="runtimeId">Runtime identifier</param>
    /// <returns>Runtime status information</returns>
    [HttpGet("{runtimeId}/status")]
    public ActionResult<RuntimeStatusResponse> GetRuntimeStatus(string runtimeId)
    {
        try
        {
            var runtime = _runtimeService.GetRuntime(runtimeId);
            if (runtime is null)
            {
                return NotFound(new { error = $"Runtime {runtimeId} not found" });
            }

            return Ok(new RuntimeStatusResponse
            {
                RuntimeId = runtimeId,
                Status = "active",
                StreamingEnabled = true, // runtime config may expose this later
                MessageCount = runtime.RuntimeConfiguration.GetMessages().Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get runtime status for {RuntimeId}", runtimeId);
            return StatusCode(500, new { error = "Failed to get runtime status", details = ex.Message });
        }
    }

    /// <summary>
    /// Lists all active ChatRuntime instances
    /// </summary>
    /// <returns>List of active runtime IDs</returns>
    [HttpGet("list")]
    public ActionResult<IEnumerable<string>> ListActiveRuntimes()
    {
        try
        {
            return Ok(_runtimeService.GetActiveRuntimeIds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list active runtimes");
            return StatusCode(500, new { error = "Failed to list runtimes", details = ex.Message });
        }
    }

    /// <summary>
    /// Removes a ChatRuntime instance
    /// </summary>
    /// <param name="runtimeId">Runtime identifier</param>
    /// <returns>Success status</returns>
    [HttpDelete("{runtimeId}")]
    public ActionResult RemoveRuntime(string runtimeId)
    {
        try
        {
            if (!_runtimeService.RemoveRuntime(runtimeId))
            {
                return NotFound(new { error = $"Runtime {runtimeId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove runtime {RuntimeId}", runtimeId);
            return StatusCode(500, new { error = "Failed to remove runtime", details = ex.Message });
        }
    }

    /// <summary>
    /// Cancels execution for a specific ChatRuntime
    /// </summary>
    /// <param name="runtimeId">Runtime identifier</param>
    /// <returns>Success status</returns>
    [HttpPost("{runtimeId}/cancel")]
    public ActionResult CancelRuntime(string runtimeId)
    {
        try
        {
            var runtime = _runtimeService.GetRuntime(runtimeId);
            if (runtime is null)
            {
                return NotFound(new { error = $"Runtime {runtimeId} not found" });
            }
            runtime.CancelExecution();
            return Ok(new { message = "Runtime execution cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel runtime {RuntimeId}", runtimeId);
            return StatusCode(500, new { error = "Failed to cancel runtime", details = ex.Message });
        }
    }
}