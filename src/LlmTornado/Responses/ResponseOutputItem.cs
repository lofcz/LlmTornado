using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Responses;

/// <summary>
/// Interface for all response output item types.
/// </summary>
public interface IResponseOutputItem
{
    /// <summary>
    /// The type of the output item.
    /// </summary>
    string Type { get; }
}

/// <summary>
/// Interface for all output content types.
/// </summary>
public interface IOutputContent
{
    /// <summary>
    /// The type of the content part.
    /// </summary>
    string Type { get; }
}

/// <summary>
/// Log probability object for output tokens.
/// </summary>
public class LogProbProperties
{
    /// <summary>
    /// The token that was used to generate the log probability.
    /// </summary>
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
    /// <summary>
    /// The log probability of the token.
    /// </summary>
    [JsonProperty("logprob")]
    public double LogProb { get; set; }
    /// <summary>
    /// The bytes that were used to generate the log probability.
    /// </summary>
    [JsonProperty("bytes")]
    public List<int> Bytes { get; set; } = new();
    /// <summary>
    /// The top log probabilities for the token.
    /// </summary>
    [JsonProperty("top_logprobs")]
    public List<LogProbProperties>? TopLogprobs { get; set; }
}

/// <summary>
/// Output text content from the model.
/// </summary>
public class OutputTextContent : IOutputContent
{
    [JsonProperty("type")]
    public string Type { get; set; } = "output_text";

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("annotations")]
    [JsonConverter(typeof(OutputContentAnnotationListConverter))]
    public List<IResponseOutputContentAnnotation>? Annotations { get; set; }

    /// <summary>
    /// Log probability objects for output tokens.
    /// </summary>
    [JsonProperty("logprobs")]
    public List<LogProbProperties>? Logprobs { get; set; }
}

/// <summary>
/// Refusal content from the model.
/// </summary>
public class RefusalContent : IOutputContent
{
    [JsonProperty("type")]
    public string Type { get; set; } = "refusal";

    [JsonProperty("refusal")]
    public string Refusal { get; set; } = string.Empty;
}

/// <summary>
/// Custom JsonConverter for List<IOutputContent>.
/// </summary>
internal class OutputContentListConverter : JsonConverter<List<IOutputContent>>
{
    public override void WriteJson(JsonWriter writer, List<IOutputContent>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override List<IOutputContent>? ReadJson(JsonReader reader, Type objectType, List<IOutputContent>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JArray array = JArray.Load(reader);
        List<IOutputContent> result = new List<IOutputContent>();
        foreach (JToken? token in array)
        {
            string? type = token["type"]?.ToString();
            IOutputContent? content = type switch
            {
                "output_text" => token.ToObject<OutputTextContent>(serializer),
                "refusal" => token.ToObject<RefusalContent>(serializer),
                _ => null
            };
            if (content != null)
                result.Add(content);
        }
        return result;
    }
}

/// <summary>
/// Status of an output message or tool call.
/// </summary>
public enum OutputItemStatus
{
    InProgress,
    Searching,
    Completed,
    Incomplete,
    Failed
}

/// <summary>
/// Output message from the model.
/// </summary>
public class OutputMessageItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "message";

    /// <summary>
    /// The unique ID of the output message.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The role of the output message. Always "assistant".
    /// </summary>
    [JsonProperty("role")]
    public string Role { get; set; } = "assistant";

    /// <summary>
    /// The content of the output message.
    /// </summary>
    [JsonProperty("content")]
    [JsonConverter(typeof(OutputContentListConverter))]
    public List<IOutputContent> Content { get; set; } = [];

    /// <summary>
    /// The status of the message input.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }
}

/// <summary>
/// The results of a file search tool call.
/// </summary>
public class FileSearchToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "file_search_call";

    /// <summary>
    /// The unique ID of the file search tool call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The status of the file search tool call.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }

    /// <summary>
    /// The queries used to search for files.
    /// </summary>
    [JsonProperty("queries")]
    public List<string> Queries { get; set; } = new();

    /// <summary>
    /// The results of the file search tool call.
    /// </summary>
    [JsonProperty("results")]
    public List<FileSearchResult>? Results { get; set; }

    /// <summary>
    /// File search result item.
    /// </summary>
    public class FileSearchResult
    {
        [JsonProperty("file_id")]
        public string FileId { get; set; } = string.Empty;
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
        [JsonProperty("filename")]
        public string Filename { get; set; } = string.Empty;
        [JsonProperty("attributes")]
        public object? Attributes { get; set; } // TODO: Strong type if needed
        [JsonProperty("score")]
        public float? Score { get; set; }
    }
}

