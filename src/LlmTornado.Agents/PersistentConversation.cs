using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images;
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
    public string? ImageMimeType { get; set; }
    public string? AudioData { get; set; }          // base64 if you used audio
    public string? AudioFormat { get; set; }
    public string? AudioUrl { get; set; }            // URL-based audio
    public string? DocumentBase64 { get; set; }      // base64 PDF
    public string? DocumentUrl { get; set; }         // URL-based document
    
    /// <summary>
    /// Relative path to externalized media file (in media/ subfolder).
    /// When set, the actual binary data should be loaded from this file
    /// instead of from the inline base64 fields.
    /// </summary>
    public string? MediaFilePath { get; set; }
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
                using FileStream fs = File.Create(ConversationPath);
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

        using StreamWriter writer = new StreamWriter(ConversationPath, append); // append mode

        lock (lockObject)
        {
            if (_unsavedMessages.IsEmpty)
                return;

            while (_unsavedMessages.TryDequeue(out ChatMessage? msg))
            {
                PersistentMessage dto = ConversationIOUtility.ConvertChatMessageToPersistent(msg, ConversationPath);

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
            return []; 
        }

        List<ChatMessage> messages = [];

        if (!File.Exists(ConversationPath))
            throw new FileNotFoundException("Conversation file not found", ConversationPath);

        using StreamReader reader = new StreamReader(ConversationPath);
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            PersistentMessage? dto = JsonConvert.DeserializeObject<PersistentMessage>(line);
            if (dto == null)
                continue;

            messages.Add(ConversationIOUtility.ConvertPersistantToChatMessage(dto, ConversationPath));
        }

        return messages;
    }

}


public static class ConversationIOUtility
{

