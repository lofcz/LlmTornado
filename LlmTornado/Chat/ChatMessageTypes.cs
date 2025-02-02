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
    /// Message part is URI-based file. Supported only by Google.
    /// </summary>
    FileLink
}