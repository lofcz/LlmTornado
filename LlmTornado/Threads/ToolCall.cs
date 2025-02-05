using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a tool call, describing the tool, its type, and specific details
/// relevant to its execution within a workflow or process.
/// </summary>
[JsonConverter(typeof(ToolCallConverter))]
public abstract class ToolCall
{
    /// <summary>
    ///     The ID of the tool call.
    ///     This ID must be referenced when you submit the tool outputs in using the Submit tool outputs to run endpoint.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    ///     The type of tool call the output is required for.
    /// </summary>
    [JsonProperty("type")]
    public ToolCallType Type { get; set; }
}

/// <summary>
/// Represents a function-based tool call, containing details about the specific function
/// being invoked, its parameters, and execution context.
/// </summary>
public sealed class FunctionToolCall : ToolCall
{
    /// <summary>
    ///     The definition of the function that was called.
    /// </summary>
    [JsonProperty("function")]
    public FunctionCall FunctionCall { get; set; } = null!;
}

/// <summary>
/// Represents a tool call designed for executing code interpretation tasks,
/// encapsulating the details of the code interpreter being used and its execution context.
/// </summary>
public sealed class CodeInterpreterToolCall : ToolCall
{
    /// <summary>
    /// Represents the Code Interpreter component of a tool call, which is responsible
    /// for processing input code, executing it, and producing specific outputs such
    /// as logs or image files.
    /// </summary>
    public CodeInterpreter CodeInterpreter { get; set; } = null!;
}

/// <summary>
/// Represents a tool call specifically tailored for file search operations,
/// encapsulating details related to file search tasks and their execution context.
/// </summary>
public sealed class FileSearchToolCall : ToolCall
{
    /// <summary>
    ///     For now, this is always going to be an empty object. TODO: When OpenAI finished implementation, map it here
    /// </summary>
    /// 
    public object? FileSearch { get; set; }
}

/// <summary>
/// Enumerates the different types of tool calls available within the system,
/// categorizing them based on their functionality or purpose.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ToolCallType
{
    /// <summary>
    /// Represents a tool call of type FunctionToolCall
    /// </summary>
    [EnumMember(Value = "function")] FunctionToolCall,

    /// <summary>
    /// Represents a tool call of type CodeInterpreterToolCall
    /// </summary>
    [EnumMember(Value = "code_interpreter")]
    CodeInterpreterToolCall,

    /// <summary>
    /// Represents a tool call of type FileSearchToolCall
    /// </summary>
    [EnumMember(Value = "file_search")] FileSearchToolCall
}

internal class ToolCallConverter : JsonConverter<ToolCall>
{
    public override void WriteJson(JsonWriter writer, ToolCall? value, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.FromObject(value!, serializer);
        jsonObject.WriteTo(writer);
    }

    public override ToolCall? ReadJson(JsonReader reader, Type objectType, ToolCall? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        string? typeToken = jsonObject["type"]?.ToString();
        if (!Enum.TryParse(typeToken, true, out ToolCallType toolCallType))
        {
            return null;
        }

        return toolCallType switch
        {
            ToolCallType.FunctionToolCall => jsonObject
                .ToObject<FunctionToolCall>(serializer)!,
            ToolCallType.CodeInterpreterToolCall => jsonObject
                .ToObject<CodeInterpreterToolCall>(serializer)!,
            ToolCallType.FileSearchToolCall => jsonObject
                .ToObject<FileSearchToolCall>(serializer)!,
            _ => null
        };
    }
}