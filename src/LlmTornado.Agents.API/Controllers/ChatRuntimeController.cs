using Microsoft.AspNetCore.Mvc;
using LlmTornado.Agents.API.Models;
using LlmTornado.Agents.API.Services;
using LlmTornado.Agents.API.Hubs;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Agents.DataModels;

namespace LlmTornado.Agents.API.Controllers;

/// <summary>
/// Controller for managing ChatRuntime instances and communication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatRuntimeController : ControllerBase
{
    private readonly IChatRuntimeService _runtimeService;
    private readonly IStreamingEventService _streamingEventService;
    private readonly ILogger<ChatRuntimeController> _logger;

    public ChatRuntimeController(
        IChatRuntimeService runtimeService,
        IStreamingEventService streamingEventService,
        ILogger<ChatRuntimeController> logger)
    {
        _runtimeService = runtimeService;
        _streamingEventService = streamingEventService;
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
            var runtimeId = await _runtimeService.CreateRuntimeAsync(
                request.ConfigurationType,
                request.AgentName,
                request.Instructions,
                request.EnableStreaming);

            var response = new CreateChatRuntimeResponse
            {
                RuntimeId = runtimeId,
                Status = "created"
            };

            _logger.LogInformation("Created runtime {RuntimeId} for agent {AgentName}", runtimeId, request.AgentName);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create runtime");
            return StatusCode(500, new { error = "Failed to create runtime", details = ex.Message });
        }
    }

    /// <summary>
    /// Sends a message to a specific ChatRuntime
    /// </summary>
    /// <param name="runtimeId">Runtime identifier</param>
    /// <param name="request">Message request</param>
    /// <returns>Message response</returns>
    [HttpPost("{runtimeId}/message")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(string runtimeId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var runtime = _runtimeService.GetRuntime(runtimeId);
            if (runtime == null)
            {
                return NotFound(new { error = $"Runtime {runtimeId} not found" });
            }

            // Create a chat message from the request
            var message = new ChatMessage(
                request.Role == "user" ? ChatMessageRoles.User : ChatMessageRoles.Assistant,
                request.Content);

            // Send streaming event for message received
            if (request.EnableStreaming ?? true)
            {
                await _streamingEventService.BroadcastEventAsync(runtimeId, new StreamingEventResponse
                {
                    EventType = "MessageReceived",
                    SequenceNumber = 1,
                    Data = new { content = request.Content, role = request.Role },
                    RuntimeId = runtimeId
                });
            }

            // Send the message to the runtime
            var responseMessage = await _runtimeService.SendMessageAsync(runtimeId, message);

            // Send streaming event for response
            if (request.EnableStreaming ?? true)
            {
                await _streamingEventService.BroadcastEventAsync(runtimeId, new StreamingEventResponse
                {
                    EventType = "MessageResponse",
                    SequenceNumber = 2,
                    Data = new { content = responseMessage.Content, role = responseMessage.Role },
                    RuntimeId = runtimeId
                });
            }

            var response = new SendMessageResponse
            {
                Content = responseMessage.Content ?? string.Empty,
                Role = responseMessage.Role?.ToString() ?? "assistant",
                RequestId = Guid.NewGuid().ToString(),
                IsStreamed = request.EnableStreaming ?? true
            };

            return Ok(response);
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
            if (runtime == null)
            {
                return NotFound(new { error = $"Runtime {runtimeId} not found" });
            }

            var response = new RuntimeStatusResponse
            {
                RuntimeId = runtimeId,
                Status = "active",
                StreamingEnabled = true,
                MessageCount = runtime.RuntimeConfiguration.GetMessages().Count
            };

            return Ok(response);
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
            var runtimeIds = _runtimeService.GetActiveRuntimeIds();
            return Ok(runtimeIds);
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
            var success = _runtimeService.RemoveRuntime(runtimeId);
            if (!success)
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
            if (runtime == null)
            {
                return NotFound(new { error = $"Runtime {runtimeId} not found" });
            }

            runtime.CancelExecution();
            
            // Send cancellation event
            _streamingEventService.BroadcastEventAsync(runtimeId, new StreamingEventResponse
            {
                EventType = "RuntimeCancelled",
                SequenceNumber = 0,
                Data = new { message = "Runtime execution cancelled" },
                RuntimeId = runtimeId
            });

            return Ok(new { message = "Runtime execution cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel runtime {RuntimeId}", runtimeId);
            return StatusCode(500, new { error = "Failed to cancel runtime", details = ex.Message });
        }
    }
}