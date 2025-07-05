using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
public interface IResponseOutputContent
{
    /// <summary>
    /// The type of the content part.
    /// </summary>
    string Type { get; }
}

/// <summary>
/// Interface for all response content part types.
/// </summary>
public interface IResponseContentPart
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
    public List<int> Bytes { get; set; } = new List<int>();
    /// <summary>
    /// The top log probabilities for the token.
    /// </summary>
    [JsonProperty("top_logprobs")]
    public List<LogProbProperties>? TopLogprobs { get; set; }
}

/// <summary>
/// Output text content from the model.
/// </summary>
public class ResponseOutputTextContent : IResponseOutputContent
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
public class RefusalContent : IResponseOutputContent
{
    [JsonProperty("type")]
    public string Type { get; set; } = "refusal";

    [JsonProperty("refusal")]
    public string Refusal { get; set; } = string.Empty;
}

/// <summary>
/// Response content part for output text from the model.
/// </summary>
public class ResponseContentPartOutputText : IResponseContentPart
{
    /// <summary>
    /// The type of the content part. Always "output_text".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "output_text";

    /// <summary>
    /// The text output from the model.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The annotations of the text output.
    /// </summary>
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
/// Response content part for refusal from the model.
/// </summary>
public class ResponseContentPartRefusal : IResponseContentPart
{
    /// <summary>
    /// The type of the content part. Always "refusal".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "refusal";

    /// <summary>
    /// The refusal explanation from the model.
    /// </summary>
    [JsonProperty("refusal")]
    public string Refusal { get; set; } = string.Empty;
}

/// <summary>
/// Custom JsonConverter for List<IOutputContent>.
/// </summary>
internal class OutputContentListConverter : JsonConverter<List<IResponseOutputContent>>
{
    public override void WriteJson(JsonWriter writer, List<IResponseOutputContent>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override List<IResponseOutputContent>? ReadJson(JsonReader reader, Type objectType, List<IResponseOutputContent>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JArray array = JArray.Load(reader);
        List<IResponseOutputContent> result = new List<IResponseOutputContent>();
        foreach (JToken? token in array)
        {
            string? type = token["type"]?.ToString();
            IResponseOutputContent? content = type switch
            {
                "output_text" => token.ToObject<ResponseOutputTextContent>(serializer),
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
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseOutputItemStatus
{
    [EnumMember(Value = "in_progress")]
    InProgress,
    [EnumMember(Value = "searching")]
    Searching,
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "incomplete")]
    Incomplete,
    [EnumMember(Value = "failed")]
    Failed
}

/// <summary>
/// Output message from the model.
/// </summary>
public class ResponseOutputMessageItem : IResponseOutputItem
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
    public List<IResponseOutputContent> Content { get; set; } = [];

    /// <summary>
    /// The status of the message input.
    /// </summary>
    [JsonProperty("status")]
    public ResponseOutputItemStatus Status { get; set; }
}

/// <summary>
/// The results of a file search tool call.
/// </summary>
public class ResponseFileSearchToolCallItem : IResponseOutputItem
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
    public ResponseOutputItemStatus Status { get; set; }

    /// <summary>
    /// The queries used to search for files.
    /// </summary>
    [JsonProperty("queries")]
    public List<string> Queries { get; set; } = new List<string>();

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
public class ResponseFunctionToolCallItem : IResponseOutputItem
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
    public ResponseOutputItemStatus? Status { get; set; }
}

/// <summary>
/// The results of a web search tool call.
/// </summary>
public class ResponseWebSearchToolCallItem : IResponseOutputItem
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
    public ResponseOutputItemStatus Status { get; set; }

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
/// Action type "find"/"find_in_page" - Searches for a pattern within a loaded page.
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
public class ResponseComputerToolCallItem : IResponseOutputItem
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
    public List<ComputerToolCallSafetyCheck> PendingSafetyChecks { get; set; } = new List<ComputerToolCallSafetyCheck>();

