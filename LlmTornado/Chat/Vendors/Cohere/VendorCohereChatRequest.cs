using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Plugins;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Cohere;

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
        
        public VendorCohereChatRequestMessage(ChatMessageRoles role, ChatMessage msg)
        {
            Role = role switch
            {
                ChatMessageRoles.Assistant => "CHATBOT",
                ChatMessageRoles.System => "SYSTEM",
                ChatMessageRoles.User => "USER",
                _ => "USER"
            };

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
    [JsonProperty("connectors")]
    public List<ChatVendorCohereExtensionConnector>? Connectors { get; set; }
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
    [JsonProperty("tools")]
    public List<VendorCohereChatTool>? Tools { get; set; }
    [JsonProperty("tool_results")]
    public List<VendorCohereChatToolResult>? ToolResults { get; set; }

    public VendorCohereChatRequest()
    {
        
    }
    
    public VendorCohereChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        IList<ChatMessage>? msgs = request.Messages;
        string? preamble = null;
        string? respondMsg = null;
        ChatMessage? lastToolCallsMsg = null;
        
        if (msgs is not null)
        {
            foreach (ChatMessage msg in msgs)
            {
                msg.ExcludeFromRequest = false;
            }
            
            ChatMessage? systemMsg = msgs.FirstOrDefault(x => x.Role is ChatMessageRoles.System);

            if (systemMsg is not null)
            {
                systemMsg.ExcludeFromRequest = true;
                preamble = systemMsg.Content;
            }

            ChatMessage? lastMsg = msgs.LastOrDefault(x => x.Role is ChatMessageRoles.User);
            int? lastUserMsgIndex;
            
            if (lastMsg is not null)
            {
                lastMsg.ExcludeFromRequest = true;
                respondMsg = lastMsg.Content;
                lastUserMsgIndex = msgs.IndexOf(lastMsg);

                for (int i = 0; i < lastUserMsgIndex; i++)
                {
                    ChatMessage msg = msgs[i];

                    msg.ExcludeFromRequest = msg.Role switch
                    {
                        ChatMessageRoles.Tool => true,
                        ChatMessageRoles.Assistant when msg.ToolCalls?.Count > 0 => true,
                        _ => msg.ExcludeFromRequest
                    };
                }
            }

            lastToolCallsMsg = msgs.LastOrDefault(x => x is { Role: ChatMessageRoles.Assistant, ToolCalls.Count: > 0 });
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

        if (msgs is not null && request.Messages is not null && request.Messages.Any(x => !x.ExcludeFromRequest))
        {
            ChatHistory = [];
            int toolCallIndex = 0;
            ChatMessage? lastAssistantMsg = null;
            
            foreach (ChatMessage msg in msgs.Where(x => !x.ExcludeFromRequest))
            {
                switch (msg.Role)
                {
                    case ChatMessageRoles.Assistant:
                    {
                        if (msg.Content is not null && msg != lastToolCallsMsg)
                        {
                            ChatHistory.Add(new VendorCohereChatRequestMessage(ChatMessageRoles.Assistant, msg));
                        }

                        lastAssistantMsg = msg;
                        break;
                    }
                    case ChatMessageRoles.User:
                    {
                        ChatHistory.Add(new VendorCohereChatRequestMessage(ChatMessageRoles.User, msg));
                        break;
                    }
                    case ChatMessageRoles.System:
                    {
                        ChatHistory.Add(new VendorCohereChatRequestMessage(ChatMessageRoles.System, msg));
                        break;
                    }
                    case ChatMessageRoles.Tool:
                    {
                        if (lastAssistantMsg != lastToolCallsMsg)
                        {
                            continue;
                        }
                        
                        ToolResults ??= [];
                        ToolCall? toolCall = lastToolCallsMsg?.ToolCalls?.Count > toolCallIndex ? lastToolCallsMsg.ToolCalls[toolCallIndex] : null;

                        if (toolCall is not null)
                        {
                            VendorCohereChatToolResult vr = new VendorCohereChatToolResult
                            {
                                Call = new VendorCohereChatToolResultCallObject
                                {
                                    Name = toolCall.FunctionCall.Name,
                                    Parameters = toolCall.FunctionCall.Arguments.JsonDecode<object>()
                                },
                                Outputs = []
                            };
                            
                            if (msg.Content is not null)
                            {
                                vr.Outputs.Add(msg.Content.JsonDecode<object>());
                            }
                            
                            ToolResults.Add(vr);
                        }

                        toolCallIndex++;
                        break;
                    }
                }
            }

            foreach (ChatMessage msg in msgs)
            {
                msg.ExcludeFromRequest = false;
            }
        }

        if (request.VendorExtensions?.Cohere is not null)
        {
            if (request.VendorExtensions.Cohere.Connectors?.Count > 0)
            {
                Connectors = request.VendorExtensions.Cohere.Connectors;
            }
        }

        if (request.Tools is not null)
        {
            Tools = [];

            foreach (Tool tool in request.Tools)
            {
                if (tool.Function is null)
                {
                    continue;
                }
                
                VendorCohereChatTool cohereTool = new VendorCohereChatTool
                {
                    Name = tool.Function.Name,
                    Description = tool.Function.Description
                };

                if (tool.Function.RawParameters is ChatPluginFunctionTypeObject obj)
                {
                    foreach (ChatFunctionParam prop in obj.Properties)
                    {
                        cohereTool.ParameterDefinitions ??= [];

                        object compiled = prop.Type.Compile(ChatPluginCompileBackends.Python);

                        if (compiled is VendorCohereChatToolParameter param)
                        {
                            cohereTool.ParameterDefinitions.Add(prop.Name, param);
                        }
                        
                        cohereTool.ParameterDefinitions.Add(prop.Name, new VendorCohereChatToolParameter
                        {
                            Required = prop.Required
                        });
                    }
                }

                Tools.Add(cohereTool);
            }
        }
    }
 }