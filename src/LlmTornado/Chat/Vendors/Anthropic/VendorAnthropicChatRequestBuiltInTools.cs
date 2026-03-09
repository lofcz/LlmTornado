using System.Collections.Generic;
using System.Runtime.Serialization;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Anthropic;

/// <summary>
/// A tool the model may call.
/// </summary>
public interface IVendorAnthropicChatRequestBuiltInTool : IVendorAnthropicChatRequestTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public VendorAnthropicChatRequestBuiltInToolTypes Type { get; }
    
    /// <summary>
    /// Name of the tool.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Cache settings.
    /// </summary>
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// Known built in tool types.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum VendorAnthropicChatRequestBuiltInToolTypes
{
    /// <summary>
    /// Bash tool.
    /// </summary>
    [EnumMember(Value = "bash_20250124")]
    Bash20250124,
    
    /// <summary>
    /// Code execution tool.
    /// </summary>
    [EnumMember(Value = "code_execution_20250522")]
    CodeExecution20250522,
    
    /// <summary>
    /// Computer tool.
    /// </summary>
    [EnumMember(Value = "computer_20250124")]
    Computer20250124,
    
    /// <summary>
    /// Text editor tool.
    /// </summary>
    [EnumMember(Value = "text_editor_20250728")]
    TextEditor20250728,
    
    /// <summary>
    /// Text editor tool (older version).
    /// </summary>
    [EnumMember(Value = "text_editor_20250429")]
    TextEditor20250429,
    
    /// <summary>
    /// Code execution tool.
    /// </summary>
    [EnumMember(Value = "code_execution_20250825")]
    CodeExecution20250825,
    
    /// <summary>
    /// Memory tool.
    /// </summary>
    [EnumMember(Value = "memory_20250818")]
    Memory20250818,
    
    /// <summary>
    /// Tool search tool using regex patterns.
    /// </summary>
    [EnumMember(Value = "tool_search_tool_regex_20251119")]
    ToolSearchRegex20251119,
    
    /// <summary>
    /// Tool search tool using BM25 natural language queries.
    /// </summary>
    [EnumMember(Value = "tool_search_tool_bm25_20251119")]
    ToolSearchBm2520251119,
    
    /// <summary>
    /// Web search tool (standard). Gives Claude access to real-time web content with automatic citations.
    /// </summary>
    [EnumMember(Value = "web_search_20250305")]
    WebSearch20250305,
    
    /// <summary>
    /// Web search tool with dynamic filtering (Claude Opus 4.6 and Sonnet 4.6).
    /// Claude can write and execute code to filter search results before they reach the context window.
    /// Requires the <c>code-execution-web-tools-2026-02-09</c> beta header (added automatically).
    /// </summary>
    [EnumMember(Value = "web_search_20260209")]
    WebSearch20260209,
    
    /// <summary>
    /// Web fetch tool (standard). Allows Claude to retrieve full content from specified URLs and PDFs.
    /// </summary>
    [EnumMember(Value = "web_fetch_20250910")]
    WebFetch20250910,
    
    /// <summary>
    /// Web fetch tool with dynamic filtering (Claude Opus 4.6 and Sonnet 4.6).
    /// Claude can write and execute code to filter fetched content before it reaches the context window.
    /// Requires the <c>code-execution-web-tools-2026-02-09</c> beta header (added automatically).
    /// </summary>
    [EnumMember(Value = "web_fetch_20260209")]
    WebFetch20260209,
}

/// <summary>
/// A built-in bash tool.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolBash20250124 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    [JsonProperty("type")]
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.Bash20250124;

    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "bash";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// A built-in code execution tool.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolMemory20250825 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.Memory20250818;
    
    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "memory";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// A built-in code execution tool.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolCodeExecution20250825 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.CodeExecution20250825;
    
    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "code_execution";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// A built-in code execution tool.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolCodeExecution20250522 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.CodeExecution20250522;
    
    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "code_execution";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// A built-in computer tool.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolComputer20250124 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.Computer20250124;
    
    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "computer";
    
    /// <summary>
    /// The height of the display in pixels.
    /// </summary>
    public int DisplayHeightPx { get; set; }
    
    /// <summary>
    /// The width of the display in pixels.
    /// </summary>
    public int DisplayWidthPx { get; set; }
    
    /// <summary>
    /// The X11 display number (e.g. 0, 1) for the display.
    /// </summary>
    public int? DisplayNumber { get; set; }

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// A built-in text editor tool. Only supported by Claude 4+.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolTextEditor20250728 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    [JsonProperty("name")]
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.TextEditor20250728;
    
    /// <summary>
    /// The name of the tool.
    /// </summary>
    public string Name => "str_replace_based_edit_tool";
    
    /// <summary>
    /// Optional parameter that allows you to control the truncation length when viewing large files.
    /// </summary>
    public int? MaxCharacters { get; set; }
    
    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// Tool search tool using regex patterns. Claude constructs regex patterns to search for tools.
