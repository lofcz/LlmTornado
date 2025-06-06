using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Embedding.Vendors.OpenAi;

internal class VendorOpenAiEmbeddingRequest
{
    /// <summary>
    ///     Model to use.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; }

    /// <summary>
    ///     string | string[]
    /// </summary>
    [JsonProperty("input")]
    internal object Input { get; set; }

    /// <summary>
    ///     The dimensions length to be returned. Only supported by newer models.
    /// </summary>
    [JsonProperty("dimensions")]
    public int? Dimensions { get; set; }
    
    public VendorOpenAiEmbeddingRequest(EmbeddingRequest request, IEndpointProvider provider)
    {
        Model = request.Model.Name;

        if (request.InputVector is not null)
        {
            Input = request.InputVector;
        }
        else
        {
            Input = request.InputScalar ?? string.Empty;
        }

        Dimensions = request.Dimensions;
    }
}