/// <summary>
/// A tool call to run a function.
/// </summary>
public class FunctionToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "function_call";

    /// <summary>
    /// The unique ID of the function tool call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The unique ID of the function tool call generated by the model.
    /// </summary>
    [JsonProperty("call_id")]
    public string CallId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the function to run.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A JSON string of the arguments to pass to the function.
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// The status of the item.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus? Status { get; set; }
}

/// <summary>
/// The results of a web search tool call.
/// </summary>
public class WebSearchToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "web_search_call";

    /// <summary>
    /// The unique ID of the web search tool call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The status of the web search tool call.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }

    /// <summary>
    /// The action taken in this web search call.
    /// </summary>
    [JsonProperty("action")]
    [JsonConverter(typeof(WebSearchActionConverter))]
    public IWebSearchAction Action { get; set; } = null!;
}

/// <summary>
/// Interface for all web search actions.
/// </summary>
public interface IWebSearchAction
{
    /// <summary>
    /// The action type.
    /// </summary>
    string Type { get; }
}

/// <summary>
/// Action type "search" - Performs a web search query.
/// </summary>
public class WebSearchActionSearch : IWebSearchAction
{
    [JsonProperty("type")]
    public string Type { get; set; } = "search";

    [JsonProperty("query")]
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Action type "open_page" - Opens a specific URL from search results.
/// </summary>
public class WebSearchActionOpenPage : IWebSearchAction
{
    [JsonProperty("type")]
    public string Type { get; set; } = "open_page";

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Action type "find" - Searches for a pattern within a loaded page.
/// </summary>
public class WebSearchActionFind : IWebSearchAction
{
    [JsonProperty("type")]
    public string Type { get; set; } = "find";

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("pattern")]
    public string Pattern { get; set; } = string.Empty;
}

/// <summary>
/// A tool call to a computer use tool.
/// </summary>
public class ComputerToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "computer_call";

    /// <summary>
    /// The unique ID of the computer call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// An identifier used when responding to the tool call with output.
    /// </summary>
    [JsonProperty("call_id")]
    public string CallId { get; set; } = string.Empty;

    /// <summary>
    /// The action taken in this computer call.
    /// </summary>
    [JsonProperty("action")]
    [JsonConverter(typeof(ComputerActionConverter))]
    public IComputerAction Action { get; set; } = null!;

    /// <summary>
    /// The pending safety checks for the computer call.
    /// </summary>
    [JsonProperty("pending_safety_checks")]
    public List<ComputerToolCallSafetyCheck> PendingSafetyChecks { get; set; } = new();

    /// <summary>
    /// The status of the item.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }

    /// <summary>
    /// A pending safety check for the computer call.
    /// </summary>
    public class ComputerToolCallSafetyCheck
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }
}

/// <summary>
/// Interface for all computer actions.
/// </summary>
public interface IComputerAction
{
    /// <summary>
    /// The action type.
    /// </summary>
    string Type { get; }
}

/// <summary>
/// A click action.
/// </summary>
public class ClickAction : IComputerAction
{
    [JsonProperty("type")]
    public string Type { get; set; } = "click";
    [JsonProperty("button")]
    public string Button { get; set; } = string.Empty;
    [JsonProperty("x")]
    public int X { get; set; }
    [JsonProperty("y")]
    public int Y { get; set; }
}

/// <summary>
/// A double click action.
/// </summary>
public class DoubleClickAction : IComputerAction
{
    [JsonProperty("type")]
    public string Type { get; set; } = "double_click";
    [JsonProperty("x")]
    public int X { get; set; }
    [JsonProperty("y")]
    public int Y { get; set; }
}

/// <summary>
/// A drag action.
/// </summary>
public class DragAction : IComputerAction
{
    [JsonProperty("type")]
    public string Type { get; set; } = "drag";
    [JsonProperty("path")]
    public List<Coordinate> Path { get; set; } = new();

    /// <summary>
    /// A series of x/y coordinate pairs in the drag path.
    /// </summary>
    public class Coordinate
    {
        [JsonProperty("x")]
        public int X { get; set; }
        [JsonProperty("y")]
        public int Y { get; set; }
    }
}

