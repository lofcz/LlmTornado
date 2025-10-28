using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Images;

/// <summary>
/// Output format for generated images (gpt-image-1 only).
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TornadoImageOutputFormats
{
	/// <summary>
	/// PNG format (default, supports transparency).
	/// </summary>
	[EnumMember(Value = "png")]
	Png,
	
	/// <summary>
	/// JPEG format (does not support transparency).
	/// </summary>
	[EnumMember(Value = "jpeg")]
	Jpeg,
	
	/// <summary>
	/// WebP format (supports transparency).
	/// </summary>
	[EnumMember(Value = "webp")]
	Webp
}

