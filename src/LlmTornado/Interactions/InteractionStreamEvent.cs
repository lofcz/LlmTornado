using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Threads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

/// <summary>
/// SSE event from a streaming Interactions API call.
/// Supports both May 2026 events (<c>step.delta</c>) and legacy events (<c>content.delta</c>).
/// </summary>
public class InteractionStreamEvent
{
    /// <summary>
    /// Raw SSE event type.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Parsed event type with legacy aliases normalized.
    /// </summary>
    public InteractionStreamEventTypes NormalizedEventType => InteractionStreamEventTypesMapper.FromRaw(EventType);

    /// <summary>
    /// Raw JSON payload.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Parsed interaction resource (for lifecycle events).
    /// </summary>
    public Interaction? Interaction { get; set; }

    /// <summary>
    /// Step index for step events.
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// Partial step delta (text, arguments, etc.).
    /// </summary>
    public JObject? Delta { get; set; }

    /// <summary>
    /// Step snapshot on <c>step.start</c>.
    /// </summary>
    public InteractionStep? Step { get; set; }

    internal static InteractionStreamEvent FromServerSentEvent(ServerSentEvent sse)
    {
        InteractionStreamEvent evt = new InteractionStreamEvent
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
            JObject obj = JObject.Parse(sse.Data);
            evt.Index = obj["index"]?.Value<int?>();

            if (obj["delta"] is JObject delta)
            {
                evt.Delta = delta;
            }

            if (obj["step"] is JObject stepObj)
            {
                evt.Step = stepObj.ToObject<InteractionStep>(JsonSerializer.Create(VendorGoogleInteractionsJson.Settings));
            }

            if (obj["interaction"] is JObject interactionObj)
            {
                evt.Interaction = VendorGoogleInteractionsJson.DeserializeInteraction(interactionObj.ToString(Formatting.None));
            }
            else if (obj["event_type"]?.ToString() is "interaction.completed" && obj["interaction"] is null && obj["id"] is not null)
            {
                evt.Interaction = VendorGoogleInteractionsJson.DeserializeInteraction(sse.Data);
            }
        }
        catch
        {
            // leave partially parsed
        }

        return evt;
    }
}

/// <summary>
/// Normalized Interactions streaming event types (May 2026 + legacy aliases).
/// </summary>
public enum InteractionStreamEventTypes
{
    Unknown,
    InteractionCreated,
    InteractionCompleted,
    InteractionInProgress,
    InteractionRequiresAction,
    InteractionStatusUpdate,
    StepStart,
    StepDelta,
    StepStop,
    Error
}

internal static class InteractionStreamEventTypesMapper
{
    private static readonly FrozenDictionary<string, InteractionStreamEventTypes> Map = new Dictionary<string, InteractionStreamEventTypes>(StringComparer.Ordinal)
    {
        ["interaction.created"] = InteractionStreamEventTypes.InteractionCreated,
        ["interaction.start"] = InteractionStreamEventTypes.InteractionCreated,
        ["interaction.completed"] = InteractionStreamEventTypes.InteractionCompleted,
        ["interaction.complete"] = InteractionStreamEventTypes.InteractionCompleted,
        ["interaction.in_progress"] = InteractionStreamEventTypes.InteractionInProgress,
        ["interaction.requires_action"] = InteractionStreamEventTypes.InteractionRequiresAction,
        ["interaction.status_update"] = InteractionStreamEventTypes.InteractionStatusUpdate,
        ["step.start"] = InteractionStreamEventTypes.StepStart,
        ["content.start"] = InteractionStreamEventTypes.StepStart,
        ["step.delta"] = InteractionStreamEventTypes.StepDelta,
        ["content.delta"] = InteractionStreamEventTypes.StepDelta,
        ["step.stop"] = InteractionStreamEventTypes.StepStop,
        ["content.stop"] = InteractionStreamEventTypes.StepStop,
        ["error"] = InteractionStreamEventTypes.Error
    }.ToFrozenDictionary();

    internal static InteractionStreamEventTypes FromRaw(string? eventType) =>
        eventType is not null && Map.TryGetValue(eventType, out InteractionStreamEventTypes mapped)
            ? mapped
            : InteractionStreamEventTypes.Unknown;
}

/// <summary>
/// Handler for Interactions API streaming events.
/// </summary>
public class InteractionStreamEventHandler
{
    /// <summary>Called for each parsed stream event.</summary>
    public Func<InteractionStreamEvent, ValueTask>? OnEvent { get; set; }

    /// <summary>Called for raw SSE events.</summary>
    public Func<ServerSentEvent, ValueTask>? OnSse { get; set; }

    /// <summary>Called when a text delta arrives (<c>step.delta</c> or legacy <c>content.delta</c>).</summary>
    public Func<string, ValueTask>? OnTextDelta { get; set; }

    /// <summary>Called when a thought or thought summary delta arrives.</summary>
    public Func<string, ValueTask>? OnThoughtDelta { get; set; }

    /// <summary>Called when an image delta arrives (base64 data).</summary>
    public Func<string, ValueTask>? OnImageDelta { get; set; }

    /// <summary>Called when the interaction completes.</summary>
    public Func<Interaction, ValueTask>? OnCompleted { get; set; }

    /// <summary>Called when an error event arrives.</summary>
    public Func<InteractionError, ValueTask>? OnError { get; set; }
}
