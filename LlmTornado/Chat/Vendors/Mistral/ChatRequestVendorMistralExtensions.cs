using System.Collections.Generic;
using LlmTornado.Caching;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Mistral;

/// <summary>
/// Chat features supported only by Mistral.
/// </summary>
public class ChatRequestVendorMistralExtensions
{
    /// <summary>
    /// Whether to inject a safety prompt before all conversations.
    /// </summary>
    public bool? SafePrompt { get; set; }
    
    /// <summary>
    /// Enable users to specify expected results, optimizing response times by leveraging known or predictable content. This approach is especially effective for updating text documents or code files with minimal changes, reducing latency while maintaining high-quality results.
    /// </summary>
    public string? Prediction { get; set; }
    
    /// <summary>
    /// Random Seed (integer) or Random Seed (null) (Random Seed)
    /// The seed to use for random sampling. If set, different calls will generate deterministic results.
    /// </summary>
    public int? RandomSeed { get; set; }
    
    /// <summary>
    /// The role of the prefix message is to force the model to start its answer by the content of the message.
    /// Important: the conversation has to end with a user message for the prefix to be applied (at the time of sending the request).
    /// </summary>
    public string? Prefix { get; set; }
}