/// <summary>
/// A description of the chain of thought used by a reasoning model while generating a response.
/// </summary>
public class ReasoningItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "reasoning";

    /// <summary>
    /// The unique identifier of the reasoning content.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The encrypted content of the reasoning item - populated when a response is generated with reasoning.encrypted_content in the include parameter.
    /// </summary>
    [JsonProperty("encrypted_content")]
    public string? EncryptedContent { get; set; }

    /// <summary>
    /// Reasoning text contents.
    /// </summary>
    [JsonProperty("summary")]
    public List<ReasoningSummaryText> Summary { get; set; } = new();

    /// <summary>
    /// The status of the item.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }

    /// <summary>
    /// A short summary of the reasoning used by the model when generating the response.
    /// </summary>
    public class ReasoningSummaryText
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "summary_text";
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }
}

/// <summary>
/// An image generation request made by the model.
/// </summary>
public class ImageGenToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "image_generation_call";

    /// <summary>
    /// The unique ID of the image generation call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The status of the image generation call.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }

    /// <summary>
    /// The generated image encoded in base64.
    /// </summary>
    [JsonProperty("result")]
    public string? Result { get; set; }
}

/// <summary>
/// A tool call to run code.
/// </summary>
public class CodeInterpreterToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "code_interpreter_call";

    /// <summary>
    /// The unique ID of the code interpreter tool call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The status of the code interpreter tool call.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }

    /// <summary>
    /// The ID of the container used to run the code.
    /// </summary>
    [JsonProperty("container_id")]
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// The code to run, or null if not available.
    /// </summary>
    [JsonProperty("code")]
    public string? Code { get; set; }

    /// <summary>
    /// The outputs generated by the code interpreter, such as logs or images. Can be null if no outputs are available.
    /// </summary>
    [JsonProperty("outputs")]
    [JsonConverter(typeof(CodeInterpreterOutputConverter))]
    public List<ICodeInterpreterOutput>? Outputs { get; set; }
}

/// <summary>
/// Interface for all code interpreter outputs.
/// </summary>
public interface ICodeInterpreterOutput
{
    /// <summary>
    /// The output type.
    /// </summary>
    string Type { get; }
}

/// <summary>
/// The logs output from the code interpreter.
/// </summary>
public class CodeInterpreterOutputLogs : ICodeInterpreterOutput
{
    [JsonProperty("type")]
    public string Type { get; set; } = "logs";
    [JsonProperty("logs")]
    public string Logs { get; set; } = string.Empty;
}

/// <summary>
/// The image output from the code interpreter.
/// </summary>
public class CodeInterpreterOutputImage : ICodeInterpreterOutput
{
    [JsonProperty("type")]
    public string Type { get; set; } = "image";
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Custom converter for ICodeInterpreterOutput polymorphic deserialization.
/// </summary>
internal class CodeInterpreterOutputConverter : JsonConverter<ICodeInterpreterOutput>
{
    public override ICodeInterpreterOutput? ReadJson(JsonReader reader, Type objectType, ICodeInterpreterOutput? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();
        return type switch
        {
            "logs" => jo.ToObject<CodeInterpreterOutputLogs>(serializer),
            "image" => jo.ToObject<CodeInterpreterOutputImage>(serializer),
            _ => throw new JsonSerializationException($"Unknown code interpreter output type: {type}")
        };
    }
    public override void WriteJson(JsonWriter writer, ICodeInterpreterOutput? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

/// <summary>
/// A tool call to run a command on the local shell.
/// </summary>
public class LocalShellToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "local_shell_call";

    /// <summary>
    /// The unique ID of the local shell call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The unique ID of the local shell tool call generated by the model.
    /// </summary>
    [JsonProperty("call_id")]
    public string CallId { get; set; } = string.Empty;

    /// <summary>
    /// The action to execute on the shell.
    /// </summary>
    [JsonProperty("action")]
    public LocalShellExecAction Action { get; set; } = new();

    /// <summary>
    /// The status of the local shell call.
    /// </summary>
    [JsonProperty("status")]
    public OutputItemStatus Status { get; set; }
}

/// <summary>
/// Execute a shell command on the server.
/// </summary>
public class LocalShellExecAction
{
    [JsonProperty("type")]
    public string Type { get; set; } = "exec";
    [JsonProperty("command")]
    public List<string> Command { get; set; } = new();
    [JsonProperty("timeout_ms")]
    public int? TimeoutMs { get; set; }
    [JsonProperty("working_directory")]
    public string? WorkingDirectory { get; set; }
    [JsonProperty("env")]
    public Dictionary<string, string> Env { get; set; } = new();
    [JsonProperty("user")]
    public string? User { get; set; }
}

/// <summary>
/// An invocation of a tool on an MCP server.
/// </summary>
public class McpToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "mcp_call";
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("server_label")]
    public string ServerLabel { get; set; } = string.Empty;
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = string.Empty;
    [JsonProperty("output")]
    public string? Output { get; set; }
    [JsonProperty("error")]
    public string? Error { get; set; }
}

