using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Rerank;

/// <summary>
/// Represents a response from the rerank API.
/// </summary>
public class RerankResult : ApiResultBase
{
    /// <summary>
    /// The object type, which is always "list".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; }

    /// <summary>
    /// An array of the reranking results, sorted by the descending order of relevance scores.
    /// </summary>
    [JsonProperty("data")]
    public List<RerankData> Data { get; set; }

    /// <summary>
    /// Name of the model.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; }

    /// <summary>
    /// The total number of tokens used for computing the reranking.
    /// </summary>
    [JsonProperty("usage")]
    public Usage Usage { get; set; }
}

/// <summary>
/// Represents a single reranking result.
/// </summary>
public class RerankData
{
    /// <summary>
    /// The index of the document in the input list.
    /// </summary>
    [JsonProperty("index")]
    public int Index { get; set; }

    /// <summary>
    /// The relevance score of the document with respect to the query.
    /// </summary>
    [JsonProperty("relevance_score")]
    public float RelevanceScore { get; set; }

    /// <summary>
    /// The document string. Only returned when return_documents is set to true.
    /// </summary>
    [JsonProperty("document")]
    public string? Document { get; set; }
}