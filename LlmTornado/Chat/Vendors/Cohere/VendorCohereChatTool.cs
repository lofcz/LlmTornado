using System.Collections.Generic;
using Argon;

namespace LlmTornado.Chat.Vendors.Cohere;

internal class VendorCohereChatTool
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("description")]
    public string Description { get; set; }
    
    [JsonProperty("parameter_definitions")]
    public Dictionary<string, VendorCohereChatToolParameter>? ParameterDefinitions { get; set; }
}

internal class VendorCohereChatInboundTool
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("parameters")] 
    public Dictionary<string, object?> Parameters { get; set; } = [];
}

internal class VendorCohereChatToolParameter
{
    [JsonProperty("description")]
    public string Description { get; set; }
    
    [JsonProperty("type")]
    public string Type { get; set; }
    
    [JsonProperty("required")]
    public bool Required { get; set; }
}