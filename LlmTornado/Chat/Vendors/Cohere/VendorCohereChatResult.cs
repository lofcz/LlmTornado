using System;
using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Cohere;

internal class VendorCohereChatResult : VendorChatResult
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
    [JsonProperty("citations")]
    public List<VendorCohereChatCitation>? Citations { get; set; }
    [JsonProperty("documents")]
    public List<VendorCohereChatDocument>? Documents { get; set; }
    [JsonProperty("search_results")]
    public List<VendorCohereChatSearchResult>? SearchResults { get; set; }
    [JsonProperty("search_queries")]
    public List<VendorCohereChatSearchQuery>? SearchQueries { get; set; }
    
    public override ChatResult ToChatResult(string? postData)
    {
        string model = ChatModel.Cohere.Command.Default;
        
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
            Object = string.Empty,
            VendorExtensions = new ChatResponseVendorExtensions
            {
                Cohere = new ChatResponseVendorCohereExtensions(this)
            }
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

        ChatResult = result;
        return result;
    }
}