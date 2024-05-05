using System.Collections.Generic;
using LlmTornado.Code;

namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunctionTypeObject : ChatPluginFunctionTypeBase
{
    public override string Type => "object";
    public List<ChatFunctionParam> Properties { get; set; }
    
    public ChatPluginFunctionTypeObject(string? description, bool required, List<ChatFunctionParam> properties)
    {
        Properties = properties;
        Description = description;
        Required = required;
    }

    public override object Compile(ChatPluginCompileBackends schema)
    {
        SerializedObject so = new SerializedObject
        {
            Type = "object",
            Description = Description,
            Properties = [],
            SourceObject = this
        };

        foreach (ChatFunctionParam prop in Properties)
        {
            if (prop.Type.Required)
            {
                so.Required ??= [];
                so.Required.Add(prop.Name);
            }
            
            so.Properties.AddOrUpdate(prop.Name, prop.Type.Compile(schema));
        }

        return so;
    }
}
