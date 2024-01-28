using Newtonsoft.Json;
using OpenAiNg.Models;

namespace OpenAiNg.Images;

/// <summary>
///     Represents a request to the Images API.  Mostly matches the parameters in
///     <see href="https://platform.openai.com/docs/api-reference/images/create">the OpenAI docs</see>, although some have
///     been renamed or expanded into single/multiple properties for ease of use.
/// </summary>
public class ImageGenerationRequest
{
	/// <summary>
	///     Cretes a new, empty <see cref="ImageGenerationRequest" />
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
	/// <param name="model">
	///     Model to use, this should be either <see cref="Models.Model.Dalle2" /> or
	///     <see cref="Models.Model.Dalle3" />
	/// </param>
	/// <param name="quality">Empty or "hd" for dalle3</param>
	/// <param name="style">Empty or "vivid" / "natural" for dalle3</param>
	public ImageGenerationRequest(string prompt, int? numOfImages = 1, ImageSize? size = null, string? user = null, ImageResponseFormat? responseFormat = null, Model? model = null, ImageQuality? quality = null, ImageStyles? style = null)
    {
        Prompt = prompt;
        NumOfImages = numOfImages;
        User = user;
        Size = size ?? ImageSize._1024;
        ResponseFormat = responseFormat ?? ImageResponseFormat.Url;
        Model = model?.ModelID ?? Models.Model.Dalle2.ModelID;
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
	///     The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024. Defauls to 1024x1024
	/// </summary>
	[JsonProperty("size")]
    [JsonConverter(typeof(ImageSize.ImageSizeJsonConverter))]
    public ImageSize Size { get; set; }

	/// <summary>
	///     The format in which the generated images are returned. Must be one of url or b64_json. Defaults to Url.
	/// </summary>
	[JsonProperty("response_format")]
    [JsonConverter(typeof(ImageResponseFormat.ImageResponseJsonConverter))]
    public ImageResponseFormat ResponseFormat { get; set; }

	/// <summary>
	///     A model to use.
	/// </summary>
	[JsonProperty("model")]
    public string Model { get; set; }

	/// <summary>
	///     Either empty or "hd" for dalle3.
	/// </summary>
	[JsonProperty("quality")]
    [JsonConverter(typeof(ImageQuality.ImageQualityJsonConverter))]
    public ImageQuality? Quality { get; set; }

	/// <summary>
	///     Either empty or "vivid" or "natural" for dalle3.
	/// </summary>
	[JsonProperty("style")]
    [JsonConverter(typeof(ImageStyles.ImageStyleJsonConverter))]
    public ImageStyles? Style { get; set; }
}