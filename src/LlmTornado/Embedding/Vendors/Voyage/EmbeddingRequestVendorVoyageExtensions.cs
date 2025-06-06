using System;

namespace LlmTornado.Embedding.Vendors.Voyage;

/// <summary>
/// Embedding features supported only by Voyage.
/// </summary>
public class EmbeddingRequestVendorVoyageExtensions
{
    /// <summary>
    /// Type of the input text. Defaults to null. Other options: query, document.
    /// </summary>
    public EmbeddingVendorVoyageInputTypes? InputType { get; set; }
    
    /// <summary>
    /// Whether to truncate the input texts to fit within the context length. Defaults to true.
    /// </summary>
    public bool? Truncation { get; set; }
    
    /// <summary>
    /// The data type for the embeddings to be returned. Defaults to float.
    /// </summary>
    [Obsolete("Will be removed. Please use OutputDtype on the EmbeddingRequest itself.")]
    public EmbeddingOutputDtypes? OutputDtype { get; set; }
}

/// <summary>
/// The data type for the embeddings to be returned. Defaults to float.
/// </summary>
public enum EmbeddingOutputDtypes
{
    /// <summary>
    /// <see cref="float"/>
    /// </summary>
    Float,
    
    /// <summary>
    /// <see cref="sbyte"/>
    /// </summary>
    Int8,
    
    /// <summary>
    /// <see cref="byte"/>
    /// </summary>
    Uint8,
    
    /// <summary>
    /// List<sbyte/>
    /// </summary>
    Binary,
    
    /// <summary>
    /// List<byte/>
    /// </summary>
    Ubinary
}

/// <summary>
/// Type of the input text. Defaults to null. Other options: query, document.
/// </summary>
public enum EmbeddingVendorVoyageInputTypes
{
    /// <summary>
    /// Represent the query for retrieving supporting documents:
    /// </summary>
    Query,
    
    /// <summary>
    /// Represent the document for retrieval:
    /// </summary>
    Document
}