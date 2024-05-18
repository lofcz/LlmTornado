using System.Collections.Generic;
using Argon;

namespace LlmTornado.Chat.Plugins;

public class SerializedObject
{
    [JsonProperty("type")]
    public string? Type { get; set; }
    
    [JsonProperty("properties")]
    public Dictionary<string, object>? Properties { get; set; }
    
    [JsonProperty("required")]
    public List<string>? Required { get; set; }
    
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    [JsonIgnore] 
    internal ChatPluginFunctionTypeObject? SourceObject { get; set; }
}