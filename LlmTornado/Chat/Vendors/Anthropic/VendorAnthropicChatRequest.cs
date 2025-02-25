using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Common;
using LlmTornado.Images;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Anthropic;

public interface IChatMessagePartVendorExtensions
{
    
}

/// <summary>
///     Tool functionality specific to a vendor
/// </summary>
public interface IToolVendorExtensions
{
    
}

public class AnthropicToolVendorExtensions : IToolVendorExtensions
{
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; } 
}

public class ToolVendorExtensions
{
    public AnthropicToolVendorExtensions? Anthropic { get; set; }
    
    public ToolVendorExtensions(AnthropicToolVendorExtensions anthropic)
    {
        Anthropic = anthropic;
    }
}

public class ChatMessagePartAnthropicExtensions : IChatMessagePartVendorExtensions
{
    public AnthropicCacheSettings? Cache { get; set; }
}

public class VendorAnthropicChatRequestMessageContent
{
    [JsonIgnore]
    internal ChatMessage Msg { get; set; }
    
    public List<ChatMessagePart> Parts { get; internal set; }
    public ChatMessageRoles Role { get; internal set; }
    
    public VendorAnthropicChatRequestMessageContent(ChatMessage msg)
    {
        Msg = msg;
        Parts = msg.Parts?.Count > 0 ? msg.Parts.ToList() : msg.Content is not null ? new List<ChatMessagePart>
        {
            new ChatMessagePart(msg.Content)
        } : [];
        Role = msg.Role ?? ChatMessageRoles.Unknown;
    }

    public VendorAnthropicChatRequestMessageContent()
    {
        
    }

    internal class VendorAnthropicChatRequestMessageContentJsonConverter : JsonConverter<VendorAnthropicChatRequestMessageContent>
    {
        public override void WriteJson(JsonWriter writer, VendorAnthropicChatRequestMessageContent value, JsonSerializer serializer)
        {
            if (value.Msg.ChatMessageSerializeData is VendorAnthropicChatMessageToolResults vd)
            {
                writer.WriteStartArray();
                
                foreach (VendorAnthropicChatMessageToolResult block in vd.ToolResults)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("type");
                    writer.WriteValue("tool_result");
                    writer.WritePropertyName("tool_use_id");
                    writer.WriteValue(block.ToolCallId);
                    writer.WritePropertyName("content");

                    if (block.Content.TrimStart().StartsWith('"')) // [todo] hack?
                    {
                        writer.WriteRawValue(block.Content);
                    }
                    else
                    {
                        writer.WriteValue(block.Content);   
                    }

                    if (block.ToolInvocationSucceeded is false)
                    {
                        writer.WritePropertyName("is_error");
                        writer.WriteValue(true);
                    } 
                
                    writer.WriteEndObject();
                }
                
                writer.WriteEndArray();
            }
            else if (value.Parts.Count > 0)
            {
                writer.WriteStartArray();
                
                foreach (ChatMessagePart part in value.Parts)
                {
                    writer.WriteStartObject();
                    
                    writer.WritePropertyName("type");
                    writer.WriteValue(part.Type is ChatMessageTypes.Text ? "text" : "image");

                    switch (part.Type)
                    {
                        case ChatMessageTypes.Text:
                        {
                            writer.WritePropertyName("text");
                            writer.WriteValue(part.Text);
                            break;
                        }
                        case ChatMessageTypes.Image:
                        {
                            if (part.Image is null)
                            {
                                throw new Exception("Image property of ChatMessagePart is null and cannot be encoded.");
                            }

                            if (part.Image.MimeType is null)
                            {
                                throw new Exception("MIME type of the image must be set, supported values for Anthropic are: image/jpeg, image/png, image/gif, image/webp");
                            }
                            
                            writer.WritePropertyName("source");
                            writer.WriteStartObject();
                        
                            writer.WritePropertyName("type");
                            writer.WriteValue("base64");
                        
                            writer.WritePropertyName("media_type");
                            writer.WriteValue(part.Image.MimeType);
                            
                            writer.WritePropertyName("data");
                            writer.WriteValue(part.Image.Url);
                        
                            writer.WriteEndObject();
                            break;
                        }
                    }
                    
                    SerializeCache(part);
                    
                    writer.WriteEndObject();
                }
                
                writer.WriteEndArray();
            }
            else if (value.Msg.ToolCallId is not null)
            {
                writer.WriteStartArray();
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue("tool_result");
                writer.WritePropertyName("tool_use_id");
                writer.WriteValue(value.Msg.ToolCallId);
                writer.WritePropertyName("content");
                writer.WriteRawValue(value.Msg.Content);

                if (value.Msg.ToolInvocationSucceeded is false)
                {
                    writer.WritePropertyName("is_error");
                    writer.WriteValue(true);
                } 
                
                writer.WriteEndObject();
                writer.WriteEndArray();
            }
            else if (value.Msg.ToolCalls?.Count > 0)
            {
                writer.WriteStartArray();
                
                if (value.Msg.Content is not null)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("type");
                    writer.WriteValue("text");
                    writer.WritePropertyName("text");
                    writer.WriteValue(value.Msg.Content);
                    writer.WriteEndObject();
                }