/// <summary>
/// A list of tools available on an MCP server.
/// </summary>
public class McpListToolsItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "mcp_list_tools";
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("server_label")]
    public string ServerLabel { get; set; } = string.Empty;
    [JsonProperty("tools")]
    public List<McpListToolsTool> Tools { get; set; } = new();
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>
    /// A tool available on an MCP server.
    /// </summary>
    public class McpListToolsTool
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("description")]
        public string? Description { get; set; }
        [JsonProperty("input_schema")]
        public object InputSchema { get; set; } = new();
        [JsonProperty("annotations")]
        public object? Annotations { get; set; }
    }
}

/// <summary>
/// A request for human approval of a tool invocation.
/// </summary>
public class McpApprovalRequestItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "mcp_approval_request";
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("server_label")]
    public string ServerLabel { get; set; } = string.Empty;
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Custom JsonConverter for List<IResponseOutputItem>.
/// </summary>
internal class ResponseOutputItemListConverter : JsonConverter<List<IResponseOutputItem>>
{
    public override void WriteJson(JsonWriter writer, List<IResponseOutputItem>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override List<IResponseOutputItem>? ReadJson(JsonReader reader, Type objectType, List<IResponseOutputItem>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JArray array = JArray.Load(reader);
        List<IResponseOutputItem> result = new List<IResponseOutputItem>();
        foreach (JToken? token in array)
        {
            string? type = token["type"]?.ToString();
            IResponseOutputItem? item = type switch
            {
                "message" => token.ToObject<OutputMessageItem>(serializer),
                "file_search_call" => token.ToObject<FileSearchToolCallItem>(serializer),
                "function_call" => token.ToObject<FunctionToolCallItem>(serializer),
                "web_search_call" => token.ToObject<WebSearchToolCallItem>(serializer),
                "computer_call" => token.ToObject<ComputerToolCallItem>(serializer),
                "reasoning" => token.ToObject<ReasoningItem>(serializer),
                "image_generation_call" => token.ToObject<ImageGenToolCallItem>(serializer),
                "code_interpreter_call" => token.ToObject<CodeInterpreterToolCallItem>(serializer),
                "local_shell_call" => token.ToObject<LocalShellToolCallItem>(serializer),
                "mcp_call" => token.ToObject<McpToolCallItem>(serializer),
                "mcp_list_tools" => token.ToObject<McpListToolsItem>(serializer),
                "mcp_approval_request" => token.ToObject<McpApprovalRequestItem>(serializer),
                _ => null
            };
            if (item != null)
                result.Add(item);
        }
        return result;
    }
}

/// <summary>
/// Custom converter for IWebSearchAction polymorphic deserialization.
/// </summary>
internal class WebSearchActionConverter : JsonConverter<IWebSearchAction>
{
    public override IWebSearchAction? ReadJson(JsonReader reader, Type objectType, IWebSearchAction? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();
        return type switch
        {
            "search" => jo.ToObject<WebSearchActionSearch>(serializer),
            "open_page" => jo.ToObject<WebSearchActionOpenPage>(serializer),
            "find" => jo.ToObject<WebSearchActionFind>(serializer),
            _ => throw new JsonSerializationException($"Unknown web search action type: {type}")
        };
    }
    public override void WriteJson(JsonWriter writer, IWebSearchAction? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

/// <summary>
/// Custom converter for IComputerAction polymorphic deserialization.
/// </summary>
internal class ComputerActionConverter : JsonConverter<IComputerAction>
{
    public override IComputerAction? ReadJson(JsonReader reader, Type objectType, IComputerAction? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();
        return type switch
        {
            "click" => jo.ToObject<ClickAction>(serializer),
            "double_click" => jo.ToObject<DoubleClickAction>(serializer),
            "drag" => jo.ToObject<DragAction>(serializer),
            // Add other ComputerAction types as needed
            _ => throw new JsonSerializationException($"Unknown computer action type: {type}")
        };
    }
    public override void WriteJson(JsonWriter writer, IComputerAction? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
} 