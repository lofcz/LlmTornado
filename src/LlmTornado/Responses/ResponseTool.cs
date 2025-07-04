using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
    public JObject? Filters { get; set; }

    /// <summary>
    /// The maximum number of results to return (optional, 1-50).
    /// </summary>
    [JsonProperty("max_num_results")]
    public int? MaxNumResults { get; set; }

    /// <summary>
    /// Ranking options for search (optional).
    /// </summary>
    [JsonProperty("ranking_options")]
    public JObject? RankingOptions { get; set; }
}

/// <summary>
/// Represents a web search tool.
/// </summary>
public class ResponseWebSearchTool : ResponseTool
{
    public override string Type => "web_search";

    // Optionally add properties like location, sites, etc. as needed
    [JsonProperty("location")]
    public JObject? Location { get; set; }

    [JsonProperty("sites")]
    public List<string>? Sites { get; set; }
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

    [JsonProperty("container")]
    public JObject? Container { get; set; }
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
        var jo = JObject.Load(reader);
        var type = jo["type"]?.ToString();
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
                    Filters = (JObject?)jo["filters"],
                    MaxNumResults = (int?)jo["max_num_results"],
                    RankingOptions = (JObject?)jo["ranking_options"]
                };
            case "web_search":
                return new ResponseWebSearchTool
                {
                    Location = (JObject?)jo["location"],
                    Sites = jo["sites"]?.ToObject<List<string>>(serializer)
                };
            case "code_interpreter":
                return new ResponseCodeInterpreterTool
                {
                    Container = (JObject?)jo["container"]
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
                if (web.Location != null)
                {
                    writer.WritePropertyName("location");
                    serializer.Serialize(writer, web.Location);
                }
                if (web.Sites != null)
                {
                    writer.WritePropertyName("sites");
                    serializer.Serialize(writer, web.Sites);
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