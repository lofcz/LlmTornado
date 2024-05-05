namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunctionTypeString: ChatPluginFunctionTypeBase
{
    public override string Type => "string";
 
    public ChatPluginFunctionTypeString(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}