using LlmTornado.Common;
using Argon;

namespace LlmTornado.Vendor.Anthropic;

/// <summary>
/// 
/// </summary>
internal class VendorAnthropicToolFunction
{
    /// <summary>
    ///     Converts a generic tool function into Anthropic schema.
    /// </summary>
    /// <param name="toolFunction">tool to be used as a source</param>
    public VendorAnthropicToolFunction(ToolFunction toolFunction)
    {
        Parameters = toolFunction.Parameters;
        Name = toolFunction.Name;
        Description = toolFunction.Description;
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
    public string Description { get; set; }

    /// <summary>
    ///     The input parameters of the tool, if any.
    /// </summary>
    [JsonProperty("input_schema")]
    public JObject? Parameters { get; set; }
}