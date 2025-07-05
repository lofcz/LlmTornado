using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LlmTornado.Common;
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
    public List<string>? VectorStoreIds { get; set; }

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
    public override string Type => "computer_use";

    [JsonProperty("display_height")]
    public int? DisplayHeight { get; set; }

    [JsonProperty("display_width")]
    public int? DisplayWidth { get; set; }

    [JsonProperty("environment")]
    public string? Environment { get; set; }
}

/// <summary>
/// Represents an MCP tool (remote Model Context Protocol server).
/// </summary>
public class ResponseMcpTool : ResponseTool
{
    public override string Type => "mcp";

    [JsonProperty("server_label")]
    public string? ServerLabel { get; set; }

    [JsonProperty("server_url")]
    public string? ServerUrl { get; set; }

    [JsonProperty("allowed_tools")]
    public List<string>? AllowedTools { get; set; }

    [JsonProperty("headers")]
    public JObject? Headers { get; set; }

    [JsonProperty("require_approval")]
    public bool? RequireApproval { get; set; }
}

/// <summary>
/// Represents an image generation tool.
/// </summary>
public class ResponseImageGenerationTool : ResponseTool
{
    public override string Type => "image_generation";

    [JsonProperty("background")]
    public string? Background { get; set; }

    [JsonProperty("input_image_mask")]
    public JObject? InputImageMask { get; set; }
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
public class ResponseCodeInterpreterContainerConverter : JsonConverter<ResponseCodeInterpreterContainer>
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
public class ResponseToolConverter : JsonConverter
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
                    Background = (string?)jo["background"],
                    InputImageMask = (JObject?)jo["input_image_mask"]
                };
            case "mcp":
                return new ResponseMcpTool
                {
                    ServerLabel = (string?)jo["server_label"],
                    ServerUrl = (string?)jo["server_url"],
                    AllowedTools = jo["allowed_tools"]?.ToObject<List<string>>(serializer),
                    Headers = (JObject?)jo["headers"],
                    RequireApproval = (bool?)jo["require_approval"]
                };
            case "computer_use":
                return new ResponseComputerUseTool
                {
                    DisplayHeight = (int?)jo["display_height"],
                    DisplayWidth = (int?)jo["display_width"],
                    Environment = (string?)jo["environment"]
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
                    writer.WriteValue(img.Background);
                }
                if (img.InputImageMask != null)
                {
                    writer.WritePropertyName("input_image_mask");
                    serializer.Serialize(writer, img.InputImageMask);
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
                    writer.WriteValue(mcp.RequireApproval);
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
                    writer.WriteValue(comp.Environment);
                }
                break;
        }
        writer.WriteEndObject();
    }
} 