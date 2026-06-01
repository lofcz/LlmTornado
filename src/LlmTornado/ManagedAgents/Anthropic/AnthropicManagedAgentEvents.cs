using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Threads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Text block in a user message event.
/// </summary>
public class AnthropicManagedAgentTextBlock
{
    [JsonProperty("type")]
    public string Type { get; set; } = "text";

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// User message event sent to a session.
/// </summary>
public class AnthropicManagedAgentUserMessageEvent
{
    [JsonProperty("type")]
    public string Type { get; set; } = "user.message";

    [JsonProperty("content")]
    public List<AnthropicManagedAgentTextBlock> Content { get; set; } = [];
}

/// <summary>
/// Request body for <c>POST /v1/sessions/{id}/events</c>.
/// </summary>
public class AnthropicManagedAgentSendEventsRequest
{
    [JsonProperty("events")]
    public List<object> Events { get; set; } = [];

    internal string Serialize() => VendorAnthropicManagedAgentsJson.Serialize(this);

    public static AnthropicManagedAgentSendEventsRequest UserMessage(string text) => new()
    {
        Events =
        [
            new AnthropicManagedAgentUserMessageEvent
            {
                Content = [new AnthropicManagedAgentTextBlock { Text = text }]
            }
        ]
    };
}

/// <summary>
/// Session event from list or SSE stream.
/// </summary>
public class AnthropicManagedAgentEvent
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("processed_at")]
    public string? ProcessedAt { get; set; }

    [JsonProperty("content")]
    public JArray? Content { get; set; }

    [JsonProperty("stop_reason")]
    public JObject? StopReason { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("model_usage")]
    public JObject? ModelUsage { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JToken>? ExtensionData { get; set; }
}

/// <summary>
/// Parsed SSE event from <c>GET /v1/sessions/{id}/events/stream</c>.
/// </summary>
public class AnthropicManagedAgentStreamEvent
{
    public string? EventType { get; set; }
    public string Data { get; set; } = string.Empty;
    public AnthropicManagedAgentEvent? Event { get; set; }

    internal static AnthropicManagedAgentStreamEvent FromServerSentEvent(ServerSentEvent sse)
    {
        AnthropicManagedAgentStreamEvent evt = new AnthropicManagedAgentStreamEvent
        {
            EventType = sse.EventType,
            Data = sse.Data
        };

        if (string.IsNullOrWhiteSpace(sse.Data))
        {
            return evt;
        }

        try
        {
            evt.Event = JsonConvert.DeserializeObject<AnthropicManagedAgentEvent>(sse.Data, VendorAnthropicManagedAgentsJson.Settings);
        }
        catch
        {
            // keep raw Data for callers that need it
        }

        return evt;
    }
}

/// <summary>
/// Optional callbacks for Managed Agent session SSE streams.
/// </summary>
public class AnthropicManagedAgentStreamEventHandler
{
    public Func<ServerSentEvent, Task>? OnSse { get; set; }
    public Func<AnthropicManagedAgentStreamEvent, Task>? OnEvent { get; set; }
    public Func<string, Task>? OnTextDelta { get; set; }
    public Func<AnthropicManagedAgentEvent, Task>? OnSessionIdle { get; set; }
}
