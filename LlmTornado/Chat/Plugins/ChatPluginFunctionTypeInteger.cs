namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunctionTypeInteger: ChatPluginFunctionTypeBase
{
    public override string Type => "integer";
    
    public ChatPluginFunctionTypeInteger(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}