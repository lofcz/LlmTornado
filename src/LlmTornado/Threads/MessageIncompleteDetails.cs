using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents the details related to an incomplete message state.
/// Provides information about why the message was not completed successfully.
/// </summary>
public class MessageIncompleteDetails
{
    /// <summary>
    /// Describes the cause or justification for the incomplete state of a message.
    /// </summary>
    [JsonProperty("reason")]
    public string Reason { get; set; } = null!;
}