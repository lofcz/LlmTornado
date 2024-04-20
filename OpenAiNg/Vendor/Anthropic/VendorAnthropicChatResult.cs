using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OpenAiNg.Chat;
using OpenAiNg.ChatFunctions;

namespace OpenAiNg.Vendor.Anthropic;

internal class VendorAnthropicChatResult
{
    internal class VendorAnthropicChatResultContentBlock
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("input")]
        public object Arguments { get; set; }
        [JsonProperty("tool_use_id")]
        public string Tool_use_id { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }

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
    
    public ChatResult ToChatResult()
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
                result.Choices.Add(new ChatChoice
                {
                    Delta = null,
                    FinishReason = StopReason,
                    Index = result.Choices.Count + 1,
                    Message = contentBlock.Type == VendorAnthropicChatMessageTypes.ToolUse
                        ? new ChatMessage(ChatMessageRole.Tool, (string?)null) { ToolCalls = CreateToolCals(contentBlock) } 
                        : new ChatMessage(ChatMessageRole.Assistant, contentBlock.Text)
                });
        }

        return result;
    }

    private List<ToolCall> CreateToolCals(VendorAnthropicChatResultContentBlock contentBlock)
    {
        var toolCall = new ToolCall()
        {
            Id = contentBlock.Id,
            Type = "function",
            FunctionCall = new FunctionCall()
            {
                Name = contentBlock.Name,
                Arguments = contentBlock.Arguments.ToString()
            }
        };
        return new List<ToolCall>(){ toolCall };
    }
}