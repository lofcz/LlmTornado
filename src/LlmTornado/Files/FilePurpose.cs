using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Files;

/// <summary>
///     Represents the file purpose, either the file is for fine-tuning and needs to be in JSONL format or for messages &
///     assistants.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum FilePurpose
{
    /// <summary>
    ///     Expects JSONL content
    /// </summary>
    [EnumMember(Value = "finetune")]
    Finetune,

    /// <summary>
    ///     Supported content: https://platform.openai.com/docs/assistants/tools/supported-files
    /// </summary>
    [EnumMember(Value = "assistants")]
    Assistants,

    /// <summary>
    ///     Agent purpose for ZAI file uploads.
    /// </summary>
    [EnumMember(Value = "agent")]
    Agent
}