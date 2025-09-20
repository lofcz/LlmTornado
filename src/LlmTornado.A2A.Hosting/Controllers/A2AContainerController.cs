using A2A;
using LlmTornado.A2A.Hosting.Models;
using LlmTornado.A2A.Hosting.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

    private async Task HandleMessageEvent(AgentMessage data)
    {
        var convertedParts = ConvertPartsToJson(data.Parts);
        var metadata = data.Metadata != null ? ConvertDictionaryToJson(data.Metadata) : "{}";
        var associatedTaskIds = data.ReferenceTaskIds != null ? ConvertArrayToJson(data.ReferenceTaskIds.ToArray()) : "[]";
        var extensions = data.Extensions != null ? ConvertArrayToJson(data.Extensions.ToArray()) : "[]";

        await Response.WriteAsync($"event: message_event\n");
        await Response.WriteAsync($"data: {{\n");
        await Response.WriteAsync($"data:   \"role\": \"{data.Role}\",\n");
        await Response.WriteAsync($"data:   \"kind\": \"{data.Kind.ToString()}\",\n");
        await Response.WriteAsync($"data:   \"messageId\": \"{data.MessageId}\",\n");
        await Response.WriteAsync($"data:   \"contextId\": \"{data.ContextId}\",\n");
        await Response.WriteAsync($"data:   \"taskId\": \"{data.TaskId}\",\n");
        await Response.WriteAsync($"data:   \"associatedTaskIds\": {associatedTaskIds},\n");
        await Response.WriteAsync($"data:   \"parts\": {convertedParts},\n");
        await Response.WriteAsync($"data:   \"metadata\": {metadata},\n");
        await Response.WriteAsync($"data:   \"extensions\": {extensions}\n");
        await Response.WriteAsync($"data: }}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task HandleStateUpdateEvent(TaskStatusUpdateEvent data)
    {
        var metadata = data.Metadata != null ? ConvertDictionaryToJson(data.Metadata) : "{}";

        await Response.WriteAsync($"event: task_status_update_event\n");
        await Response.WriteAsync($"data: {{\n");
        await Response.WriteAsync($"data:   \"kind\": \"{data.Kind.ToString()}\",\n");
        await Response.WriteAsync($"data:   \"contextId\": \"{data.ContextId}\",\n");
        await Response.WriteAsync($"data:   \"taskId\": \"{data.TaskId}\",\n");
        await Response.WriteAsync($"data:   \"final\": \"{data.Final}\",\n");
        await Response.WriteAsync($"data:   \"status\": \"{data.Status.State}\",\n");
        await Response.WriteAsync($"data:   \"timestamp\": \"{data.Status.Timestamp.DateTime}\",\n");
        await Response.WriteAsync($"data:   \"metadata\": {metadata}\n");
        await Response.WriteAsync($"data: }}\n\n");

        if(data.Status.Message != null)
        {
            await HandleMessageEvent(data.Status.Message);
        }

        await Response.Body.FlushAsync();
    }

    private async Task HandleTaskEvent(AgentTask data)
    {
        var metadata = data.Metadata != null ? ConvertDictionaryToJson(data.Metadata) : "{}";

        await Response.WriteAsync($"event: task_status_update_event\n");
        await Response.WriteAsync($"data: {{\n");
        await Response.WriteAsync($"data:   \"kind\": \"{data.Kind}\",\n");
        await Response.WriteAsync($"data:   \"contextId\": \"{data.ContextId}\",\n");
        await Response.WriteAsync($"data:   \"taskId\": \"{data.Id}\",\n");
        await Response.WriteAsync($"data:   \"status\": \"{data.Status.State}\",\n");
        await Response.WriteAsync($"data:   \"timestamp\": \"{data.Status.Timestamp.DateTime}\",\n");
        await Response.WriteAsync($"data:   \"metadata\": {metadata}\n");
        await Response.WriteAsync($"data: }}\n\n");

        if (data.Status.Message != null)
        {
            await HandleMessageEvent(data.Status.Message);
        }

        await Response.Body.FlushAsync();
    }

    private async Task HandleArtifactUpdateEvent(TaskArtifactUpdateEvent data)
    {
        var convertedParts = ConvertPartsToJson(data.Artifact.Parts);
        var metadata = data.Metadata != null ? ConvertDictionaryToJson(data.Metadata) : "{}";
        var artifactMetadata = data.Artifact.Metadata != null ? ConvertDictionaryToJson(data.Artifact.Metadata) : "{}";
        var extensions = data.Artifact.Extensions != null ? ConvertArrayToJson(data.Artifact.Extensions.ToArray()) : "[]";

        await Response.WriteAsync($"event: artifact_update_event\n");
        await Response.WriteAsync($"data: {{\n");
        await Response.WriteAsync($"data:   \"kind\": \"{data.Kind.ToString()}\",\n");
        await Response.WriteAsync($"data:   \"contextId\": \"{data.ContextId}\",\n");
        await Response.WriteAsync($"data:   \"taskId\": \"{data.TaskId}\",\n");
        await Response.WriteAsync($"data:   \"metadata\": {metadata},\n");
        await Response.WriteAsync($"data:   \"artifactId\": \"{data.Artifact.ArtifactId}\",\n");
        await Response.WriteAsync($"data:   \"name\": \"{EscapeJsonString(data.Artifact.Name)}\",\n");
        await Response.WriteAsync($"data:   \"description\": \"{EscapeJsonString(data.Artifact.Description)}\",\n");
        await Response.WriteAsync($"data:   \"append\": \"{data.Append}\",\n");
        await Response.WriteAsync($"data:   \"lastChunk\": \"{data.LastChunk}\",\n");
        await Response.WriteAsync($"data:   \"parts\": {convertedParts},\n");
        await Response.WriteAsync($"data:   \"artifactMetadata\": {artifactMetadata},\n");
        await Response.WriteAsync($"data:   \"extensions\": {extensions}\n");
        await Response.WriteAsync($"data: }}\n\n");
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

    private static string ConvertPartsToJson(IEnumerable<Part> parts)
    {
        var partsList = new List<string>();
        
        foreach (var part in parts)
        {
            switch (part)
            {
                case TextPart textPart:
                    partsList.Add($"{{\"type\": \"text\", \"content\": \"{EscapeJsonString(textPart.Text)}\"}}");
                    break;
                case FilePart filePart:
                    if (filePart.File is FileWithBytes bytesPart)
                    {
                        partsList.Add($"{{\"type\": \"file\", \"name\": \"{EscapeJsonString(bytesPart.Name)}\", \"mimeType\": \"{EscapeJsonString(bytesPart.MimeType ?? "")}\", \"bytes\": \"{EscapeJsonString(bytesPart.Bytes)}\"}}");
                    }
                    else if (filePart.File is FileWithUri uriPart)
                    {
                        partsList.Add($"{{\"type\": \"file\", \"name\": \"{EscapeJsonString(uriPart.Name)}\", \"mimeType\": \"{EscapeJsonString(uriPart.MimeType)}\", \"uri\": \"{EscapeJsonString(uriPart.Uri ?? "")}\"}}");
                    }
                    else
                    {
                        partsList.Add($"{{\"type\": \"file\",\"content\": \"unknown file type\"}}");
                    }
                    break;
                case DataPart dataPart:
                    partsList.Add($"{{\"type\": \"data\", \"data\": \"{ConvertDictionaryToJson(dataPart.Data)}\"}}");
                    break;
                default:
                    break;
            }
        }

        if (partsList.Count == 0)
        {
            return "[]";
        }

        return $"[{string.Join(", ", partsList)}]";
    }

    private static string ConvertArrayToJson(string[] array)
    {
        var escapedItems = array.Select(item => $"\"{EscapeJsonString(item)}\"");
        return $"[{string.Join(", ", escapedItems)}]";
    }

    private static string ConvertDictionaryToJson(Dictionary<string, System.Text.Json.JsonElement> dictionary)
    {
        var keyValuePairs = new List<string>();

        foreach (var kvp in dictionary)
        {
            var value = ConvertJsonElementToString(kvp.Value);
            keyValuePairs.Add($"\"{EscapeJsonString(kvp.Key)}\": \"{EscapeJsonString(value)}\"");
        }

        return $"{{{string.Join(", ", keyValuePairs)}}}";
    }

    private static string ConvertJsonElementToString(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString() ?? "",
            System.Text.Json.JsonValueKind.Number => element.GetRawText(),
            System.Text.Json.JsonValueKind.True => "true",
            System.Text.Json.JsonValueKind.False => "false",
            System.Text.Json.JsonValueKind.Null => "null",
            System.Text.Json.JsonValueKind.Object => element.GetRawText(),
            System.Text.Json.JsonValueKind.Array => element.GetRawText(),
            _ => element.GetRawText()
        };
    }
}