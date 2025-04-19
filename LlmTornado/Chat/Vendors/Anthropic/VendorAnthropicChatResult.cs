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
    internal class VendorAnthropicChatResultContentBlock
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
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
    /// "tool_use": the model invoked one or more tools
    /// </summary>
    [JsonProperty("stop_reason")]
    public string StopReason { get; set; }
    
    [JsonProperty("stop_sequence")]
    public string? StopSequence { get; set; }
    
    [JsonProperty("usage")]
    public VendorAnthropicUsage Usage { get; set; }
    
    public override ChatResult ToChatResult(string? postData)
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
                ChatMessage textBlockMsg = new ChatMessage(ChatMessageRoles.Assistant, [ new ChatMessagePart(contentBlock.Text ?? string.Empty) ] );

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
}