using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
/// Used to select the safety instruction inserted into the prompt. Defaults to CONTEXTUAL. When NONE is specified, the safety instruction will be omitted.
/// This parameter is only compatible with models Command R 08-2024, Command R+ 08-2024 and newer.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatVendorCohereExtensionSafetyMode
{
    /// <summary>
    /// Default, allows some "harmful" content, such as sexually explicit, legal advice, medical.
    /// </summary>
    [EnumMember(Value = "CONTEXTUAL")] 
    Contextual,
    /// <summary>
    /// Disallows any explicit content.
    /// </summary>
    [EnumMember(Value = "STRICT")] 
    Strict,
    /// <summary>
    /// Cohere claims to disable prompt poisoning with safety instructions with this mode selected.
    /// </summary>
    [EnumMember(Value = "NONE")] 
    None
}