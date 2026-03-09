using System.Collections.Generic;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Vendor.Anthropic;

/// <summary>
/// Tool converted to Anthropic schema.
/// </summary>
public class VendorAnthropicToolFunction : IVendorAnthropicChatRequestTool
{
    /// <summary>
    ///     Converts a generic tool function into Anthropic schema.
    /// </summary>
    /// <param name="tool">tool to be used as a source</param>
    public VendorAnthropicToolFunction(Tool tool)
    {
        ToolFunction? func = tool.Function;
        
        Parameters = func?.Parameters;
        Name = func?.Name ?? string.Empty;
        Description = func?.Description;
        Cache = tool.VendorExtensions?.Anthropic?.Cache;
        Strict = tool.Strict;
        AllowedCallers = tool.AllowedCallers;
        DeferLoading = tool.DeferLoading;
        EagerInputStreaming = tool.EagerInputStreaming;
    }

    /// <summary>
    ///     Converts a built-in tool into Anthropic schema.
    /// </summary>
    /// <param name="builtInTool"></param>
    public VendorAnthropicToolFunction(IVendorAnthropicChatRequestBuiltInTool builtInTool)
    {
        Name = builtInTool.Name;
        Type = builtInTool.Type.ToEnumMember();
        Cache = builtInTool.Cache;
        
        switch (builtInTool)
        {
            case VendorAnthropicChatRequestBuiltInToolBash20250124:
            case VendorAnthropicChatRequestBuiltInToolCodeExecution20250522:
            case VendorAnthropicChatRequestBuiltInToolCodeExecution20250825:
            case VendorAnthropicChatRequestBuiltInToolMemory20250825:
                // nothing specific
                break;
            case VendorAnthropicChatRequestBuiltInToolComputer20250124 computer:
                DisplayHeightPx = computer.DisplayHeightPx;
                DisplayWidthPx = computer.DisplayWidthPx;
                DisplayNumber = computer.DisplayNumber;
                break;
            case VendorAnthropicChatRequestBuiltInToolTextEditor20250728 textEditor:
                MaxCharacters = textEditor.MaxCharacters;
                break;
            case VendorAnthropicChatRequestBuiltInToolWebSearch20250305 webSearch:
                MaxUses = webSearch.MaxUses;
                AllowedDomains = webSearch.AllowedDomains;
                BlockedDomains = webSearch.BlockedDomains;
                UserLocation = webSearch.UserLocation;
                break;
            case VendorAnthropicChatRequestBuiltInToolWebSearch20260209 webSearch2:
                MaxUses = webSearch2.MaxUses;
                AllowedDomains = webSearch2.AllowedDomains;
                BlockedDomains = webSearch2.BlockedDomains;
                UserLocation = webSearch2.UserLocation;
                break;
            case VendorAnthropicChatRequestBuiltInToolWebFetch20250910 webFetch:
                MaxUses = webFetch.MaxUses;
                AllowedDomains = webFetch.AllowedDomains;
                BlockedDomains = webFetch.BlockedDomains;
                MaxContentTokens = webFetch.MaxContentTokens;
                if (webFetch.CitationsEnabled.HasValue)
                    Citations = new VendorAnthropicWebFetchCitations { Enabled = webFetch.CitationsEnabled.Value };
                break;
            case VendorAnthropicChatRequestBuiltInToolWebFetch20260209 webFetch2:
                MaxUses = webFetch2.MaxUses;
                AllowedDomains = webFetch2.AllowedDomains;
                BlockedDomains = webFetch2.BlockedDomains;
                MaxContentTokens = webFetch2.MaxContentTokens;
                if (webFetch2.CitationsEnabled.HasValue)
                    Citations = new VendorAnthropicWebFetchCitations { Enabled = webFetch2.CitationsEnabled.Value };
                break;
        }
    }
    