    /// <summary>
    /// The status of the item.
    /// </summary>
    [JsonProperty("status")]
    public ResponseOutputItemStatus Status { get; set; }

    /// <summary>
    /// A pending safety check for the computer call.
    /// </summary>
    public class ComputerToolCallSafetyCheck
    {
        /// <summary>
        /// The ID of the pending safety check.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// The type of the pending safety check.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Details about the pending safety check.
        /// </summary>
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
    /// <summary>
    /// Specifies the event type. For a click action, this property is always set to "click".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "click";
    
    /// <summary>
    /// Indicates which mouse button was pressed during the click. One of "left", "right", "wheel", "back", or "forward".
    /// </summary>
    [JsonProperty("button")]
    public string Button { get; set; } = string.Empty;
    
    /// <summary>
    /// The x-coordinate where the click occurred.
    /// </summary>
    [JsonProperty("x")]
    public int X { get; set; }
    
    /// <summary>
    /// The y-coordinate where the click occurred.
    /// </summary>
    [JsonProperty("y")]
    public int Y { get; set; }
}

/// <summary>
/// A double click action.
/// </summary>
public class DoubleClickAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a double click action, this property is always set to "double_click".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "double_click";
    
    /// <summary>
    /// The x-coordinate where the double click occurred.
    /// </summary>
    [JsonProperty("x")]
    public int X { get; set; }
    
    /// <summary>
    /// The y-coordinate where the double click occurred.
    /// </summary>
    [JsonProperty("y")]
    public int Y { get; set; }
}

/// <summary>
/// A drag action.
/// </summary>
public class DragAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a drag action, this property is always set to "drag".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "drag";
    
    /// <summary>
    /// An array of coordinates representing the path of the drag action. Coordinates will appear as an array of objects, eg [{ x: 100, y: 200 }, { x: 200, y: 300 }]
    /// </summary>
    [JsonProperty("path")]
    public List<Coordinate> Path { get; set; } = new List<Coordinate>();

    /// <summary>
    /// A series of x/y coordinate pairs in the drag path.
    /// </summary>
    public class Coordinate
    {
        /// <summary>
        /// The x-coordinate.
        /// </summary>
        [JsonProperty("x")]
        public int X { get; set; }
        
        /// <summary>
        /// The y-coordinate.
        /// </summary>
        [JsonProperty("y")]
        public int Y { get; set; }
    }
}

/// <summary>
/// A collection of keypresses the model would like to perform.
/// </summary>
public class KeyPressAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a keypress action, this property is always set to "keypress".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "keypress";
    
    /// <summary>
    /// The combination of keys the model is requesting to be pressed. This is an array of strings, each representing a key.
    /// </summary>
    [JsonProperty("keys")]
    public List<string> Keys { get; set; } = new List<string>();
}

/// <summary>
/// A mouse move action.
/// </summary>
public class MoveAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a move action, this property is always set to "move".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "move";
    
    /// <summary>
    /// The x-coordinate to move to.
    /// </summary>
    [JsonProperty("x")]
    public int X { get; set; }
    
    /// <summary>
    /// The y-coordinate to move to.
    /// </summary>
    [JsonProperty("y")]
    public int Y { get; set; }
}

/// <summary>
/// A screenshot action.
/// </summary>
public class ScreenshotAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a screenshot action, this property is always set to "screenshot".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "screenshot";
}

/// <summary>
/// A scroll action.
/// </summary>
public class ScrollAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a scroll action, this property is always set to "scroll".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "scroll";
    
    /// <summary>
    /// The x-coordinate where the scroll occurred.
    /// </summary>
    [JsonProperty("x")]
    public int X { get; set; }
    
    /// <summary>
    /// The y-coordinate where the scroll occurred.
    /// </summary>
    [JsonProperty("y")]
    public int Y { get; set; }
    
    /// <summary>
    /// The horizontal scroll distance.
    /// </summary>
    [JsonProperty("scroll_x")]
    public int ScrollX { get; set; }
    
    /// <summary>
    /// The vertical scroll distance.
    /// </summary>
    [JsonProperty("scroll_y")]
    public int ScrollY { get; set; }
}

