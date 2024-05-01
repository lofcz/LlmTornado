using System.Collections.Generic;
using System.Linq;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Chat;

/// <summary>
/// Response blocks are of various types but generally a block represents a feature such as text, image, function call, etc.
/// </summary>
public enum ChatRichResponseBlockTypes
{
    /// <summary>
    /// Unknown or unsupported block.
    /// </summary>
    Unknown,
    /// <summary>
    /// Text block.
    /// </summary>
    Message,
    /// <summary>
    /// Function call, optionally paired with the response if the function already resolved.
    /// </summary>
    Function
}

/// <summary>
/// The response is represented by one or more blocks.
/// </summary>
public class ChatRichResponse
{
    /// <summary>
    /// The blocks which together constitute the received response.
    /// </summary>
    public List<ChatRichResponseBlock> Blocks { get; set; } = [];

    /// <summary>
    /// Gets the text parts and joins them by a separator.
    /// </summary>
    /// <returns></returns>
    public string GetText(string blockSeparator = " ")
    {
        return string.Join(blockSeparator, Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Message && !x.Message.IsNullOrWhiteSpace()).Select(x => x.Message));
    }
}

/// <summary>
/// A single block of the LLM's response.
/// </summary>
public class ChatRichResponseBlock
{
    /// <summary>
    /// Kind of the block.
    /// </summary>
    public ChatRichResponseBlockTypes Type { get; set; }
    /// <summary>
    /// If the <see cref="Type"/> is <see cref="ChatRichResponseBlockTypes.Message"/>, this is the text content.
    /// </summary>
    public string? Message { get; set; }
    /// <summary>
    /// If the <see cref="Type"/> is <see cref="ChatRichResponseBlockTypes.Function"/> and the function already resolved via a handler, this the resolved value.
    /// </summary>
    public FunctionResult? FunctionResult { get; set; }
    /// <summary>
    /// If the <see cref="Type"/> is <see cref="ChatRichResponseBlockTypes.Function"/>, this is the function the tool requested calling.
    /// </summary>
    public FunctionCall? FunctionCall { get; set; }
}