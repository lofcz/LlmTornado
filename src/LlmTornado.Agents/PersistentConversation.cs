using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents;
public class PersistentMessage
{
    public Guid Id { get; set; }
    public ChatMessageRoles? Role { get; set; }
    public string? Content { get; set; }
    public List<PersistentPart>? Parts { get; set; }
}

public class PersistentPart
{
    public string Type { get; set; } = "";
    public string? Text { get; set; }
    public string? ImageUrl { get; set; }
    public string? AudioData { get; set; }          // base64 if you used audio
    public string? AudioFormat { get; set; }
}

public class PersistentConversation
{
    private readonly object lockObject = new object();
    public List<ChatMessage> Messages => GetMessages();
    private ConcurrentStack<ChatMessage> _messages { get; set; } = new ConcurrentStack<ChatMessage>();

    private ConcurrentQueue<ChatMessage> _unsavedMessages = new ConcurrentQueue<ChatMessage>();
    public bool ContinuousSaving { get; set; } = false;

    public readonly string ConversationPath;

    public PersistentConversation(string conversationPath, bool continuousSave = false)
    {
        ConversationPath = conversationPath;
        if (string.IsNullOrEmpty(ConversationPath))
        {
            throw new ArgumentException("conversationPath cannot be null or empty", nameof(conversationPath));
        }

        ContinuousSaving = continuousSave;

        // Load existing conversation
        if (File.Exists(ConversationPath))
        {
            Task.Run(async () => await LoadAsync()).Wait();
        }
        else // file does not exist, ensure directory exists
        {
            if (!Directory.Exists(Path.GetDirectoryName(ConversationPath))) // create directory if it doesn't exist
            {
                string? dir = Path.GetDirectoryName(ConversationPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
            }
            else // file does not exist but directory does, create empty file
            {
                using var fs = File.Create(ConversationPath);
            }
        }
    }

    /// <summary>
    /// Clears the conversation history from memory (does not delete any saved files)
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            _messages.Clear();
            _unsavedMessages = new ConcurrentQueue<ChatMessage>();
        }
    }

    /// <summary>
    /// Get messages from the conversation in chronological order
    /// </summary>
    /// <returns></returns>
    public List<ChatMessage> GetMessages()
    {
        lock (lockObject)
        {
            List<ChatMessage> msgs = _messages.ToList();
            msgs.Reverse();
            return msgs;
        }
    }

    /// <summary>
    /// Append a message to the conversation memory and save if ContinuousSaving is enabled
    /// </summary>
    /// <param name="message"></param>
    public void AppendMessage(ChatMessage message)
    {
        lock (lockObject)
        {
            _messages.Push(message);
            _unsavedMessages.Enqueue(message);
            if (ContinuousSaving) SaveChanges();
        }
    }

    /// <summary>
    /// Saves any unsaved messages to the conversation file
    /// </summary>
    public void SaveChanges()
    {
        if (string.IsNullOrEmpty(ConversationPath))
        {
            Console.WriteLine("Warning: ConversationPath is not set. Cannot save conversation.");
            return;
        }
            
        UpdateConversationFile();
    }

    /// <summary>
    /// Delete the conversation file
    /// </summary>
    /// <param name="conversationPath"></param>
    public static void DeleteConversation(string conversationPath)
    {
        if (File.Exists(conversationPath))
        {
            File.Delete(conversationPath);
        }
        else
        {
           Console.WriteLine("Warning: Conversation file does not exist. Cannot delete.");
        }
    }

    /// <summary>
    /// Load messages from the conversation file
    /// </summary>
    /// <returns></returns>
    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(ConversationPath))
        {
            Console.WriteLine("Warning: ConversationPath is not set. Cannot load conversation.");
            return;
        }

        if (!File.Exists(ConversationPath))
        {
            Console.WriteLine("Warning: Conversation file does not exist. Cannot load conversation.");
            return;
        }

        List<ChatMessage> loadedMessages = await LoadMessagesJsonlAsync();

        lock (lockObject)
            _messages = new ConcurrentStack<ChatMessage>(loadedMessages);
    }

    /// <summary>
    /// Appends messages to a JSONL file without rewriting existing content
    /// </summary>
    /// <param name="messages">Messages to append</param>
    /// <param name="filePath">File path</param>
    private void UpdateConversationFile()
    {
        if (string.IsNullOrEmpty(ConversationPath))
        {
            Console.WriteLine("Warning: ConversationPath is not set. Cannot save conversation.");
            return;
        }

        bool append = File.Exists(ConversationPath);

        using var writer = new StreamWriter(ConversationPath, append); // append mode

        lock (lockObject)
        {
            if (_unsavedMessages.IsEmpty)
                return;

            while (_unsavedMessages.TryDequeue(out var msg))
            {
                var dto = ConversationIOUtility.ConvertChatMessageToPersistent(msg);

                string json = JsonConvert.SerializeObject(dto);
                writer.WriteLine(json);
            }
        }

    }

    /// <summary>
    /// Loads messages from a JSONL file format
    /// </summary>
    /// <param name="messages">List to load into</param>
    /// <param name="filePath">Path to JSONL file</param>
    /// <returns>The list with messages loaded</returns>
    private async Task<List<ChatMessage>> LoadMessagesJsonlAsync()
    {
        if (string.IsNullOrEmpty(ConversationPath))
        {
            Console.WriteLine("Warning: ConversationPath is not set. Cannot save conversation.");
            return new List<ChatMessage>(); 
        }

        List<ChatMessage> messages = new List<ChatMessage>();

        if (!File.Exists(ConversationPath))
            throw new FileNotFoundException("Conversation file not found", ConversationPath);

        using var reader = new StreamReader(ConversationPath);
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var dto = JsonConvert.DeserializeObject<PersistentMessage>(line);
            if (dto == null)
                continue;

            messages.Add(ConversationIOUtility.ConvertPersistantToChatMessage(dto));
        }

        return messages;
    }

}


