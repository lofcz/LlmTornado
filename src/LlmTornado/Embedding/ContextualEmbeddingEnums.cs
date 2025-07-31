using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Embedding;

/// <summary>
/// Type of the input text.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ContextualEmbeddingInputType
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
/// The data type for the embeddings to be returned.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ContextualEmbeddingOutputDataType
{
    /// <summary>
    /// Each returned embedding is a list of 32-bit (4-byte) single-precision floating-point numbers.
    /// </summary>
    [EnumMember(Value = "float")]
    Float,
    /// <summary>
    /// Each returned embedding is a list of 8-bit (1-byte) integers ranging from -128 to 127.
    /// </summary>
    [EnumMember(Value = "int8")]
    Int8,
    /// <summary>
    /// Each returned embedding is a list of 8-bit (1-byte) integers ranging from 0 to 255.
    /// </summary>
    [EnumMember(Value = "uint8")]
    Uint8,
    /// <summary>
    /// Each returned embedding is a list of 8-bit integers that represent bit-packed, quantized single-bit embedding values.
    /// </summary>
    [EnumMember(Value = "binary")]
    Binary,
    /// <summary>
    /// Each returned embedding is a list of 8-bit integers that represent bit-packed, quantized single-bit embedding values.
    /// </summary>
    [EnumMember(Value = "ubinary")]
    UnsignedBinary
}

/// <summary>
/// Format in which the embeddings are encoded.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ContextualEmbeddingEncodingFormat
{
    /// <summary>
    /// Each embedding is an array of float numbers or an array of integers.
    /// </summary>
    [EnumMember(Value = "null")]
    None,
    /// <summary>
    /// The embeddings are represented as a Base64-encoded NumPy array.
    /// </summary>
    [EnumMember(Value = "base64")]
    Base64
}