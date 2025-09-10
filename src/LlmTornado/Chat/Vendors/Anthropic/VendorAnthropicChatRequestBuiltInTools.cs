using System.Collections.Generic;
using System.Runtime.Serialization;
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
    CodeExecution20250825
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