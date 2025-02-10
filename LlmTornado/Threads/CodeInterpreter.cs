using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a code interpreter capable of processing input code and producing
/// outputs in various forms, such as logs or images, with a defined structure and format.
/// </summary>
/// <remarks>
/// This class is used in conjunction with the CodeInterpreterToolCall to encapsulate
/// details related to interpreting and executing code workflows.
/// </remarks>
public sealed class CodeInterpreter
{
    /// <summary>
    ///     The input to the Code Interpreter tool call.
    /// </summary>
    [JsonProperty("input")]
    public string Input { get; set; } = null!;

    /// <summary>
    ///     The outputs from the Code Interpreter tool call.
    ///     Code Interpreter can output one or more items, including text (logs) or images (image).
    ///     Each of these are represented by a different object type.
    /// </summary>
    [JsonProperty("outputs")]
    [JsonConverter(typeof(CodeInterpreterOutputListConverter))]
    public IReadOnlyList<CodeInterpreterOutput> Outputs { get; set; } = null!;
}