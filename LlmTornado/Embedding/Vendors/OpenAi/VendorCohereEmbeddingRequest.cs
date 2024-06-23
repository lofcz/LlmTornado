using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using LlmTornado.Embedding.Vendors.Cohere;
using Newtonsoft.Json;

namespace LlmTornado.Embedding.Vendors.OpenAi;

internal class VendorCohereEmbeddingRequest
{
    internal static List<string> DefaultEmbeddingTypes = [ "float" ];
    
    /// <summary>
    ///     Model to use.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("texts")]
    internal List<string> Texts { get; set; }
    
    /// <summary>
    /// search_document | search_query | classification | clustering
    /// required for gen3 models
    /// </summary>
    [JsonProperty("input_type")]
    internal string? InputType { get; set; }

    /// <summary>
    /// float | int8 | uint8 | binary | ubinary
    /// options other than "float" are available only for gen3 models.
    /// </summary>
    [JsonProperty("embedding_types")]
    internal List<string> EmbeddingTypes { get; set; } = DefaultEmbeddingTypes;
    
    /// <summary>
    /// NONE | START | END
    /// defaults to "END"
    /// </summary>
    [JsonProperty("truncate")]
    internal string? Truncate { get; set; }

    private static readonly Dictionary<EmbeddingVendorCohereExtensionInputTypes, string> inputTypesMap = new Dictionary<EmbeddingVendorCohereExtensionInputTypes, string>
    {
        { EmbeddingVendorCohereExtensionInputTypes.SearchDocument, "search_document" },
        { EmbeddingVendorCohereExtensionInputTypes.SearchQuery, "search_query" },
        { EmbeddingVendorCohereExtensionInputTypes.Classification, "classification" },
        { EmbeddingVendorCohereExtensionInputTypes.Clustering, "clustering" }
    };
    
    private static readonly Dictionary<EmbeddingVendorCohereExtensionTruncation, string> truncateMap = new Dictionary<EmbeddingVendorCohereExtensionTruncation, string>
    {
        { EmbeddingVendorCohereExtensionTruncation.None, "NONE" },
        { EmbeddingVendorCohereExtensionTruncation.Start, "START" },
        { EmbeddingVendorCohereExtensionTruncation.End, "END" }
    };
    
    public VendorCohereEmbeddingRequest(EmbeddingRequest request, IEndpointProvider provider)
    {
        Model = request.Model.Name;

        if (request.InputVector is not null)
        {
            Texts = request.InputVector.ToList();
        }
        else
        {
            Texts = [ 
                request.InputScalar ?? string.Empty 
            ];
        }

        InputType = request.VendorExtensions?.Cohere is not null ? inputTypesMap.GetValueOrDefault(request.VendorExtensions.Cohere.InputType, "search_document") : "search_document";

        if (request.VendorExtensions?.Cohere is not null)
        {
            if (request.VendorExtensions.Cohere.Truncate is not null)
            {
                if (truncateMap.TryGetValue(request.VendorExtensions.Cohere.Truncate.Value, out string? str))
                {
                    Truncate = str;
                }
            }
        }
    }
}