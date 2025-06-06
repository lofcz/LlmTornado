using System.Collections.Generic;
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
	/// For gpt-image-1 only, the token usage information for the image generation.
	/// </summary>
	[JsonProperty("usage")]
	public TornadoImageUsage? Usage { get; set; }
	
	/// <summary>
	///     Gets the url or base64-encoded image data of the first result, or null if there are no results
	/// </summary>
	/// <returns></returns>
	public override string? ToString()
    {
	    if (Data?.Count > 0)
	    {
		    return Data[0].Url ?? Data[0].Base64;
	    }

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
/// For gpt-image-1 only, the token usage information for the image generation.
/// </summary>
public class TornadoImageUsage
{
	/// <summary>
	/// The number of tokens (images and text) in the input prompt.
	/// </summary>
	[JsonProperty("input_tokens")]
	public int InputTokens { get; set; }
	
	/// <summary>
	/// The input tokens detailed information for the image generation.
	/// </summary>
	[JsonProperty("input_tokens_details")]
	public TornadoImageUsageDetails? InputTokenDetails { get; set; }
	
	/// <summary>
	/// The number of image tokens in the output image.
	/// </summary>
	[JsonProperty("output_tokens")]
	public int OutputTokens { get; set; }
	
	/// <summary>
	/// The total number of tokens (images and text) used for the image generation.
	/// </summary>
	[JsonProperty("total_tokens")]
	public int TotalTokens { get; set; }
}

/// <summary>
/// The input tokens detailed information for the image generation.
/// </summary>
public class TornadoImageUsageDetails
{
	/// <summary>
	/// The number of image tokens in the input prompt.
	/// </summary>
	[JsonProperty("image_tokens")]
	public int ImageTokens { get; set; }
	
	/// <summary>
	/// The number of text tokens in the input prompt.
	/// </summary>
	[JsonProperty("text_tokens")]
	public int TextTokens { get; set; }
}

/// <summary>
///     Data returned from the Image API.
/// </summary>
public class TornadoGeneratedImage
{
	/// <summary>
	///     When using dall-e-2 or dall-e-3, the URL of the generated image if response_format is set to url (default value). Unsupported for gpt-image-1.
	/// </summary>
	[JsonProperty("url")]
    public string? Url { get; set; }

	/// <summary>
	///     The base64-encoded JSON of the generated image. Default value for gpt-image-1, and only present if response_format is set to b64_json for dall-e-2 and dall-e-3.
	/// </summary>
	[JsonProperty("b64_json")]
    public string? Base64 { get; set; }
	
	/// <summary>
	///     Mime type of the image. Supported only by Google.
	/// </summary>
	[JsonIgnore]
	public string? MimeType { get; set; }
	
	/// <summary>
	///		For dall-e-3 only, the revised prompt that was used to generate the image.
	/// </summary>
	[JsonProperty("revised_prompt")]
	public string? RevisedPrompt { get; set; }
}