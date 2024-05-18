using Argon;

namespace LlmTornado.Chat.Plugins;

public interface IChatPluginFunctionType
{
    [JsonProperty("type")]
    public string Type { get; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public object Compile(ChatPluginCompileBackends schema);
}

public enum ChatPluginCompileBackends
{
    JsonSchema,
    Python
}