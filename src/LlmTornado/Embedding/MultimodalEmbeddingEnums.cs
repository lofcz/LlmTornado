using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Embedding;

/// <summary>
/// Type of the input text.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MultimodalEmbeddingInputType
{
    /// <summary>
    /// The embedding model directly converts the inputs into numerical vectors.
    /// </summary>
    [EnumMember(Value = "null")]
    None,
    /// <summary>
    /// The prompt is "Represent the query for retrieving supporting documents:".
    /// </summary>
    [EnumMember(Value = "query")]
    Query,
    /// <summary>
    /// The prompt is "Represent the document for retrieval:".
    /// </summary>
    [EnumMember(Value = "document")]
    Document
}

/// <summary>
/// Format in which the embeddings are encoded.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MultimodalEmbeddingEncodingFormat
{
    /// <summary>
    /// The embeddings are represented as a list of floating-point numbers.
    /// </summary>
    [EnumMember(Value = "null")]
    None,
    /// <summary>
    /// The embeddings are represented as a Base64-encoded NumPy array of single-precision floats.
    /// </summary>
    [EnumMember(Value = "base64")]
    Base64
}