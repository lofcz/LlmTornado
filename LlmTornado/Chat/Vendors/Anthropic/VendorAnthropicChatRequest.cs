using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;
using LlmTornado.Common;
using LlmTornado.Images;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Anthropic;

internal class VendorAnthropicChatRequest
{
    internal class VendorAnthropicChatRequestMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }
        [JsonProperty("content")]
        [JsonConverter(typeof(VendorAnthropicChatRequestMessageContent.VendorAnthropicChatRequestMessageContentJsonConverter))]
        public VendorAnthropicChatRequestMessageContent Content { get; set; }
        
        internal class VendorAnthropicChatRequestMessageContent
        {
            public ChatMessage Msg { get; set; }

            public VendorAnthropicChatRequestMessageContent(ChatMessage msg)
            {
                Msg = msg;
            }

            public VendorAnthropicChatRequestMessageContent()
            {
                
            }

            internal class VendorAnthropicChatRequestMessageContentJsonConverter : JsonConverter<VendorAnthropicChatRequestMessageContent>
            {
                public override void WriteJson(JsonWriter writer, VendorAnthropicChatRequestMessageContent value, JsonSerializer serializer)
                {
                    if (value.Msg.Parts?.Count > 0)
                    {
                        writer.WriteStartArray();
                        
                        foreach (ChatMessagePart part in value.Msg.Parts)
                        {
                            writer.WriteStartObject();
                            
                            writer.WritePropertyName("type");
                            writer.WriteValue(part.Type == ChatMessageTypes.Text ? "text" : "image");

                            if (part.Type == ChatMessageTypes.Text)
                            {
                                writer.WritePropertyName("text");
                                writer.WriteValue(part.Text);
                            }
                            else if (part.Type == ChatMessageTypes.Image)
                            {
                                writer.WritePropertyName("source");
                                writer.WriteStartObject();
                                
                                writer.WritePropertyName("type");
                                writer.WriteValue("base64");
                                
                                writer.WritePropertyName("media_type");
                                writer.WriteValue("image/png");
                                
                                // [todo] async pre-fetch url content, expects url to be base64 encoded img now
                                writer.WritePropertyName("data");
                                writer.WriteValue(part.Image?.Url ?? string.Empty);
                                
                                writer.WriteEndObject();
                            }
                            
                            writer.WriteEndObject();
                        }
                        
                        writer.WriteEndArray();
                    }
                    else if (value.Msg.ToolCallId is not null)
                    {
                        writer.WriteStartArray();
                        writer.WriteStartObject();
                        writer.WritePropertyName("type");
                        writer.WriteValue(VendorAnthropicChatMessageTypes.ToolResult);
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
                        writer.WriteRawValue(value.Msg.Content ?? string.Empty);
                    }
                    else
                    {
                        writer.WriteValue(value.Msg.Content ?? string.Empty);   
                    }
                }

                public override VendorAnthropicChatRequestMessageContent ReadJson(JsonReader reader, Type objectType, VendorAnthropicChatRequestMessageContent existingValue, bool hasExistingValue, JsonSerializer serializer)
                {
                    return new VendorAnthropicChatRequestMessageContent();
                }
            }
        }
        
        public VendorAnthropicChatRequestMessage(ChatMessageRole role, ChatMessage msg)
        {
            Role = role;
            Content = new VendorAnthropicChatRequestMessageContent(msg);
        }
    }

    internal class VendorAnthropicChatRequestMetadata
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; }
    }
    
    [JsonProperty("messages")]
    public List<VendorAnthropicChatRequestMessage> Messages { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("system")]
    public string? System { get; set; }
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
    [JsonProperty("tools")]
    public List<VendorAnthropicToolFunction>? Tools { get; set; }

    public VendorAnthropicChatRequest(ChatRequest request)
    {
        Model = request.Model?.Name ?? ChatModel.Anthropic.Claude3.Opus.Name;
        System = request.Messages?.FirstOrDefault(x => x.Role == ChatMessageRole.System)?.Content;
        MaxTokens = request.MaxTokens ?? 1024;
        StopSequences = request.StopSequence?.Split(',').ToList();
        Stream = request.Stream;
        Temperature = request.Temperature;
        TopP = request.TopP;
        TopK = null;
        Messages = [];

        if (request.Messages is not null)
        {
            foreach (ChatMessage msg in request.Messages)
            {
                if (msg.Role == ChatMessageRole.Assistant || msg.Role == ChatMessageRole.User)
                {
                    Messages.Add(new VendorAnthropicChatRequestMessage(msg.Role, msg));
                }
                else if (msg.Role == ChatMessageRole.Tool)
                {
                    Messages.Add(new VendorAnthropicChatRequestMessage(ChatMessageRole.User, msg));
                }
            }   
        }

        if (request.Tools is not null)
        {
            Stream = false; // Claude 3 models (Haiku, Sonnet, Opus) don't support streaming in conjunction with tools.
            Tools = request.Tools.Where(x => x.Function is not null).Select(t => new VendorAnthropicToolFunction(t.Function!)).ToList();
        }
    }
 }