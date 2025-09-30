using LlmTornado.A2A.Hosting.Models;
using A2A;
using System.Text;
using System.Text.Json;

namespace LlmTornado.A2A.Hosting.Client;

/// <summary>
/// Service for handling Server-Sent Events (SSE) streaming from A2A endpoints
/// </summary>
public partial class A2AHostingClient 
{
    public event Action<AgentMessage>? OnMessageReceived;
    public event Action<TaskStatusUpdateEvent>? OnTaskStatusUpdate;
    public event Action<AgentTask>? OnTaskUpdate;
    public event Action<TaskArtifactUpdateEvent>? OnArtifactUpdate;
    public event Action<string>? OnDebugMessage;
    public CancellationTokenSource? CancellationToken = new CancellationTokenSource();

    /// <summary>
    /// Start streaming messages from an A2A endpoint
    /// </summary>
    public async Task StartStreamingAsync(string endpoint, List<Part> parts)
    {

        var message = new ContainerAgentMessage(parts.ToArray(), endpoint);
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/A2AContainer/SendStreamingMessage")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.Token);
            response.EnsureSuccessStatusCode();

            await ProcessStreamAsync(response, CancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            // Stream was cancelled, this is expected
        }
        catch (Exception ex)
        {
            OnDebugMessage?.Invoke($"Streaming error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop the current streaming operation
    /// </summary>
    public void StopStreaming()
    {
        CancellationToken?.Cancel();
    }

    private async Task ProcessStreamAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        string eventType = "";
        StringBuilder dataBuilder = new();

        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            if (string.IsNullOrEmpty(line))
            {
                // Empty line indicates end of event
                if (!string.IsNullOrEmpty(eventType) && dataBuilder.Length > 0)
                {
                    ProcessEvent(eventType, dataBuilder.ToString());
                }
                eventType = "";
                dataBuilder.Clear();
                continue;
            }

            if (line.StartsWith("event:"))
            {
                eventType = line.Substring(6).Trim();
            }
            else if (line.StartsWith("data:"))
            {
                var data = line.Substring(5).Trim();
                dataBuilder.AppendLine(data);
            }
        }
    }

    private void ProcessEvent(string eventType, string data)
    {
        try
        {
            var eventData = new StreamingEventData
            {
                EventType = eventType,
                Data = data,
                Timestamp = DateTime.Now
            };

            //OnDebugMessage?.Invoke($"[{eventType}] {data}");

            switch (eventType)
            {
                case "message_event":
                    OnMessageReceivedEvent(eventData);
                    break;
                case "task_status_update_event":
                    OnTaskStatusUpdateEvent(eventData);
                    break;
                case "task_update_event":
                    OnTaskUpdateEvent(eventData);
                    break;
                case "artifact_update_event":
                    OnArtifactUpdateEvent(eventData);
                    break;
                default:
                    OnDebugMessage?.Invoke($"Unknown event type: {eventType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            OnDebugMessage?.Invoke($"Error processing event: {ex.Message}");
        }
    }

    private void OnMessageReceivedEvent(StreamingEventData eventData)
    {
        var agentMessage = JsonSerializer.Deserialize<AgentMessage>(eventData.Data, _jsonOptions);
        if(agentMessage == null)
        {
            OnDebugMessage?.Invoke("Failed to deserialize AgentMessage");
            return;
        }
        OnMessageReceived?.Invoke(agentMessage);
    }

    private void OnTaskStatusUpdateEvent(StreamingEventData eventData)
    {
        var taskStatus = JsonSerializer.Deserialize<TaskStatusUpdateEvent>(eventData.Data, _jsonOptions);
        if (taskStatus == null)
        {
            OnDebugMessage?.Invoke("Failed to deserialize TaskStatusUpdateEvent");
            return;
        }
        OnTaskStatusUpdate?.Invoke(taskStatus);
    }
    private void OnTaskUpdateEvent(StreamingEventData eventData)
    {
        var taskUpdate = JsonSerializer.Deserialize<AgentTask>(eventData.Data, _jsonOptions);
        if (taskUpdate == null)
        {
            OnDebugMessage?.Invoke("Failed to deserialize AgentTask");
            return;
        }
        OnTaskUpdate?.Invoke(taskUpdate);
    }
    private void OnArtifactUpdateEvent(StreamingEventData eventData)
    {
        var artifactUpdate = JsonSerializer.Deserialize<TaskArtifactUpdateEvent>(eventData.Data, _jsonOptions);
        if (artifactUpdate == null)
        {
            OnDebugMessage?.Invoke("Failed to deserialize TaskArtifactUpdateEvent");
            return;
        }
        OnArtifactUpdate?.Invoke(artifactUpdate);
    }

    public void Dispose()
    {
        CancellationToken?.Cancel();
        CancellationToken?.Dispose();
    }
}

/// <summary>
/// Data structure for streaming events
/// </summary>
public class StreamingEventData
{
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}