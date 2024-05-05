namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunctionTypeNumber: ChatPluginFunctionTypeBase
{
    public override string Type => "number";
    
    public ChatPluginFunctionTypeNumber(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}