using System;
using System.Collections.Generic;
using LlmTornado.Code;
using Argon;

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
    /// The behavior is inferred from the <see cref="OutboundToolChoice.Function.Name"/>.
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

                    if (functionCallType is not null && KnownFunctionNames.Contains(functionCallType))
                    {
                        return new OutboundToolChoice
                        {
                            Function = new OutboundToolCallFunction
                            {
                                Name = functionCallType
                            }
                        };
                    }

                    break;
                }
                case JsonToken.StartObject:
                {
                    return serializer.Deserialize<OutboundToolCallFunction>(reader);
                }
            }
            
            return serializer.Deserialize<OutboundToolCallFunction>(reader);
        }
    }
}