using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Images.Models;
using LlmTornado.Images.Vendors.Google;
using Newtonsoft.Json;

namespace LlmTornado.Images;

/// <summary>
///     Represents a request to the Images API.  Mostly matches the parameters in
///     <see href="https://platform.openai.com/docs/api-reference/images/create">the OpenAI docs</see>, although some have
///     been renamed or expanded into single/multiple properties for ease of use.
/// </summary>
public class ImageGenerationRequest
{
	/// <summary>
	///     Creates a new, empty <see cref="ImageGenerationRequest" />
	/// </summary>
	public ImageGenerationRequest()
    {
    }

	/// <summary>
	///     Creates a new <see cref="ImageGenerationRequest" /> with the specified parameters
	/// </summary>
	/// <param name="prompt">A text description of the desired image(s). The maximum length is 1000 characters.</param>
	/// <param name="numOfImages">How many different choices to request for each prompt.  Defaults to 1.</param>
	/// <param name="size">The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024.</param>
	/// <param name="user">A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.</param>
	/// <param name="responseFormat">The format in which the generated images are returned. Must be one of url or b64_json.</param>
	/// <param name="model">Model to use</param>
	/// <param name="quality">Empty or "hd" for dalle3</param>
	/// <param name="style">Empty or "vivid" / "natural" for dalle3</param>
	public ImageGenerationRequest(string prompt, int? numOfImages = 1, TornadoImageSizes? size = null, string? user = null, TornadoImageResponseFormats? responseFormat = null, ImageModel? model = null, TornadoImageQualities? quality = null, TornadoImageStyles? style = null)
    {
        Prompt = prompt;
        NumOfImages = numOfImages;
        User = user;
        Size = size ?? TornadoImageSizes.Size1024x1024;
        ResponseFormat = responseFormat ?? TornadoImageResponseFormats.Url;
        Model = model ?? ImageModel.OpenAi.Dalle.V3;
        Quality = quality;
        Style = style;
    }

	/// <summary>
	///     A text description of the desired image(s). The maximum length is 1000 characters.
	/// </summary>
	[JsonProperty("prompt")]
    public string Prompt { get; set; }

	/// <summary>
	///     How many different choices to request for each prompt.  Defaults to 1.
	/// </summary>
	[JsonProperty("n")]
    public int? NumOfImages { get; set; } = 1;

	/// <summary>
	///     A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse. Optional.
	/// </summary>
	[JsonProperty("user")]
    public string? User { get; set; }

	/// <summary>
	///     The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024. Defaults to 1024x1024
	/// </summary>
	[JsonProperty("size")]
    public TornadoImageSizes Size { get; set; }

	/// <summary>
	///     The format in which the generated images are returned. Must be one of url or b64_json. Defaults to Url.
	/// </summary>
	[JsonProperty("response_format")]
    public TornadoImageResponseFormats ResponseFormat { get; set; }

	/// <summary>
	///     A model to use.
	/// </summary>
	[JsonProperty("model")]
	[JsonConverter(typeof(ImageModelJsonConverter))]
	public ImageModel? Model { get; set; } = ImageModel.OpenAi.Dalle.V3;

	/// <summary>
	///     Either empty or "hd" for dalle3.
	/// </summary>
	[JsonProperty("quality")]
    public TornadoImageQualities? Quality { get; set; }

	/// <summary>
	///     Either empty or "vivid" or "natural" for dalle3.
	/// </summary>
	[JsonProperty("style")]
    public TornadoImageStyles? Style { get; set; }
	
	/// <summary>
	///		Features supported only by a single/few providers with no shared equivalent.
	/// </summary>
	[JsonIgnore]
	public ImageGenerationRequestVendorExtensions? VendorExtensions { get; set; }
	
	[JsonIgnore]
	internal string? UrlOverride { get; set; }

	internal void OverrideUrl(string url)
	{
		UrlOverride = url;
	}
	
	/// <summary>
	///		Serializes the chat request into the request body, based on the conventions used by the LLM provider.
	/// </summary>
	/// <param name="provider"></param>
	/// <returns></returns>
	public TornadoRequestContent Serialize(IEndpointProvider provider)
	{
		return SerializeMap.TryGetValue(provider.Provider, out Func<ImageGenerationRequest, IEndpointProvider, string>? serializerFn) ? new TornadoRequestContent(serializerFn.Invoke(this, provider), UrlOverride) : new TornadoRequestContent(string.Empty, UrlOverride);
	}
	
	private static readonly FrozenDictionary<LLmProviders, Func<ImageGenerationRequest, IEndpointProvider, string>> SerializeMap = new Dictionary<LLmProviders, Func<ImageGenerationRequest, IEndpointProvider, string>>
	{
		{ LLmProviders.OpenAi, (x, y) => JsonConvert.SerializeObject(x, EndpointBase.NullSettings)},
		{ LLmProviders.Google, (x, y) => JsonConvert.SerializeObject(new VendorGoogleImageRequest(x, y), EndpointBase.NullSettings) }
	}.ToFrozenDictionary();
}

internal class ImageModelJsonConverter : JsonConverter<ImageModel>
{
	public override void WriteJson(JsonWriter writer, ImageModel? value, JsonSerializer serializer)
	{
		writer.WriteValue(value?.GetApiName);
	}

	public override ImageModel? ReadJson(JsonReader reader, Type objectType, ImageModel? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return existingValue;
	}
}