public static class ConversationIOUtility
{

    public static PersistentMessage ConvertChatMessageToPersistent(ChatMessage message)
    {
        return new PersistentMessage
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            Parts = message.Parts?.Select(p => new PersistentPart
            {
                Type = p.Type.ToString(),
                Text = p.Text,
                ImageUrl = p.Image?.Url,
                AudioData = p.Audio?.Data,
                AudioFormat = p.Audio?.Format.ToString()
            }).ToList()
        };
    }

    public static ChatMessage ConvertPersistantToChatMessage(PersistentMessage persisted)
    {
        // Rebuild parts (fallback to Content)
        List<ChatMessagePart>? parts = null;
        if (persisted.Parts is { Count: > 0 })
        {
            parts = new List<ChatMessagePart>();
            foreach (var part in persisted.Parts)
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

        if (parts is { Count: > 0 })
        {
            return persisted.Role switch
                {
                ChatMessageRoles.System => new ChatMessage(ChatMessageRoles.System, parts),
                ChatMessageRoles.User => new ChatMessage(ChatMessageRoles.User, parts),
                ChatMessageRoles.Assistant => new ChatMessage(ChatMessageRoles.Assistant, parts),
                _ => new ChatMessage(ChatMessageRoles.User, parts),
            };
        }
        else
        {
            return persisted.Role switch 
            {
                ChatMessageRoles.System => new ChatMessage(ChatMessageRoles.System, persisted.Content ?? ""),
                ChatMessageRoles.User => new ChatMessage(ChatMessageRoles.User, persisted.Content ?? ""),
                ChatMessageRoles.Assistant => new ChatMessage(ChatMessageRoles.Assistant, persisted.Content ?? ""),
                _ => new ChatMessage(ChatMessageRoles.User, persisted.Content ?? ""),
            };
        }
    }

    /// <summary>
    /// Save a conversation to a file
    /// </summary>
    /// <param name="Messages"></param>
    /// <param name="filePath"></param>
    public static void SaveConversation(this List<ChatMessage> Messages, string filePath)
    {

        if (!Directory.Exists(Path.GetDirectoryName(filePath))) // create directory if it doesn't exist
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
        else // file does not exist but directory does, create empty file
        {
            using var fs = File.Create(filePath);
        }

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Conversation file not found", filePath);

        var dto = Messages
            .Select(m => ConvertChatMessageToPersistent(m)).ToList();

        var json = JsonConvert.SerializeObject(dto, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

   
    /// <summary>
    /// Recreate a new Conversation from a persisted file
    /// </summary>
    /// <param name="filePath"> File to load conversation from</param>
    /// <returns></returns>
    public static async Task<List<ChatMessage>> LoadMessagesAsync(this List<ChatMessage> messages,  string conversationPath)
    {
        if (string.IsNullOrEmpty(conversationPath))
        {
            Console.WriteLine("Warning: ConversationPath is not set. Cannot save conversation.");
            return new List<ChatMessage>();
        }

        if (!File.Exists(conversationPath))
            throw new FileNotFoundException("Conversation file not found", conversationPath);

        using var reader = new StreamReader(conversationPath);
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var dto = JsonConvert.DeserializeObject<PersistentMessage>(line);
            if (dto == null)
                continue;

            messages.Add(ConvertPersistantToChatMessage(dto));
        }

        return messages;
    }

    /// <summary>
    /// Recreate a new Conversation from a persisted file
    /// </summary>
    /// <param name="filePath"> File to load conversation from</param>
    /// <returns></returns>
    public static List<ChatMessage> LoadMessages(this List<ChatMessage> messages, string conversationPath)
    {
        if (string.IsNullOrEmpty(conversationPath))
        {
            Console.WriteLine("Warning: ConversationPath is not set. Cannot save conversation.");
            return new List<ChatMessage>();
        }

        if (!File.Exists(conversationPath))
            throw new FileNotFoundException("Conversation file not found", conversationPath);

        var json = File.ReadAllText(conversationPath);

        var dtos = JsonConvert.DeserializeObject<List<PersistentMessage>>(json);
        if (dtos == null)
            return new List<ChatMessage>();

        foreach (var dto in dtos)
        {
            messages.Add(ConvertPersistantToChatMessage(dto));
        }

        return messages;
    }

    /// <summary>
    /// Recreate a new Conversation from a persisted file
    /// </summary>
    /// <param name="filePath"> File to load conversation from</param>
    /// <returns></returns>
    public static void LoadConversation(this Conversation conversation, List<ChatMessage> messagesToAppend)
    {
        conversation.Clear();

        foreach (var m in messagesToAppend)
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
                        case ChatMessageTypes.Text:
                            if (!string.IsNullOrEmpty(part.Text))
                                parts.Add(new ChatMessagePart(part.Text));
                            break;
                        case ChatMessageTypes.Image:
                            if (!string.IsNullOrEmpty(part.Image.Url) && Uri.TryCreate(part.Image.Url, UriKind.Absolute, out var uri))
                                parts.Add(new ChatMessagePart(uri));
                            break;
                        case ChatMessageTypes.Audio:
                            if (!string.IsNullOrEmpty(part.Audio.Data))
                                parts.Add(new ChatMessagePart(part.Audio.Data, part.Audio.Format ?? ChatAudioFormats.Wav));
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
