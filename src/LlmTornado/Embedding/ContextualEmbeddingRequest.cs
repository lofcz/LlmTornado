using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using Newtonsoft.Json;

namespace LlmTornado.Embedding;

/// <summary>
/// A request for the contextual embeddings API.
/// </summary>
public class ContextualEmbeddingRequest : ISerializableRequest
{
    /// <summary>
    /// Creates a new request for the contextual embeddings API.
    /// </summary>
    /// <param name="model">The model to use for the embeddings.</param>
    /// <param name="inputs">A list of lists, where each inner list contains a query, a document, or document chunks to be vectorized.</param>
    public ContextualEmbeddingRequest(ContextualEmbeddingModel model, List<List<string>> inputs)
    {
        Model = model;
        Inputs = inputs;
    }

    /// <summary>
    /// The model to use for the embeddings.
    /// </summary>
    [JsonProperty("model")]
    [JsonConverter(typeof(IModelConverter))]
    public ContextualEmbeddingModel Model { get; set; }

    /// <summary>
    /// A list of lists, where each inner list contains a query, a document, or document chunks to be vectorized.
    /// </summary>
    [JsonProperty("inputs")]
    public List<List<string>> Inputs { get; set; }

    /// <summary>
    /// Type of the input text.
    /// </summary>
    [JsonProperty("input_type")]
    public ContextualEmbeddingInputType? InputType { get; set; }

    /// <summary>
    /// The number of dimensions for resulting output embeddings.
    /// </summary>
    [JsonProperty("output_dimension")]
    public int? OutputDimension { get; set; }

    /// <summary>
    /// The data type for the embeddings to be returned.
    /// </summary>
    [JsonProperty("output_dtype")]
    public ContextualEmbeddingOutputDataType? OutputDataType { get; set; }

    /// <summary>
    /// Format in which the embeddings are encoded.
    /// </summary>
    [JsonProperty("encoding_format")]
    public ContextualEmbeddingEncodingFormat? EncodingFormat { get; set; }

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
        return new TornadoRequestContent(this.ToJson(options?.Pretty ?? false), Model, UrlOverride ?? EndpointBase.BuildRequestUrl(null, provider, CapabilityEndpoints.ContextualEmbeddings, Model), provider, CapabilityEndpoints.ContextualEmbeddings);
    }
}