                foreach (ToolCall toolCall in value.Msg.ToolCalls)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("type");
                    writer.WriteValue("tool_use");
                    writer.WritePropertyName("id");
                    writer.WriteValue(toolCall.Id);
                    writer.WritePropertyName("name");
                    writer.WriteValue(toolCall.FunctionCall.Name);
                    writer.WritePropertyName("input");
                    writer.WriteRawValue(toolCall.FunctionCall.Arguments.IsNullOrWhiteSpace() ? "{}" : toolCall.FunctionCall.Arguments);
                    writer.WriteEndObject();
                }
                
                writer.WriteEndArray();
            }
            
            void SerializeCache(ChatMessagePart part)
            {
                if (part.VendorExtensions is ChatMessagePartAnthropicExtensions { Cache: not null } ac)
                {
                    writer.WritePropertyName("cache_control");
                    writer.WriteStartObject();
                    
                    writer.WritePropertyName("type");
                    writer.WriteValue(ac.Cache.Type);
                    
                    writer.WriteEndObject();
                }
            }
        }

        public override VendorAnthropicChatRequestMessageContent ReadJson(JsonReader reader, Type objectType, VendorAnthropicChatRequestMessageContent existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new VendorAnthropicChatRequestMessageContent();
        }
    }
}

    
public class VendorAnthropicChatRequestMessage
{
    [JsonProperty("role")]
    internal string Role { get; set; }
    [JsonProperty("content")]
    [JsonConverter(typeof(VendorAnthropicChatRequestMessageContent.VendorAnthropicChatRequestMessageContentJsonConverter))]
    public VendorAnthropicChatRequestMessageContent Content { get; set; }
        
    public VendorAnthropicChatRequestMessage(ChatMessageRoles role, ChatMessage msg)
    {
        Role = ChatMessageRolesCls.MemberToString(role) ?? "user";
        Content = new VendorAnthropicChatRequestMessageContent(msg);
    }
}

internal class VendorAnthropicChatRequest
{
    internal static Dictionary<OutboundToolChoiceModes, string> toolChoiceMap = new Dictionary<OutboundToolChoiceModes, string>
    {
        { OutboundToolChoiceModes.Auto, "auto" },
        { OutboundToolChoiceModes.Legacy, "auto" },
        { OutboundToolChoiceModes.None, "auto" },
        { OutboundToolChoiceModes.Required, "any" },
        { OutboundToolChoiceModes.ToolFunction, "tool" }
    };

