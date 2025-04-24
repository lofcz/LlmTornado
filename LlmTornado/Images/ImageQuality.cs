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
    /// Standard image (supported by dall-e-3)
    /// </summary>
    [EnumMember(Value = "standard")]
    Standard,
    
    /// <summary>
    /// HD image (supported by dall-e-3)
    /// </summary>
    [EnumMember(Value = "hd")]
    Hd,
    
    /// <summary>
    /// Low quality (supported by gpt-image-1).
    /// </summary>
    [EnumMember(Value = "low")]
    Low,
    
    /// <summary>
    /// Medium quality (supported by gpt-image-1).
    /// </summary>
    [EnumMember(Value = "medium")]
    Medium,
    
    /// <summary>
    /// High quality (supported by gpt-image-1).
    /// </summary>
    [EnumMember(Value = "high")]
    High,
    
    /// <summary>
    /// auto (default value) will automatically select the best quality for the given model.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto
}