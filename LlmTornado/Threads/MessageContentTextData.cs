using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// 
/// </summary>
public class MessageContentTextData
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("annotations")]
    public IReadOnlyList<Annotation>? Annotations { get; set; }
}