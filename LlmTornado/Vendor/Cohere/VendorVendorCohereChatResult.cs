using System;
using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Vendor.Cohere;

internal class VendorCohereChatResult
{
    [JsonProperty("response_id")]
    public string ResponseId { get; set; }
    [JsonProperty("text")]
    public string? Text { get; set; }
    [JsonProperty("generation_id")]
    public string GenerationId { get; set; }
    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
    [JsonProperty("meta")]
    public VendorCohereUsage Meta { get; set; }
    
    public ChatResult ToChatResult(string? postData)
    {
        string model = ChatModel.Cohere.CommandRPlus;
        
        if (postData is not null)
        {
            // [todo] crashes on deserialization, the content is not the type specified here, prolly one wrapper deeper
            /*VendorCohereChatRequest? request = JsonConvert.DeserializeObject<VendorCohereChatRequest>(postData);

            if (request is not null)
            {
                model = request.Model;
            }*/
        }
        
        ChatResult result = new ChatResult
        {
            Id = ResponseId,
            RequestId = ResponseId,
            Choices = [],
            Usage = new ChatUsage(Meta),
            Model = model,
            ProcessingTime = TimeSpan.Zero,
            Object = string.Empty
        };

        if (Text is not null)
        {
            result.Choices.Add(new ChatChoice
            {
                FinishReason = FinishReason,
                Message = new ChatMessage
                {
                    Content = Text
                },
                Delta = new ChatMessage
                {
                    Content = Text
                }
            });
        }

        return result;
    }
}