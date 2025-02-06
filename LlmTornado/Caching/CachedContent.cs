using System.Collections.Generic;
using LlmTornado.Chat;

namespace LlmTornado.Caching;

/// <summary>
/// The producer of the content.
/// </summary>
public enum CachedContentRoles
{
    /// <summary>
    /// User input.
    /// </summary>
    User,
    /// <summary>
    /// API output.
    /// </summary>
    Model
}

/// <summary>
/// The base structured datatype containing multi-part content of a message.
/// </summary>
public class CachedContent
{
    /// <summary>
    /// Ordered Parts that constitute a single message. Parts may have different MIME types.
    /// </summary>
    public List<ChatMessagePart> Parts { get; set; }
    
    /// <summary>
    /// The producer of the content. Must be either 'user' or 'model'. Useful to set for multi-turn conversations, otherwise can be left blank or unset.
    /// </summary>
    public CachedContentRoles? Role { get; set; }

    /// <summary>
    /// Creates cached content.
    /// </summary>
    /// <param name="parts"></param>
    /// <param name="role"></param>
    public CachedContent(List<ChatMessagePart> parts, CachedContentRoles role)
    {
        Parts = parts;
        Role = role;
    }
    
    /// <summary>
    /// Creates cached content. Use this only for system messages. For user input / assistants output, specifying <see cref="Role"/> is required.
    /// </summary>
    /// <param name="parts"></param>
    public CachedContent(List<ChatMessagePart> parts)
    {
        Parts = parts;
    }
}