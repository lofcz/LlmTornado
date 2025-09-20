using A2A;
using LlmTornado.A2A.Hosting.Models;
using LlmTornado.A2A.Hosting.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LlmTornado.A2A.Hosting.Controllers;

[Route("api/[controller]")]
[ApiController]
public class A2AContainerController : ControllerBase
{
    private readonly ILogger<A2AContainerController> _logger;
    private readonly IA2AContainerService _containerService;
    public A2AContainerController(
       ILogger<A2AContainerController> logger,
       IA2AContainerService containerService)
    {
        _logger = logger;
        _containerService = containerService;
    }
    /// <summary>
    /// Gets list of active container instances
    /// </summary>
    [HttpGet("GetAgentCard")]
    public async Task<ActionResult<AgentCard>> GetAgentCard(string endpoint)
    {
        try
        {
            var containers = await _containerService.GetAgentCardAsync(endpoint);
            return Ok(containers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list containers");
            return StatusCode(500, new { error = "Failed to list containers", details = ex.Message });
        }
    }

    [HttpPost("SendMessage")]
    public async Task<ActionResult<AgentMessage>> SendMessage([FromBody] ContainerAgentMessage message)
    {
        try
        {
            var response = await _containerService.SendMessageAsync(message.Endpoint,message.Parts.ToList());
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to container");
            return StatusCode(500, new { error = "Failed to send message to container", details = ex.Message });
        }
    }

    [HttpPost("CancelTask")]
    public async Task<ActionResult> CancelTask([FromBody] ContainerCancelTaskRequest request)
    {
        try
        {
            await _containerService.CancelTaskAsync(request.Endpoint, request.TaskId);
            return Ok(new { message = "Task cancellation requested" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel task");
            return StatusCode(500, new { error = "Failed to cancel task", details = ex.Message });
        }
    }

    [HttpGet("GetTaskStatus")]
    public async Task<ActionResult<AgentTask>> GetTaskStatus(string endpoint, string taskId)
    {
        try
        {
            var status = await _containerService.GetTaskAsync(endpoint, taskId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task status");
            return StatusCode(500, new { error = "Failed to get task status", details = ex.Message });
        }
    }

    [HttpPost("SendStreamingMessage")]
    public async Task TaskSendStreamingMessage([FromBody] ContainerAgentMessage message)
    {
        try
        {
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

            await _containerService.SendMessageStreamingAsync(message.Endpoint, message.Parts.ToList(), async (a2aEvent) =>
            {
                if (a2aEvent.Data.Kind == A2AEventKind.Message)
                {
                    await HandleMessageEvent((AgentMessage)a2aEvent.Data);
                }
                else if(a2aEvent.Data.Kind == A2AEventKind.StatusUpdate)
                {
                    await HandleStateUpdateEvent((TaskStatusUpdateEvent)a2aEvent.Data);
                }
                else if(a2aEvent.Data.Kind == A2AEventKind.Task)
                {
                    await HandleTaskEvent((AgentTask)a2aEvent.Data);
                }
                else if (a2aEvent.Data.Kind == A2AEventKind.ArtifactUpdate)
                {
                    await HandleArtifactUpdateEvent((TaskArtifactUpdateEvent)a2aEvent.Data);
                }
                else
                {
                    await Response.Body.FlushAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send streaming message to container");
        }
    }

    private async Task HandleArtifactUpdateEvent(TaskArtifactUpdateEvent data)
    {
        var eventData = JsonSerializer.Serialize(data);

        await Response.WriteAsync($"event: artifact_update_event\n");
        await Response.WriteAsync($"data: {eventData}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task HandleStateUpdateEvent(TaskStatusUpdateEvent data)
    {
        var eventData = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"event: task_status_update_event\n");
        await Response.WriteAsync($"data: {eventData}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task HandleTaskEvent(AgentTask data)
    {
        var eventData = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"event: task_update_event\n");
        await Response.WriteAsync($"data: {eventData}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task HandleMessageEvent(AgentMessage data)
    {
        var eventData = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"event: message_event\n");
        await Response.WriteAsync($"data: {eventData}\n\n");
        await Response.Body.FlushAsync();
    }
}