/// <summary>
/// An action to type in text.
/// </summary>
public class TypeAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a type action, this property is always set to "type".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "type";
    
    /// <summary>
    /// The text to type.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// A wait action.
/// </summary>
public class WaitAction : IComputerAction
{
    /// <summary>
    /// Specifies the event type. For a wait action, this property is always set to "wait".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "wait";
}

/// <summary>
/// A description of the chain of thought used by a reasoning model while generating a response.
/// </summary>
public class ResponseReasoningItem : IResponseOutputItem
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
    public List<ReasoningSummaryText> Summary { get; set; } = new List<ReasoningSummaryText>();

    /// <summary>
    /// The status of the item.
    /// </summary>
    [JsonProperty("status")]
    public ResponseOutputItemStatus Status { get; set; }

    /// <summary>
    /// A short summary of the reasoning used by the model when generating the response.
    /// </summary>
    public class ReasoningSummaryText
    {
        /// <summary>
        /// The type of the object. Always "summary_text".
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "summary_text";
        
        /// <summary>
        /// A short summary of the reasoning used by the model when generating the response.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }
}

/// <summary>
/// An image generation request made by the model.
/// </summary>
public class ResponseImageGenToolCallItem : IResponseOutputItem
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
    public ResponseOutputItemStatus Status { get; set; }

    /// <summary>
    /// The generated image encoded in base64.
    /// </summary>
    [JsonProperty("result")]
    public string? Result { get; set; }
}

/// <summary>
/// A tool call to run code.
/// </summary>
public class ResponseCodeInterpreterToolCallItem : IResponseOutputItem
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
    public ResponseOutputItemStatus Status { get; set; }

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
    [JsonConverter(typeof(CodeInterpreterOutputsConverter))]
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
    /// <summary>
    /// The type of the output. Always "logs".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "logs";
    
    /// <summary>
    /// The logs output from the code interpreter.
    /// </summary>
    [JsonProperty("logs")]
    public string Logs { get; set; } = string.Empty;
}

/// <summary>
/// The image output from the code interpreter.
/// </summary>
public class CodeInterpreterOutputImage : ICodeInterpreterOutput
{
    /// <summary>
    /// The type of the output. Always "image".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "image";
    
    /// <summary>
    /// The URL of the image output from the code interpreter.
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Converter for the <c>outputs</c> array in a code interpreter call. Handles <c>null</c> as well as an array of diverse output objects.
/// </summary>
internal class CodeInterpreterOutputsConverter : JsonConverter<List<ICodeInterpreterOutput>>
{
    public override List<ICodeInterpreterOutput>? ReadJson(JsonReader reader, Type objectType, List<ICodeInterpreterOutput>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonToken.StartArray)
        {
            throw new JsonSerializationException($"Expected JSON array for code interpreter outputs but found {reader.TokenType}.");
        }

        // Load the array token to iterate over items.
        JArray array = JArray.Load(reader);
        var result = new List<ICodeInterpreterOutput>(array.Count);

        foreach (JToken item in array)
        {
            // Each item should be an object with a "type" field; delegate to the single-item converter.
            if (item.Type == JTokenType.Object)
            {
                result.Add(item.ToObject<ICodeInterpreterOutput>(serializer)!);
            }
            else
            {
                throw new JsonSerializationException($"Expected object in outputs array but found {item.Type}.");
            }
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, List<ICodeInterpreterOutput>? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        foreach (var output in value)
        {
            serializer.Serialize(writer, output);
        }
        writer.WriteEndArray();
    }
}

/// <summary>
/// Custom converter for a single <see cref="ICodeInterpreterOutput"/> object based on its <c>type</c> discriminator.
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
public class ResponseLocalShellToolCallItem : IResponseOutputItem
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
    public LocalShellExecAction Action { get; set; } = new LocalShellExecAction();

