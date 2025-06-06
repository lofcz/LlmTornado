using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Images;

/// <summary>
/// Represents available styles for image generation endpoints, only supported by dalle3.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TornadoImageStyles
{
    /// <summary>
    /// Good for photographs.
    /// </summary>
    [EnumMember(Value = "natural")] 
    Natural,
    
    /// <summary>
    /// Catchy, lively.
    /// </summary>
    [EnumMember(Value = "vivid")] 
    Vivid
}