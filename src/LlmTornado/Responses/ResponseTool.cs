using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LlmTornado.Common;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Base class for all response tools in the OpenAI Responses API.
/// </summary>
[JsonConverter(typeof(ResponseToolConverter))]
public abstract class ResponseTool
{
    /// <summary>
    /// The type discriminator for the tool.
    /// </summary>
    [JsonProperty("type", Required = Required.Always)]
    public abstract string Type { get; }
}

/// <summary>
/// Represents a function tool in the Responses API.
/// </summary>
public class ResponseFunctionTool : ResponseTool
{
    public override string Type => "function";

    /// <summary>
    /// The name of the function to call.
    /// </summary>
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// A description of the function. Used by the model to determine whether or not to call the function.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// A JSON schema object describing the parameters of the function.
    /// </summary>
    [JsonProperty("parameters", Required = Required.Always)]
    public JObject Parameters { get; set; } = null!;

    /// <summary>
    /// Whether to enforce strict parameter validation. Default true.
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }
}

/// <summary>
/// Represents a file search tool.
/// </summary>
public class ResponseFileSearchTool : ResponseTool
{
    public override string Type => "file_search";

    /// <summary>
    /// The IDs of the vector stores to search.
    /// </summary>
    [JsonProperty("vector_store_ids")]
    public List<string> VectorStoreIds { get; set; } = [];

    /// <summary>
    /// A filter to apply (optional).
    /// </summary>
    [JsonProperty("filters")]
    public ResponseFilter? Filters { get; set; }

    /// <summary>
    /// The maximum number of results to return (optional, 1-50).
    /// </summary>
    [JsonProperty("max_num_results")]
    public int? MaxNumResults { get; set; }

    /// <summary>
    /// Ranking options for search (optional).
    /// </summary>
    [JsonProperty("ranking_options")]
    public RankingOptions? RankingOptions { get; set; }
}

/// <summary>
/// Represents a web search tool.
/// </summary>
public class ResponseWebSearchTool : ResponseTool
{
    /// <summary>
    /// Type of the web search tool.
    /// </summary>
    public override string Type => 
        WebSearchToolType switch
        {
            ResponseWebSearchToolType.WebSearchPreview => "web_search_preview",
            ResponseWebSearchToolType.WebSearchPreview20250311 => "web_search_preview_2025_03_11",
            _ => "web_search_preview"
        };

    /// <summary>
    /// The type of the web search tool. One of web_search_preview or web_search_preview_2025_03_11.
    /// </summary>
    [JsonIgnore]
    public ResponseWebSearchToolType WebSearchToolType { get; set; } = ResponseWebSearchToolType.WebSearchPreview;

    /// <summary>
    /// High level guidance for the amount of context window space to use for the search.
    /// </summary>
    [JsonProperty("search_context_size")]
    public ResponseSearchContextSize? SearchContextSize { get; set; }

    /// <summary>
    /// The user's location.
    /// </summary>
    [JsonProperty("user_location")]
    public ResponseUserLocation? UserLocation { get; set; }
}

/// <summary>
/// Web search tool types
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseWebSearchToolType
{
    /// <summary>
    /// Web search preview
    /// </summary>
    [EnumMember(Value = "web_search_preview")]
    WebSearchPreview,

    /// <summary>
    /// Web search preview 2025-03-11
    /// </summary>
    [EnumMember(Value = "web_search_preview_2025_03_11")]
    WebSearchPreview20250311
}

/// <summary>
/// Search context size options for web search
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseSearchContextSize
{
    /// <summary>
    /// Low context size
    /// </summary>
    [EnumMember(Value = "low")]
    Low,

    /// <summary>
    /// Medium context size (default)
    /// </summary>
    [EnumMember(Value = "medium")]
    Medium,

    /// <summary>
    /// High context size
    /// </summary>
    [EnumMember(Value = "high")]
    High
}

/// <summary>
/// User location for web search
/// </summary>
public class ResponseUserLocation
{
    /// <summary>
    /// The type of location approximation. Always "approximate".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "approximate";

    /// <summary>
    /// Free text input for the city of the user, e.g. "San Francisco".
    /// </summary>
    [JsonProperty("city")]
    public string? City { get; set; }

