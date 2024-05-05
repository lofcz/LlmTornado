namespace LlmTornado.Chat.Plugins;

public class ChatFunctionParam
{
    /// <summary>
    ///     A descriptive name of the param, LLM uses this
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    ///     Type of the parameter, will be converted into JSON schema object
    /// </summary>
    public IChatPluginFunctionType Type { get; set; }

    /// <summary>
    ///     Whether the param is required.
    /// </summary>
    public bool Required { get; set; }

    public ChatFunctionParam(string name, IChatPluginFunctionType type)
    {
        Name = name;
        Type = type;
        Required = true;
    }

    public ChatFunctionParam(string name, string description, ChatPluginFunctionAtomicParamTypes type, bool required)
    {
        Name = name;
        Required = required;
        Type = type switch
        {
            ChatPluginFunctionAtomicParamTypes.Bool => new ChatPluginFunctionTypeBool(description, required),
            ChatPluginFunctionAtomicParamTypes.Float => new ChatPluginFunctionTypeNumber(description, required),
            ChatPluginFunctionAtomicParamTypes.Int => new ChatPluginFunctionTypeInteger(description, required),
            ChatPluginFunctionAtomicParamTypes.String => new ChatPluginFunctionTypeString(description, required),
            _ => new ChatPluginFunctionTypeError(name, required)
        };
    }
    
    public ChatFunctionParam(string name, string description, ChatPluginFunctionAtomicParamTypes type)
    {
        Name = name;
        Required = true;
        Type = type switch
        {
            ChatPluginFunctionAtomicParamTypes.Bool => new ChatPluginFunctionTypeBool(description, true),
            ChatPluginFunctionAtomicParamTypes.Float => new ChatPluginFunctionTypeNumber(description, true),
            ChatPluginFunctionAtomicParamTypes.Int => new ChatPluginFunctionTypeInteger(description, true),
            ChatPluginFunctionAtomicParamTypes.String => new ChatPluginFunctionTypeString(description, true),
            _ => new ChatPluginFunctionTypeError(name, true)
        };
    }
}