using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Images;

/// <summary>
/// Background transparency options for image generation.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TornadoImageBackgrounds
{
	/// <summary>
	/// Transparent background (supported by gpt-image-1). Output format must support transparency (png or webp).
	/// </summary>
	[EnumMember(Value = "transparent")]
	Transparent,
	
	/// <summary>
	/// Opaque background (supported by gpt-image-1).
	/// </summary>
	[EnumMember(Value = "opaque")]
	Opaque,
	
	/// <summary>
	/// Auto (default) - model automatically determines the best background.
	/// </summary>
	[EnumMember(Value = "auto")]
	Auto
}