    /// <summary>
    /// The two-letter ISO country code of the user, e.g. "US".
    /// </summary>
    [JsonProperty("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Free text input for the region of the user, e.g. "California".
    /// </summary>
    [JsonProperty("region")]
    public string? Region { get; set; }

    /// <summary>
    /// The IANA timezone of the user, e.g. "America/Los_Angeles".
    /// </summary>
    [JsonProperty("timezone")]
    public string? Timezone { get; set; }
}

/// <summary>
/// Represents a computer use tool.
/// </summary>
public class ResponseComputerUseTool : ResponseTool
{
    public override string Type => "computer_use_preview";

    [JsonProperty("display_height")]
    public int? DisplayHeight { get; set; }

    [JsonProperty("display_width")]
    public int? DisplayWidth { get; set; }

    /// <summary>
    /// Execution environment for the computer use tool (browser / mac / windows / ubuntu).
    /// </summary>
    [JsonProperty("environment")]
    public ResponseComputerEnvironment Environment { get; set; } = ResponseComputerEnvironment.Browser;
}

/// <summary>
/// Supported execution environments for <see cref="ResponseComputerUseTool"/>.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseComputerEnvironment
{
    /// <summary>
    /// Run inside a headless browser container.
    /// </summary>
    [EnumMember(Value = "browser")]
    Browser,

    /// <summary>
    /// macOS environment.
    /// </summary>
    [EnumMember(Value = "mac")]
    Mac,

    /// <summary>
    /// Microsoft Windows environment.
    /// </summary>
    [EnumMember(Value = "windows")]
    Windows,

    /// <summary>
    /// Ubuntu Linux environment.
    /// </summary>
    [EnumMember(Value = "ubuntu")]
    Ubuntu
}

/// <summary>
/// Represents an MCP (Model Context Protocol) tool.
/// </summary>
public class ResponseMcpTool : ResponseTool
{
    /// <summary>
    /// The type of the MCP tool. Always "mcp".
    /// </summary>
    [JsonIgnore]
    public override string Type => "mcp";

    /// <summary>
    /// A label for this MCP server, used to identify it in tool calls.
    /// </summary>
    [JsonProperty("server_label")]
    public string ServerLabel { get; set; }

    /// <summary>
    /// The URL for the MCP server.
    /// </summary>
    [JsonProperty("server_url")]
    public string ServerUrl { get; set; }

    /// <summary>
    /// List of allowed tool names or a filter object.
    /// </summary>
    [JsonProperty("allowed_tools")]
    [JsonConverter(typeof(ResponseMcpAllowedToolsConverter))]
    public ResponseMcpAllowedTools? AllowedTools { get; set; }

    /// <summary>
    /// Optional HTTP headers to send to the MCP server. Use for authentication or other purposes.
    /// </summary>
    [JsonProperty("headers")]
    public JObject? Headers { get; set; }

    /// <summary>
    /// Specify which of the MCP server's tools require approval.<br/>
    /// Either a filter or <c>ResponseMcpRequireApprovalOption.Never</c> / <c>ResponseMcpRequireApprovalOption.Always</c>
    /// </summary>
    [JsonProperty("require_approval")]
    public ResponseMcpRequireApproval? RequireApproval { get; set; }
}

/// <summary>
/// Base class for MCP allowed tools configuration
/// </summary>
public abstract class ResponseMcpAllowedTools
{
}

/// <summary>
/// A simple array of allowed tool names
/// </summary>
public class ResponseMcpAllowedToolsArray : ResponseMcpAllowedTools
{
    /// <summary>
    /// List of allowed tool names
    /// </summary>
    public List<string> ToolNames { get; set; } = [];

    public ResponseMcpAllowedToolsArray() { }

    public ResponseMcpAllowedToolsArray(List<string> toolNames)
    {
        ToolNames = toolNames;
    }

    public static implicit operator ResponseMcpAllowedToolsArray(List<string> toolNames)
    {
        return new ResponseMcpAllowedToolsArray(toolNames);
    }

    public static implicit operator List<string>(ResponseMcpAllowedToolsArray allowedTools)
    {
        return allowedTools.ToolNames;
    }
}

/// <summary>
/// A filter object to specify which tools are allowed
/// </summary>
public class ResponseMcpAllowedToolsFilter : ResponseMcpAllowedTools
{
    /// <summary>
    /// List of allowed tool names
    /// </summary>
    [JsonProperty("tool_names")]
    public List<string>? ToolNames { get; set; }
}

/// <summary>
/// Base class for MCP require approval configuration
/// </summary>
[JsonConverter(typeof(ResponseMcpRequireApprovalConverter))]
public abstract class ResponseMcpRequireApproval
{
    /// <summary>
    /// Implicit conversion from ResponseMcpApprovalPolicy to ResponseMcpRequireApproval
    /// </summary>
    public static implicit operator ResponseMcpRequireApproval(ResponseMcpApprovalPolicy policy)
    {
        return policy switch
        {
            ResponseMcpApprovalPolicy.Always => ResponseMcpRequireApprovalOption.Always,
            ResponseMcpApprovalPolicy.Never => ResponseMcpRequireApprovalOption.Never,
            _ => throw new ArgumentException($"Invalid approval policy: {policy}")
        };
    }
}

/// <summary>
/// Approval policy options for MCP tools
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseMcpApprovalPolicy
{
    /// <summary>
    /// All tools require approval
    /// </summary>
    [EnumMember(Value = "always")]
    Always = 1,

    /// <summary>
    /// No tools require approval
    /// </summary>
    [EnumMember(Value = "never")]
    Never = 2
}

/// <summary>
/// Simple option setting for approval policy - "always" or "never"
/// </summary>
public class ResponseMcpRequireApprovalOption : ResponseMcpRequireApproval
{
    /// <summary>
    /// Static instance for "always" policy
    /// </summary>
    public static readonly ResponseMcpRequireApprovalOption Always = new ResponseMcpRequireApprovalOption(ResponseMcpApprovalPolicy.Always);

    /// <summary>
    /// Static instance for "never" policy
    /// </summary>
    public static readonly ResponseMcpRequireApprovalOption Never = new ResponseMcpRequireApprovalOption(ResponseMcpApprovalPolicy.Never);

    /// <summary>
    /// The approval policy string value
    /// </summary>
    internal ResponseMcpApprovalPolicy PolicyValue { get; }

    private ResponseMcpRequireApprovalOption(ResponseMcpApprovalPolicy value)
    {
        PolicyValue = value;
    }

    public static implicit operator ResponseMcpRequireApprovalOption(ResponseMcpApprovalPolicy policy)
    {
        return policy switch
        {
            ResponseMcpApprovalPolicy.Always => Always,
            ResponseMcpApprovalPolicy.Never => Never,
            _ => throw new ArgumentException($"Invalid approval policy: {policy}")
        };
    }
}

/// <summary>
/// Object with detailed approval configuration
/// </summary>
public class ResponseMcpRequireApprovalFilter : ResponseMcpRequireApproval
{
    /// <summary>
    /// Tools that always require approval
    /// </summary>
    [JsonProperty("always")]
    public ResponseMcpApprovalToolList? Always { get; set; }

    /// <summary>
    /// Tools that never require approval
    /// </summary>
    [JsonProperty("never")]
    public ResponseMcpApprovalToolList? Never { get; set; }
}

/// <summary>
/// List of tools for approval configuration
/// </summary>
public class ResponseMcpApprovalToolList
{
    /// <summary>
    /// List of tool names
    /// </summary>
    [JsonProperty("tool_names")]
    public List<string>? ToolNames { get; set; }
}

/// <summary>
/// Custom converter for polymorphic deserialization of MCP allowed tools
/// </summary>
internal class ResponseMcpAllowedToolsConverter : JsonConverter<ResponseMcpAllowedTools>
{
    public override ResponseMcpAllowedTools? ReadJson(JsonReader reader, Type objectType, ResponseMcpAllowedTools? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JToken token = JToken.ReadFrom(reader);

        switch (token.Type)
        {
            case JTokenType.Array:
                // It's an array of strings
                List<string>? toolNames = token.ToObject<List<string>>();
                return toolNames != null ? new ResponseMcpAllowedToolsArray(toolNames) : null;

            case JTokenType.Object:
                // It's a filter object
                return new ResponseMcpAllowedToolsFilter
                {
                    ToolNames = token["tool_names"]?.ToObject<List<string>>()
                };

            default:
                throw new JsonSerializationException($"Unable to deserialize allowed_tools from token type: {token.Type}");
        }
    }

    public override void WriteJson(JsonWriter writer, ResponseMcpAllowedTools? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        switch (value)
        {
            case ResponseMcpAllowedToolsArray array:
                serializer.Serialize(writer, array.ToolNames);
                break;

            case ResponseMcpAllowedToolsFilter filter:
                writer.WriteStartObject();
                if (filter.ToolNames != null)
                {
                    writer.WritePropertyName("tool_names");
                    serializer.Serialize(writer, filter.ToolNames);
                }
                writer.WriteEndObject();
                break;

            default:
                throw new JsonSerializationException($"Unknown MCP allowed tools type: {value.GetType()}");
        }
    }
}

/// <summary>
/// Custom converter for polymorphic deserialization of MCP require approval
/// </summary>
internal class ResponseMcpRequireApprovalConverter : JsonConverter<ResponseMcpRequireApproval>
{
    public override ResponseMcpRequireApproval? ReadJson(JsonReader reader, Type objectType, ResponseMcpRequireApproval? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JToken token = JToken.ReadFrom(reader);

        switch (token.Type)
        {
            case JTokenType.String:
                // It's a simple string value
                string stringValue = token.ToString();
                return stringValue.ToLowerInvariant() switch
                {
                    "always" => ResponseMcpRequireApprovalOption.Always,
                    "never" => ResponseMcpRequireApprovalOption.Never,
                    _ => throw new JsonSerializationException($"Invalid approval policy: {stringValue}")
                };

            case JTokenType.Object:
                // It's a filter object
                return new ResponseMcpRequireApprovalFilter
                {
                    Always = token["always"]?.ToObject<ResponseMcpApprovalToolList>(),
                    Never = token["never"]?.ToObject<ResponseMcpApprovalToolList>()
                };

            default:
                throw new JsonSerializationException($"Unable to deserialize require_approval from token type: {token.Type}");
        }
    }

    public override void WriteJson(JsonWriter writer, ResponseMcpRequireApproval? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        switch (value)
        {
            case ResponseMcpRequireApprovalOption optionValue:
                // Write the string value directly
                writer.WriteValue(optionValue.PolicyValue);
                break;

            case ResponseMcpRequireApprovalFilter filter:
                writer.WriteStartObject();
                if (filter.Always != null)
                {
                    writer.WritePropertyName("always");
                    serializer.Serialize(writer, filter.Always);
                }
                if (filter.Never != null)
                {
                    writer.WritePropertyName("never");
                    serializer.Serialize(writer, filter.Never);
                }
                writer.WriteEndObject();
                break;

            default:
                throw new JsonSerializationException($"Unknown MCP require approval type: {value.GetType()}");
        }
    }
}

/// <summary>
/// Represents an image generation tool.
/// </summary>
public class ResponseImageGenerationTool : ResponseTool
{
    /// <summary>
    /// The type of the image generation tool. Always "image_generation".
    /// </summary>
    [JsonIgnore]
    public override string Type => "image_generation";

    /// <summary>
    /// Background type for the generated image. One of "transparent", "opaque", or "auto". Default: "auto".
    /// </summary>
    [JsonProperty("background")]
    public ImageBackgroundTypes? Background { get; set; }

    /// <summary>
    /// Optional mask for inpainting.
    /// </summary>
    [JsonProperty("input_image_mask")]
    public ResponseImageMask? InputImageMask { get; set; }

    /// <summary>
    /// The image generation model to use. Default: "gpt-image-1".
    /// </summary>
    [JsonProperty("model")]
    public ImageModel? Model { get; set; }

    /// <summary>
    /// Moderation level for the generated image. Default: "auto".
    /// </summary>
    [JsonProperty("moderation")]
    public ImageModerationTypes? Moderation { get; set; }

    /// <summary>
    /// Compression level for the output image. Default: 100.
    /// </summary>
    [JsonProperty("output_compression")]
    public int? OutputCompression { get; set; }

    /// <summary>
    /// The output format of the generated image. One of "png", "webp", or "jpeg". Default: "png".
    /// </summary>
    [JsonProperty("output_format")]
    public ImageOutputFormats? OutputFormat { get; set; }

    /// <summary>
    /// Number of partial images to generate in streaming mode, from 0 (default value) to 3.
    /// </summary>
    [JsonProperty("partial_images")]
    public int? PartialImages { get; set; }

    /// <summary>
    /// The quality of the generated image. One of "low", "medium", "high", or "auto". Default: "auto".
    /// </summary>
    [JsonProperty("quality")]
    public TornadoImageQualities? Quality { get; set; }

    /// <summary>
    /// The size of the generated image. One of "1024x1024", "1024x1536", "1536x1024", or "auto". Default: "auto".
    /// </summary>
    [JsonProperty("size")]
    public TornadoImageSizes? Size { get; set; }
}

/// <summary>
/// Optional mask for inpainting.
/// </summary>
public class ResponseImageMask
{
    /// <summary>
    /// File ID for the mask image.
    /// </summary>
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    /// <summary>
    /// Base64-encoded mask image.
    /// </summary>
    [JsonProperty("image_url")]
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Represents a code interpreter tool.
/// </summary>
public class ResponseCodeInterpreterTool : ResponseTool
{
    public override string Type => "code_interpreter";

    /// <summary>
    /// The code interpreter container. Can be a container ID or an object that specifies uploaded file IDs to make available to your code.
    /// </summary>
    [JsonProperty("container")]
    [JsonConverter(typeof(ResponseCodeInterpreterContainerConverter))]
    public ResponseCodeInterpreterContainer Container { get; set; } = new ResponseCodeInterpreterContainerAuto();
}

/// <summary>
/// Base class for code interpreter containers
/// </summary>
public abstract class ResponseCodeInterpreterContainer
{
    
}

/// <summary>
/// Container ID as a string
/// </summary>
public class ResponseCodeInterpreterContainerString : ResponseCodeInterpreterContainer
{
    /// <summary>
    /// The container ID
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    public ResponseCodeInterpreterContainerString() { }
    
    public ResponseCodeInterpreterContainerString(string containerId)
    {
        ContainerId = containerId;
    }

    public static implicit operator ResponseCodeInterpreterContainerString(string containerId)
    {
        return new ResponseCodeInterpreterContainerString(containerId);
    }

    public static implicit operator string(ResponseCodeInterpreterContainerString container)
    {
        return container.ContainerId;
    }
}

/// <summary>
/// Configuration for a code interpreter container. Optionally specify the IDs of the files to run the code on.
/// </summary>
public class ResponseCodeInterpreterContainerAuto : ResponseCodeInterpreterContainer
{
    /// <summary>
    /// Always "auto"
    /// </summary>
    [JsonProperty("type")]
    public string Type => "auto";

    /// <summary>
    /// An optional list of uploaded files to make available to your code
    /// </summary>
    [JsonProperty("file_ids")]
    public List<string>? FileIds { get; set; }
}

/// <summary>
/// Custom converter for polymorphic deserialization of code interpreter containers
/// </summary>
internal class ResponseCodeInterpreterContainerConverter : JsonConverter<ResponseCodeInterpreterContainer>
{
    public override ResponseCodeInterpreterContainer? ReadJson(JsonReader reader, Type objectType, ResponseCodeInterpreterContainer? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
            {
                // Container is a string (container ID)
                string? containerId = (string?)reader.Value;
                return containerId != null ? new ResponseCodeInterpreterContainerString(containerId) : null;
            }
            case JsonToken.StartObject:
            {
                // Container is an object
                JToken token = JToken.ReadFrom(reader);
                return token.ToObject<ResponseCodeInterpreterContainerAuto>(serializer);
            }
            default:
            {
                return null;
            }
        }
    }

    public override void WriteJson(JsonWriter writer, ResponseCodeInterpreterContainer? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        switch (value)
        {
            case ResponseCodeInterpreterContainerString stringContainer:
                writer.WriteValue(stringContainer.ContainerId);
                break;
            case ResponseCodeInterpreterContainerAuto autoContainer:
                serializer.Serialize(writer, autoContainer);
                break;
            default:
                throw new JsonSerializationException($"Unknown container type: {value.GetType()}");
        }
    }
}

/// <summary>
/// Custom converter for polymorphic deserialization of response tools.
/// </summary>
internal class ResponseToolConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(ResponseTool);
    public override bool CanWrite => true;
    public override bool CanRead => true;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();
        switch (type)
        {
            case "function":
                return new ResponseFunctionTool
                {
                    Name = (string)jo["name"]!,
                    Description = (string?)jo["description"],
                    Parameters = (JObject)jo["parameters"]!,
                    Strict = (bool?)jo["strict"]
                };
            case "file_search":
                return new ResponseFileSearchTool
                {
                    VectorStoreIds = jo["vector_store_ids"]?.ToObject<List<string>>(serializer),
                    Filters = jo["filters"]?.ToObject<ResponseFilter>(serializer),
                    MaxNumResults = (int?)jo["max_num_results"],
                    RankingOptions = jo["ranking_options"]?.ToObject<RankingOptions>(serializer)
                };
            case "web_search_preview":
            case "web_search_preview_2025_03_11":
                ResponseWebSearchToolType webSearchType = type == "web_search_preview_2025_03_11" 
                    ? ResponseWebSearchToolType.WebSearchPreview20250311 
                    : ResponseWebSearchToolType.WebSearchPreview;
                
                return new ResponseWebSearchTool
                {
                    WebSearchToolType = webSearchType,
                    SearchContextSize = jo["search_context_size"]?.ToObject<ResponseSearchContextSize>(serializer),
                    UserLocation = jo["user_location"]?.ToObject<ResponseUserLocation>(serializer)
                };
            case "code_interpreter":
                ResponseCodeInterpreterContainer? container = null;
                if (jo["container"] != null)
                {
                    ResponseCodeInterpreterContainerConverter containerConverter = new ResponseCodeInterpreterContainerConverter();
                    using JsonReader containerReader = jo["container"]!.CreateReader();
                    container = containerConverter.ReadJson(containerReader, typeof(ResponseCodeInterpreterContainer), null, false, serializer);
                }
                
                return new ResponseCodeInterpreterTool
                {
                    Container = container ?? new ResponseCodeInterpreterContainerAuto()
                };
            case "image_generation":
                return new ResponseImageGenerationTool
                {
                    Background = jo["background"]?.ToObject<ImageBackgroundTypes>(serializer),
                    InputImageMask = jo["input_image_mask"]?.ToObject<ResponseImageMask>(serializer),
                    Model = jo["model"]?.ToString(),
                    Moderation = jo["moderation"]?.ToObject<ImageModerationTypes>(serializer),
                    OutputCompression = (int?)jo["output_compression"],
                    OutputFormat = jo["output_format"]?.ToObject<ImageOutputFormats>(serializer),
                    PartialImages = (int?)jo["partial_images"],
                    Quality = jo["quality"]?.ToObject<TornadoImageQualities>(serializer),
                    Size = jo["size"]?.ToObject<TornadoImageSizes>(serializer)
                };
            case "mcp":
                JToken? headersToken = jo["headers"];
                JObject? headersObject = headersToken != null && headersToken.Type != JTokenType.Null
                    ? headersToken.ToObject<JObject>(serializer)
                    : null;

                return new ResponseMcpTool
                {
                    ServerLabel = (string?)jo["server_label"]!,
                    ServerUrl = (string?)jo["server_url"]!,
                    AllowedTools = jo["allowed_tools"]?.Type == JTokenType.Null ? null : jo["allowed_tools"]?.ToObject<ResponseMcpAllowedTools>(serializer),
                    Headers = headersObject,
                    RequireApproval = jo["require_approval"]?.ToObject<ResponseMcpRequireApproval>(serializer)
                };
            case "computer_use_preview":
            case "computer_use":
                return new ResponseComputerUseTool
                {
                    DisplayHeight = (int?)jo["display_height"],
                    DisplayWidth = (int?)jo["display_width"],
                    Environment = jo["environment"]?.ToObject<ResponseComputerEnvironment>(serializer) ?? ResponseComputerEnvironment.Browser
                };
            default:
                throw new JsonSerializationException($"Unknown tool type: {type}");
        }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteStartObject();
        switch (value)
        {
            case ResponseFunctionTool func:
                writer.WritePropertyName("type");
                writer.WriteValue(func.Type);
                writer.WritePropertyName("name");
                writer.WriteValue(func.Name);
                if (func.Description != null)
                {
                    writer.WritePropertyName("description");
                    writer.WriteValue(func.Description);
                }
                writer.WritePropertyName("parameters");
                serializer.Serialize(writer, func.Parameters);
                if (func.Strict != null)
                {
                    writer.WritePropertyName("strict");
                    writer.WriteValue(func.Strict);
                }
                break;
            case ResponseFileSearchTool file:
                writer.WritePropertyName("type");
                writer.WriteValue(file.Type);
                if (file.VectorStoreIds != null)
                {
                    writer.WritePropertyName("vector_store_ids");
                    serializer.Serialize(writer, file.VectorStoreIds);
                }
                if (file.Filters != null)
                {
                    writer.WritePropertyName("filters");
                    serializer.Serialize(writer, file.Filters);
                }
                if (file.MaxNumResults != null)
                {
                    writer.WritePropertyName("max_num_results");
                    writer.WriteValue(file.MaxNumResults);
                }
                if (file.RankingOptions != null)
                {
                    writer.WritePropertyName("ranking_options");
                    serializer.Serialize(writer, file.RankingOptions);
                }
                break;
            case ResponseWebSearchTool web:
                writer.WritePropertyName("type");
                writer.WriteValue(web.Type);
                if (web.SearchContextSize != null)
                {
                    writer.WritePropertyName("search_context_size");
                    serializer.Serialize(writer, web.SearchContextSize);
                }
                if (web.UserLocation != null)
                {
                    writer.WritePropertyName("user_location");
                    serializer.Serialize(writer, web.UserLocation);
                }
                break;
            case ResponseCodeInterpreterTool code:
                writer.WritePropertyName("type");
                writer.WriteValue(code.Type);
                if (code.Container != null)
                {
                    writer.WritePropertyName("container");
                    serializer.Serialize(writer, code.Container);
                }
                break;
            case ResponseImageGenerationTool img:
                writer.WritePropertyName("type");
                writer.WriteValue(img.Type);
                if (img.Background != null)
                {
                    writer.WritePropertyName("background");
                    serializer.Serialize(writer, img.Background);
                }
                if (img.InputImageMask != null)
                {
                    writer.WritePropertyName("input_image_mask");
                    serializer.Serialize(writer, img.InputImageMask);
                }
                if (img.Model != null)
                {
                    writer.WritePropertyName("model");
                    writer.WriteValue(img.Model);
                }
                if (img.Moderation != null)
                {
                    writer.WritePropertyName("moderation");
                    serializer.Serialize(writer, img.Moderation);
                }
                if (img.OutputCompression != null)
                {
                    writer.WritePropertyName("output_compression");
                    writer.WriteValue(img.OutputCompression);
                }
                if (img.OutputFormat != null)
                {
                    writer.WritePropertyName("output_format");
                    serializer.Serialize(writer, img.OutputFormat);
                }
                if (img.PartialImages != null)
                {
                    writer.WritePropertyName("partial_images");
                    writer.WriteValue(img.PartialImages);
                }
                if (img.Quality != null)
                {
                    writer.WritePropertyName("quality");
                    serializer.Serialize(writer, img.Quality);
                }
                if (img.Size != null)
                {
                    writer.WritePropertyName("size");
                    serializer.Serialize(writer, img.Size);
                }
                break;
            case ResponseMcpTool mcp:
                writer.WritePropertyName("type");
                writer.WriteValue(mcp.Type);
                if (mcp.ServerLabel != null)
                {
                    writer.WritePropertyName("server_label");
                    writer.WriteValue(mcp.ServerLabel);
                }
                if (mcp.ServerUrl != null)
                {
                    writer.WritePropertyName("server_url");
                    writer.WriteValue(mcp.ServerUrl);
                }
                if (mcp.AllowedTools != null)
                {
                    writer.WritePropertyName("allowed_tools");
                    serializer.Serialize(writer, mcp.AllowedTools);
                }
                if (mcp.Headers != null)
                {
                    writer.WritePropertyName("headers");
                    serializer.Serialize(writer, mcp.Headers);
                }
                if (mcp.RequireApproval != null)
                {
                    writer.WritePropertyName("require_approval");
                    serializer.Serialize(writer, mcp.RequireApproval);
                }
                break;
            case ResponseComputerUseTool comp:
                writer.WritePropertyName("type");
                writer.WriteValue(comp.Type);
                if (comp.DisplayHeight != null)
                {
                    writer.WritePropertyName("display_height");
                    writer.WriteValue(comp.DisplayHeight);
                }
                if (comp.DisplayWidth != null)
                {
                    writer.WritePropertyName("display_width");
                    writer.WriteValue(comp.DisplayWidth);
                }
                if (comp.Environment != null)
                {
                    writer.WritePropertyName("environment");
                    serializer.Serialize(writer, comp.Environment);
                }
                break;
        }
        writer.WriteEndObject();
    }
} 