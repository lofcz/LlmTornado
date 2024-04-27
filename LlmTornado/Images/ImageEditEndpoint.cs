using System.Threading.Tasks;
using LlmTornado.Code;

namespace LlmTornado.Images;

/// <summary>
///     Given a prompt, the model will generate a new image.
/// </summary>
public class ImageEditEndpoint : EndpointBase, IImageEditEndpoint
{
    /// <summary>
    ///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
    ///     <see cref="TornadoApi" /> as <see cref="TornadoApi.ImageGenerations" />.
    /// </summary>
    /// <param name="api"></param>
    internal ImageEditEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  For example, "image".
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.ImageEdit;

    /// <summary>
    ///     Ask the API to Creates an image given a prompt.
    /// </summary>
    /// <param name="request">Request to be send</param>
    /// <returns>Asynchronously returns the image result. Look in its <see cref="Data.Url" /> </returns>
    public async Task<ImageResult?> EditImageAsync(ImageEditRequest request)
    {
        return await HttpPost1<ImageResult>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, postData: request);
    }
}