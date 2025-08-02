using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Embedding;

/// <summary>
/// Represents a response from the multimodal embeddings API.
/// </summary>
public class MultimodalEmbeddingResult : ApiResultBase
{
    /// <summary>
    /// The object type, which is always "list".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; }

    /// <summary>
    /// An array of embedding objects.
    /// </summary>
    [JsonProperty("data")]
    public List<MultimodalEmbedding> Data { get; set; }

    /// <summary>
    /// Name of the model.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; }

    /// <summary>
    /// The usage information for the request.
    /// </summary>
    [JsonProperty("usage")]
    public MultimodalUsage Usage { get; set; }
}

/// <summary>
/// Represents a single multimodal embedding.
/// </summary>
public class MultimodalEmbedding
{
    /// <summary>
    /// The object type, which is always "embedding".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; }

    /// <summary>
    /// The embedding vector.
    /// </summary>
    [JsonProperty("embedding")]
    [JsonConverter(typeof(MultimodalEmbeddingValueConverter))]
    public MultimodalEmbeddingValue Embedding { get; set; }

    /// <summary>
    /// An integer representing the index of the embedding within the list of embeddings.
    /// </summary>
    [JsonProperty("index")]
    public int Index { get; set; }
}

/// <summary>
/// Represents the usage information for a multimodal embedding request.
/// </summary>
public class MultimodalUsage
{
    /// <summary>
    /// The total number of text tokens in the list of inputs.
    /// </summary>
    [JsonProperty("text_tokens")]
    public int TextTokens { get; set; }

    /// <summary>
    /// The total number of image pixels in the list of inputs.
    /// </summary>
    [JsonProperty("image_pixels")]
    public int ImagePixels { get; set; }

    /// <summary>
    /// The combined total of text and image tokens.
    /// </summary>
    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}