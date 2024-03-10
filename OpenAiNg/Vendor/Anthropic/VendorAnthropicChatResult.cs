using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OpenAiNg.Chat;

namespace OpenAiNg.Vendor.Anthropic;

internal class VendorAnthropicChatResult
{
    internal class VendorAnthropicChatResultContentBlock
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
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
            ProcessingTime = TimeSpan.Zero
        };

        foreach (VendorAnthropicChatResultContentBlock contentBlock in Content)
        {
            result.Choices.Add(new ChatChoice
            {
                Delta = null,
                FinishReason = StopReason,
                Index = result.Choices.Count + 1,
                Message = new ChatMessage(ChatMessageRole.Assistant, contentBlock.Text)
            });
        }

        return result;
    }
}