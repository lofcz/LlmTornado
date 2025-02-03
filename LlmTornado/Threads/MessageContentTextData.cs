using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// 
/// </summary>
public class MessageContentTextData
{
    /// <summary>
    ///     The data that makes up the text.
    /// </summary>
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// A collection of annotations that provide additional metadata
    /// or context about specific segments of the text.
    /// </summary>
    [JsonProperty("annotations")]
    public IReadOnlyList<MessageAnnotation>? Annotations { get; set; }
}