using System.Collections.Generic;
using System.Linq;
using System.Text;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;

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
    /// The blocks, which together constitute the received response. A block can be either textual, tool call or an image.
    /// Different providers support different block types.
    /// </summary>
    public List<ChatRichResponseBlock> Blocks { get; set; }

    /// <summary>
    /// Extension information if the vendor used returns any.
    /// </summary>
    public ChatResponseVendorExtensions? VendorExtensions => Result?.VendorExtensions;

    /// <summary>
    /// The usage statistics for the interaction.
    /// </summary>
    public ChatUsage? Usage => Result?.Usage;
    
    /// <summary>
    /// Raw response from the API.
    /// </summary>
    public string? RawResponse => Result?.RawResponse;

    /// <summary>
    /// Reason why the response ended.    
    /// </summary>
    public ChatMessageFinishReasons FinishReason => Result?.Choices?.FirstOrDefault()?.FinishReason ?? ChatMessageFinishReasons.Unknown;
    
    /// <summary>
    /// The full result.
    /// </summary>
    public ChatResult? Result { get; set; }

    /// <summary>
    /// The full request.
    /// </summary>
    public HttpCallRequest? Request { get; set; }
    
    /// <summary>
    /// Constructs rich response.
    /// </summary>
    public ChatRichResponse(ChatResult? result, List<ChatRichResponseBlock>? blocks)
    {
        Result = result;
        Blocks = blocks ?? [];
    }

    /// <summary>
    /// Text of the response.
    /// </summary>
    public string Text => text ?? GetText();
    
    /// <summary>
    /// Gets the text parts and joins them by a separator.
    /// </summary>
    public string GetText(string blockSeparator = " ")
    {
        text = Blocks is null ? string.Empty : string.Join(blockSeparator, Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Message && !x.Message.IsNullOrWhiteSpace()).Select(x => x.Message));
        return text;
    }

    /// <summary>
    /// Iterates over blocks, aggregating the response for debugging view.
    /// </summary>
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

            switch (x.Type)
            {
                case ChatRichResponseBlockTypes.Function:
                {
                    sb.AppendLine($"name: {x.FunctionCall?.Name}");
                    sb.AppendLine($"arguments:");
                    sb.AppendLine(x.FunctionCall?.GetArguments().ToJson(true));
                    break;
                }
                case ChatRichResponseBlockTypes.Reasoning or ChatRichResponseBlockTypes.Message:
                {
                    sb.AppendLine(x.Type is ChatRichResponseBlockTypes.Reasoning ? x.Reasoning?.Content : x.Message);
                    break;
                }
                case ChatRichResponseBlockTypes.Audio:
                {
                    if (x.ChatAudio is not null && !x.ChatAudio.Transcript.IsNullOrWhiteSpace())
                    {
                        sb.AppendLine($"transcript:");
                        sb.AppendLine(x.ChatAudio.Transcript);
                    }
                    
                    sb.AppendLine($"data: {x.ChatAudio?.Data}");
                    sb.AppendLine($"mime: {x.ChatAudio?.MimeType}");
                    break;
                }
                case ChatRichResponseBlockTypes.Image:
                {
                    sb.AppendLine($"url/data: {x.ChatImage?.Url}");
                    sb.AppendLine($"mime: {x.ChatImage?.MimeType}");
                    break;
                }
                default:
                {
                    break;
                }
            }
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
    /// Citations associated with the block, if any.
    /// </summary>
    [JsonIgnore]
    public List<IChatMessagePartCitation>? Citations => Part?.Citations;

    /// <summary>
    /// Vendor extensions associated with the block, if any.
    /// </summary>
    [JsonIgnore]
    public IChatMessagePartVendorExtensions? VendorExtensions => Part?.VendorExtensions;
    
    /// <summary>
    /// Search result of the message part if type is SearchResult.
    /// </summary>
    public ChatSearchResult? SearchResults => Part?.SearchResult;
    
    /// <summary>
    /// The part this block is associated with.
    /// </summary>
    [JsonIgnore]
    public ChatMessagePart? Part { get; set; }
    
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

        Part = part;
        
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
            case ChatMessageTypes.Audio:
            {
                if (part.Audio is not null)
                {
                    ChatAudio = new ChatMessageAudio
                    {
                        Data = part.Audio.Data,
                        MimeType = part.Audio.MimeType,
                        Format = part.Audio.Format
                    };   
                }
                
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