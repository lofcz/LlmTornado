using A2A;
using LlmTornado.Chat;
using LlmTornado.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.A2A;
/// <summary>
/// ToDo:
/// - Handle more file types (e.g. video, other documents)
/// </summary>
public static partial class A2ATornadoExtension
{
    public static ChatMessage ToTornadoMessage(this AgentMessage agentMessage)
    {
        List<ChatMessagePart> parts = new List<ChatMessagePart>();
        if (agentMessage.Parts != null)
        {
            foreach (var part in agentMessage.Parts)
            {
                if (part is TextPart textPart)
                {
                    parts.Add(new ChatMessagePart { Text = textPart.Text });
                }
                else if (part is FilePart filePart)
                {
                    if (filePart.File is FileWithUri fileWithUri)
                    {
                        parts.Add(ChatMessagePart.Create(new Uri(fileWithUri.Uri), ChatMessageTypes.Image));
                    }
                    else if (filePart.File is FileWithBytes fileWithBytes)
                    {
                        if (fileWithBytes.Bytes.Contains("image"))
                        {
                            parts.Add(new ChatMessagePart(fileWithBytes.Bytes, Images.ImageDetail.Auto));
                        }
                        else if(fileWithBytes.Bytes.Contains("audio"))
                        {
                            if(fileWithBytes.MimeType == "audio/wav" || fileWithBytes.MimeType == "audio/x-wav")
                                parts.Add(ChatMessagePart.Create(fileWithBytes.Bytes, ChatAudioFormats.Wav));
                            else if (fileWithBytes.MimeType == "audio/mpeg" || fileWithBytes.MimeType == "audio/mp3" || fileWithBytes.MimeType == "audio/x-mp3")
                                parts.Add(ChatMessagePart.Create(fileWithBytes.Bytes, ChatAudioFormats.Mp3));
                        }
                        //ToDo - handle other types
                        else
                        {
                            parts.Add(new ChatMessagePart(fileWithBytes.Bytes, DocumentLinkTypes.Base64));
                        }
                    }
                }
            }
        }

        return new ChatMessage
        {
            Role = ToTornadoMessageRole(agentMessage.Role),
            Parts = parts
        };
    }

    public static AgentMessage ToA2AAgentMessage(this ChatMessage chatMessage,
        Dictionary<string, JsonElement>? metadata = null,
        string? contextId = null,
        string? taskId = null,
        string[]? referenceTaskIds = null,
        string[]? extensions = null
        )
    {
        List<Part> parts = new List<Part>();

        if (chatMessage.Content != null)
        {
            parts.Add(new TextPart() { Text = chatMessage.Content });
        }
        else if (chatMessage.Parts != null)
        {
            foreach (var part in chatMessage.Parts)
            {
                parts.Add(part.ToA2APart());
            }
        }

        return new AgentMessage
        {
            Role = chatMessage.Role.ToA2AMessageRole(),
            MessageId = chatMessage.Id.ToString(),
            Parts = parts,
            Metadata = metadata,
            ContextId = contextId,
            TaskId = taskId,
            ReferenceTaskIds = referenceTaskIds?.ToList(),
            Extensions = extensions?.ToList()
        };
    }

    /// <summary>
    /// Work in progress
    /// </summary>
    /// <param name="chatMessage"></param>
    /// <param name="metadata"></param>
    /// <param name="contextId"></param>
    /// <param name="taskId"></param>
    /// <param name="referenceTaskIds"></param>
    /// <param name="extensions"></param>
    /// <returns></returns>
    public static Artifact ToA2AArtifact(this ChatMessage chatMessage,
       Dictionary<string, JsonElement>? metadata = null,
       string? description = null,
       string? name = null,
       string[]? extensions = null
       )
    {
        List<Part> parts = new List<Part>();

        if (chatMessage.Content != null)
        {
            parts.Add(new TextPart() { Text = chatMessage.Content });
        }
        else if (chatMessage.Parts != null)
        {
            foreach (var part in chatMessage.Parts)
            {
                var a2aPart = part.ToA2APart();
                if (a2aPart != null) { parts.Add(a2aPart); }
            }
        }

        return new Artifact
        {
            Metadata = metadata,
            Parts = new List<Part>(),
            ArtifactId = chatMessage.Id.ToString(),
            Description = description,
            Name = name,
            Extensions = extensions?.ToList()
        };
    }

    public static Part? ToA2APart(this ChatMessagePart part)
    {
        if (part.Text != null)
        {
            return new TextPart() { Text = part.Text };
        }
        else if (part.Image != null)
        {
            return new FilePart() { File = new FileWithUri() { Uri = part.Image.Url, MimeType = part.Image.MimeType } };
        }
        else if (part.Document != null)
        {
            if (part.Document.Uri != null)
                return new FilePart() { File = new FileWithUri() { Uri = part.Document.Uri.AbsoluteUri } };
            else
                return new FilePart() { File = new FileWithBytes() { Bytes = part.Document.Base64 } };
        }
        else if (part.Audio != null)
        {
            if (part.Audio.Url != null)
                return new FilePart() { File = new FileWithUri() { Uri = part.Audio.Url.AbsoluteUri } };
            else
                return new FilePart() { File = new FileWithBytes() { Bytes = part.Audio.Data, MimeType = part.Audio.MimeType } };
        }
        else if (part.Reasoning != null)
        {
            if (part.Reasoning?.IsRedacted ?? true)
            {
                JsonElement stringElement = JsonDocument.Parse($"{{\"message\": \"{part.Reasoning.Content}\"}}").RootElement.GetProperty("message");
                return new TextPart() { Text = "Reasoning", Metadata = new Dictionary<string, JsonElement>() { { "Content", stringElement } } };
            }  
        }
        else if (part.Video != null) 
        { 
            return new FilePart() { File = new FileWithUri() { Uri = part.Video.Url.AbsoluteUri} };
        }

        return null;
    }

    public static MessageRole ToA2AMessageRole(this ChatMessageRoles? role)
    {
        return role switch
        {
            ChatMessageRoles.User => MessageRole.User,
            ChatMessageRoles.System => MessageRole.User,
            ChatMessageRoles.Assistant => MessageRole.Agent,
            _ => MessageRole.Agent,
        };
    }

    public static ChatMessageRoles ToTornadoMessageRole(this MessageRole? role)
    {
        return role switch
        {
            MessageRole.User => ChatMessageRoles.User,
            MessageRole.Agent => ChatMessageRoles.Assistant,
            _ => ChatMessageRoles.User,
        };
    }
}
