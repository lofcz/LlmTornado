using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Embedding.Vendors.Voyage;

internal class VendorVoyageEmbeddingRequest
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
    /// Null | query | document
    /// </summary>
    [JsonProperty("input_type")]
    public string? InputType { get; set; }
    
    /// <summary>
    /// Whether to truncate the input texts to fit within the context length. Defaults to true.
    /// </summary>
    [JsonProperty("truncation")]
    public bool? Truncation { get; set; }
    
    /// <summary>
    /// float (default) | int8 | uint8 | binary | ubinary 
    /// </summary>
    /// <returns></returns>
    [JsonProperty("output_dtype")]
    public string? OutputDtype { get; set; }

    /// <summary>
    /// Format in which the embeddings are encoded. Defaults to null. Other options: base64.
    /// </summary>
    [JsonProperty("encoding_format")]
    public string? EncodingFormat { get; set; }
    
    /// <summary>
    ///     The dimensions length to be returned. Only supported by newer models.
    /// </summary>
    [JsonProperty("output_dimension")]
    public int? Dimensions { get; set; }
    
    public VendorVoyageEmbeddingRequest(EmbeddingRequest request, IEndpointProvider provider)
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
        
        if (request.VendorExtensions?.Voyage is not null)
        {
            Truncation = request.VendorExtensions.Voyage.Truncation;
            OutputDtype = request.VendorExtensions.Voyage.OutputDtype switch
            {
                null => null,
                EmbeddingVendorVoyageOutputDtypes.Float => "float",
                EmbeddingVendorVoyageOutputDtypes.Int8 => "int8",
                EmbeddingVendorVoyageOutputDtypes.Uint8 => "uint8",
                EmbeddingVendorVoyageOutputDtypes.Binary => "binary",
                EmbeddingVendorVoyageOutputDtypes.Ubinary => "ubinary",
                _ => "float"
            };
            InputType = request.VendorExtensions.Voyage.InputType switch
            {
                null => null,
                EmbeddingVendorVoyageInputTypes.Query => "query",
                EmbeddingVendorVoyageInputTypes.Document => "document",
                _ => null
            };
        }
    }
}