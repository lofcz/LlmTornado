using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads;

/// <summary>
/// Serves as the base class for defining various tool selection behaviors
/// in the execution context of a thread.
/// </summary>
public class ToolChoice
{
    /// <summary>
    /// Represents the base class for tool selection behavior in thread execution.
    /// Holds the type of the tool choice as a required property.
    /// </summary>
    public ToolChoice(ToolChoiceType type)
    {
        Type = type;
    }

    /// <summary>
    /// Specifies the type of the tool choice. This property indicates the category
    /// or classification of the tool as defined by the implementing subclass.
    /// </summary>
    [JsonProperty("type")]
    public ToolChoiceType Type { get; set; }
}

public sealed class ToolChoiceFunction : ToolChoice
{
    /// <summary>
    /// Represents a specialized implementation of the `ToolChoice` class
    /// where the selection is a function-based tool. Includes specific
    /// data and behavior related to the function type.
    /// </summary>
    public ToolChoiceFunction() : base(ToolChoiceType.function)
    {
    }

    /// <summary>
    /// Stores information about the function associated with a tool choice.
    /// Used in the context of `ToolChoiceFunction` to specify function details.
    /// </summary>
    [JsonProperty("function")]
    public ToolChoiceFunctionData Function { get; set; } = null!;
}

/// <summary>
/// Represents the data associated with a function tool choice, containing specific details about a function.
/// Used in conjunction with `ToolChoiceFunction` to define the function's properties.
/// </summary>
public class ToolChoiceFunctionData
{
    /// <summary>
    /// Specifies the name associated with the function in the tool choice data.
    /// This property identifies the function being referred to in the context of a tool choice.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// Enum representing various tool choice types as string values for JSON serialization.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ToolChoiceType
{
    none,
    required,
    auto,
    function,
    file_search,
    code_interpreter,
}

internal class ToolChoiceConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(ToolChoice).IsAssignableFrom(objectType);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
            {
                var jsonObject = Newtonsoft.Json.Linq.JObject.Load(reader);
                var typeToken = jsonObject["type"]?.ToString();
                if (!Enum.TryParse(typeToken, true, out ToolChoiceType toolType))
                {
                    return null;
                }

                return toolType switch
                {
                    ToolChoiceType.function => jsonObject
                        .ToObject<ToolChoiceFunction>(serializer)!,
                    _ => jsonObject.ToObject<ToolChoice>(),
                };
            }
            case JsonToken.String:
            {
                return !Enum.TryParse(typeof(ToolChoiceType), reader.Value?.ToString(), true, out var toolType) ? null : new ToolChoice((ToolChoiceType)toolType);
            }
            default:
                return null;
        }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not ToolChoice toolChoice)
        {
            throw new JsonSerializationException("Expected ToolChoice object value.");
        }

        var toolType = toolChoice.Type;

        if (toolType is ToolChoiceType.auto or ToolChoiceType.none or ToolChoiceType.required)
        {
            writer.WriteValue(toolType.ToString().ToLowerInvariant());
        }
        else
        {
            var jsonObject = Newtonsoft.Json.Linq.JObject.FromObject(value, serializer);
            jsonObject.WriteTo(writer);
        }
    }
}