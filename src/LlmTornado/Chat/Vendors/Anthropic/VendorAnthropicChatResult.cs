using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Anthropic;

internal class VendorAnthropicChatResult : VendorChatResult
{
    internal class VendorAnthropicChatResultContentBlockCitation
    {
        /// <summary>
        /// char_location / page_location / content_block_location / web_search_result_location / search_result_location
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("text")]
        public string? Text { get; set; }
        
        [JsonProperty("encrypted_index")]
        public string? EncryptedIndex { get; set; }
        
        [JsonProperty("cited_text")]
        public string? CitedText { get; set; }
        
        [JsonProperty("title")]
        public string? Title { get; set; }
        
        [JsonProperty("url")]
        public string? Url { get; set; }
        
        [JsonProperty("document_index")]
        public int? DocumentIndex { get; set; }
        
        [JsonProperty("document_title")]
        public string? DocumentTitle { get; set; }
        
        [JsonProperty("end_char_index")]
        public int? EndCharIndex { get; set; }
        
        [JsonProperty("start_char_index")]
        public int? StartCharIndex { get; set; }
        
        [JsonProperty("end_page_number")]
        public int? EndPageNumber { get; set; }
        
        [JsonProperty("start_page_number")]
        public int? StartPageNumber { get; set; }
        
        [JsonProperty("end_block_index")]
        public int? EndBlockIndex { get; set; }
        
        [JsonProperty("start_block_index")]
        public int? StartBlockIndex { get; set; }
        
        [JsonProperty("search_result_index")]
        public int? SearchResultIndex { get; set; }
        
