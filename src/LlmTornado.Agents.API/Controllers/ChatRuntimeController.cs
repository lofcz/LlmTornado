using LlmTornado.Agents.API.Models;
using LlmTornado.Agents.API.Services;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LlmTornado.Agents.API.Controllers;

/// <summary>
/// Controller for managing ChatRuntime instances and communication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public partial class ChatRuntimeController : ControllerBase
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

            ChatMessage message = new ChatMessage(ChatMessageRoles.User,request.Content);

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