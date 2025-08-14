

using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LlmTornado.Infra;

/// <summary>
/// Specifies a custom name for a method, parameter, or property when generating the JSON schema for a tool/structured output.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property)]
public class SchemaNameAttribute : Attribute
{
    /// <summary>
    /// The custom name to use in the JSON schema.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaNameAttribute"/> class with the specified name.
    /// </summary>
    /// <param name="name">The custom name to use in the JSON schema.</param>
    public SchemaNameAttribute(string name)
    {
        Name = name;
    }
}

internal class SchemaNameContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        SchemaNameAttribute? schemaNameAttr = member.GetCustomAttribute<SchemaNameAttribute>();
        
        if (schemaNameAttr != null)
        {
            property.PropertyName = schemaNameAttr.Name;
        }
        
        return property;
    }
}