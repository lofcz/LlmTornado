using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Code;

namespace LlmTornado.Images;

/// <summary>
///     Given a prompt, the model will generate a new image.
/// </summary>
public class ImageEditEndpoint : EndpointBase
{
    /// <summary>
    ///     Constructor of the api endpoint. Rather than instantiating this yourself, access it through an instance of
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
    ///     Ask the API to edit an image with a given a prompt.
    /// </summary>
    /// <param name="request">Request to be sent</param>
    /// <returns>Asynchronously returns the image result. Look in its <see cref="TornadoGeneratedImage.Url" /> </returns>
    public async Task<ImageGenerationResult?> EditImage(ImageEditRequest request)
    {
        byte[] bytes = [];
        
        if (request.Image?.Base64 is not null)
        {
            bytes = Convert.FromBase64String(request.Image.Base64);
        }
      
        using MultipartFormDataContent content = new MultipartFormDataContent();
        using MemoryStream ms = new MemoryStream(bytes);
        using StreamContent sc = new StreamContent(ms);
        sc.Headers.ContentLength = bytes.Length;
        sc.Headers.ContentType = new MediaTypeHeaderValue(request.Image?.MimeType ?? "image/png");
        
        content.Add(sc, "image", "image.png");
        content.Add(new StringContent(request.Prompt), "prompt");

        if (request.Model is not null)
        {
            content.Add(new StringContent(request.Model.Name), "model");   
        }

        if (request.Quality is not null)
        {
            string quality = request.Quality.Value switch
            { 
                TornadoImageQualities.Hd => "hd",
                TornadoImageQualities.Auto => "auto",
                TornadoImageQualities.High => "high",
                TornadoImageQualities.Low => "low",
                TornadoImageQualities.Medium => "medium",
                TornadoImageQualities.Standard => "standard",
                _ => "auto"
            };
            
            content.Add(new StringContent(quality), "quality");   
        }

        if (request.Mask is not null)
        {
            if (request.Mask?.Base64 is not null)
            {
                bytes = Convert.FromBase64String(request.Mask.Base64);
            }
            
            // todo: dispose
            MemoryStream maskMs = new MemoryStream(bytes);
            StreamContent maskSc = new StreamContent(maskMs);
            maskSc.Headers.ContentLength = bytes.Length;
            maskSc.Headers.ContentType = new MediaTypeHeaderValue(request.Mask?.MimeType ?? "image/png");
        
            content.Add(maskSc, "mask", "mask.png");
        }

        if (request.Size is not null)
        {
            string size = request.Size.Value switch
            { 
                TornadoImageSizes.Auto => "auto",
                TornadoImageSizes.Size1024x1024 => "1024x1024",
                TornadoImageSizes.Size1024x1536 => "1024x1536",
                TornadoImageSizes.Size1536x1024 => "1536x1024",
                _ => "auto"
            };
            
            content.Add(new StringContent(size), "size");   
        }

        if (request.User is not null)
        {
            content.Add(new StringContent(request.User), "user");   
        }

        if (request.ResponseFormat is not null)
        {
            string format = request.ResponseFormat.Value switch
            { 
                TornadoImageResponseFormats.Base64 => "base64",
                TornadoImageResponseFormats.Url => "url",
                _ => "base64"
            };
            
            content.Add(new StringContent(format), "response_format");   
        }

        if (request.NumOfImages is not null)
        {
            content.Add(new StringContent(request.NumOfImages.ToString() ?? string.Empty), "n");   
        }
        
        ImageGenerationResult? data = await HttpPost1<ImageGenerationResult>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, postData: content).ConfigureAwait(ConfigureAwaitOptions.None);
        return data;
    }
}