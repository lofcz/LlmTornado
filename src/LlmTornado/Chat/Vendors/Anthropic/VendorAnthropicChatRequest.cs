using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

/// <summary>
/// Anthropic extensions to chat message parts.
/// </summary>
public class ChatMessagePartAnthropicExtensions : IChatMessagePartVendorExtensions
{
    /// <summary>
    /// Cache settings.
    /// </summary>
    public AnthropicCacheSettings? Cache { get; set; }
}

public partial class VendorAnthropicChatRequestMessageContent
{
    [JsonIgnore]
    internal ChatMessage Msg { get; set; }
    
    public List<ChatMessagePart> Parts { get; internal set; }
    public ChatMessageRoles Role { get; internal set; }
    
    public VendorAnthropicChatRequestMessageContent(ChatMessage msg)
    {
        Msg = msg;
        Parts = msg.Parts?.Count > 0 ? msg.Parts.ToList() : msg.Content is not null ? [
            new ChatMessagePart(msg.Content)
        ] : [];
        Role = msg.Role ?? ChatMessageRoles.Unknown;
    }

    public VendorAnthropicChatRequestMessageContent()
    {
        
    }

    internal partial class VendorAnthropicChatRequestMessageContentJsonConverter : JsonConverter<VendorAnthropicChatRequestMessageContent>
    {
        public override void WriteJson(JsonWriter writer, VendorAnthropicChatRequestMessageContent value, JsonSerializer serializer)
        {
            if (value.Msg.ChatMessageSerializeData is VendorAnthropicChatMessageToolResults vd)
            {
                writer.WriteStartArray();
                
                foreach (VendorAnthropicChatMessageToolResult block in vd.ToolResults)
                {
                    if (block.ToolCallId?.Length is 0)
                    {
                        continue;
                    }
                    
                    writer.WriteStartObject();
                    
                    writer.WritePropertyName("type");
                    writer.WriteValue("tool_result");
                    
                    writer.WritePropertyName("tool_use_id");
                    writer.WriteValue(block.ToolCallId);
                    
                    if (block.SourceMessage.FunctionCall?.Result?.RawContentBlocks is not null)
                    {
                        writer.WritePropertyName("content");    
                        writer.WriteStartArray();

                        foreach (IFunctionResultBlock resultBlock in block.SourceMessage.FunctionCall.Result.RawContentBlocks)
                        {
                            writer.Serialize(resultBlock);
                        }
                        
                        writer.WriteEndArray();
                    }
                    else if (block.Content is not null)
                    {
                        writer.WritePropertyName("content");
                        
                        if (block.Content.TrimStart().StartsWith('"'))
                        {
                            writer.WriteRawValue(block.Content);
                        }
                        else
                        {
                            writer.WriteValue(block.Content);   
                        }     
                    }

                    switch (block.ToolInvocationSucceeded)
                    {
                        case false:
                        {
                            writer.WritePropertyName("is_error");
                            writer.WriteValue(true);
                            break;
                        }
                        case true:
                        {
                            writer.WritePropertyName("is_error");
                            writer.WriteValue(false);
                            break;
                        }
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
                    
                    string? type = part.Type switch
                    {
                        ChatMessageTypes.Text => "text",
                        ChatMessageTypes.Image => "image",
                        _ => null
                    };

                    if (type is not null)
                    {
                        writer.WritePropertyName("type");
                        writer.WriteValue(type);
                    }
                    
                    switch (part.Type)
                    {
                        case ChatMessageTypes.Text:
                        {
                            writer.WritePropertyName("text");
                            writer.WriteValue(part.Text);
                         
                            if (part.Citations?.Count > 0)
                            {
                                writer.WritePropertyName("citations");
                                writer.WriteStartArray();
                                
                                foreach (IChatMessagePartCitation cit in part.Citations)
                                {
                                    cit.Serialize(LLmProviders.Anthropic, writer);
                                }
                                
                                writer.WriteEndArray();
                            }
                            break;
                        }
                        case ChatMessageTypes.Image:
                        {
                            if (part.Image is null)
                            {
                                throw new Exception("Image property of ChatMessagePart is null and cannot be encoded.");
                            }

                            writer.WritePropertyName("source");
                            writer.WriteStartObject();

                            bool dataPrefix = part.Image.Url.StartsWith("data:");
                            
                            if (dataPrefix || !Uri.TryCreate(part.Image.Url, UriKind.Absolute, out _))
                            {
                                if (part.Image.MimeType is null)
                                {
                                    throw new Exception("MIME type of the image must be set, supported values for Anthropic are: image/jpeg, image/png, image/gif, image/webp");
                                }

                                // anthropic expects bare64, remove the prefix
                                string img = part.Image.Url;
                                
                                if (dataPrefix)
                                {
#if MODERN
                                    img = Base64HeaderRegex().Replace(img, string.Empty, 1);

#else
                                    img = Base64HeaderRegex.Replace(img, string.Empty, 1);
#endif
                                    
                                }
                        
                                writer.WritePropertyName("type");
                                writer.WriteValue("base64");
                        
                                writer.WritePropertyName("media_type");
                                writer.WriteValue(part.Image.MimeType);
                            
                                writer.WritePropertyName("data");
                                writer.WriteValue(img);
                            }
                            else
                            {
                                writer.WritePropertyName("type");
                                writer.WriteValue("url");
     
                                writer.WritePropertyName("url");
                                writer.WriteValue(part.Image.Url);   
                            }
                        
                            writer.WriteEndObject();
                            break;
                        }
                        case ChatMessageTypes.Reasoning:
                        {
                            if (part.Reasoning is not null)
                            {
                                if (part.Reasoning.Content is not null)
                                {
                                    writer.WritePropertyName("type");
                                    writer.WriteValue("thinking");
                            
                                    writer.WritePropertyName("thinking");
                                    writer.WriteValue(part.Reasoning.Content);
                            
                                    writer.WritePropertyName("signature");
                                }
                                else
                                {
                                    writer.WritePropertyName("type");
                                    writer.WriteValue("redacted_thinking");
                                    
                                    writer.WritePropertyName("data");
                                }

                                writer.WriteValue(part.Reasoning.Signature);
                            }

                            break;
                        }
                        case ChatMessageTypes.Document:
                        {
                            if (part.Document is not null)
                            {
                                bool ok = part.Document.Base64 is not null || part.Document.Uri is not null;

                                if (!ok)
                                {
                                    break;
                                }
                                
                                writer.WritePropertyName("type");
                                writer.WriteValue("document");
                                
                                writer.WritePropertyName("source");
                                writer.WriteStartObject();
                                
                                if (part.Document.Base64 is not null)
                                {
                                    writer.WritePropertyName("media_type");
                                    writer.WriteValue("application/pdf");
                                    
                                    writer.WritePropertyName("type");
                                    writer.WriteValue("base64");
                                    
                                    writer.WritePropertyName("data");
                                    writer.WriteValue(part.Document.Base64);
                                }
                                else if (part.Document.Uri is not null)
                                {
                                    writer.WritePropertyName("type");
                                    writer.WriteValue("url");
                                    
                                    writer.WritePropertyName("url");
                                    writer.WriteValue(part.Document.Uri);
                                }
                                
                                writer.WriteEndObject();
                            }

                            break;
                        }
                        case ChatMessageTypes.FileLink:
                        {
                            writer.WritePropertyName("type");
                            writer.WriteValue("document");
                            
                            writer.WritePropertyName("source");
                            writer.WriteStartObject();
                            
                            writer.WritePropertyName("type");
                            writer.WriteValue("file");
                            
                            writer.WritePropertyName("file_id");
                            writer.WriteValue(part.FileLinkData?.File?.Id ?? part.FileLinkData?.FileUri);
                            
                            writer.WriteEndObject();
                            
                            break;
                        }
                        case ChatMessageTypes.ContainerUpload:
                        {
                            writer.WritePropertyName("type");
                            writer.WriteValue("container_upload");
                            
                            writer.WritePropertyName("file_id");
                            writer.WriteValue(part.ContainerUploadData?.FileId);
                            
                            if (part.ContainerUploadData?.Cache is not null)
                            {
                                writer.WritePropertyName("cache_control");
                                JToken cacheToken = JToken.FromObject(part.ContainerUploadData.Cache);
                                cacheToken.WriteTo(writer);
                            }
                            
                            break;
                        }
                        case ChatMessageTypes.SearchResult:
                        {
                            writer.WritePropertyName("type");
                            writer.WriteValue("search_result");

                            if (part.SearchResult is null)
                            {
                                throw new Exception("SearchResult of this part is empty, expected not null");
                            }
                            
                            writer.WritePropertyName("source");
                            writer.WriteValue(part.SearchResult.Source);
                            
                            writer.WritePropertyName("title");
                            writer.WriteValue(part.SearchResult.Title);
                            
                            writer.WritePropertyName("content");
                            writer.WriteStartArray();

                            foreach (ChatSearchResultContent item in part.SearchResult.Content)
                            {
                                writer.Serialize(item);
                            }
                            
                            writer.WriteEndArray();

                            if (part.SearchResult.Citations is not null)
                            {
                                writer.WritePropertyName("citations");
                                writer.WriteStartObject();
                                
                                writer.WritePropertyName("enabled");
                                writer.WriteValue(part.SearchResult.Citations.Enabled);
                                
                                writer.WriteEndObject();
                            }

                            if (part.SearchResult.Cache is not null)
                            {
                                writer.WritePropertyName("cache_control");
                                JToken cacheToken = JToken.FromObject(part.SearchResult.Cache);
                                cacheToken.WriteTo(writer);
                            }
                            
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
                    JToken cacheToken = JToken.FromObject(ac.Cache);
                    cacheToken.WriteTo(writer);
                }
            }
        }

        public override VendorAnthropicChatRequestMessageContent ReadJson(JsonReader reader, Type objectType, VendorAnthropicChatRequestMessageContent existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new VendorAnthropicChatRequestMessageContent();
        }

#if !MODERN
        private static readonly Regex Base64HeaderRegex = new Regex(@"^data:image\/[a-zA-Z]+;base64,", RegexOptions.Compiled);
#else
        [GeneratedRegex(@"^data:image\/[a-zA-Z]+;base64,", RegexOptions.Compiled)]
        private static partial Regex Base64HeaderRegex();
#endif

    }
}

internal class VendorAnthropicChatRequestMessage
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
    internal static readonly Dictionary<OutboundToolChoiceModes, string> ToolChoiceMap = new Dictionary<OutboundToolChoiceModes, string>(5)
    {
        { OutboundToolChoiceModes.Auto, "auto" },
        { OutboundToolChoiceModes.Legacy, "auto" },
        { OutboundToolChoiceModes.None, "none" },
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
        Model = request.Model?.Name ?? ChatModel.Anthropic.Claude4.Sonnet250514.Name;
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
                Type = ToolChoiceMap.GetValueOrDefault(request.ToolChoice.Mode) ?? "auto",
                Name = request.ToolChoice.Mode is OutboundToolChoiceModes.ToolFunction ? request.ToolChoice.Function?.Name : null
            };
        }

        if (request.Tools is not null)
        {
            Tools = request.Tools.Where(x => x.Function is not null).Select(t => new VendorAnthropicToolFunction(t)).ToList();
        }

        if (request.ReasoningBudget > 0)
        {
            Thinking = new VendorAnthropicThinkingSettings
            {
                BudgetTokens = request.ReasoningBudget,
                Type = "enabled"
            };
        }

        if (request.VendorExtensions?.Anthropic is not null)
        {
            if (request.VendorExtensions.Anthropic.BuiltInTools is not null)
            {
                Tools ??= [];
                Tools.AddRange(request.VendorExtensions.Anthropic.BuiltInTools.Select(x => new VendorAnthropicToolFunction(x)));
            }
            
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