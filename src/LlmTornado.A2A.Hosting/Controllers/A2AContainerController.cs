using A2A;
using LlmTornado.A2A.Hosting.Models;
using LlmTornado.A2A.Hosting.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LlmTornado.A2A.Hosting.Controllers
{
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
                await _containerService.CancelTask(request.Endpoint, request.TaskId);
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
                var status = await _containerService.GetTask(endpoint, taskId);
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
                        await Response.WriteAsync($"event: streamed_content\n");
                        await Response.WriteAsync($"data: {{\n");
                        await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(((AgentMessage)a2aEvent.Data).Parts.OfType<TextPart>().Last().Text)}\"\n");
                        await Response.WriteAsync($"data: }}\n\n");
                    }
                    else if(a2aEvent.Data.Kind == A2AEventKind.StatusUpdate)
                    {
                        await Response.WriteAsync($"event: task_status_update\n");
                        await Response.WriteAsync($"data: {{\n");
                        await Response.WriteAsync($"data: \"status\": \"{((TaskStatusUpdateEvent)a2aEvent.Data).Status.State}\",\n");
                        await Response.WriteAsync($"data: \"taskId\": \"{((TaskStatusUpdateEvent)a2aEvent.Data).TaskId}\"\n");
                        await Response.WriteAsync($"data: }}\n\n");
                    }
                    else if(a2aEvent.Data.Kind == A2AEventKind.Task)
                    {
                        AgentTask task = (AgentTask)a2aEvent.Data;
                        await Response.WriteAsync($"event: task_info\n");
                        await Response.WriteAsync($"data: {{\n");
                        await Response.WriteAsync($"data: \"taskId\": \"{task.Id}\",\n");
                        await Response.WriteAsync($"data: \"taskState\": \"{task.Status.State}\",\n");
                        await Response.WriteAsync($"data: \"taskStateMessage\": \"{EscapeJsonString(task.Status.Message.Parts.OfType<TextPart>().Last().Text) ?? ""}\"\n");
                        await Response.WriteAsync($"data: }}\n\n");
                    }
                    else if (a2aEvent.Data.Kind == A2AEventKind.ArtifactUpdate)
                    {
                        TaskArtifactUpdateEvent artifactUpdate = (TaskArtifactUpdateEvent)a2aEvent.Data;
                        await Response.WriteAsync($"event: artifact_update\n");
                        await Response.WriteAsync($"data: {{\n");
                        await Response.WriteAsync($"data: \"taskId\": \"{artifactUpdate.TaskId}\",\n");
                        await Response.WriteAsync($"data: \"artifact\": \"{EscapeJsonString(artifactUpdate.Artifact.Parts.OfType<TextPart>().Last().Text) ?? ""}\"\n");
                        await Response.WriteAsync($"data: }}\n\n");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send streaming message to container");
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


}
