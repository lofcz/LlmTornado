using System;
using System.Collections.Generic;
using System.Reflection;
using LlmTornado.Chat.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LlmTornado.Chat;

/// <summary>
/// Strategies for serializing <see cref="ChatRequest.MaxTokens"/> property, broken by OpenAI 9/24.
/// </summary>
public enum ChatRequestMaxTokensSerializers
{
    /// <summary>
    /// If a known o1 model from OpenAI is used, resolves to <see cref="MaxCompletionTokens"/>, else <see cref="MaxTokens"/>
    /// </summary>
    Auto,
    /// <summary>
    /// Serializes as max_tokens, use for legacy / self-hosted providers.
    /// </summary>
    MaxTokens,
    /// <summary>
    /// Serializes as max_completion_tokens, use for a peace of mind if you only use OpenAI
    /// </summary>
    MaxCompletionTokens
}

internal class PropertyRenameAndIgnoreSerializerContractResolver : DefaultContractResolver
{
    private readonly Dictionary<Type, HashSet<string>> ignores = [];
    private readonly Dictionary<Type, Dictionary<string, string>> renames = [];

    public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
    {
        if (!ignores.ContainsKey(type))
            ignores[type] = [];

        foreach (string prop in jsonPropertyNames)
            ignores[type].Add(prop);
    }

    public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
    {
        if (!renames.ContainsKey(type))
            renames[type] = new Dictionary<string, string>();

        renames[type][propertyName] = newJsonPropertyName;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        if (IsIgnored(property.DeclaringType, property.PropertyName))
            property.ShouldSerialize = i => false;

        if (IsRenamed(property.DeclaringType, property.PropertyName, out string? newJsonPropertyName))
            property.PropertyName = newJsonPropertyName;

        return property;
    }

    private bool IsIgnored(Type type, string jsonPropertyName)
    {
        return ignores.TryGetValue(type, out HashSet<string>? ignore) && ignore.Contains(jsonPropertyName);
    }

    private bool IsRenamed(Type type, string jsonPropertyName, out string? newJsonPropertyName)
    {
        if (this.renames.TryGetValue(type, out Dictionary<string, string>? renames) && renames.TryGetValue(jsonPropertyName, out newJsonPropertyName))
        {
            return true;
        }
        
        newJsonPropertyName = null;
        return false;
    }
}