    internal class VendorAnthropicChatRequestMetadata
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; }
    }

    internal class VendorAnthropicChatRequestToolChoice
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    internal class VendorAnthropicThinkingSettings
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("budget_tokens")]
        public int? BudgetTokens { get; set; }
    }
    
    [JsonProperty("messages")]
    public List<VendorAnthropicChatRequestMessage> Messages { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("system", Order = -2)]
    [JsonConverter(typeof(VendorAnthropicChatRequestMessageContent.VendorAnthropicChatRequestMessageContentJsonConverter))]
    public VendorAnthropicChatRequestMessageContent? System { get; set; }
    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; }
    [JsonProperty("metadata")]
    public VendorAnthropicChatRequestMetadata? Metadata { get; set; }
    [JsonProperty("stop_seqences")]
    public List<string>? StopSequences { get; set; }
    [JsonProperty("stream")]
    public bool? Stream { get; set; }
    [JsonProperty("temperature")]
    public double? Temperature { get; set; }
    [JsonProperty("top_p")]
    public double? TopP { get; set; }
    [JsonProperty("top_k")]
    public int? TopK { get; set; }
    [JsonProperty("thinking")]
    public VendorAnthropicThinkingSettings? Thinking { get; set; }
    [JsonProperty("tool_choice")]
    public VendorAnthropicChatRequestToolChoice? ToolChoice { get; set; }
    [JsonProperty("tools")]
    public List<VendorAnthropicToolFunction>? Tools { get; set; }

    public VendorAnthropicChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        Model = request.Model?.Name ?? ChatModel.Anthropic.Claude3.Opus.Name;
        MaxTokens = request.MaxTokens ?? 1024;
        StopSequences = request.StopSequence?.Split(',').ToList();
        Stream = request.Stream;
        Temperature = request.Temperature;
        TopP = request.TopP;
        TopK = null;
        Messages = [];
        System = null;
        
        if (request.Messages is not null)
        {
            ChatMessage? toolsMessage = null;
            
            foreach (ChatMessage msg in request.Messages)
            {
                switch (msg.Role)
                {
                    case ChatMessageRoles.Assistant or ChatMessageRoles.User:
                    {
                        if (toolsMessage is not null)
                        {
                            Messages.Add(new VendorAnthropicChatRequestMessage(ChatMessageRoles.User, toolsMessage));
                        }
                        
                        Messages.Add(new VendorAnthropicChatRequestMessage(msg.Role.Value, msg));
                        toolsMessage = null;
                        break;
                    }
                    case ChatMessageRoles.Tool: // multiple tool messages must be compressed into one
                    {
                        if (toolsMessage is null)
                        {
                            toolsMessage = msg;
                            toolsMessage.ChatMessageSerializeData = new VendorAnthropicChatMessageToolResults
                            {
                                ToolResults =
                                [
                                    new VendorAnthropicChatMessageToolResult(msg)
                                ]
                            };
                            
                            continue;
                        }

                        if (toolsMessage.ChatMessageSerializeData is VendorAnthropicChatMessageToolResults vd)
                        {
                            vd.ToolResults.Add(new VendorAnthropicChatMessageToolResult(msg));
                        }
                        
                        break;
                    }
                    case ChatMessageRoles.System:
                    {
                        System = new VendorAnthropicChatRequestMessageContent(msg);
                        break;
                    }
                }
            } 
            
            if (toolsMessage is not null)
            {
                Messages.Add(new VendorAnthropicChatRequestMessage(ChatMessageRoles.User, toolsMessage));
                toolsMessage = null;
            }
        }

        if (request.ToolChoice is not null)
        {
            ToolChoice = new VendorAnthropicChatRequestToolChoice
            {
                Type = toolChoiceMap.GetValueOrDefault(request.ToolChoice.Mode) ?? "auto",
                Name = request.ToolChoice.Mode is OutboundToolChoiceModes.ToolFunction ? request.ToolChoice.Function?.Name : null
            };
        }

        if (request.Tools is not null)
        {
            //Stream = false; // Claude 3 models (Haiku, Sonnet, Opus) don't support streaming in conjunction with tools.
            Tools = request.Tools.Where(x => x.Function is not null).Select(t => new VendorAnthropicToolFunction(t)).ToList();
        }

        if (request.VendorExtensions?.Anthropic is not null)
        {
            if (request.VendorExtensions.Anthropic.Thinking is not null)
            {
                if (request.VendorExtensions.Anthropic.Thinking.Enabled)
                {
                    Thinking = new VendorAnthropicThinkingSettings
                    {
                        BudgetTokens = request.VendorExtensions.Anthropic.Thinking.BudgetTokens,
                        Type = "enabled"
                    };

                    // if budget tokens are set, max tokens must also be set
                    if (MaxTokens < Thinking.BudgetTokens)
                    {
                        MaxTokens = Thinking.BudgetTokens.Value + 4_096;
                    }
                }
            }
            
            request.VendorExtensions.Anthropic.OutboundRequest?.Invoke(System, Messages.Select(x => x.Content).ToList(), Tools);
        }
    }
 }