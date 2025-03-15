using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Embedding.Vendors.Google;

internal class VendorGoogleEmbeddingResult : VendorEmbeddingResult
{
    internal class VendorGoogleEmbeddingResultEntry
    {
        /// <summary>
        /// A list of floats representing an embedding.
        /// </summary>
        [JsonProperty("values")]
        public float[] Values { get; set; }
    }
    
    /// <summary>
    /// Output only. The embeddings for each request, in the same order as provided in the batch request. (for batch requests)
    /// </summary>
    [JsonProperty("embeddings")]
    public List<VendorGoogleEmbeddingResultEntry>? Embeddings { get; set; }
    
    /// <summary>
    /// Output only. The embedding generated from the input content. (for scalar requests)
    /// </summary>
    [JsonProperty("embedding")]
    public VendorGoogleEmbeddingResultEntry? Embedding { get; set; }
    
    public override EmbeddingResult ToResult(string? postData)
    {
        if (Embeddings?.Count > 0)
        {
            EmbeddingResult result = new EmbeddingResult
            {
                Data = Embeddings.Select((x, index) => new EmbeddingEntry
                {
                    Embedding = x.Values,
                    Index = index
                }).ToList()
            };

            return result;   
        }

        if (Embedding is null)
        {
            return new EmbeddingResult();
        }
        
        return new EmbeddingResult
        {
            Data = [
                new EmbeddingEntry
                {
                    Embedding = Embedding.Values,
                    Index = 0
                }
            ]
        };
    }
}