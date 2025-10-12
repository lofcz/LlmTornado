using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Zai;

/// <summary>
/// Known ZAI tool types.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum VendorZaiToolType
{
    /// <summary>
    /// Function tool.
    /// </summary>
    [EnumMember(Value = "function")]
    Function,

    /// <summary>
    /// Web search tool.
    /// </summary>
    [EnumMember(Value = "web_search")]
    WebSearch,

    /// <summary>
    /// Retrieval tool.
    /// </summary>
    [EnumMember(Value = "retrieval")]
    Retrieval
}

/// <summary>
/// Interface for ZAI built-in tools.
/// </summary>
public interface IVendorZaiChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    VendorZaiToolType Type { get; }
    
    /// <summary>
    /// Name of the tool.
    /// </summary>
    string Name { get; }
}

/// <summary>
/// ZAI web search built-in tool.
/// </summary>
public class VendorZaiWebSearchTool : IVendorZaiChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool. Always "web_search".
    /// </summary>
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public VendorZaiToolType Type => VendorZaiToolType.WebSearch;

    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "web_search";

    /// <summary>
    /// Web search configuration.
    /// </summary>
    [JsonProperty("web_search")]
    public VendorZaiWebSearchObject WebSearch { get; set; } = new VendorZaiWebSearchObject();
}

/// <summary>
/// ZAI web search object configuration.
/// </summary>
public class VendorZaiWebSearchObject
{
    /// <summary>
    /// Whether to enable search functionality. Default is true.
    /// </summary>
    [JsonProperty("enable")]
    public bool Enable { get; set; } = true;

    /// <summary>
    /// Type of search engine. Default is SearchProJina.
    /// </summary>
    [JsonProperty("search_engine")]
    [JsonConverter(typeof(StringEnumConverter))]
    public VendorZaiSearchEngine? SearchEngine { get; set; }

    /// <summary>
    /// Force trigger a search query.
    /// </summary>
    [JsonProperty("search_query")]
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Number of returned results. Range: 1-50, max 50 results per search. Default is 10.
    /// </summary>
    [JsonProperty("count")]
    public int? Count { get; set; }

    /// <summary>
    /// Limits search results to specified whitelisted domains.
    /// </summary>
    [JsonProperty("search_domain_filter")]
    public string? SearchDomainFilter { get; set; }

    /// <summary>
    /// Limits search to a specific time range. Default is "noLimit".
    /// </summary>
    [JsonProperty("search_recency_filter")]
    [JsonConverter(typeof(StringEnumConverter))]
    public VendorZaiWebSearchRecencyFilter? SearchRecencyFilter { get; set; }

    /// <summary>
    /// Number of characters for webpage summaries. Default is "medium".
    /// </summary>
    [JsonProperty("content_size")]
    [JsonConverter(typeof(StringEnumConverter))]
    public VendorZaiWebSearchContentSize? ContentSize { get; set; }

    /// <summary>
    /// Specifies whether search results are shown before or after model response. Default is "after".
    /// </summary>
    [JsonProperty("result_sequence")]
    [JsonConverter(typeof(StringEnumConverter))]
    public VendorZaiWebSearchResultSequence? ResultSequence { get; set; }

    /// <summary>
    /// Whether to return search results in the response. Default is false.
    /// </summary>
    [JsonProperty("search_result")]
    public bool? SearchResult { get; set; }

    /// <summary>
    /// Whether to force model response based on search result. Default is false.
    /// </summary>
    [JsonProperty("require_search")]
    public bool? RequireSearch { get; set; }

    /// <summary>
    /// Prompt to customize how search results are processed.
    /// </summary>
    [JsonProperty("search_prompt")]
    public string? SearchPrompt { get; set; }
}

