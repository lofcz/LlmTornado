using System;

namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunctionTypeError : ChatPluginFunctionTypeBase
{
    public override string Type => "";
 
    public ChatPluginFunctionTypeError(string description, bool required)
    {
        Description = description;
        Required = required;
    }

    public override object Compile(ChatPluginCompileBackends schema)
    {
        throw new Exception("Error type can't be compiled");
    }
}