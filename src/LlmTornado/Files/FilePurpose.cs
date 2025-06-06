namespace LlmTornado.Files;

/// <summary>
///     Represents the file purpose, either the file is for fine-tuning and needs to be in JSONL format or for messages &
///     assistants.
/// </summary>
public enum FilePurpose
{
    /// <summary>
    ///     Expects JSONL content
    /// </summary>
    Finetune,

    /// <summary>
    ///     Supported content: https://platform.openai.com/docs/assistants/tools/supported-files
    /// </summary>
    Assistants
}