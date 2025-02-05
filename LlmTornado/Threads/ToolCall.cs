using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a tool call, describing the tool, its type, and specific details
/// relevant to its execution within a workflow or process.
/// </summary>
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
/// Enumerates the different types of tool calls available within the system,
/// categorizing them based on their functionality or purpose.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ToolCallType
{
    /// <summary>
    /// Represents a tool call of type FunctionToolCall
    /// </summary>
    [JsonProperty("function")] FunctionToolCall,

    /// <summary>
    /// Represents a tool call of type CodeInterpreterToolCall
    /// </summary>
    [JsonProperty("code_interpreter")]
    CodeInterpreterToolCall,

    /// <summary>
    /// Represents a tool call of type FileSearchToolCall
    /// </summary>
    [JsonProperty("file_search")] FileSearchToolCall
}