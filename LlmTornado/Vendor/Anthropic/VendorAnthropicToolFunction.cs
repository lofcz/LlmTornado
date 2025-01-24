using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Vendor.Anthropic;

/// <summary>
/// 
/// </summary>
public class VendorAnthropicToolFunction : IAnthropicChatRequestItem
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
    ///     The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum
    ///     length of 64.
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
    public JObject? Parameters { get; set; }

    /// <summary>
    ///     Cache indicator for the tool.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
}