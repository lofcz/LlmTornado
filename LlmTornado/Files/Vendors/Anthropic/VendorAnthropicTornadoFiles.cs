using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors.Anthropic;

internal class VendorAnthropicTornadoFiles
{
    [JsonProperty("data")] 
    public List<VendorAnthropicTornadoFile>? Data { get; set; }

    [JsonProperty("has_more")] 
    public bool HasMore { get; set; }
    
    [JsonProperty("first_id")] 
    public string? FirstId { get; set; }
    
    [JsonProperty("last_id")] 
    public string? LastId { get; set; }

    public List<TornadoFile> ToTornadoFiles()
    {
        return Data?.Select(x => x.ToFile()).ToList() ?? [];
    }

    public TornadoPagingList<TornadoFile> ToList()
    {
        return new TornadoPagingList<TornadoFile>
        {
            Items = ToTornadoFiles(),
            PageToken = LastId,
            MoreData = HasMore
        };
    }
}