    /// <summary>
    /// The status of the local shell call.
    /// </summary>
    [JsonProperty("status")]
    public ResponseOutputItemStatus Status { get; set; }
}

/// <summary>
/// Execute a shell command on the server.
/// </summary>
public class LocalShellExecAction
{
    /// <summary>
    /// The type of the local shell action. Always "exec".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "exec";
    
    /// <summary>
    /// The command to run.
    /// </summary>
    [JsonProperty("command")]
    public List<string> Command { get; set; } = new List<string>();
    
    /// <summary>
    /// Optional timeout in milliseconds for the command.
    /// </summary>
    [JsonProperty("timeout_ms")]
    public int? TimeoutMs { get; set; }
    
    /// <summary>
    /// Optional working directory to run the command in.
    /// </summary>
    [JsonProperty("working_directory")]
    public string? WorkingDirectory { get; set; }
    
    /// <summary>
    /// Environment variables to set for the command.
    /// </summary>
    [JsonProperty("env")]
    public Dictionary<string, string> Env { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Optional user to run the command as.
    /// </summary>
    [JsonProperty("user")]
    public string? User { get; set; }
}

/// <summary>
/// An invocation of a tool on an MCP server.
/// </summary>
public class ResponseMcpToolCallItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "mcp_call";
    
    /// <summary>
    /// The unique ID of the tool call.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The label of the MCP server running the tool.
    /// </summary>
    [JsonProperty("server_label")]
    public string ServerLabel { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the tool that was run.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// A JSON string of the arguments passed to the tool.
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = string.Empty;
    
    /// <summary>
    /// The output from the tool call.
    /// </summary>
    [JsonProperty("output")]
    public string? Output { get; set; }
    
    /// <summary>
    /// The error from the tool call, if any.
    /// </summary>
    [JsonProperty("error")]
    public string? Error { get; set; }
}

/// <summary>
/// A list of tools available on an MCP server.
/// </summary>
public class ResponseMcpListToolsItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "mcp_list_tools";
    
    /// <summary>
    /// The unique ID of the list.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The label of the MCP server.
    /// </summary>
    [JsonProperty("server_label")]
    public string ServerLabel { get; set; } = string.Empty;
    
    /// <summary>
    /// The tools available on the server.
    /// </summary>
    [JsonProperty("tools")]
    public List<McpListToolsTool> Tools { get; set; } = new List<McpListToolsTool>();
    
    /// <summary>
    /// Error message if the server could not list tools.
    /// </summary>
    [JsonProperty("error")]
    public string? Error { get; set; }

    /// <summary>
    /// A tool available on an MCP server.
    /// </summary>
    public class McpListToolsTool
    {
        /// <summary>
        /// The name of the tool.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The description of the tool.
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }
        
        /// <summary>
        /// The JSON schema describing the tool's input.
        /// </summary>
        [JsonProperty("input_schema")]
        public object InputSchema { get; set; } = new object();
        
        /// <summary>
        /// Additional annotations about the tool.
        /// </summary>
        [JsonProperty("annotations")]
        public object? Annotations { get; set; }
    }
}

