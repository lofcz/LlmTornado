using System;
using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Vendor.Anthropic;
using LlmTornado.Vendor.Google;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Cohere;

internal class VendorGoogleChatResult : VendorChatResult
{
    internal class VendorGoogleChatResultMessage
    {
        [JsonProperty("content")]
        public VendorGoogleChatRequest.VendorGoogleChatRequestMessage Content { get; set; }
        
        [JsonProperty("finishReason")]
        public string FinishReason { get; set; }
        
        [JsonProperty("index")]
        public int Index { get; set; }
        
        [JsonProperty("safetyRatings")]
        public List<VendorGoogleChatRequest.VendorGoogleChatRequestSafetySetting>? SafetyRatings { get; set; }
    }
    
    [JsonProperty("candidates")] 
    public List<VendorGoogleChatResultMessage> Candidates { get; set; } = [];
    
    [JsonProperty("usageMetadata")]
    public VendorGoogleUsage Meta { get; set; }
    
    [JsonProperty("promptFeedback")]
    public VendorGooglePromptFeedback? PromptFeedback { get; set; }
    
    public override ChatResult ToChatResult(string? postData)
    {
        ChatResult result = new ChatResult
        {
            Choices = [],
            Usage = new ChatUsage(Meta),
            ProcessingTime = TimeSpan.Zero
        };

        VendorGoogleChatRequest? request = null;
        
        if (postData is not null)
        {
            request = postData.JsonDecode<VendorGoogleChatRequest>();
        }

        foreach (VendorGoogleChatResultMessage candidate in Candidates)
        {
            ChatMessage msg = candidate.Content.ToChatMessage(request);
            
            result.Choices.Add(new ChatChoice
            {
                Message = msg,
                Delta = msg,
                FinishReason = candidate.FinishReason
            });
        }
        
        return result;
    }
}