using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Cohere;

[JsonConverter(typeof(StringEnumConverter))]
internal enum VendorCohereChatRole
{
    [EnumMember(Value = "assistant")]
    Assistant
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum VendorCohereContentType
{
    [EnumMember(Value = "text")]
    Text,

    [EnumMember(Value = "thinking")]
    Thinking
}

internal class VendorCohereToolSource
{
    [JsonProperty("type")]
    public string Type { get; set; } = "tool";

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("tool_output")]
    public Dictionary<string, object> ToolOutput { get; set; }
}

internal class VendorCohereDocumentSource
{
    [JsonProperty("type")]
    public string Type { get; set; } = "document";

    [JsonProperty("document")]
    public Dictionary<string, object> Document { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }
}

internal class VendorCohereChatResult : VendorChatResult
{
    [JsonProperty("id")]
    public string ResponseId { get; set; }

    [JsonProperty("message")]
    public VendorCohereMessage? Message { get; set; }

    [JsonProperty("finish_reason")]
    public VendorCohereChatFinishReason FinishReason { get; set; }

    [JsonProperty("usage")]
    public VendorCohereUsage? Usage { get; set; }

    [JsonProperty("logprobs")]
    public List<VendorCohereLogProbs>? Logprobs { get; set; }

    [JsonProperty("citations")]
    public List<VendorCohereChatCitation>? Citations { get; set; }

    /// <summary>
    /// A message from the assistant role can contain text and tool call information.
    /// </summary>
    public class VendorCohereMessage
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("role")]
        public VendorCohereChatRole Role { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("tool_calls")]
        public List<VendorCohereToolCall>? ToolCalls { get; set; }

        /// <summary>
        /// A chain-of-thought style reflection and plan that the model generates when working with Tools.
        /// </summary>
        [JsonProperty("tool_plan")]
        public string? ToolPlan { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("content")]
        public List<VendorCohereContent>? Content { get; set; }

        [JsonProperty("citations")]
        public List<VendorCohereChatCitation>? Citations { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class VendorCohereContent
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("thinking")]
        public string Thinking { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("type")]
        public VendorCohereContentType Type { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class VendorCohereToolCall
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("type")]
        public VendorCohereToolCallType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("function")]
        public VendorCohereToolCallFunction Function { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class VendorCohereToolCallFunction
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("arguments")]
        public string Arguments { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class VendorCohereChatCitation
    {
        [JsonProperty("start")]
        public int? Start { get; set; }

        [JsonProperty("end")]
        public int? End { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("sources")]
        public List<object> Sources { get; set; }

        [JsonProperty("content_index")]
        public int? ContentIndex { get; set; }

        [JsonProperty("type")]
        public VendorCohereCitationType Type { get; set; }
    }

    public static ChatResult? Deserialize(string json)
    {
        VendorCohereChatResult? resultEx = JsonConvert.DeserializeObject<VendorCohereChatResult>(json);
        return resultEx?.ToChatResult(null, null);
    }

    public override ChatResult ToChatResult(string? postData, object? chatRequest)
    {
        ChatResult result = new ChatResult
        {
            Id = ResponseId,
            RequestId = ResponseId,
            Choices = [],
            Usage = Usage is null ? null : new ChatUsage(Usage),
            ProcessingTime = TimeSpan.Zero,
            Object = string.Empty,
            VendorExtensions = new ChatResponseVendorExtensions
            {
                Cohere = new ChatResponseVendorCohereExtensions(this)
            }
        };

        if (Message is not null)
        {
            List<ToolCall>? toolCalls = Message.ToolCalls?.Select(x => new ToolCall
            {
                Id = x.Id,
                FunctionCall = new FunctionCall
                {
                    Arguments = x.Function.Arguments,
                    Name = x.Function.Name
                }
            }).ToList();

            if (toolCalls is not null)
            {
                foreach (ToolCall tc in toolCalls)
                {
                    if (tc.FunctionCall is not null)
                    {
                        tc.FunctionCall.ToolCall = tc;
                    }
                }
            }

            result.Choices.Add(new ChatChoice
            {
                FinishReason = FinishReason switch
                {
                    VendorCohereChatFinishReason.Complete => ChatMessageFinishReasons.EndTurn,
                    VendorCohereChatFinishReason.StopSequence => ChatMessageFinishReasons.EndTurn,
                    VendorCohereChatFinishReason.MaxTokens => ChatMessageFinishReasons.Length,
                    VendorCohereChatFinishReason.ToolCall => ChatMessageFinishReasons.ToolCalls,
                    VendorCohereChatFinishReason.Error => ChatMessageFinishReasons.Error,
                    _ => ChatMessageFinishReasons.Unknown
                },
                Message = new ChatMessage
                {
                    Parts = Message.Content?.Select(x =>
                    {
                        return x.Type switch
                        {
                            VendorCohereContentType.Text => new ChatMessagePart(x.Text),
                            VendorCohereContentType.Thinking => new ChatMessagePart { Type = ChatMessageTypes.Reasoning, Reasoning = new ChatMessageReasoningData { Content = x.Thinking } },
                            _ => new ChatMessagePart()
                        };
                    }).ToList(),
                    ToolCalls = toolCalls
                }
            });
        }

        ChatResult = result;
        return result;
    }
}