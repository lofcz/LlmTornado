using System.Collections.Generic;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;

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
            case VendorAnthropicChatRequestBuiltInToolBash20250124 bash:
            case VendorAnthropicChatRequestBuiltInToolCodeExecution20250522 code:
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