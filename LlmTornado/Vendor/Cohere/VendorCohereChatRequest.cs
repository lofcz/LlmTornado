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

namespace LlmTornado.Vendor.Cohere;

internal class VendorCohereChatRequest
{
    internal class VendorCohereChatRequestMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }
        [JsonProperty("message")]
        [JsonConverter(typeof(VendorCohereChatRequestMessageContent.VendorAnthropicChatRequestMessageContentJsonConverter))]
        public VendorCohereChatRequestMessageContent Content { get; set; }
        
        internal class VendorCohereChatRequestMessageContent
        {
            public ChatMessage Msg { get; set; }

            public VendorCohereChatRequestMessageContent(ChatMessage msg)
            {
                Msg = msg;
            }

            public VendorCohereChatRequestMessageContent()
            {
                
            }

            internal class VendorAnthropicChatRequestMessageContentJsonConverter : JsonConverter<VendorCohereChatRequestMessageContent>
            {
                public override void WriteJson(JsonWriter writer, VendorCohereChatRequestMessageContent value, JsonSerializer serializer)
                {
                    if (value.Msg.Parts?.Count > 0)
                    {
                        writer.WriteValue(string.Join(" ", value.Msg.Parts.Select(x => x.Text)));
                    }
                    else if (value.Msg.ToolCallId is not null)
                    {
                        // [todo]
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

                public override VendorCohereChatRequestMessageContent ReadJson(JsonReader reader, Type objectType, VendorCohereChatRequestMessageContent existingValue, bool hasExistingValue, JsonSerializer serializer)
                {
                    return new VendorCohereChatRequestMessageContent();
                }
            }
        }
        
        public VendorCohereChatRequestMessage(ChatMessageRole role, ChatMessage msg)
        {
            if (role.Equals(ChatMessageRole.Assistant))
            {
                Role = "CHATBOT";
            }
            else if (role.Equals(ChatMessageRole.System))
            {
                Role = "SYSTEM";
            }
            else if (role.Equals(ChatMessageRole.User))
            {
                Role = "USER";
            }
            
            Content = new VendorCohereChatRequestMessageContent(msg);
        }
    }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    [JsonProperty("chat_history")]
    public List<VendorCohereChatRequestMessage>? ChatHistory { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("preamble")]
    public string? Preamble { get; set; }
    [JsonProperty("prompt_truncation")]
    public string? PromptTruncation { get; set; }
    // [todo] connectors
    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; }
    [JsonProperty("max_input_tokens")]
    public int? MaxInputTokens { get; set; }
    [JsonProperty("stop_seqences")]
    public List<string>? StopSequences { get; set; }
    [JsonProperty("stream")]
    public bool? Stream { get; set; }
    [JsonProperty("temperature")]
    public double? Temperature { get; set; }
    [JsonProperty("P")]
    public double? P { get; set; }
    [JsonProperty("K")]
    public int? K { get; set; }
    [JsonProperty("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }
    [JsonProperty("presence_penalty")]
    public double? PresencePenalty { get; set; }
    // [todo] tools

    public VendorCohereChatRequest()
    {
        
    }
    
    public VendorCohereChatRequest(ChatRequest request)
    {
        IList<ChatMessage>? msgs = request.Messages;
        string? preamble = null;
        string? respondMsg = null;
        
        if (msgs is not null)
        {
            ChatMessage? systemMsg = msgs.FirstOrDefault(x => x.Role == ChatMessageRole.System);

            if (systemMsg is not null)
            {
                systemMsg.ExcludeFromRequest = true;
                preamble = systemMsg.Content;
            }

            ChatMessage? lastMsg = msgs.LastOrDefault(x => x.Role == ChatMessageRole.User || x.Role == ChatMessageRole.Assistant);

            if (lastMsg is not null)
            {
                lastMsg.ExcludeFromRequest = true;
                respondMsg = lastMsg.Content;
            }
        }

        Message = respondMsg;
        Model = request.Model?.Name ?? ChatModel.Cohere.CommandRPlus.Name;
        Preamble = preamble;
        MaxTokens = request.MaxTokens ?? 1024;
        StopSequences = request.StopSequence?.Split(',').ToList();
        Stream = request.Stream;
        Temperature = request.Temperature;
        P = request.TopP;
        ChatHistory = null;

        if (request.Messages is not null && request.Messages.Any(x => !x.ExcludeFromRequest))
        {
            foreach (ChatMessage msg in msgs.Where(x => !x.ExcludeFromRequest))
            {
                if (msg.Role == ChatMessageRole.Assistant || msg.Role == ChatMessageRole.User)
                {
                    ChatHistory.Add(new VendorCohereChatRequestMessage(msg.Role, msg));
                }
                else if (msg.Role == ChatMessageRole.System)
                {
                    ChatHistory.Add(new VendorCohereChatRequestMessage(ChatMessageRole.System, msg));
                }
                else if (msg.Role == ChatMessageRole.Tool)
                {
                    ChatHistory.Add(new VendorCohereChatRequestMessage(ChatMessageRole.User, msg));
                }
            }

            foreach (ChatMessage msg in msgs)
            {
                msg.ExcludeFromRequest = false;
            }
        }
    }
 }