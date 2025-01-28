using Newtonsoft.Json;

namespace LlmTornado.Assistants;

/// <summary>
///     Represents a tool object
/// </summary>
public abstract class AssistantTool
{
    /// <summary>
    ///     Type of the tool, should be always "function" for chat, assistants also accepts values "code_interpreter" and
    ///     "file_search"
    /// </summary>
    [JsonProperty("type", Required = Required.Default)]
    public string Type { get; set; } = null!;
}