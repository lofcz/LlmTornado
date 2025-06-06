using System;
using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Embedding.Vendors.Cohere;

internal class VendorCohereEmbeddingResult : VendorEmbeddingResult
{
    internal class VendorCohereEmbeddingResultEmbedding
    {
        [JsonProperty("float")]
        public List<float[]>? Float { get; set; }
    }
    
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("texts")]
    public List<string> Texts { get; set; }
    [JsonProperty("embeddings")]
    public VendorCohereEmbeddingResultEmbedding? Embeddings { get; set; }
    [JsonProperty("meta")]
    public VendorCohereUsage Meta { get; set; }
    
    public override EmbeddingResult ToResult(string? postData)
    {
        EmbeddingResult result = new EmbeddingResult
        {
            RequestId = Id,
            Usage = new EmbeddingUsage(Meta)
        };

        if (Embeddings is not null)
        {
            if (Embeddings.Float is not null)
            {
                int index = 0;
                
                foreach (float[] embedding in Embeddings.Float)
                {
                    result.Data.Add(new EmbeddingEntry
                    {
                        Index = index,
                        Embedding = embedding
                    });

                    index++;
                }
            }
        }
        
        Result = result;
        return result;
    }
}