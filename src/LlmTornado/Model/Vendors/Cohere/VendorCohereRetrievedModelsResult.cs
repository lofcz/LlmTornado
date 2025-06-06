using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Models.Vendors.Cohere;

internal class VendorCohereRetrievedModelsResult
{
    internal class VendorCohereRetrievedModelsResultModel
    {
        public string? Name { get; set; }
        public bool? Finetuned { get; set; }
        public List<string>? Endpoints { get; set; }
        public List<string>? Features { get; set; }
        [JsonProperty("context_length")]
        public double ContextLength { get; set; }
    }
    
    [JsonProperty("models")]
    public List<VendorCohereRetrievedModelsResultModel> Models { get; set; }
    
    public RetrievedModelsResult ToResult(string? postData)
    {
        return new RetrievedModelsResult
        {
            Data = Models.Select(x => new RetrievedModel
            {
                Id = x.Name ?? string.Empty
            }).ToList(),
            Obj = "model"
        };
    }
}