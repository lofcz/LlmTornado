using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     Details of the message creation by the run step.
/// </summary>
public sealed class RunStepMessageCreation
{
    /// <summary>
    ///     The ID of the message that was created by this run step.
    /// </summary>
    [JsonProperty("message_id")]
    public string MessageId { get; set; } = null!;
}