namespace LlmTornado.Internal.Press.DataModels;

public class ImageOutput
{
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public string PromptUsed { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public Dictionary<string, string>? Variations { get; set; } // Key: "1000x420", Value: URL/path
}