    /// <summary>
    ///     The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum
    ///     length of 64.
    ///     Special names: computer, bash, str_replace_editor, web_search
    /// </summary>
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; }

    /// <summary>
    ///     The description of what the function does.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    ///     The input parameters of the tool, if any.
    /// </summary>
    [JsonProperty("input_schema")]
    public object? Parameters { get; set; }
    
    /// <summary>
    ///     Enable strict mode for guaranteed schema validation on tool inputs.
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }

    /// <summary>
    ///     Cache indicator for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
    
    /// <summary>
    /// custom | web_search_20250305 | text_editor_20250728 | text_editor_20250124 | bash_20250124 | computer_20250124 | text_editor_20241022 | memory_20250818
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }
    
    /// <summary>
    /// If provided, only these domains will be included in results. Cannot be used alongside blocked_domains.
    /// </summary>
    [JsonProperty("allowed_domains")]
    public List<string>? AllowedDomains { get; set; }
    
    /// <summary>
    /// If provided, these domains will never appear in results. Cannot be used alongside allowed_domains.
    /// </summary>
    [JsonProperty("blocked_domains")]
    public List<string>? BlockedDomains { get; set; }
    
    /// <summary>
    /// Maximum number of times the tool can be used in the API request.
    /// x > 0
    /// </summary>
    [JsonProperty("max_uses")]
    public int? MaxUses { get; set; }
    
    /// <summary>
    /// Parameters for the user's location. Used to provide more relevant search results.
    /// </summary>
    [JsonProperty("user_location")]
    public VendorAnthropicToolFunctionUserLocation? UserLocation { get; set; }

    /// <summary>
    /// Citation settings for web fetch tools. When enabled, Claude cites specific passages from fetched documents.
    /// </summary>
    [JsonProperty("citations")]
    public VendorAnthropicWebFetchCitations? Citations { get; set; }

    /// <summary>
    /// Maximum number of content tokens to include from fetched pages. Content is truncated when the limit is exceeded.
    /// </summary>
    [JsonProperty("max_content_tokens")]
    public int? MaxContentTokens { get; set; }
    
    /// <summary>
    /// The height of the display in pixels. Required range: x > 1
    /// </summary>
    [JsonProperty("display_height_px")]
    public int? DisplayHeightPx { get; set; }
    
    /// <summary>
    /// The width of the display in pixels. Required range: x > 1
    /// </summary>
    [JsonProperty("display_width_px")]
    public int? DisplayWidthPx { get; set; }
    
    /// <summary>
    /// The X11 display number (e.g. 0, 1) for the display.
    /// </summary>
    [JsonProperty("display_number")]
    public int? DisplayNumber { get; set; }
    
    /// <summary>
    /// Optional parameter that allows you to control the truncation length when viewing large files.
    /// </summary>
    [JsonProperty("max_characters")]
    public int? MaxCharacters { get; set; }
    
    /// <summary>
    /// Specifies which contexts can invoke this tool for programmatic tool calling.
    /// </summary>
    [JsonProperty("allowed_callers", ItemConverterType = typeof(StringEnumConverter))]
    public List<ToolAllowedCallers>? AllowedCallers { get; set; }
    
    /// <summary>
    /// When true, this tool is deferred and only loaded when discovered via tool search.
    /// </summary>
    [JsonProperty("defer_loading")]
    public bool? DeferLoading { get; set; }
    
    /// <summary>
    /// When true, enables fine-grained tool streaming for this tool. Tool use parameter values
    /// stream without buffering or JSON validation, reducing latency for large parameters.
    /// </summary>
    [JsonProperty("eager_input_streaming")]
    public bool? EagerInputStreaming { get; set; }
}

/// <summary>
/// User location for web search.
/// </summary>
public class VendorAnthropicToolFunctionUserLocation
{
    /// <summary>
    /// Available options: approximate 
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "approximate";
    
    /// <summary>
    /// The city of the user. Required string length: 1 - 255
    /// </summary>
    [JsonProperty("city")]
    public string? City { get; set; }
    
    /// <summary>
    /// The two letter ISO country code of the user.
    /// </summary>
    [JsonProperty("country")]
    public string? Country { get; set; }
    
    /// <summary>
    /// The region of the user. Required string length: 1 - 255
    /// </summary>
    [JsonProperty("region")]
    public string? Region { get; set; }
    
    /// <summary>
    /// The IANA timezone of the user. Required string length: 1 - 255
    /// </summary>
    [JsonProperty("timezone")]
    public string? Timezone { get; set; }
}

/// <summary>
/// Citation settings for web fetch tools.
/// </summary>
public class VendorAnthropicWebFetchCitations
{
    /// <summary>
    /// When true, Claude cites specific passages from fetched documents in its responses.
    /// </summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; set; }
}