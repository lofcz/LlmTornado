using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Agents.Samples.ContextController;

public class ConversationContent
{
    public List<ChatMessage> SystemMessages { get; set; } = new List<ChatMessage>();
    public List<ChatMessage> MessagesToCompress { get; set; } = new List<ChatMessage>();

    /// <summary>
    ///  Get the content of the messages based on the compression options
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ConversationContent SortContent(List<ChatMessage>messages, MessageCompressionOptions options)
    {
        ConversationContent content = new ConversationContent();
        ChatMessage msg;
        // Collect messages into categories
        for (int i = 0; i < messages.Count; i++)
        {
            msg = messages[i];

            if (options.PreserveSystemmessages && msg.Role == ChatMessageRoles.System)
            {
                content.SystemMessages.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.User)
            {
                content.MessagesToCompress.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.Tool && options.CompressToolCallmessages) //Keep tool messages only if compressing them
            {
                content.MessagesToCompress.Add(msg);
            }
            else if (msg.Role == ChatMessageRoles.Assistant)
            {
                if (msg.FunctionCall != null)
                {
                    if (options.CompressToolCallmessages)
                    {
                        content.MessagesToCompress.Add(msg);
                    }
                }
                else
                {
                    content.MessagesToCompress.Add(msg);
                }
            }
        }

        return content;
    }

    /// <summary>
    ///  Get the content of the messages based on the compression options
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static List<ChatMessage> GetSystemMessages(List<ChatMessage>messages, MessageCompressionOptions options)
    {
        ConversationContent content = new ConversationContent();
        ChatMessage msg;
        // Collect messages into categories
        for (int i = 0; i < messages.Count; i++)
        {
            msg = messages[i];

            if (options.PreserveSystemmessages && msg.Role == ChatMessageRoles.System)
            {
                content.SystemMessages.Add(msg);
            }
        }

        return content.SystemMessages;
    }
}