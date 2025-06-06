using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a strategy for truncating data, specifying the type of truncation
/// and an optional parameter for the number of messages to retain.
/// </summary>
public class TruncationStrategy
{
    /// <summary>
    ///     The truncation strategy to use for the thread. The default is auto. If set to last_messages, the thread will be truncated to the n most recent messages in the thread.
    ///     When set to auto, messages in the middle of the thread will be dropped to fit the context length of the model, max_prompt_tokens.
    /// </summary>
    [JsonProperty("type")]
    public required TruncationStrategyTypes TruncationStrategyType { get; set; }

    /// <summary>
    ///     The number of most recent messages from the thread when constructing the context for the run.
    /// </summary>
    [JsonProperty("last_messages")]
    public int? LastMessages { get; set; }
}

/// <summary>
/// Defines the types of truncation strategies available for managing
/// and limiting the scope of data, such as automatically determining
/// an appropriate strategy or retaining a specified number of messages.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TruncationStrategyTypes
{
    /// <summary>
    /// Specifies an automatic truncation strategy where the system
    /// dynamically determines the appropriate truncation behavior
    /// based on context or predefined logic.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,

    /// <summary>
    /// Specifies a truncation strategy where only the most recent messages are retained.
    /// This strategy retains a configurable number of the latest messages, discarding older data.
    /// Useful for scenarios where maintaining the context of recent messages is critical.
    /// </summary>
    [EnumMember(Value = "last_messages")]
    LastMessages
}