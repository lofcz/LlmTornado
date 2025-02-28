using System;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
///     Represents available message types.
/// </summary>
public enum ChatMessageTypes
{
    /// <summary>
    /// Message part is a text fragment.
    /// </summary>
    Text,
    
    /// <summary>
    /// Message part is either base64 encoded image or a publicly available URL pointing to an image.
    /// </summary>
    Image,
    
    /// <summary>
    /// Message part is an audio fragment.
    /// </summary>
    Audio,
    
    /// <summary>
    /// Message part is URI-based file.
    /// <b>Supported only by Google.</b>
    /// </summary>
    FileLink,
    
    /// <summary>
    /// Message part is a reasoning block.
    /// <b>Supported only by Anthropic.</b>
    /// </summary>
    Reasoning,
    
    /// <summary>
    /// Message part is either base64 encoded PDF or a publicly available URL pointing to a PDF (unencrypted, no passwords).
    /// <b>Supported only by Anthropic.</b>
    /// </summary>
    Document
}