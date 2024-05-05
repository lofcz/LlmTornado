namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunctionTypeBool: ChatPluginFunctionTypeBase
{
    public override string Type => "boolean";
    
    public ChatPluginFunctionTypeBool(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}