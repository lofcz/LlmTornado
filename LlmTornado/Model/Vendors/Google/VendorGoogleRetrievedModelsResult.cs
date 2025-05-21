using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Models.Vendors.Google;

internal class VendorGoogleRetrievedModelsResult
{
    internal class VendorGoogleRetrievedModelsResultModel
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string DisplayName { get; set; }
        public int InputTokenLimit { get; set; }
        public int OutputTokenLimit { get; set; }
        public List<string> SupportedGenerationMethods { get; set; }
    }
    
    [JsonProperty("models")]
    public List<VendorGoogleRetrievedModelsResultModel> Models { get; set; }
    
    public VendorGoogleRetrievedModelsResult()
    {
        
    }

    public RetrievedModelsResult ToResult(string? postData)
    {
        return new RetrievedModelsResult
        {
            Data = Models.Select(x => new RetrievedModel
            {
                InternalDisplayName = x.DisplayName,
                Id = x.Name.StartsWith("models/") ? x.Name.ReplaceFirst("models/", string.Empty) : x.Name
            }).ToList(),
            Obj = "model"
        };
    }
}