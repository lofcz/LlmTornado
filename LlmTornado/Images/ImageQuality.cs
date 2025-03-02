using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Images;

/// <summary>
/// Represents available qualities for image generation endpoints, only supported by dalle3
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TornadoImageQualities
{
    /// <summary>
    /// Standard image
    /// </summary>
    [EnumMember(Value = "standard")]
    Standard,
    
    /// <summary>
    /// HD image
    /// </summary>
    [EnumMember(Value = "hd")]
    Hd
}