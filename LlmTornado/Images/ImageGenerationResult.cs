using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using LlmTornado.Images.Vendors.Google;
using Newtonsoft.Json;

namespace LlmTornado.Images;

/// <summary>
///     Represents an image result returned by the Image API.
/// </summary>
public class ImageGenerationResult : ApiResultBase
{
	/// <summary>
	///     List of results of the embedding
	/// </summary>
	[JsonProperty("data")]
    public List<TornadoGeneratedImage>? Data { get; set; }

	/// <summary>
	///     Gets the url or base64-encoded image data of the first result, or null if there are no results
	/// </summary>
	/// <returns></returns>
	public override string? ToString()
    {
        if (Data?.Count > 0) return Data[0].Url ?? Data[0].Base64;

        return null;
    }
	
	internal static ImageGenerationResult? Deserialize(LLmProviders provider, string jsonData, string? postData)
	{
		return provider switch
		{
			LLmProviders.OpenAi => JsonConvert.DeserializeObject<ImageGenerationResult>(jsonData),
			LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleImageResult>(jsonData)?.ToChatResult(postData),
			_ => JsonConvert.DeserializeObject<ImageGenerationResult>(jsonData)
		};
	}
}

/// <summary>
///     Data returned from the Image API.
/// </summary>
public class TornadoGeneratedImage
{
	/// <summary>
	///     The url of the image result
	/// </summary>
	[JsonProperty("url")]
    public string? Url { get; set; }

	/// <summary>
	///     The base64-encoded image data as returned by the API
	/// </summary>
	[JsonProperty("b64_json")]
    public string? Base64 { get; set; }
	
	/// <summary>
	///     Mime type of the image. Supported only by Google.
	/// </summary>
	[JsonIgnore]
	public string? MimeType { get; set; }
}