using System;
using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace LlmTornado.ChatFunctions;

/// <summary>
///     Represents a function to be called.
/// </summary>
public class OutboundToolCallFunction
{
    /// <summary>
    ///     Name of the function
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
}

/// <summary>
///     Known outbound tool choice modes.
/// </summary>
public enum OutboundToolChoiceModes
{
    /// <summary>
    /// The behavior is inferred from the Function.Name
    /// </summary>
    Legacy,
    /// <summary>
    /// Default if any tools are present.
    /// </summary>
    Auto,
    /// <summary>
    /// Default if no tools are specified.
    /// </summary>
    None,
    /// <summary>
    /// Lets the model pick one or more tools from the supplied tools at its own discretion.
    /// </summary>
    Required,
    /// <summary>
    /// Specifies which tool should the model response with.
    /// </summary>
    ToolFunction
}

/// <summary>
///     An optional class to be used with models that support returning function calls.
/// </summary>
public class OutboundToolChoice
{
    /// <summary>
    /// No tool will be used.
    /// </summary>
    public static readonly OutboundToolChoice None = new OutboundToolChoice(OutboundToolChoiceModes.None);
    
    /// <summary>
    /// Tools might be used if any are available.
    /// </summary>
    public static readonly OutboundToolChoice Auto = new OutboundToolChoice(OutboundToolChoiceModes.Auto);
    
    /// <summary>
    /// At least one tool will be used.
    /// </summary>
    public static readonly OutboundToolChoice Required = new OutboundToolChoice(OutboundToolChoiceModes.Required);

    /// <summary>
    /// Specifies a tool the model should use. Use to force the model to call a specific custom tool.
    /// </summary>
    public static OutboundToolChoice Custom(string name)
    {
        return new OutboundToolChoice
        {
            Type = "custom",
            CustomTool = new OutboundToolCallFunctionCustom
            {
                Name = name
            }
        };
    }

    /// <summary>
    /// Specifies a hosted/built-in tool the model should use. Available only when using Responses API (not Chat).
    /// </summary>
    public static OutboundToolChoice Hosted(HostedToolTypes tool)
    {
        return new OutboundToolChoice
        {
            Type = GetHostedToolTypeString(tool),
            HostedTool = tool
        };
    }

    /// <summary>
    /// Specifies a tool the model should use. Use to force the model to call a specific function.
    /// </summary>
    public static OutboundToolChoice Tool(string name)
    {
        return new OutboundToolChoice
        {
            Mode = OutboundToolChoiceModes.ToolFunction,
            Function = new OutboundToolCallFunction
            {
                Name = name
            }
        };
    }
    
    /// <summary>
    /// Manually construct the tool choice.
    /// </summary>
    public OutboundToolChoice()
    {
        
    }

    /// <summary>
    /// Specify the function the model should use.
    /// </summary>
    /// <param name="functionName"></param>
    public OutboundToolChoice(string functionName)
    {
        Mode = OutboundToolChoiceModes.ToolFunction;

        if (!functionName.IsNullOrWhiteSpace())
        {
            Function = new OutboundToolCallFunction
            {
                Name = functionName
            };   
        }
    }
    
    /// <summary>
    /// Specify the strategy the model should use when selecting one or more tools from the supplied tools.
    /// </summary>
    /// <param name="mode"></param>
    public OutboundToolChoice(OutboundToolChoiceModes mode)
    {
        Mode = mode;
    }
    
    /// <summary>
    ///     When set, this instance represents one of the simple string modes ("none", "auto", or "required").
    ///     Used to force serialization as a string.
    /// </summary>
    [JsonIgnore]
    public string? StringValue { get; set; }

    /// <summary>
    ///     Allows constructing <see cref="OutboundToolChoice"/> from a string.
    ///     - If the value is one of: "none", "auto", or "required", sets <see cref="StringValue"/> and aligns <see cref="Mode"/> accordingly.
    ///     - Otherwise, uses <see cref="OutboundToolChoiceModes.ToolFunction"/> with the provided string as the function name.
    /// </summary>
    public static implicit operator OutboundToolChoice(string value)
    {
        return value switch
        {
            "none" => new OutboundToolChoice(OutboundToolChoiceModes.None) { StringValue = value },
            "auto" => new OutboundToolChoice(OutboundToolChoiceModes.Auto) { StringValue = value },
            "required" => new OutboundToolChoice(OutboundToolChoiceModes.Required) { StringValue = value },
            _ => new OutboundToolChoice(value)
        };
    }
    
