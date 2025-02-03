using Newtonsoft.Json;

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
    public required string TruncationStrategyType { get; set; } = "auto";
    
    /// <summary>
    ///     The number of most recent messages from the thread when constructing the context for the run.
    /// </summary>
    [JsonProperty("last_messages")]
    public int? LastMessages { get; set; }
}