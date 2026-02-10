using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Acp;

/// <summary>
/// Extension methods for converting between ACP and LlmTornado types.
/// </summary>
public static partial class AcpTornadoExtension
{
    /// <summary>
    /// Converts an ACP content block to a LlmTornado ChatMessage.
    /// </summary>
    public static ChatMessage ToTornadoMessage(this List<AcpContentBlock> contentBlocks)
    {
        List<ChatMessagePart> parts = [];

        foreach (AcpContentBlock block in contentBlocks)
        {
            ChatMessagePart? part = block.ToTornadoMessagePart();

            if (part is not null)
            {
                parts.Add(part);
            }
        }

        return new ChatMessage
        {
            Role = ChatMessageRoles.User,
            Parts = parts
        };
    }

    /// <summary>
    /// Converts a single ACP content block to a LlmTornado ChatMessagePart.
    /// </summary>
    public static ChatMessagePart? ToTornadoMessagePart(this AcpContentBlock block)
    {
        return block.Type switch
        {
            AcpContentBlockTypes.Text => new ChatMessagePart(block.Text ?? string.Empty),
            AcpContentBlockTypes.Image when block.Data is not null => new ChatMessagePart(ChatMessageTypes.Image),
            AcpContentBlockTypes.Audio when block.Data is not null => new ChatMessagePart(ChatMessageTypes.Audio),
            AcpContentBlockTypes.ResourceLink => new ChatMessagePart($"[Resource: {block.Name}]({block.Uri})"),
            AcpContentBlockTypes.Resource when block.Resource?.Text is not null => new ChatMessagePart(block.Resource.Text),
            _ => null
        };
    }

    /// <summary>
    /// Converts a LlmTornado ChatMessage to a list of ACP content blocks.
    /// </summary>
    public static List<AcpContentBlock> ToAcpContentBlocks(this ChatMessage chatMessage)
    {
        List<AcpContentBlock> blocks = [];

        if (chatMessage.Content is not null)
        {
            blocks.Add(new AcpContentBlock
            {
                Type = AcpContentBlockTypes.Text,
                Text = chatMessage.Content
            });
        }
        else if (chatMessage.Parts is not null)
        {
            foreach (ChatMessagePart part in chatMessage.Parts)
            {
                AcpContentBlock? block = part.ToAcpContentBlock();

                if (block is not null)
                {
                    blocks.Add(block);
                }
            }
        }

        return blocks;
    }

    /// <summary>
    /// Converts a LlmTornado ChatMessagePart to an ACP content block.
    /// </summary>
    public static AcpContentBlock? ToAcpContentBlock(this ChatMessagePart part)
    {
        return part.Type switch
        {
            ChatMessageTypes.Text => new AcpContentBlock
            {
                Type = AcpContentBlockTypes.Text,
                Text = part.Text
            },
            ChatMessageTypes.Image => new AcpContentBlock
            {
                Type = AcpContentBlockTypes.Image,
                Data = part.Image?.Url
            },
            ChatMessageTypes.Audio => new AcpContentBlock
            {
                Type = AcpContentBlockTypes.Audio
            },
            ChatMessageTypes.Reasoning => new AcpContentBlock
            {
                Type = AcpContentBlockTypes.Text,
                Text = part.Reasoning?.Content
            },
            _ => null
        };
    }

    /// <summary>
    /// Converts an ACP role string to a LlmTornado ChatMessageRoles value.
    /// </summary>
    public static ChatMessageRoles ToTornadoMessageRole(string? role)
    {
        return role switch
        {
            "user" => ChatMessageRoles.User,
            "assistant" => ChatMessageRoles.Assistant,
            _ => ChatMessageRoles.User
        };
    }

    /// <summary>
    /// Converts a LlmTornado ChatMessageRoles value to an ACP role string.
    /// </summary>
    public static string ToAcpRole(this ChatMessageRoles? role)
    {
        return role switch
        {
            ChatMessageRoles.User => "user",
            ChatMessageRoles.System => "user",
            ChatMessageRoles.Assistant => "assistant",
            _ => "assistant"
        };
    }
}
