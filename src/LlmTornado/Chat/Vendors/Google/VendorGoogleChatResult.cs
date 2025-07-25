using System;
using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Google;
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
        
        /// <summary>
        /// FINISH_REASON_UNSPECIFIED =	Default value. This value is unused.<br/>
        /// STOP = Natural stop point of the model or provided stop sequence.<br/>
        /// MAX_TOKENS = The maximum number of tokens as specified in the request was reached.<br/>
        /// SAFETY = The response candidate content was flagged for safety reasons.<br/>
        /// RECITATION = The response candidate content was flagged for recitation reasons.<br/>
        /// LANGUAGE = The response candidate content was flagged for using an unsupported language.<br/>
        /// OTHER = Unknown reason.<br/>
        /// BLOCKLIST = Token generation stopped because the content contains forbidden terms.<br/>
        /// PROHIBITED_CONTENT = Token generation stopped for potentially containing prohibited content.<br/>
        /// SPII = Token generation stopped because the content potentially contains Sensitive Personally Identifiable Information (SPII).<br/>
        /// MALFORMED_FUNCTION_CALL = The function call generated by the model is invalid.<br/>
        /// IMAGE_SAFETY = Token generation stopped because generated images contain safety violations.<br/>
        /// </summary>
        [JsonProperty("finishReason")]
        public string? FinishReason { get; set; }
        
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
    
    public override ChatResult ToChatResult(string? postData, object? requestObject)
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
            ChatMessage msg = requestObject is ChatRequest cr ? candidate.Content.ToChatMessage(request, cr) : candidate.Content.ToChatMessage(request, null);
            
            result.Choices.Add(new ChatChoice
            {
                Message = msg,
                Delta = msg,
                FinishReason = candidate.FinishReason is null ? null : ChatMessageFinishReasonsConverter.Map.GetValueOrDefault(candidate.FinishReason, ChatMessageFinishReasons.Unknown)
            });
        }
        
        return result;
    }
}