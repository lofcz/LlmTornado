using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    Function,
    
    /// <summary>
    /// Image block.
    /// </summary>
    Image,
    
    /// <summary>
    /// Audio block.
    /// </summary>
    Audio,
    
    /// <summary>
    /// Reasoning block.
    /// </summary>
    Reasoning
}

/// <summary>
///     The response is represented by one or more blocks.
/// </summary>
public class ChatRichResponse
{
    private string? text;
    
    /// <summary>
    ///     The blocks, which together constitute the received response. A block can be either textual, tool call or an image.
    ///     Different providers support different block types.
    /// </summary>
    public List<ChatRichResponseBlock>? Blocks { get; set; }

    /// <summary>
    ///     Extension information if the vendor used returns any.
    /// </summary>
    public ChatResponseVendorExtensions? VendorExtensions => Result?.VendorExtensions;

    /// <summary>
    /// The usage statistics for the interaction.
    /// </summary>
    public ChatUsage? Usage => Result?.Usage;
    
    /// <summary>
    ///     Raw response from the API.
    /// </summary>
    public string? RawResponse => Result?.RawResponse;
    
    /// <summary>
    ///     The full result.
    /// </summary>
    public ChatResult? Result { get; set; }

    /// <summary>
    ///     Constructs rich response.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="blocks"></param>
    public ChatRichResponse(ChatResult? result, List<ChatRichResponseBlock>? blocks)
    {
        Result = result;
        Blocks = blocks;
    }

    /// <summary>
    /// Text of the response.
    /// </summary>
    public string Text => text ?? GetText();
    
    /// <summary>
    /// Gets the text parts and joins them by a separator.
    /// </summary>
    /// <returns></returns>
    public string GetText(string blockSeparator = " ")
    {
        text = Blocks is null ? string.Empty : string.Join(blockSeparator, Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Message && !x.Message.IsNullOrWhiteSpace()).Select(x => x.Message));
        return text;
    }

    /// <summary>
    /// Iterates over blocks, aggregating the response for debugging view.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (Blocks is null || Blocks.Count is 0)
        {
            return "[tornado:empty]";
        }

        StringBuilder sb = new StringBuilder();

        foreach (ChatRichResponseBlock x in Blocks)
        {
            sb.AppendLine($"[block: {x.Type.ToString()}]");
            sb.AppendLine(x.Type is ChatRichResponseBlockTypes.Reasoning ? x.Reasoning?.Content : x.Message);
        }

        return sb.ToString();
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
    /// If the <see cref="Type"/> is <see cref="ChatRichResponseBlockTypes.Image"/>, this is the image.
    /// </summary>
    public ChatImage? ChatImage { get; set; }
    
    /// <summary>
    /// If the <see cref="Type"/> is <see cref="ChatRichResponseBlockTypes.Function"/>, this is the function the tool requested calling.
    /// </summary>
    public FunctionCall? FunctionCall { get; set; }
    
    /// <summary>
    /// If the <see cref="Type"/> is <see cref="ChatRichResponseBlockTypes.Audio"/>, this is the audio.
    /// </summary>
    public ChatMessageAudio? ChatAudio { get; set; }
    
    /// <summary>
    /// If the <see cref="Type"/> is <see cref="ChatRichResponseBlockTypes.Reasoning"/>, this is the reasoning data.
    /// </summary>
    public ChatMessageReasoningData? Reasoning { get; set; }

    /// <summary>
    ///     Creates an empty block.
    /// </summary>
    public ChatRichResponseBlock()
    {
        
    }
    
    /// <summary>
    ///     Creates a block from a message part and a message.
    /// </summary>
    /// <param name="part"></param>
    /// <param name="msg"></param>
    public ChatRichResponseBlock(ChatMessagePart part, ChatMessage msg)
    {
        Type = part.Type switch
        {
            ChatMessageTypes.Text => ChatRichResponseBlockTypes.Message,
            ChatMessageTypes.Image => ChatRichResponseBlockTypes.Image,
            ChatMessageTypes.Audio => ChatRichResponseBlockTypes.Audio,
            ChatMessageTypes.Reasoning => ChatRichResponseBlockTypes.Reasoning,
            _ => ChatRichResponseBlockTypes.Unknown
        };

        switch (part.Type)
        {
            case ChatMessageTypes.Text:
            {
                Message = part.Text;
                break;
            }
            case ChatMessageTypes.Image:
            {
                ChatImage = part.Image;
                break;
            }
            case ChatMessageTypes.Reasoning:
            {
                Reasoning = part.Reasoning;
                break;
            }
        }
        

        if (msg.Audio is not null)
        {
            Type = ChatRichResponseBlockTypes.Audio;
            ChatAudio = msg.Audio;
        }
    }
}