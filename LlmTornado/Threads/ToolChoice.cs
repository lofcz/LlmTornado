using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

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

/// <summary>
/// Represents a derived class of `ToolChoice` that specifically handles the
/// tool selection process for function-based operations. This class includes
/// function-specific configurations and data essential for tool execution.
/// </summary>
public sealed class ToolChoiceFunction : ToolChoice
{
    /// <summary>
    /// Represents a specialized implementation of the `ToolChoice` class
    /// where the selection is a function-based tool. Includes specific
    /// data and behavior related to the function type.
    /// </summary>
    public ToolChoiceFunction() : base(ToolChoiceType.Function)
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
/// Defines the different types of tool choices available for selection in thread execution contexts.
/// This enum is used to specify the behavioral context or functionality associated with a tool.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ToolChoiceType
{
    /// <summary>
    /// Represents the absence of a selected tool choice.
    /// Used to indicate that no tool is currently chosen or applicable.
    /// </summary>
    [EnumMember(Value = "none")]
    None,

    /// <summary>
    /// Represents a required tool choice type in the `ToolChoiceType` enumeration.
    /// Indicates that the tool must be explicitly selected or mandatory for the execution context.
    /// This type enforces a user or system to provide the necessary input or tool before proceeding.
    /// </summary>
    [EnumMember(Value = "required")]
    Required,

    /// <summary>
    /// Represents an automatic tool selection type within the thread execution context.
    /// This enum member indicates that the tool should be chosen automatically
    /// based on predefined logic or context-aware heuristics.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,

    /// <summary>
    /// Specifies a tool choice type where the tool is function-based.
    /// This enumeration member is used to indicate that the selected tool implements
    /// functionality that is centered on reusable or specific functional logic execution.
    /// </summary>
    [EnumMember(Value = "function")]
    Function,

    /// <summary>
    /// Represents a tool choice type for searching files within a particular context.
    /// The FileSearch option defines behavior and functionality geared toward locating
    /// and retrieving file-based resources.
    /// </summary>
    [EnumMember(Value = "file_search")]
    FileSearch,

    /// <summary>
    /// Represents the tool choice type for utilizing a code interpreter.
    /// The `CodeInterpreter` option is used to enable functionality related to analysis,
    /// execution, or interpretation of code within a thread execution context.
    /// </summary>
    [EnumMember(Value = "code_interpreter")]
    CodeInterpreter
}

internal class ToolChoiceConverter : JsonConverter<ToolChoice>
{
    public override ToolChoice? ReadJson(JsonReader reader, Type objectType, ToolChoice? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
            {
                JObject jsonObject = JObject.Load(reader);
                ToolChoiceType? toolType = jsonObject["type"]?.ToObject<ToolChoiceType>();

                return toolType switch
                {
                    ToolChoiceType.Function => jsonObject
                        .ToObject<ToolChoiceFunction>(serializer)!,
                    _ => jsonObject.ToObject<ToolChoice>()
                };
            }
            case JsonToken.String:
            {
                ToolChoiceType? toolType = JsonConvert.DeserializeObject<ToolChoiceType>($"\"{reader.Value}\"");
                return toolType is null ? null : new ToolChoice(toolType.Value);
            }
            default:
                return null;
        }
    }

    public override void WriteJson(JsonWriter writer, ToolChoice? toolChoice, JsonSerializer serializer)
    {
        if (toolChoice is null)
        {
            return;
        }

        ToolChoiceType toolType = toolChoice.Type;

        if (toolType is ToolChoiceType.Auto or ToolChoiceType.None or ToolChoiceType.Required)
        {
            writer.WriteValue(toolType.ToString().ToLowerInvariant());
        }
        else
        {
            JObject jsonObject = JObject.FromObject(toolChoice, serializer);
            jsonObject.WriteTo(writer);
        }
    }
}