using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Models.Vendors.Google;
using Newtonsoft.Json;

namespace LlmTornado.Models.Vendors.Google;

internal class VendorGoogleRetrievedModelsResult
{
    internal class VendorGoogleRetrievedModelsResultModel
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public int InputTokenLimit { get; set; }
        public int OutputTokenLimit { get; set; }
        public List<string> SupportedGenerationMethods { get; set; }
        public GoogleModelStatus? ModelStatus { get; set; }
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
                InternalDescription = x.Description,
                Id = x.Name.StartsWith("models/") ? x.Name.ReplaceFirst("models/", string.Empty) : x.Name,
                InputTokenLimit = x.InputTokenLimit > 0 ? x.InputTokenLimit : null,
                OutputTokenLimit = x.OutputTokenLimit > 0 ? x.OutputTokenLimit : null,
                ContextLength = x.InputTokenLimit > 0 ? x.InputTokenLimit : null,
                ModelStatus = x.ModelStatus
            }).ToList(),
            Obj = "model"
        };
    }
}
