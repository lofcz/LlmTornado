namespace LlmTornado.Chat.Plugins;

public abstract class ChatPluginFunctionTypeBase : IChatPluginFunctionType
{
    public abstract string Type { get; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    
    public virtual object Compile(ChatPluginCompileBackends schema)
    {
        return new { type = Type, description = Description };
    }
}