        [JsonProperty("source")]
        public string? Source { get; set; }
    }
    
    internal class VendorAnthropicChatResultContentBlock
    {
        [JsonProperty("type")]
        public string Type { get; set; }
     
        [JsonProperty("cache_control")]
        public AnthropicCacheSettings? CacheControl { get; set; }
        
        [JsonProperty("citations")]
        public List<VendorAnthropicChatResultContentBlockCitation>? Citations { get; set; }
        
        /// <summary>
        /// Text block.
        /// </summary>
        [JsonProperty("text")]
        public string? Text { get; set; }
        
        /// <summary>
        /// Reasoning block.
        /// </summary>
        [JsonProperty("thinking")]
        public string? Thinking { get; set; }
        
        /// <summary>
        /// Tool out.
        /// </summary>
        [JsonProperty("id")]
        public string? Id { get; set; }
        
        /// <summary>
        /// Tool in.
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }
        
        /// <summary>
        /// JSON schema of tool out.
        /// </summary>
        [JsonProperty("input")]
        public object? Input { get; set; }
        
        /// <summary>
        /// Tool in name + nanoid to be referenced in the tool response.
        /// </summary>
        [JsonProperty("tool_use_id")]
        public string? ToolUseId { get; set; }
        
        /// <summary>
        /// Tool out response.
        /// </summary>
        [JsonProperty("content")]
        public string? Content { get; set; }
        
        /// <summary>
        /// Tool out invocation failed flag.
        /// </summary>
        [JsonProperty("is_error")]
        public bool? IsError { get; set; }
        
        /// <summary>
        /// Used by thinking blocks, this token must be passed in subsequent calls to verify COT hasn't been tampered with.
        /// </summary>
        [JsonProperty("signature")]
        public string? Signature { get; set; }
    }
    
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("type")]
    public string Type { get; set; }
    
    [JsonProperty("role")]
    public string Role { get; set; }
    
    [JsonProperty("content")] 
    public List<VendorAnthropicChatResultContentBlock> Content { get; set; } = [];
    
    [JsonProperty("model")]
    public string Model { get; set; }
    
    /// <summary>
    /// "end_turn": the model reached a natural stopping point<br/>
    /// "max_tokens": we exceeded the requested max_tokens or the model's maximum<br/>
    /// "stop_sequence": one of your provided custom stop_sequences was generated<br/>
    /// "tool_use": the model invoked one or more tools<br/>
    /// "model_context_window_exceeded": the model stopped because it reached context window limit
    /// </summary>
    [JsonProperty("stop_reason")]
    public string StopReason { get; set; }
    
    [JsonProperty("stop_sequence")]
    public string? StopSequence { get; set; }
    
    [JsonProperty("usage")]
    public VendorAnthropicUsage Usage { get; set; }
    
    public override ChatResult ToChatResult(string? postData, object? chatRequest)
    {
        ChatResult result = new ChatResult
        {
            Id = Id,
            RequestId = Id,
            Choices = [],
            Usage = new ChatUsage(Usage),
            Model = Model,
            ProcessingTime = TimeSpan.Zero,
            Object = JsonConvert.SerializeObject(Content, EndpointBase.NullSettings)
        };

        ChatMessage? toolsMsg = null;
        List<ChatChoiceAnthropicThinkingBlock>? thinkingBlocks = null;
        ChatChoice? textChoice = null;
        
        foreach (VendorAnthropicChatResultContentBlock contentBlock in Content) // we need to merge all tool blocks into one
        {
            VendorAnthropicChatMessageTypes type = VendorAnthropicChatMessageTypesCls.Map.GetValueOrDefault(contentBlock.Type, VendorAnthropicChatMessageTypes.Unknown);
                
            if (type is VendorAnthropicChatMessageTypes.ToolUse)
            {
                toolsMsg ??= new ChatMessage(ChatMessageRoles.Tool)
                {
                    ToolCalls = []
                };

                toolsMsg.ToolCalls?.Add(ParseToolCall(contentBlock));
            }
            else if (type is VendorAnthropicChatMessageTypes.Text)
            {
                ChatMessagePart textPart = new ChatMessagePart(contentBlock.Text ?? string.Empty);
                
                if (contentBlock.Citations?.Count > 0)
                {
                    List<IChatMessagePartCitation> convertedCitations = [];
                    
                    foreach (VendorAnthropicChatResultContentBlockCitation cit in contentBlock.Citations)
                    {
                        if (TryConvertCitation(cit, out IChatMessagePartCitation? conv) && conv is not null)
                        {
                            convertedCitations.Add(conv);
                        }
                    }

                    if (convertedCitations.Count > 0)
                    {
                        textPart.Citations = convertedCitations;
                    }
                }

                ChatMessage textBlockMsg = new ChatMessage(ChatMessageRoles.Assistant, [ textPart ] );

                textChoice = new ChatChoice
                {
                    FinishReason = ChatMessageFinishReasonsConverter.Map.GetValueOrDefault(StopReason, ChatMessageFinishReasons.Unknown),
                    Index = result.Choices.Count + 1,
                    Message = textBlockMsg,
                    Delta = textBlockMsg
                };
                
                result.Choices.Add(textChoice);
            }
            else if (type is VendorAnthropicChatMessageTypes.Thinking)
            {
                thinkingBlocks ??= [];
                thinkingBlocks.Add(new ChatChoiceAnthropicThinkingBlock
                {
                    Content = contentBlock.Thinking ?? string.Empty,
                    Signature = contentBlock.Signature ?? string.Empty
                });
            }
        }

        if (thinkingBlocks?.Count > 0)
        {
            if (textChoice?.Message is not null)
            {
                // we will prepend thinking blocks
                if (thinkingBlocks.Count > 1)
                {
                    thinkingBlocks.Reverse();
                }
                
                foreach (ChatChoiceAnthropicThinkingBlock x in thinkingBlocks)
                {
                    textChoice.Message.Parts ??= [];
                    
                    textChoice.Message.Parts.Insert(0, new ChatMessagePart
                    {
                        Type = ChatMessageTypes.Reasoning,
                        Reasoning = new ChatMessageReasoningData
                        {
                            Content = x.Content,
                            Signature = x.Signature
                        }
                    });
                }
            }
            else
            {
               // todo?
            }
        }

        if (toolsMsg is not null)
        {
            result.Choices.Add(new ChatChoice
            {
                FinishReason = ChatMessageFinishReasonsConverter.Map.GetValueOrDefault(StopReason, ChatMessageFinishReasons.Unknown),
                Index = result.Choices.Count + 1,
                Message = toolsMsg,
                Delta = toolsMsg
            });
        }

        ChatResult = result;
        return result;
    }

    private static ToolCall ParseToolCall(VendorAnthropicChatResultContentBlock contentBlock)
    {
        return new ToolCall
        {
            Id = contentBlock.Id ?? string.Empty,
            Type = "function",
            FunctionCall = new FunctionCall
            {
                Name = contentBlock.Name ?? string.Empty, // out tool name is equal to tool_use_id in Claude3 models
                Arguments = contentBlock.Input?.ToString() ?? string.Empty
            }
        };
    }

    private static bool TryConvertCitation(VendorAnthropicChatResultContentBlockCitation cit, out IChatMessagePartCitation? converted)
    {
        converted = null;
        switch (cit.Type)
        {
            case "char_location":
                converted = new ChatMessagePartCitationCharLocation
                {
                    CitedText = cit.CitedText ?? string.Empty,
                    DocumentIndex = cit.DocumentIndex ?? 0,
                    DocumentTitle = cit.DocumentTitle,
                    StartCharIndex = cit.StartCharIndex ?? 0,
                    EndCharIndex = cit.EndCharIndex ?? 0
                };
                break;
            case "page_location":
                converted = new ChatMessagePartCitationPageLocation
                {
                    CitedText = cit.CitedText ?? string.Empty,
                    DocumentIndex = cit.DocumentIndex ?? 0,
                    DocumentTitle = cit.DocumentTitle,
                    StartPageNumber = cit.StartPageNumber ?? 1,
                    EndPageNumber = cit.EndPageNumber ?? 1
                };
                break;
            case "content_block_location":
                converted = new ChatMessagePartCitationContentBlockLocation
                {
                    CitedText = cit.CitedText ?? string.Empty,
                    DocumentIndex = cit.DocumentIndex ?? 0,
                    DocumentTitle = cit.DocumentTitle,
                    StartBlockIndex = cit.StartBlockIndex ?? 0,
                    EndBlockIndex = cit.EndBlockIndex ?? 0
                };
                break;
            case "web_search_result_location":
                converted = new ChatMessagePartCitationWebSearchResultLocation
                {
                    CitedText = cit.CitedText ?? string.Empty,
                    EncryptedIndex = cit.EncryptedIndex ?? string.Empty,
                    Title = cit.Title,
                    Url = cit.Url ?? string.Empty
                };
                break;
            case "search_result_location":
                converted = new ChatMessagePartCitationSearchResultLocation
                {
                    CitedText = cit.CitedText ?? string.Empty,
                    Source = cit.Source ?? string.Empty,
                    Title = cit.Title,
                    SearchResultIndex = cit.SearchResultIndex ?? 0,
                    StartBlockIndex = cit.StartBlockIndex ?? 0,
                    EndBlockIndex = cit.EndBlockIndex ?? 0
                };
                break;
            default:
                return false;
        }

        return true;
    }
}