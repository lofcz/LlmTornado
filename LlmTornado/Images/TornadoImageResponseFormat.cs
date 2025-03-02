using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Images;

/// <summary>
/// Formats in which an image can be returned.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TornadoImageResponseFormats
{
    /// <summary>
    /// The image will be returned via a publicly reachable URL.
    /// Supported by: OpenAi.
    /// </summary>
    [EnumMember(Value = "url")]
    Url,
    
    /// <summary>
    /// The image will be returned as JSON.
    /// Supported by: OpenAi, Google.
    /// </summary>
    [EnumMember(Value = "b64_json")]
    Base64
}