/// <summary>
/// Base class for ZAI tools.
/// </summary>
public abstract class VendorZaiTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    [JsonProperty("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Wrapper for ZAI web search tool in the tools array.
/// </summary>
public class VendorZaiWebSearchToolWrapper : VendorZaiTool
{
    /// <summary>
    /// The type of the tool. Always "web_search".
    /// </summary>
    [JsonProperty("type")]
    public override string Type => "web_search";

    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Web search configuration.
    /// </summary>
    [JsonProperty("web_search")]
    public VendorZaiWebSearchObject WebSearch { get; set; } = new();
}

/// <summary>
/// ZAI function tool schema.
/// </summary>
public class VendorZaiFunctionTool : VendorZaiTool
{
    /// <summary>
    /// The type of the tool. Always "function".
    /// </summary>
    [JsonProperty("type")]
    public override string Type => "function";

    /// <summary>
    /// Function configuration.
    /// </summary>
    [JsonProperty("function")]
    public VendorZaiFunctionObject Function { get; set; } = new();
}

/// <summary>
/// ZAI function object configuration.
/// </summary>
public class VendorZaiFunctionObject
{
    /// <summary>
    /// The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what the function does, used by the model to choose when and how to call the function.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Parameters defined using JSON Schema. Must pass a JSON Schema object to accurately define accepted parameters.
    /// </summary>
    [JsonProperty("parameters")]
    public object? Parameters { get; set; }
}

/// <summary>
/// ZAI retrieval tool schema.
/// </summary>
public class VendorZaiRetrievalTool : VendorZaiTool
{
    /// <summary>
    /// The type of the tool. Always "retrieval".
    /// </summary>
    [JsonProperty("type")]
    public override string Type => "retrieval";

    /// <summary>
    /// Retrieval configuration.
    /// </summary>
    [JsonProperty("retrieval")]
    public VendorZaiRetrievalObject Retrieval { get; set; } = new();
}

/// <summary>
/// ZAI retrieval object configuration.
/// </summary>
public class VendorZaiRetrievalObject
{
    /// <summary>
    /// Knowledge base ID, created or obtained from the platform.
    /// </summary>
    [JsonProperty("knowledge_id")]
    public string KnowledgeId { get; set; } = string.Empty;

    /// <summary>
    /// Prompt template for requesting the model, a custom request template containing placeholders {{ knowledge }} and {{ question }}.
    /// </summary>
    [JsonProperty("prompt_template")]
    public string? PromptTemplate { get; set; }
}

/// <summary>
/// Web search recency filter options.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum VendorZaiWebSearchRecencyFilter
{
    /// <summary>
    /// Within a day.
    /// </summary>
    [EnumMember(Value = "oneDay")]
    OneDay,

    /// <summary>
    /// Within a week.
    /// </summary>
    [EnumMember(Value = "oneWeek")]
    OneWeek,

    /// <summary>
    /// Within a month.
    /// </summary>
    [EnumMember(Value = "oneMonth")]
    OneMonth,

    /// <summary>
    /// Within a year.
    /// </summary>
    [EnumMember(Value = "oneYear")]
    OneYear,

    /// <summary>
    /// No limit (default).
    /// </summary>
    [EnumMember(Value = "noLimit")]
    NoLimit
}

/// <summary>
/// Web search content size options.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum VendorZaiWebSearchContentSize
{
    /// <summary>
    /// Balanced mode for most queries. 400-600 characters.
    /// </summary>
    [EnumMember(Value = "medium")]
    Medium,

    /// <summary>
    /// Maximizes context for comprehensive answers, 2500 characters.
    /// </summary>
    [EnumMember(Value = "high")]
    High
}

/// <summary>
/// Web search result sequence options.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum VendorZaiWebSearchResultSequence
{
    /// <summary>
    /// Show results before model response.
    /// </summary>
    [EnumMember(Value = "before")]
    Before,

    /// <summary>
    /// Show results after model response (default).
    /// </summary>
    [EnumMember(Value = "after")]
    After
}

/// <summary>
/// Web search engine options.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum VendorZaiSearchEngine
{
    /// <summary>
    /// Search Pro Jina engine (default).
    /// </summary>
    [EnumMember(Value = "search_pro_jina")]
    SearchProJina
}
