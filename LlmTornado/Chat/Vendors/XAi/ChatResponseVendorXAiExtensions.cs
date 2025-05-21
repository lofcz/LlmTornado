using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.XAi;

/// <summary>
/// Extensions to chat response supported only by xAI.
/// </summary>
public class ChatResponseVendorXAiExtensions
{
    /// <summary>
    /// Citations if Live Search was enabled for the request.
    /// </summary>
    [JsonProperty("citations")]
    public List<string>? Citations { get; set; }
}