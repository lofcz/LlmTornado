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

public static class A2ATornadoExtension
{
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
        else if(chatMessage.Parts != null)
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
                parts.Add(part.ToA2APart());
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

    public static Part ToA2APart(this ChatMessagePart part)
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
            return new FilePart() { File = new FileWithBytes() { Bytes = part.Document.Base64 } };
        }
        else if (part.Audio != null)
        {
            return new FilePart() { File = new FileWithBytes() { Bytes = part.Audio.Data, MimeType = part.Audio.MimeType } };
        }
        else
        {
            throw new NotSupportedException("Unsupported ChatMessagePart type.");
        }
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
}