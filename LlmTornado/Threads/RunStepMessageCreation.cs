using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

public sealed class RunStepMessageCreation
{
    /// <summary>
    ///     The ID of the message that was created by this run step.
    /// </summary>
    [JsonInclude]
    [JsonProperty("message_id")]
    public string MessageId { get; private set; }
}