    /// <summary>
    ///     The type of the tool. Currently, this should be always "function".
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; } = "function";

    /// <summary>
    ///     The type of the tool. Currently, this should be always "function".
    /// </summary>
    [JsonProperty("function")]
    public OutboundToolCallFunction? Function { get; set; }

    /// <summary>
    ///     Specifies a tool the model should use. Use to force the model to call a specific custom tool.
    /// </summary>
    [JsonProperty("custom")]
    public OutboundToolCallFunctionCustom? CustomTool { get; set; }
    
    /// <summary>
    ///     Hosted tool to use. Available only when using Responses API (not Chat).
    /// </summary>
    [JsonIgnore]
    public HostedToolTypes? HostedTool { get; set; }
    
    /// <summary>
    ///     Controls which tool(s) the model selects from the supplied tools.
    /// </summary>
    [JsonIgnore]
    public OutboundToolChoiceModes Mode { get; set; } = OutboundToolChoiceModes.Legacy;
    
    internal class OutboundToolChoiceConverter : JsonConverter
    {
        internal static readonly HashSet<string> KnownFunctionNames = [ "none", "auto", "required" ];
        
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OutboundToolChoice);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is OutboundToolChoice functionCall)
            {
                // If hosted tool is specified, always serialize as an object with only type.
                if (functionCall.HostedTool.HasValue)
                {
                    string hostedType = GetHostedToolTypeString(functionCall.HostedTool.Value);
                    serializer.Serialize(writer, new { type = hostedType });
                    return;
                }

                // If this is a custom tool specification, always serialize the object regardless of mode or StringValue.
                if (string.Equals(functionCall.Type, "custom", StringComparison.OrdinalIgnoreCase) && functionCall.CustomTool is not null)
                {
                    serializer.Serialize(writer, functionCall);
                    return;
                }

                // If StringValue is set, serialize as a simple string.
                if (!string.IsNullOrEmpty(functionCall.StringValue))
                {
                    serializer.Serialize(writer, functionCall.StringValue);
                    return;
                }

                switch (functionCall.Mode)
                {
                    // old behavior
                    case OutboundToolChoiceModes.Legacy when functionCall.Function is not null && KnownFunctionNames.Contains(functionCall.Function.Name):
                        serializer.Serialize(writer, functionCall.Function.Name);
                        break;
                    case OutboundToolChoiceModes.Legacy:
                    case OutboundToolChoiceModes.ToolFunction:
                        serializer.Serialize(writer, functionCall);
                        break;
                    case OutboundToolChoiceModes.Auto:
                        serializer.Serialize(writer, "auto");
                        break;
                    case OutboundToolChoiceModes.None:
                        serializer.Serialize(writer, "none");
                        break;
                    case OutboundToolChoiceModes.Required:
                        serializer.Serialize(writer, "required");
                        break;
                }
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                {
                    string? functionCallType = (string?)serializer.Deserialize(reader, typeof(string));

                    if (functionCallType is not null)
                    {
                        // Use implicit conversion for all string values. Known values will set StringValue; others become ToolFunction.
                        OutboundToolChoice choice = functionCallType;
                        return choice;
                    }

                    break;
                }
                case JsonToken.StartObject:
                {
                    JObject jo = JObject.Load(reader);

                    // Handle custom tool object: { "type": "custom", "custom": { "name": "..." } }
                    string? type = jo["type"]?.Value<string>();
                    if (string.Equals(type, "custom", StringComparison.OrdinalIgnoreCase))
                    {
                        string? customName = jo["custom"]?["name"]?.Value<string>();
                        return new OutboundToolChoice
                        {
                            Type = "custom",
                            CustomTool = customName != null ? new OutboundToolCallFunctionCustom { Name = customName } : null
                        };
                    }

                    // Handle hosted tool object: { "type": "file_search" } etc. (Responses API only)
                    if (!string.IsNullOrWhiteSpace(type) 
                        && TryGetHostedToolTypeEnum(type, out HostedToolTypes hosted)
                        && jo["function"] is null && jo["custom"] is null)
                    {
                        return new OutboundToolChoice
                        {
                            Type = type,
                            HostedTool = hosted
                        };
                    }

                    // Handle function tool object: { "type": "function", "function": { "name": "..." } } or missing type
                    string? functionName = jo["function"]?["name"]?.Value<string>();
                    if (!functionName.IsNullOrWhiteSpace())
                    {
                        return new OutboundToolChoice(functionName);
                    }

                    // Fallback: return a minimal OutboundToolChoice with type if present
                    return new OutboundToolChoice
                    {
                        Type = type
                    };
                }
            }
            
            return serializer.Deserialize<OutboundToolCallFunction>(reader);
        }
    }
    
    private static string GetHostedToolTypeString(HostedToolTypes type)
    {
        return type switch
        {
            HostedToolTypes.FileSearch => "file_search",
            HostedToolTypes.WebSearchPreview => "web_search_preview",
            HostedToolTypes.ComputerUsePreview => "computer_use_preview",
            HostedToolTypes.CodeInterpreter => "code_interpreter",
            HostedToolTypes.ImageGeneration => "image_generation",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static bool TryGetHostedToolTypeEnum(string type, out HostedToolTypes value)
    {
        switch (type)
        {
            case "file_search": value = HostedToolTypes.FileSearch; return true;
            case "web_search_preview": value = HostedToolTypes.WebSearchPreview; return true;
            case "computer_use_preview": value = HostedToolTypes.ComputerUsePreview; return true;
            case "code_interpreter": value = HostedToolTypes.CodeInterpreter; return true;
            case "image_generation": value = HostedToolTypes.ImageGeneration; return true;
            default: value = default; return false;
        }
    }
}

/// <summary>
/// Use to force the model to call a specific custom tool.
/// </summary>
public class OutboundToolCallFunctionCustom
{
    /// <summary>
    /// The name of the custom tool to call.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
}

/// <summary>
/// The type of hosted tool the model should use. Available only with Responses API (not Chat).
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum HostedToolTypes
{
    /// <summary>
    /// Search the contents of uploaded files for context when generating a response.
    /// </summary>
    [EnumMember(Value = "file_search")] 
    FileSearch,
    
    /// <summary>
    /// Include data from the Internet in model response generation.
    /// </summary>
    [EnumMember(Value = "web_search_preview")] 
    WebSearchPreview,
    
    /// <summary>
    /// Create agentic workflows that enable a model to control a computer interface.
    /// </summary>
    [EnumMember(Value = "computer_use_preview")] 
    ComputerUsePreview,
    
    /// <summary>
    /// Allow the model to execute code in a secure container.
    /// </summary>
    [EnumMember(Value = "code_interpreter")] 
    CodeInterpreter,
    
    /// <summary>
    /// Generate or edit images using GPT Image.
    /// </summary>
    [EnumMember(Value = "image_generation")] 
    ImageGeneration
}