/// <summary>
/// A request for human approval of a tool invocation.
/// </summary>
public class ResponseMcpApprovalRequestItem : IResponseOutputItem
{
    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; set; } = "mcp_approval_request";
    
    /// <summary>
    /// The unique ID of the approval request.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The label of the MCP server making the request.
    /// </summary>
    [JsonProperty("server_label")]
    public string ServerLabel { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the tool to run.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// A JSON string of arguments for the tool.
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Custom JsonConverter for IResponseOutputItem that handles the oneOf structure.
/// </summary>
internal class ResponseOutputItemConverter : JsonConverter<IResponseOutputItem>
{
    public override void WriteJson(JsonWriter writer, IResponseOutputItem? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override IResponseOutputItem? ReadJson(JsonReader reader, Type objectType, IResponseOutputItem? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jsonObject = JObject.Load(reader);
        string? type = jsonObject["type"]?.ToString();

        return type switch
        {
            "message" => jsonObject.ToObject<ResponseOutputMessageItem>(serializer),
            "file_search_call" => jsonObject.ToObject<ResponseFileSearchToolCallItem>(serializer),
            "function_call" => jsonObject.ToObject<ResponseFunctionToolCallItem>(serializer),
            "web_search_call" => jsonObject.ToObject<ResponseWebSearchToolCallItem>(serializer),
            "computer_call" => jsonObject.ToObject<ResponseComputerToolCallItem>(serializer),
            "reasoning" => jsonObject.ToObject<ResponseReasoningItem>(serializer),
            "image_generation_call" => jsonObject.ToObject<ResponseImageGenToolCallItem>(serializer),
            "code_interpreter_call" => jsonObject.ToObject<ResponseCodeInterpreterToolCallItem>(serializer),
            "local_shell_call" => jsonObject.ToObject<ResponseLocalShellToolCallItem>(serializer),
            "mcp_call" => jsonObject.ToObject<ResponseMcpToolCallItem>(serializer),
            "mcp_list_tools" => jsonObject.ToObject<ResponseMcpListToolsItem>(serializer),
            "mcp_approval_request" => jsonObject.ToObject<ResponseMcpApprovalRequestItem>(serializer),
            _ => null
        };
    }
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
                "message" => token.ToObject<ResponseOutputMessageItem>(serializer),
                "file_search_call" => token.ToObject<ResponseFileSearchToolCallItem>(serializer),
                "function_call" => token.ToObject<ResponseFunctionToolCallItem>(serializer),
                "web_search_call" => token.ToObject<ResponseWebSearchToolCallItem>(serializer),
                "computer_call" => token.ToObject<ResponseComputerToolCallItem>(serializer),
                "reasoning" => token.ToObject<ResponseReasoningItem>(serializer),
                "image_generation_call" => token.ToObject<ResponseImageGenToolCallItem>(serializer),
                "code_interpreter_call" => token.ToObject<ResponseCodeInterpreterToolCallItem>(serializer),
                "local_shell_call" => token.ToObject<ResponseLocalShellToolCallItem>(serializer),
                "mcp_call" => token.ToObject<ResponseMcpToolCallItem>(serializer),
                "mcp_list_tools" => token.ToObject<ResponseMcpListToolsItem>(serializer),
                "mcp_approval_request" => token.ToObject<ResponseMcpApprovalRequestItem>(serializer),
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
            "find_in_page" => jo.ToObject<WebSearchActionFind>(serializer),
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
            "keypress" => jo.ToObject<KeyPressAction>(serializer),
            "move" => jo.ToObject<MoveAction>(serializer),
            "screenshot" => jo.ToObject<ScreenshotAction>(serializer),
            "scroll" => jo.ToObject<ScrollAction>(serializer),
            "type" => jo.ToObject<TypeAction>(serializer),
            "wait" => jo.ToObject<WaitAction>(serializer),
            _ => throw new JsonSerializationException($"Unknown computer action type: {type}")
        };
    }
    public override void WriteJson(JsonWriter writer, IComputerAction? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

/// <summary>
/// Custom JsonConverter for IResponseContentPart that handles the oneOf structure.
/// </summary>
internal class ResponseContentPartConverter : JsonConverter<IResponseContentPart>
{
    public override void WriteJson(JsonWriter writer, IResponseContentPart? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override IResponseContentPart? ReadJson(JsonReader reader, Type objectType, IResponseContentPart? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jsonObject = JObject.Load(reader);
        string? type = jsonObject["type"]?.ToString();

        return type switch
        {
            "output_text" => jsonObject.ToObject<ResponseContentPartOutputText>(serializer),
            "refusal" => jsonObject.ToObject<ResponseContentPartRefusal>(serializer),
            _ => null
        };
    }
} 