using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Utility;
public class PersistedMessage
{
    public Guid Id { get; set; }
    public ChatMessageRoles? Role { get; set; }
    public string? Content { get; set; }
    public List<PersistedPart>? Parts { get; set; }
}

public class PersistedPart
{
    public string Type { get; set; } = "";
    public string? Text { get; set; }
    public string? ImageUrl { get; set; }
    public string? AudioData { get; set; }          // base64 if you used audio
    public string? AudioFormat { get; set; }
}

public static class PersistentConversation
{
    /// <summary>
    /// Save the conversation to a file
    /// </summary>
    /// <param name="Messages"></param>
    /// <param name="filePath"></param>
    public static void SaveConversation(this List<ChatMessage> Messages, string filePath)
    {
        var dto = Messages
            .Select(m => new PersistedMessage
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                Parts = m.Parts?.Select(p => new PersistedPart
                {
                    Type = p.Type.ToString(),
                    Text = p.Text,
                    ImageUrl = p.Image?.Url,
                    AudioData = p.Audio?.Data,
                    AudioFormat = p.Audio?.Format.ToString()
                }).ToList()
            }).ToList();

        var json = JsonConvert.SerializeObject(dto, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Save the conversation to a file
    /// </summary>
    /// <param name="conversation"></param>
    /// <param name="filePath"></param>
    public static void SaveConversation(this Conversation conversation, string filePath)
    {
        conversation.Messages.ToList().SaveConversation(filePath);
    }

    /// <summary>
    /// Recreate a new Conversation from a persisted file
    /// </summary>
    /// <param name="filePath"> File to load conversation from</param>
    /// <returns></returns>
    public static async Task<List<ChatMessage>> LoadConversationAsync(this List<ChatMessage> messages,  string filePath)
    {
        using var sr = new StreamReader(filePath);
        string json = await sr.ReadToEndAsync();
        var dto = JsonConvert.DeserializeObject<List<PersistedMessage>>(json) ?? [];

        foreach (var m in dto)
        {
            // Rebuild parts (fallback to Content)
            List<ChatMessagePart>? parts = null;
            if (m.Parts is { Count: > 0 })
            {
                parts = new List<ChatMessagePart>();
                foreach (var part in m.Parts)
                {
                    switch (part.Type)
                    {
                        case nameof(ChatMessageTypes.Text):
                            if (!string.IsNullOrEmpty(part.Text))
                                parts.Add(new ChatMessagePart(part.Text));
                            break;
                        case nameof(ChatMessageTypes.Image):
                            if (!string.IsNullOrEmpty(part.ImageUrl) && Uri.TryCreate(part.ImageUrl, UriKind.Absolute, out var uri))
                                parts.Add(new ChatMessagePart(uri));
                            break;
                        case nameof(ChatMessageTypes.Audio):
                            if (!string.IsNullOrEmpty(part.AudioData) && Enum.TryParse<ChatAudioFormats>(part.AudioFormat, true, out var fmt))
                                parts.Add(new ChatMessagePart(part.AudioData, fmt));
                            break;
                        default:
                            // ignore unsupported types for now
                            break;
                    }
                }
            }

            void AppendSimple()
            {
                if (string.IsNullOrEmpty(m.Content))
                    return;

                switch (m.Role)
                {
                    case ChatMessageRoles.System: messages.Add(new ChatMessage(ChatMessageRoles.System, m.Content ?? "")); break;
                    case ChatMessageRoles.User: messages.Add(new ChatMessage(ChatMessageRoles.User, m.Content ?? "")); break;
                    case ChatMessageRoles.Assistant: messages.Add(new ChatMessage(ChatMessageRoles.Assistant, m.Content ?? "")); break;
                }
            }

            if (parts is { Count: > 0 })
            {
                switch (m.Role)
                {
                    case ChatMessageRoles.System: messages.Add(new ChatMessage(ChatMessageRoles.System, parts)); break;
                    case ChatMessageRoles.User: messages.Add(new ChatMessage(ChatMessageRoles.User, parts)); break;
                    case ChatMessageRoles.Assistant: messages.Add(new ChatMessage(ChatMessageRoles.Assistant, parts)); break;
                    default: AppendSimple(); break;
                }
            }
            else
            {
                AppendSimple();
            }
        }

        return messages;
    }

    /// <summary>
    /// Recreate a new Conversation from a persisted file
    /// </summary>
    /// <param name="filePath"> File to load conversation from</param>
    /// <returns></returns>
    public static List<ChatMessage> LoadConversation(this List<ChatMessage> messages, string filePath)
    {
        using var sr = new StreamReader(filePath);
        string json = sr.ReadToEnd();
        var dto = JsonConvert.DeserializeObject<List<PersistedMessage>>(json) ?? [];

        foreach (var m in dto)
        {
            // Rebuild parts (fallback to Content)
            List<ChatMessagePart>? parts = null;
            if (m.Parts is { Count: > 0 })
            {
                parts = new List<ChatMessagePart>();
                foreach (var part in m.Parts)
                {
                    switch (part.Type)
                    {
                        case nameof(ChatMessageTypes.Text):
                            if (!string.IsNullOrEmpty(part.Text))
                                parts.Add(new ChatMessagePart(part.Text));
                            break;
                        case nameof(ChatMessageTypes.Image):
                            if (!string.IsNullOrEmpty(part.ImageUrl) && Uri.TryCreate(part.ImageUrl, UriKind.Absolute, out var uri))
                                parts.Add(new ChatMessagePart(uri));
                            break;
                        case nameof(ChatMessageTypes.Audio):
                            if (!string.IsNullOrEmpty(part.AudioData) && Enum.TryParse<ChatAudioFormats>(part.AudioFormat, true, out var fmt))
                                parts.Add(new ChatMessagePart(part.AudioData, fmt));
                            break;
                        default:
                            // ignore unsupported types for now
                            break;
                    }
                }
            }

            void AppendSimple()
            {
                if (string.IsNullOrEmpty(m.Content))
                    return;

                switch (m.Role)
                {
                    case ChatMessageRoles.System: messages.Add(new ChatMessage(ChatMessageRoles.System, m.Content ?? "")); break;
                    case ChatMessageRoles.User: messages.Add(new ChatMessage(ChatMessageRoles.User, m.Content ?? "")); break;
                    case ChatMessageRoles.Assistant: messages.Add(new ChatMessage(ChatMessageRoles.Assistant, m.Content ?? "")); break;
                }
            }

            if (parts is { Count: > 0 })
            {
                switch (m.Role)
                {
                    case ChatMessageRoles.System: messages.Add(new ChatMessage(ChatMessageRoles.System, parts)); break;
                    case ChatMessageRoles.User: messages.Add(new ChatMessage(ChatMessageRoles.User, parts)); break;
                    case ChatMessageRoles.Assistant: messages.Add(new ChatMessage(ChatMessageRoles.Assistant, parts)); break;
                    default: AppendSimple(); break;
                }
            }
            else
            {
                AppendSimple();
            }
        }

        return messages;
    }

    /// <summary>
    /// Recreate a new Conversation from a persisted file
    /// </summary>
    /// <param name="filePath"> File to load conversation from</param>
    /// <returns></returns>
    public static async Task LoadConversationAsync(this Conversation conversation, string filePath)
    {
        using var sr = new StreamReader(filePath);
        string json = await sr.ReadToEndAsync();
        var dto = JsonConvert.DeserializeObject<List<PersistedMessage>>(json) ?? [];

        conversation.Clear();

        foreach (var m in dto)
        {
            // Rebuild parts (fallback to Content)
            List<ChatMessagePart>? parts = null;
            if (m.Parts is { Count: > 0 })
            {
                parts = new List<ChatMessagePart>();
                foreach (var part in m.Parts)
                {
                    switch (part.Type)
                    {
                        case nameof(ChatMessageTypes.Text):
                            if (!string.IsNullOrEmpty(part.Text))
                                parts.Add(new ChatMessagePart(part.Text));
                            break;
                        case nameof(ChatMessageTypes.Image):
                            if (!string.IsNullOrEmpty(part.ImageUrl) && Uri.TryCreate(part.ImageUrl, UriKind.Absolute, out var uri))
                                parts.Add(new ChatMessagePart(uri));
                            break;
                        case nameof(ChatMessageTypes.Audio):
                            if (!string.IsNullOrEmpty(part.AudioData) && Enum.TryParse<ChatAudioFormats>(part.AudioFormat, true, out var fmt))
                                parts.Add(new ChatMessagePart(part.AudioData, fmt));
                            break;
                        default:
                            // ignore unsupported types for now
                            break;
                    }
                }
            }

            void AppendSimple()
            {
                if (string.IsNullOrEmpty(m.Content))
                    return;

                switch (m.Role)
                {
                    case ChatMessageRoles.System: conversation.AddSystemMessage(m.Content ?? ""); break;
                    case ChatMessageRoles.User: conversation.AddUserMessage(m.Content ?? ""); break;
                    case ChatMessageRoles.Assistant: conversation.AddAssistantMessage(m.Content ?? ""); break;
                }
            }

            if (parts is { Count: > 0 })
            {
                switch (m.Role)
                {
                    case ChatMessageRoles.System: conversation.AppendMessage(new ChatMessage(ChatMessageRoles.System, parts)); break;
                    case ChatMessageRoles.User: conversation.AppendMessage(new ChatMessage(ChatMessageRoles.User, parts)); break;
                    case ChatMessageRoles.Assistant: conversation.AppendMessage(new ChatMessage(ChatMessageRoles.Assistant, parts)); break;
                    default: AppendSimple(); break;
                }
            }
            else
            {
                AppendSimple();
            }
        }
    }
}
