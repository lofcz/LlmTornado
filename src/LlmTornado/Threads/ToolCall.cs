using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

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
    [JsonProperty("code_interpreter")]
    public CodeInterpreter CodeInterpreter { get; set; } = null!;
}

/// <summary>
/// Represents a tool call specifically tailored for file search operations,
/// encapsulating details related to file search tasks and their execution context.
/// </summary>
public sealed class FileSearchToolCall : ToolCall
{
    /// <summary>
    ///     For now, this is always going to be an empty object.
    /// </summary>
    /// 
    public FileSearchToolCallData? FileSearch { get; set; }
}

/// <summary>
/// Represents the data structure for file search tool call inputs and outputs,
/// including ranking options and a collection of search results.
/// </summary>
public class FileSearchToolCallData
{
    /// <summary>
    /// Specifies ranking options for file search operations, including the ranker type and minimum score threshold.
    /// Controls how search results are ranked based on predefined criteria.
    /// </summary>
    [JsonProperty("ranking_options")]
    public RankingOptions RankingOptions { get; set; } = null!;

    /// <summary>
    /// The collection of results returned by a file search tool call.
    /// Each result includes metadata and content details of the matched files.
    /// </summary>
    [JsonProperty("results")]
    public IReadOnlyList<FileSearchToolCallResult> Results { get; set; } = null!;
}

/// <summary>
/// Represents the result of a file search tool call, including metadata
/// such as file ID, name, and associated score.
/// </summary>
public class FileSearchToolCallResult
{
    /// <summary>
    /// The unique identifier of the file associated with a file search result.
    /// Used to reference the specific file in related operations or responses.
    /// </summary>
    [JsonProperty("file_id")]
    public string FileId { get; set; } = null!;

    /// <summary>
    /// The name of the file associated with the tool call result.
    /// Used to identify the file in the context of file search operations.
    /// </summary>
    [JsonProperty("file_name")]
    public string FileName { get; set; } = null!;

    /// <summary>
    /// The score representing the relevance or quality of the file in the search result.
    /// Used to evaluate and rank the file among other search results.
    /// </summary>
    [JsonProperty("file_type")]
    public double Score { get; set; }

    /// <summary>
    /// The content of the result that was found. The content is only included if requested via the include query parameter.
    ///</summary>
    [JsonProperty("content")]
    public IReadOnlyCollection<FileSearchToolCallContent> Contents { get; set; } = null!;
}

/// <summary>
/// Encapsulates the content details of a file search tool call, including its type and text representation.
/// </summary>
public class FileSearchToolCallContent
{
    /// <summary>
    /// Specifies the type of the tool call, indicating the functional category
    /// or purpose of the tool within a workflow or process.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Represents the text content associated with the file search tool call result.
    /// This property typically holds detailed information or content derived during the file search process.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = null!;
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
    [EnumMember(Value = "function")]
    FunctionToolCall,

    /// <summary>
    /// Represents a tool call of type CodeInterpreterToolCall
    /// </summary>
    [EnumMember(Value = "code_interpreter")]
    CodeInterpreterToolCall,

    /// <summary>
    /// Represents a tool call of type FileSearchToolCall
    /// </summary>
    [EnumMember(Value = "file_search")]
    FileSearchToolCall
}

internal class ToolCallListConverter : JsonConverter<IReadOnlyList<ToolCall>>
{
    public override void WriteJson(JsonWriter writer, IReadOnlyList<ToolCall>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override IReadOnlyList<ToolCall>? ReadJson(
        JsonReader reader,
        Type objectType,
        IReadOnlyList<ToolCall>? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        JArray array = JArray.Load(reader);
        List<ToolCall> items = [];

        foreach (JToken? token in array)
        {
            JObject jsonObject = (JObject)token;
            ToolCallType? toolCallType = jsonObject["type"]?.ToObject<ToolCallType>();

            ToolCall? toolCall = toolCallType switch
            {
                ToolCallType.FunctionToolCall => jsonObject.ToObject<FunctionToolCall>(serializer),
                ToolCallType.CodeInterpreterToolCall => jsonObject.ToObject<CodeInterpreterToolCall>(serializer),
                ToolCallType.FileSearchToolCall => jsonObject.ToObject<FileSearchToolCall>(serializer),
                _ => null
            };

            if (toolCall is not null)
            {
                items.Add(toolCall);
            }
        }

        return items.AsReadOnly();
    }
}