    public static PersistentMessage ConvertChatMessageToPersistent(ChatMessage message, string? conversationFilePath = null)
    {
        return new PersistentMessage
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            Parts = message.Parts?.Select(p => ConvertPartToPersistent(p, conversationFilePath)).ToList()
        };
    }

    private static PersistentPart ConvertPartToPersistent(ChatMessagePart p, string? conversationFilePath)
    {
        PersistentPart result = new()
        {
            Type = p.Type.ToString(),
            Text = p.Text,
            ImageMimeType = p.Image?.MimeType,
            AudioFormat = p.Audio?.Format?.ToString(),
            AudioUrl = p.Audio?.Url?.AbsoluteUri,
            DocumentUrl = p.Document?.Uri?.AbsoluteUri,
        };

        // Externalize large binary data to media files when possible
        bool canExternalize = !string.IsNullOrEmpty(conversationFilePath);

        switch (p.Type)
        {
            case ChatMessageTypes.Image when p.Image is not null:
            {
                string? imageData = p.Image.Url;
                if (canExternalize && !string.IsNullOrEmpty(imageData) && IsBase64Content(imageData))
                {
                    byte[] bytes = ExtractBase64Bytes(imageData);
                    string ext = MediaStorage.GetExtensionForMime(p.Image.MimeType);
                    result.MediaFilePath = MediaStorage.SaveMedia(conversationFilePath!, bytes, ext);
                    result.ImageMimeType = p.Image.MimeType;
                    // Don't store inline base64
                }
                else
                {
                    result.ImageUrl = imageData;
                }
                break;
            }
            case ChatMessageTypes.Audio when p.Audio is not null:
            {
                if (canExternalize && !string.IsNullOrEmpty(p.Audio.Data))
                {
                    byte[] bytes = Convert.FromBase64String(p.Audio.Data);
                    string ext = MediaStorage.GetExtensionForMime(MediaStorage.GetMimeForAudioFormat(p.Audio.Format));
                    result.MediaFilePath = MediaStorage.SaveMedia(conversationFilePath!, bytes, ext);
                    // Don't store inline base64
                }
                else
                {
                    result.AudioData = p.Audio.Data;
                }
                break;
            }
            case ChatMessageTypes.Document when p.Document is not null:
            {
                if (canExternalize && !string.IsNullOrEmpty(p.Document.Base64))
                {
                    byte[] bytes = Convert.FromBase64String(p.Document.Base64);
                    result.MediaFilePath = MediaStorage.SaveMedia(conversationFilePath!, bytes, ".pdf");
                    // Don't store inline base64
                }
                else
                {
                    result.DocumentBase64 = p.Document.Base64;
                    result.DocumentUrl = p.Document.Uri?.AbsoluteUri;
                }
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Check if the string is base64 content (either raw base64 or data: URI).
    /// </summary>
    private static bool IsBase64Content(string value)
    {
        return value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && value.Length > 256); // raw base64 is always much longer than a URL
    }

    /// <summary>
    /// Extract raw bytes from a base64 string or data: URI.
    /// </summary>
    private static byte[] ExtractBase64Bytes(string value)
    {
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            int commaIndex = value.IndexOf(',');
            if (commaIndex >= 0)
                return Convert.FromBase64String(value[(commaIndex + 1)..]);
        }

        return Convert.FromBase64String(value);
    }

    public static ChatMessage ConvertPersistantToChatMessage(PersistentMessage persisted, string? conversationFilePath = null)
    {
        // Rebuild parts (fallback to Content)
        List<ChatMessagePart>? parts = null;
        if (persisted.Parts is { Count: > 0 })
        {
            parts = [];
            foreach (PersistentPart part in persisted.Parts)
            {
                ChatMessagePart? converted = ConvertPersistentPartToChat(part, conversationFilePath);
                if (converted is not null)
                    parts.Add(converted);
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

    private static ChatMessagePart? ConvertPersistentPartToChat(PersistentPart part, string? conversationFilePath)
    {
        switch (part.Type)
        {
            case nameof(ChatMessageTypes.Text):
            {
                return !string.IsNullOrEmpty(part.Text) ? new ChatMessagePart(part.Text) : null;
            }
            case nameof(ChatMessageTypes.Image):
            {
                // Try loading from externalized media file first
                if (!string.IsNullOrEmpty(part.MediaFilePath) && !string.IsNullOrEmpty(conversationFilePath))
                {
                    byte[]? bytes = MediaStorage.LoadMedia(conversationFilePath, part.MediaFilePath);
                    if (bytes is not null)
                    {
                        string base64 = Convert.ToBase64String(bytes);
                        string dataUri = $"data:{part.ImageMimeType ?? "image/png"};base64,{base64}";
                        return new ChatMessagePart(dataUri, Images.ImageDetail.Auto, part.ImageMimeType);
                    }
                }

                // Fall back to inline ImageUrl (URL or old inline base64)
                if (!string.IsNullOrEmpty(part.ImageUrl))
                {
                    // Try as URI first (handles http:// and data: URIs)
                    if (Uri.TryCreate(part.ImageUrl, UriKind.Absolute, out Uri? uri))
                        return new ChatMessagePart(uri);

                    // Fallback: treat as raw base64 string (fixes old persistence bug)
                    return new ChatMessagePart(part.ImageUrl, Images.ImageDetail.Auto, part.ImageMimeType);
                }

                return null;
            }
            case nameof(ChatMessageTypes.Audio):
            {
                // Try loading from externalized media file first
                if (!string.IsNullOrEmpty(part.MediaFilePath) && !string.IsNullOrEmpty(conversationFilePath))
                {
                    byte[]? bytes = MediaStorage.LoadMedia(conversationFilePath, part.MediaFilePath);
                    if (bytes is not null)
                    {
                        Enum.TryParse<ChatAudioFormats>(part.AudioFormat, true, out ChatAudioFormats fmt);
                        return new ChatMessagePart(bytes, fmt);
                    }
                }

                // Try URL-based audio
                if (!string.IsNullOrEmpty(part.AudioUrl) && Uri.TryCreate(part.AudioUrl, UriKind.Absolute, out Uri? audioUri))
                    return new ChatMessagePart(audioUri, ChatMessageTypes.Audio);

                // Fall back to inline base64 audio
                if (!string.IsNullOrEmpty(part.AudioData) && Enum.TryParse<ChatAudioFormats>(part.AudioFormat, true, out ChatAudioFormats audioFmt))
                    return new ChatMessagePart(part.AudioData, audioFmt);

                return null;
            }
            case nameof(ChatMessageTypes.Document):
            {
                // Try loading from externalized media file first
                if (!string.IsNullOrEmpty(part.MediaFilePath) && !string.IsNullOrEmpty(conversationFilePath))
                {
                    byte[]? bytes = MediaStorage.LoadMedia(conversationFilePath, part.MediaFilePath);
                    if (bytes is not null)
                    {
                        string base64 = Convert.ToBase64String(bytes);
                        return new ChatMessagePart(new ChatDocument(base64));
                    }
                }

                // Fall back to inline document data
                if (!string.IsNullOrEmpty(part.DocumentBase64))
                    return new ChatMessagePart(new ChatDocument(part.DocumentBase64));

                if (!string.IsNullOrEmpty(part.DocumentUrl) && Uri.TryCreate(part.DocumentUrl, UriKind.Absolute, out Uri? docUri))
                    return new ChatMessagePart(new ChatDocument(docUri));

                return null;
            }
            default:
                return null;
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
            using FileStream fs = File.Create(filePath);
        }

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Conversation file not found", filePath);

        List<PersistentMessage> dto = Messages
            .Select(m => ConvertChatMessageToPersistent(m, filePath)).ToList();

        string json = JsonConvert.SerializeObject(dto, Formatting.Indented);
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
            return [];
        }

        if (!File.Exists(conversationPath))
            throw new FileNotFoundException("Conversation file not found", conversationPath);

        using StreamReader reader = new StreamReader(conversationPath);
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            PersistentMessage? dto = JsonConvert.DeserializeObject<PersistentMessage>(line);
            if (dto == null)
                continue;

            messages.Add(ConvertPersistantToChatMessage(dto, conversationPath));
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
            return [];
        }

        if (!File.Exists(conversationPath))
            throw new FileNotFoundException("Conversation file not found", conversationPath);

        string json = File.ReadAllText(conversationPath);

        List<PersistentMessage>? dtos = JsonConvert.DeserializeObject<List<PersistentMessage>>(json);
        if (dtos == null)
            return [];

        foreach (PersistentMessage dto in dtos)
        {
            messages.Add(ConvertPersistantToChatMessage(dto, conversationPath));
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

        foreach (ChatMessage m in messagesToAppend)
        {
            // Rebuild parts (fallback to Content)
            List<ChatMessagePart>? parts = null;
            if (m.Parts is { Count: > 0 })
            {
                parts = [];
                foreach (ChatMessagePart part in m.Parts)
                {
                    switch (part.Type)
                    {
                        case ChatMessageTypes.Text:
                            if (!string.IsNullOrEmpty(part.Text))
                                parts.Add(new ChatMessagePart(part.Text));
                            break;
                        case ChatMessageTypes.Image:
                            if (!string.IsNullOrEmpty(part.Image.Url) && Uri.TryCreate(part.Image.Url, UriKind.Absolute, out Uri? uri))
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
