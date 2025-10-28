using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Images;

/// <summary>
/// Controls how much effort the model will exert to match the style and features of input images.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TornadoImageInputFidelity
{
	/// <summary>
	/// Low fidelity - less effort to match input image style (default, supported by gpt-image-1).
	/// </summary>
	[EnumMember(Value = "low")]
	Low,
	
	/// <summary>
	/// High fidelity - more effort to match input image style and facial features (supported by gpt-image-1, not gpt-image-1-mini).
	/// </summary>
	[EnumMember(Value = "high")]
	High
}

