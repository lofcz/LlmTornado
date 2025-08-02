using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Rerank.Models;
using Newtonsoft.Json;

namespace LlmTornado.Rerank;

/// <summary>
/// A request for the rerank API.
/// </summary>
public class RerankRequest : ISerializableRequest
{
    /// <summary>
    /// Creates a new request for the rerank API.
    /// </summary>
    /// <param name="model">The model to use for the reranking.</param>
    /// <param name="query">The query to use for the reranking.</param>
    /// <param name="documents">The documents to be reranked.</param>
    public RerankRequest(RerankModel model, string query, List<string> documents)
    {
        Model = model;
        Query = query;
        Documents = documents;
    }

    /// <summary>
    /// The model to use for the reranking.
    /// </summary>
    [JsonProperty("model")]
    [JsonConverter(typeof(IModelConverter))]
    public RerankModel Model { get; set; }

    /// <summary>
    /// The query to use for the reranking.
    /// </summary>
    [JsonProperty("query")]
    public string Query { get; set; }

    /// <summary>
    /// The documents to be reranked.
    /// </summary>
    [JsonProperty("documents")]
    public List<string> Documents { get; set; }

    /// <summary>
    /// The number of most relevant documents to return.
    /// </summary>
    [JsonProperty("top_k")]
    public int? TopK { get; set; }

    /// <summary>
    /// Whether to return the documents in the response.
    /// </summary>
    [JsonProperty("return_documents")]
    public bool? ReturnDocuments { get; set; }

    /// <summary>
    /// Whether to truncate the input to satisfy the "context length limit".
    /// </summary>
    [JsonProperty("truncation")]
    public bool? Truncation { get; set; }

    [JsonIgnore]
    internal string? UrlOverride { get; set; }
    
    internal void OverrideUrl(string url)
    {
        UrlOverride = url;
    }
    
    /// <summary>
    /// Serializes the request.
    /// </summary>
    public TornadoRequestContent Serialize(IEndpointProvider provider, RequestSerializeOptions options)
    {
        return SerializeInternal(provider, options);
    }
    
    /// <summary>
    /// Serializes the request.
    /// </summary>
    public TornadoRequestContent Serialize(IEndpointProvider provider)
    {
        return SerializeInternal(provider, null);
    }
    
    /// <summary>
    /// Serializes the request.
    /// </summary>
    internal TornadoRequestContent SerializeInternal(IEndpointProvider provider, RequestSerializeOptions? options)
    {
        return new TornadoRequestContent(this.ToJson(options?.Pretty ?? false), Model, UrlOverride ?? EndpointBase.BuildRequestUrl(null, provider, CapabilityEndpoints.Rerank, Model), provider, CapabilityEndpoints.Rerank);
    }
}