using System.Text.Json.Serialization;

namespace LlmTornado.Docs.Code;

public class BlazorResources
{
    public string Hash { get; set; }
    public Dictionary<string, string> Assembly { get; set; }
    
    [JsonPropertyName("fingerprinting")]
    public Dictionary<string, string> Fingerprinting { get; set; }
    public Dictionary<string, string> WasmNative { get; set; }
    public Dictionary<string, string> CoreAssembly { get; set; }
    public Dictionary<string, string> Pdb { get; set; }
    public Dictionary<string, Dictionary<string, string>> SatelliteResources { get; set; }
    public Dictionary<string, string> JsModuleNative { get; set; }
    public Dictionary<string, string> JsModuleRuntime { get; set; }
    public Dictionary<string, string> LibraryInitializers { get; set; }
    public Dictionary<string, string> ModulesAfterConfigLoaded { get; set; }
}