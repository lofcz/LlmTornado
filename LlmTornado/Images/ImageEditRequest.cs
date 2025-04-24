using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Images.Models;
using Newtonsoft.Json;

namespace LlmTornado.Images;

/// <summary>
///     Represents a request to the Images API.  Mostly matches the parameters in
///     <see href="https://platform.openai.com/docs/api-reference/images/create">the OpenAI docs</see>, although some have
///     been renamed or expanded into single/multiple properties for ease of use.
/// </summary>
public class ImageEditRequest
{
	/// <summary>
	///     Creates a new, empty <see cref="ImageGenerationRequest" />
	/// </summary>
	public ImageEditRequest()
    {
    }
	
	/// <summary>
	///     Creates a new, minimal <see cref="ImageGenerationRequest" />
	/// </summary>
	public ImageEditRequest(string prompt)
	{
		Prompt = prompt;
	}

	/// <summary>
	///     Creates a new <see cref="ImageEditRequest" /> with the specified parameters
	/// </summary>
	/// <param name="image"></param>
	/// <param name="prompt">A text description of the desired image(s). The maximum length is 1000 characters.</param>
	/// <param name="numOfImages">How many different choices to request for each prompt. Defaults to 1.</param>
	/// <param name="size">The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024.</param>
	/// <param name="user">A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.</param>
	/// <param name="responseFormat">The format in which the generated images are returned. Must be one of url or b64_json.</param>
	/// <param name="model">Which model will be used.</param>
	public ImageEditRequest(TornadoInputFile image, string prompt, int numOfImages = 1, TornadoImageSizes? size = null, string? user = null, TornadoImageResponseFormats? responseFormat = null, ImageModel? model = null)
    {
        Image = image;
        Prompt = prompt;
        NumOfImages = numOfImages;
        User = user;
        Size = size;
        ResponseFormat = responseFormat;
        Model = model;
    }
	
	/// <summary>
	///     A model to use.
	/// </summary>
	[JsonProperty("model")]
	[JsonConverter(typeof(ImageModelJsonConverter))]
	public ImageModel? Model { get; set; } = ImageModel.OpenAi.Dalle.V2;

	/// <summary>
	/// The image(s) to edit. Must be a supported image file or an array of images. For gpt-image-1, each image should be a png, webp, or jpg file less than 25MB.
	/// For dall-e-2, you can only provide one image, and it should be a square png file less than 4MB.
	/// </summary>
	[JsonIgnore]
    public TornadoInputFile? Image { get; set; }
	
	/// <summary>
	/// The image(s) to edit. Must be a supported image file or an array of images. For gpt-image-1, each image should be a png, webp, or jpg file less than 25MB.
	/// For dall-e-2, you can only provide one image, and it should be a square png file less than 4MB.
	/// Setting this has priority over <see cref="Image"/>
	/// </summary>
	[JsonIgnore]
	public List<TornadoInputFile>? Images { get; set; }

	/// <summary>
	/// Serialized image/images.
	/// </summary>
	[JsonProperty("image")]
	internal object? SerializedImages => Images?.Count > 0 ? Images : Image;

	/// <summary>
	///     An additional image whose fully transparent areas (e.g. where alpha is zero) indicate where image should be edited.
	///     Must be a valid PNG file, less than 4MB, and have the same.
	/// </summary>
	[JsonProperty("mask")]
    public TornadoInputFile? Mask { get; set; }

	/// <summary>
	///     A text description of the desired image(s). The maximum length is 1000 characters for dall-e-2, and 32000 characters for gpt-image-1.
	/// </summary>
	[JsonProperty("prompt")]
    public string Prompt { get; set; }
	
	/// <summary>
	///     Number of images to generate
	/// </summary>
	[JsonProperty("n")]
    public int? NumOfImages { get; set; }

	/// <summary>
	///     The size of the generated images. Must be one of 256x256, 512x512, or 1024x1024. Defauls to 1024x1024
	/// </summary>
	[JsonProperty("size")]
    public TornadoImageSizes? Size { get; set; }
	
	/// <summary>
	///     Either empty or "hd" for dalle3.
	/// </summary>
	[JsonProperty("quality")]
	public TornadoImageQualities? Quality { get; set; }
	
	/// <summary>
	///     The format in which the generated images are returned. Must be one of url or b64_json. URLs are only valid for 60 minutes after the image has been generated. This parameter is only supported for dall-e-2, as gpt-image-1 will always return base64-encoded images.
	/// </summary>
	[JsonProperty("response_format")]
    public TornadoImageResponseFormats? ResponseFormat { get; set; }

	/// <summary>
	///     A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse. Optional.
	/// </summary>
	[JsonProperty("user")]
    public string? User { get; set; }
}