/// Requires beta header "advanced-tool-use-2025-11-20".
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolSearchRegex20251119 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.ToolSearchRegex20251119;
    
    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "tool_search_tool_regex";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// Tool search tool using BM25 natural language queries. Claude uses natural language to search for tools.
/// Requires beta header "advanced-tool-use-2025-11-20".
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolSearchBm2520251119 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.ToolSearchBm2520251119;
    
    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "tool_search_tool_bm25";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}

/// <summary>
/// Standard web search tool. Gives Claude access to real-time web content with automatic citations.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolWebSearch20250305 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.WebSearch20250305;

    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "web_search";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }

    /// <summary>
    /// Maximum number of searches Claude may perform per request. Must be greater than 0.
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// If set, only results from these domains are included. Cannot be used with <see cref="BlockedDomains"/>.
    /// </summary>
    public List<string>? AllowedDomains { get; set; }

    /// <summary>
    /// If set, results from these domains are excluded. Cannot be used with <see cref="AllowedDomains"/>.
    /// </summary>
    public List<string>? BlockedDomains { get; set; }

    /// <summary>
    /// Optional user location used to localise search results.
    /// </summary>
    public VendorAnthropicToolFunctionUserLocation? UserLocation { get; set; }
}

/// <summary>
/// Web search tool with dynamic filtering. Available on Claude Opus 4.6 and Sonnet 4.6.
/// Claude writes and executes code to filter search results before they reach the context window,
/// improving accuracy while reducing token consumption.
/// The <c>code-execution-web-tools-2026-02-09</c> beta header is added automatically.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolWebSearch20260209 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.WebSearch20260209;

    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "web_search";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }

    /// <summary>
    /// Maximum number of searches Claude may perform per request. Must be greater than 0.
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// If set, only results from these domains are included. Cannot be used with <see cref="BlockedDomains"/>.
    /// </summary>
    public List<string>? AllowedDomains { get; set; }

    /// <summary>
    /// If set, results from these domains are excluded. Cannot be used with <see cref="AllowedDomains"/>.
    /// </summary>
    public List<string>? BlockedDomains { get; set; }

    /// <summary>
    /// Optional user location used to localise search results.
    /// </summary>
    public VendorAnthropicToolFunctionUserLocation? UserLocation { get; set; }
}

/// <summary>
/// Standard web fetch tool. Allows Claude to retrieve full content from specified URLs and PDFs.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolWebFetch20250910 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.WebFetch20250910;

    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "web_fetch";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }

    /// <summary>
    /// Maximum number of fetches Claude may perform per request. Must be greater than 0.
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// If set, only these domains may be fetched. Cannot be used with <see cref="BlockedDomains"/>.
    /// </summary>
    public List<string>? AllowedDomains { get; set; }

    /// <summary>
    /// If set, these domains will never be fetched. Cannot be used with <see cref="AllowedDomains"/>.
    /// </summary>
    public List<string>? BlockedDomains { get; set; }

    /// <summary>
    /// When enabled, Claude cites specific passages from fetched documents.
    /// </summary>
    public bool? CitationsEnabled { get; set; }

    /// <summary>
    /// Maximum number of content tokens to include from fetched pages. Content is truncated when exceeded.
    /// </summary>
    public int? MaxContentTokens { get; set; }
}

/// <summary>
/// Web fetch tool with dynamic filtering. Available on Claude Opus 4.6 and Sonnet 4.6.
/// Claude writes and executes code to filter fetched content before it reaches the context window,
/// reducing token consumption while maintaining response quality.
/// The <c>code-execution-web-tools-2026-02-09</c> beta header is added automatically.
/// </summary>
public class VendorAnthropicChatRequestBuiltInToolWebFetch20260209 : IVendorAnthropicChatRequestBuiltInTool
{
    /// <summary>
    /// The type of the tool.
    /// </summary>
    public VendorAnthropicChatRequestBuiltInToolTypes Type => VendorAnthropicChatRequestBuiltInToolTypes.WebFetch20260209;

    /// <summary>
    /// The name of the tool.
    /// </summary>
    [JsonProperty("name")]
    public string Name => "web_fetch";

    /// <summary>
    /// Cache control settings for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }

    /// <summary>
    /// Maximum number of fetches Claude may perform per request. Must be greater than 0.
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// If set, only these domains may be fetched. Cannot be used with <see cref="BlockedDomains"/>.
    /// </summary>
    public List<string>? AllowedDomains { get; set; }

    /// <summary>
    /// If set, these domains will never be fetched. Cannot be used with <see cref="AllowedDomains"/>.
    /// </summary>
    public List<string>? BlockedDomains { get; set; }

    /// <summary>
    /// When enabled, Claude cites specific passages from fetched documents.
    /// </summary>
    public bool? CitationsEnabled { get; set; }

    /// <summary>
    /// Maximum number of content tokens to include from fetched pages. Content is truncated when exceeded.
    /// </summary>
    public int? MaxContentTokens { get; set; }
}