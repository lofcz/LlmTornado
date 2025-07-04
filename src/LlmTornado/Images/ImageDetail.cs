using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Images;

/// <summary>
///     Represents available response formats for image generation endpoints
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageDetail
{
    /// <summary>
    /// Automatically decides whether to use <see cref="High"/> or <see cref="Low"/>
    /// </summary>
    [EnumMember(Value = "auto")] 
    Auto,
    
    /// <summary>
    /// Images will be tiled.
    /// </summary>
    [EnumMember(Value = "high")] 
    High,
    
    /// <summary>
    /// Images will be passed as one tile. Some Providers require images to not exceed certain size for this.
    /// </summary>
    [EnumMember(Value = "low")] 
    Low
}