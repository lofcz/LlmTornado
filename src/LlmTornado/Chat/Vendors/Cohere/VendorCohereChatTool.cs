using System.Collections.Generic;
using Newtonsoft.Json;

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

internal class VendorCohereChatToolResult
{
    [JsonProperty("call")] 
    public VendorCohereChatToolResultCallObject Call { get; set; } = new VendorCohereChatToolResultCallObject();

    [JsonProperty("outputs")]
    public List<object> Outputs { get; set; } = [];
}

internal class VendorCohereChatToolResultCallObject
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    /// <summary>
    ///     JSON.
    /// </summary>
    [JsonProperty("parameters")]
    public object? Parameters { get; set; }
    
    /// <summary>
    ///     Only passed to the model when we got this from it? Connectors are know to produce this.
    /// </summary>
    [JsonProperty("generation_id")]
    public string? GenerationId { get; set; }
}