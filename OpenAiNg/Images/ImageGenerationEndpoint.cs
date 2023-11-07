using System.Threading.Tasks;

namespace OpenAiNg.Images;

/// <summary>
///     Given a prompt, the model will generate a new image.
/// </summary>
public class ImageGenerationEndpoint : EndpointBase, IImageGenerationEndpoint
{
	/// <summary>
	///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
	///     <see cref="OpenAiApi" /> as <see cref="OpenAiApi.ImageGenerations" />.
	/// </summary>
	/// <param name="api"></param>
	internal ImageGenerationEndpoint(OpenAiApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.  For example, "image".
	/// </summary>
	protected override string Endpoint => "images/generations";

	/// <summary>
	///     Ask the API to Creates an image given a prompt.
	/// </summary>
	/// <param name="input">A text description of the desired image(s)</param>
	/// <returns>Asynchronously returns the image result. Look in its <see cref="Data.Url" /> </returns>
	public Task<ImageResult?> CreateImageAsync(string input)
    {
        ImageGenerationRequest req = new(input);
        return CreateImageAsync(req);
    }

	/// <summary>
	///     Ask the API to Creates an image given a prompt.
	/// </summary>
	/// <param name="request">Request to be send</param>
	/// <returns>Asynchronously returns the image result. Look in its <see cref="Data.Url" /> </returns>
	public Task<ImageResult?> CreateImageAsync(ImageGenerationRequest request)
    {
        return HttpPost<ImageResult>(postData: request);
    }
}