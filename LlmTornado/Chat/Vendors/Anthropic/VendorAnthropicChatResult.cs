using System;
using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Vendor.Anthropic;
using Argon;

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
    }
    
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("role")]
    public string Role { get; set; }
    [JsonProperty("content")]
    public List<VendorAnthropicChatResultContentBlock> Content { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
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

        foreach (VendorAnthropicChatResultContentBlock contentBlock in Content)
        {
            ChatMessage blockMsg;
            
            if (contentBlock.Type == VendorAnthropicChatMessageTypes.ToolUse)
            {
                blockMsg = new ChatMessage(ChatMessageRoles.Tool)
                {
                    ToolCalls = [ ParseToolCall(contentBlock) ] // Claude3 models (Haiku, Sonnet, Opus) call tools one at a time.
                };
            }
            else
            {
                blockMsg = new ChatMessage(ChatMessageRoles.Assistant, contentBlock.Text ?? string.Empty);
            }
            
            result.Choices.Add(new ChatChoice
            {
                FinishReason = StopReason,
                Index = result.Choices.Count + 1,
                Message = blockMsg
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