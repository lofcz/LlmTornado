using System.Text.Json.Serialization;

namespace LlmTornado.Docs.Code;

public class BlazorBootJson
{
    public string MainAssemblyName { get; set; }
    [JsonPropertyName("resources")]
    public BlazorResources Resources { get; set; }
    public bool CacheBootResources { get; set; }
    public int DebugLevel { get; set; }
    public string GlobalizationMode { get; set; }
    public Dictionary<string, object> Extensions { get; set; }
}