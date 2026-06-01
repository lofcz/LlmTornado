using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

/// <summary>
/// Input content block for the Interactions API (text or image).
/// </summary>
public class InteractionContent
{
    /// <summary>
    /// Content type: <c>text</c> or <c>image</c>.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Text payload when <see cref="Type"/> is <c>text</c>.
    /// </summary>
    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }

    /// <summary>
    /// Base64 image bytes when <see cref="Type"/> is <c>image</c>.
    /// </summary>
    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public string? Data { get; set; }

    /// <summary>
    /// Remote URI for image or document content.
    /// </summary>
    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string? Uri { get; set; }

    /// <summary>
    /// Image or document MIME type (e.g. <c>image/jpeg</c>, <c>application/pdf</c>).
    /// </summary>
    [JsonProperty("mime_type", NullValueHandling = NullValueHandling.Ignore)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Creates a text content block.
    /// </summary>
    public static InteractionContent AsText(string text) => new() { Type = "text", Text = text };

    /// <summary>
    /// Creates an inline base64 image content block.
    /// </summary>
    public static InteractionContent Image(string base64Data, string mimeType) => new() { Type = "image", Data = base64Data, MimeType = mimeType };

    /// <summary>
    /// Creates an image content block from a URI.
    /// </summary>
    public static InteractionContent ImageFromUri(string uri, string mimeType) => new() { Type = "image", Uri = uri, MimeType = mimeType };

    /// <summary>
    /// Creates a document content block from a URI.
    /// </summary>
    public static InteractionContent Document(string uri, string mimeType) => new() { Type = "document", Uri = uri, MimeType = mimeType };
}

/// <summary>
/// Built-in or external tool declaration for managed agent interactions.
/// </summary>
public class InteractionTool
{
    /// <summary>
    /// Tool type: <c>google_search</c>, <c>url_context</c>, <c>code_execution</c>, <c>mcp_server</c>, or <c>file_search</c>.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Display name for an MCP server.
    /// </summary>
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    /// <summary>
    /// MCP server endpoint URL.
    /// </summary>
    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url { get; set; }

    /// <summary>
    /// HTTP headers sent with MCP server requests.
    /// </summary>
    [JsonProperty("headers", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Restricts which MCP tools the agent may call.
    /// </summary>
    [JsonProperty("allowed_tools", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? AllowedTools { get; set; }

    /// <summary>
    /// File Search store names (e.g. <c>fileSearchStores/my-store</c>).
    /// </summary>
    [JsonProperty("file_search_store_names", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? FileSearchStoreNames { get; set; }

    /// <summary>
    /// Creates a tool reference by type name.
    /// </summary>
    public static InteractionTool OfType(string type) => new() { Type = type };

    /// <summary>Google Search tool.</summary>
    public static InteractionTool GoogleSearch() => OfType("google_search");

    /// <summary>URL context tool.</summary>
    public static InteractionTool UrlContext() => OfType("url_context");

    /// <summary>Code execution tool.</summary>
    public static InteractionTool CodeExecution() => OfType("code_execution");

    /// <summary>
    /// MCP server tool.
    /// </summary>
    public static InteractionTool McpServer(string url, string? name = null, Dictionary<string, string>? headers = null, List<string>? allowedTools = null) =>
        new()
        {
            Type = "mcp_server",
            Name = name,
            Url = url,
            Headers = headers,
            AllowedTools = allowedTools
        };

    /// <summary>
    /// File Search tool over one or more stores.
    /// </summary>
    public static InteractionTool FileSearch(params string[] storeNames) =>
        new()
        {
            Type = "file_search",
            FileSearchStoreNames = storeNames.Select(EnsureFileSearchStorePrefix).ToList()
        };

    private static string EnsureFileSearchStorePrefix(string storeName)
    {
        if (string.IsNullOrEmpty(storeName))
        {
            return storeName;
        }

        const string prefix = "fileSearchStores/";
        return storeName.StartsWith(prefix) ? storeName : prefix + storeName;
    }
}

/// <summary>
/// Polymorphic interaction input: plain string, content blocks, or prior steps.
/// </summary>
[JsonConverter(typeof(InteractionInputConverter))]
public class InteractionInput
{
    /// <summary>Plain text prompt.</summary>
    public string? Text { get; set; }

    /// <summary>Multimodal content blocks.</summary>
    public List<InteractionContent>? Contents { get; set; }

    /// <summary>Prior conversation steps for multi-turn model interactions.</summary>
    public List<InteractionStep>? Steps { get; set; }

    /// <summary>Wraps a string prompt.</summary>
    public static InteractionInput FromText(string text) => new() { Text = text };

    /// <summary>Wraps content blocks.</summary>
    public static InteractionInput FromContents(IReadOnlyList<InteractionContent> contents) => new() { Contents = contents is List<InteractionContent> list ? list : [..contents] };

    /// <summary>Wraps content blocks.</summary>
    public static InteractionInput FromContents(params InteractionContent[] contents) => FromContents((IReadOnlyList<InteractionContent>)contents);

    /// <summary>Wraps prior steps.</summary>
    public static InteractionInput FromSteps(IReadOnlyList<InteractionStep> steps) => new() { Steps = steps is List<InteractionStep> list ? list : [..steps] };

    /// <summary>Wraps prior steps.</summary>
    public static InteractionInput FromSteps(params InteractionStep[] steps) => FromSteps((IReadOnlyList<InteractionStep>)steps);
}

internal sealed class InteractionInputConverter : JsonConverter<InteractionInput>
{
    public override InteractionInput? ReadJson(JsonReader reader, Type objectType, InteractionInput? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.String)
        {
            return InteractionInput.FromText(reader.Value?.ToString() ?? string.Empty);
        }

        if (reader.TokenType is JsonToken.StartArray)
        {
            JArray arr = JArray.Load(reader);
            if (arr.Count > 0 && arr[0]?["type"]?.ToString() is "user_input" or "model_output")
            {
                List<InteractionStep> steps = arr.ToObject<List<InteractionStep>>(serializer) ?? [];
                return InteractionInput.FromSteps(steps);
            }

            List<InteractionContent> contents = arr.ToObject<List<InteractionContent>>(serializer) ?? [];
            return InteractionInput.FromContents(contents);
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, InteractionInput? value, JsonSerializer serializer)
    {
        if (value?.Text is not null)
        {
            writer.WriteValue(value.Text);
            return;
        }

        if (value?.Steps is not null)
        {
            serializer.Serialize(writer, value.Steps);
            return;
        }

        serializer.Serialize(writer, value?.Contents ?? []);
    }
}
