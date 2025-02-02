using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors;

internal class VendorGoogleTornadoFilesList
{
    [JsonProperty("files")] 
    public List<VendorGoogleTornadoFileContent> Files { get; set; } = [];
    
    [JsonProperty("pageToken")]
    public string? PageToken { get; set; }
}