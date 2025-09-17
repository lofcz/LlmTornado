using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code.Models;
using LlmTornado.Code.Sse;
using LlmTornado.Embedding;
using LlmTornado.Models.Vendors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToolCall = LlmTornado.ChatFunctions.ToolCall;
using VendorCohereChatCitation = LlmTornado.Chat.Vendors.Cohere.VendorCohereChatCitation;
using VendorCohereChatFinishReason = LlmTornado.Chat.Vendors.Cohere.VendorCohereChatFinishReason;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// Built-in Cohere provider.
/// </summary>
public class CohereEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
    private const string DoneString = "[DONE]";
    private const string Event = "event:";
    private const string Data = "data:";
    private const string StreamMsgStart = $"{Event} message_start";
    private const string StreamMsgStop = $"{Event} message_stop";
    private const string StreamPing = $"{Event} ping";
    private const string StreamContentBlockDelta = $"{Event} content_block_delta";
    private const string StreamContentBlockStart = $"{Event} content_block_start";
    private const string StreamContentBlockStop = $"{Event} content_block_stop";
    private static readonly HashSet<string> StreamSkip = [StreamMsgStart, StreamMsgStop, StreamPing];
    private static readonly HashSet<string> toolFinishReasons = [ "tool_use" ];
    
    public static Version OutboundVersion { get; set; } = OutboundDefaultVersion;
    public Func<CapabilityEndpoints, string?, RequestUrlContext, string>? UrlResolver { get; set; } 
    
    public Action<HttpRequestMessage, object?, bool>? RequestResolver { get; set; }
    
    public Action<JObject, RequestSerializerContext>? RequestSerializer { get; set; }
    
    private enum StreamNextAction
    {
        Read,
        BlockStart,
        BlockDelta,
        BlockStop,
        Skip,
        MsgStart
    }
    
    enum ChatStreamParsingStates
    {
        Text,
        Tools
    }
    
    public CohereEndpointProvider() : base()
    {
        Provider = LLmProviders.Cohere;
        StoreApiAuth();
    }

    private class AnthropicStreamBlockStart
    {
        public class AnthropicStreamBlockStartData
        {
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
        }
        
        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("content_block")]
        public AnthropicStreamBlockStartData ContentBlock { get; set; }
    }

    private class AnthropicStreamBlockDelta
    {
        public class AnthropicStreamBlockDeltaData
        {
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
        }
        
        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("delta")]
        public AnthropicStreamBlockDeltaData Delta { get; set; }
    }

    private class AnthropicStreamBlockStop
    {
        [JsonProperty("index")]
        public int Index { get; set; }
    }

    private class AnthropicStreamMsgStart
    {
        [JsonProperty("message")]
        public VendorAnthropicChatResult Message { get; set; }
    }

    /// <summary>
    /// Gets endpoint url for a given capability.
    /// </summary>
    public static string GetEndpointUrlFragment(CapabilityEndpoints endpoint)
    {
        return endpoint switch
        {
            CapabilityEndpoints.Chat => "chat",
            CapabilityEndpoints.Embeddings => "embed",
            CapabilityEndpoints.Models => "models",
            _ => throw new Exception($"Cohere doesn't support endpoint {endpoint}")
        };
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public override string ApiUrl(CapabilityEndpoints endpoint, string? url, IModel? model = null)
    {
        string eStr = GetEndpointUrlFragment(endpoint);
        return UrlResolver is not null ? string.Format(UrlResolver.Invoke(endpoint, url, new RequestUrlContext(eStr, url, model)), eStr, url, model?.Name) : $"https://api.cohere.ai/v2/{eStr}{url}";
    }

    enum ChatStreamEventTypes
    {
        Unknown,
        TextGeneration,
        SearchQueriesGeneration,
        SearchResults,
        StreamStart,
        StreamEnd,
        CitationGeneration,
        ToolCallsGeneration
    }

    static readonly Dictionary<string, ChatStreamEventTypes> EventsMap = new Dictionary<string, ChatStreamEventTypes>
    {
        { "stream-start", ChatStreamEventTypes.StreamStart },
        { "stream-end", ChatStreamEventTypes.StreamEnd },
        { "text-generation", ChatStreamEventTypes.TextGeneration },
        { "search-queries-generation", ChatStreamEventTypes.SearchQueriesGeneration },
        { "search-results", ChatStreamEventTypes.SearchResults },
        { "citation-generation", ChatStreamEventTypes.CitationGeneration },
        { "tool-calls-generation", ChatStreamEventTypes.ToolCallsGeneration }
    };

    internal class ChatTextGenerationEventData
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
    
    internal class ChatStreamStartEventData
    {
        [JsonProperty("generation_id")]
        public string GenerationId { get; set; }
    }
    
    internal class ChatSearchQueriesGenerationEventData
    {
        [JsonProperty("search_queries")]
        public List<VendorCohereChatSearchQuery> SearchQueries { get; set; }
    }
    
    internal class ChatSearchResultsEventData
    {
        [JsonProperty("search_results")]
        public List<VendorCohereChatSearchResult> SearchResults { get; set; }
    }

    internal class ChatCitationGenerationEventData
    {
        [JsonProperty("citations")]
        public List<VendorCohereChatCitation> Citations { get; set; }
    }
    
    internal class ToolCallsGenerationEventData
    {
        [JsonProperty("tool_calls")]
        public List<VendorCohereChatInboundTool> ToolCalls { get; set; }
    }

    internal class ChatStreamEventBase
    {
        [JsonProperty("is_finished")]
        public bool IsFinished { get; set; }
        [JsonProperty("event_type")]
        public string EventType { get; set; }
    }

    internal class ChatStreamEventComplete
    {
        [JsonProperty("response")]
        public VendorCohereChatResult? Response { get; set; }
    }

    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request, ChatStreamEventHandler? eventHandler)
    {
        StringBuilder? plaintextBuilder = null;
        StringBuilder? reasoningBuilder = null;
        ChatUsage? usage = null;
        ChatMessageFinishReasons finishReason = ChatMessageFinishReasons.Unknown;
        ChatResponseVendorExtensions? vendorExtensions = null;
        Dictionary<int, ToolCallInboundAccumulator> toolCallAccumulators = new Dictionary<int, ToolCallInboundAccumulator>();
        List<ToolCall> finalizedToolCalls = [];

        #if DEBUG
        List<string> data = [];
        #endif
        
        await foreach (SseItem<string> item in SseParser.Create(reader.BaseStream).EnumerateAsync(request.CancellationToken))
        {
            #if DEBUG
            data.Add(item.Data);
            #endif
            
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (item.Data is null || item.Data.Length is 0)
            {
                continue;
            }

            CohereChatStreamEvent? streamEvent = JsonConvert.DeserializeObject<CohereChatStreamEvent>(item.Data);

            if (streamEvent is null)
            {
                continue;
            }

            switch (streamEvent.Type)
            {
                case CohereChatStreamEventType.MessageStart:
                {
                    break;
                }
                case CohereChatStreamEventType.ContentStart:
                {
                    break;
                }
                case CohereChatStreamEventType.ContentDelta:
                {
                    if (streamEvent is ContentDeltaEvent contentDeltaEvent)
                    {
                        List<ChatMessagePart> parts = [];
                        
                        if (contentDeltaEvent.Delta?.Message?.Content?.Text is not null)
                        {
                            plaintextBuilder ??= new StringBuilder();
                            plaintextBuilder.Append(contentDeltaEvent.Delta.Message.Content.Text);
                            
                            parts.Add(new ChatMessagePart(ChatMessageTypes.Text)
                            {
                                Text = contentDeltaEvent.Delta.Message.Content.Text
                            });
                        }
                        
                        if (contentDeltaEvent.Delta?.Message?.Content?.Thinking is not null)
                        {
                            reasoningBuilder ??= new StringBuilder();
                            reasoningBuilder.Append(contentDeltaEvent.Delta.Message.Content.Thinking);
                            
                            parts.Add(new ChatMessagePart(ChatMessageTypes.Reasoning)
                            {
                                Reasoning = new ChatMessageReasoningData
                                {
                                    Content = contentDeltaEvent.Delta.Message.Content.Thinking,
                                    Provider = LLmProviders.Cohere
                                }
                            });
                        }
                        
                        yield return new ChatResult
                        {
                            Choices =
                            [
                                new ChatChoice
                                {
                                    Delta = new ChatMessage
                                    {
                                        Parts = parts,
                                        Role = ChatMessageRoles.Assistant
                                    }
                                }
                            ]
                        };
                    }
                    break;
                }
                case CohereChatStreamEventType.ContentEnd:
                {
                    break;
                }
                case CohereChatStreamEventType.ToolPlanDelta:
                {
                    if (streamEvent is ToolPlanDeltaEvent toolPlanDeltaEvent)
                    {
                        if (toolPlanDeltaEvent.Delta?.Message?.ToolPlan is not null)
                        {
                            reasoningBuilder ??= new StringBuilder();
                            reasoningBuilder.Append(toolPlanDeltaEvent.Delta.Message.ToolPlan);
                        }
                    }
                    break;
                }
                case CohereChatStreamEventType.ToolCallStart:
                {
                    if (streamEvent is ToolCallStartEvent toolCallStartEvent && toolCallStartEvent.Index.HasValue)
                    {
                        VendorCohereToolCall? toolCallData = toolCallStartEvent.Delta?.Message?.ToolCalls;
                        
                        if (toolCallData?.Function is not null)
                        {
                            ToolCall toolCall = new ToolCall
                            {
                                Id = toolCallData.Id,
                                FunctionCall = new ChatFunctions.FunctionCall
                                {
                                    Name = toolCallData.Function.Name,
                                    Arguments = toolCallData.Function.Arguments
                                }
                            };
                            
                            toolCallAccumulators[toolCallStartEvent.Index.Value] = new ToolCallInboundAccumulator
                            {
                                ToolCall = toolCall,
                                ArgumentsBuilder = new StringBuilder(toolCall.FunctionCall.Arguments ?? string.Empty)
                            };
                        }
                    }
                    break;
                }
                case CohereChatStreamEventType.ToolCallDelta:
                {
                    if (streamEvent is ToolCallDeltaEvent toolCallDeltaEvent && toolCallDeltaEvent.Index.HasValue)
                    {
                        if (toolCallAccumulators.TryGetValue(toolCallDeltaEvent.Index.Value, out ToolCallInboundAccumulator? accumulator))
                        {
                            accumulator.ArgumentsBuilder.Append(toolCallDeltaEvent.Delta?.Message?.ToolCalls?.Function?.Arguments);
                        }
                    }
                    break;
                }
                case CohereChatStreamEventType.ToolCallEnd:
                {
                    if (streamEvent is ToolCallEndEvent toolCallEndEvent && toolCallEndEvent.Index.HasValue)
                    {
                        if (toolCallAccumulators.Remove(toolCallEndEvent.Index.Value, out ToolCallInboundAccumulator? accumulator))
                        {
                            accumulator.ToolCall.FunctionCall!.Arguments = accumulator.ArgumentsBuilder.ToString();
                            finalizedToolCalls.Add(accumulator.ToolCall);
                        }
                    }
                    break;
                }
                case CohereChatStreamEventType.CitationStart:
                {
                    break;
                }
                case CohereChatStreamEventType.CitationEnd:
                {
                    break;
                }
                case CohereChatStreamEventType.MessageEnd:
                {
                    if (streamEvent is MessageEndEvent messageEndEvent)
                    {
                        if (messageEndEvent.Delta?.Usage is not null)
                        {
                            usage = new ChatUsage(messageEndEvent.Delta.Usage);
                        }

                        if (messageEndEvent.Delta?.FinishReason is not null)
                        {
                            finishReason = messageEndEvent.Delta.FinishReason.Value switch
                            {
                                VendorCohereChatFinishReason.Complete => ChatMessageFinishReasons.EndTurn,
                                VendorCohereChatFinishReason.StopSequence => ChatMessageFinishReasons.EndTurn,
                                VendorCohereChatFinishReason.MaxTokens => ChatMessageFinishReasons.Length,
                                VendorCohereChatFinishReason.ToolCall => ChatMessageFinishReasons.ToolCalls,
                                VendorCohereChatFinishReason.Error => ChatMessageFinishReasons.Error,
                                _ => ChatMessageFinishReasons.Unknown
                            };
                        }
                    }
                    goto afterStreamEnds;
                }
                case CohereChatStreamEventType.Debug:
                {
                    break;
                }
            }
        }
        
        afterStreamEnds:

        foreach (KeyValuePair<int, ToolCallInboundAccumulator> entry in toolCallAccumulators)
        {
            entry.Value.ToolCall.FunctionCall!.Arguments = entry.Value.ArgumentsBuilder.ToString();
            finalizedToolCalls.Add(entry.Value.ToolCall);
        }

        if (finalizedToolCalls.Count > 0)
        {
            yield return new ChatResult
            {
                Choices =
                [
                    new ChatChoice
                    {
                        Delta = new ChatMessage
                        {
                            ToolCalls = finalizedToolCalls,
                            Role = ChatMessageRoles.Assistant
                        },
                        FinishReason = ChatMessageFinishReasons.ToolCalls
                    }
                ],
                Usage = usage
            };
        }

        string? accuPlaintext = plaintextBuilder?.ToString();
        string? reasoningPlaintext = reasoningBuilder?.ToString();
        
        if (accuPlaintext is not null || reasoningPlaintext is not null)
        {
            yield return new ChatResult
            {
                Usage = usage,
                Choices =
                [
                    new ChatChoice
                    {
                        Delta = new ChatMessage
                        {
                            Content = accuPlaintext,
                            ReasoningContent = reasoningPlaintext,
                            Role = ChatMessageRoles.Assistant
                        }
                    }
                ],
                StreamInternalKind = ChatResultStreamInternalKinds.AppendAssistantMessage
            };
        }

        if (vendorExtensions is not null)
        {
            yield return new ChatResult
            {
                VendorExtensions = vendorExtensions
            };
        }

        yield return new ChatResult
        {
            Usage = usage,
            Choices = [
                new ChatChoice
                {
                    FinishReason = finishReason
                }
            ],
            StreamInternalKind = ChatResultStreamInternalKinds.FinishData
        };
    }

    public override async IAsyncEnumerable<object?> InboundStream(Type type, StreamReader reader)
    {
        yield break;
    }
    
    public override async IAsyncEnumerable<T?> InboundStream<T>(StreamReader reader) where T : class
    {
        yield break;
    }

    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming)
    {
        HttpRequestMessage req = new HttpRequestMessage(verb, url) 
        {
            Version = OutboundVersion
        };
        req.Headers.Add("User-Agent", EndpointBase.GetUserAgent());

        ProviderAuthentication? auth = Api?.GetProvider(LLmProviders.Cohere).Auth;
        
        if (auth?.ApiKey is not null)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.ApiKey.Trim());
        }

        return req;
    }

    public override void ParseInboundHeaders<T>(T res, HttpResponseMessage response)
    {
        res.Provider = this;
    }
    
    public override void ParseInboundHeaders(object? res, HttpResponseMessage response)
    {
        
    }

    private static readonly Dictionary<Type, Func<string, string?, object?, object?>> inboundMessageHandlers = new Dictionary<Type, Func<string, string?, object?, object?>>
    {
        { typeof(ChatResult), (jsonData, postData, req) => ChatResult.Deserialize(LLmProviders.Cohere, jsonData, postData, req) },
        { typeof(EmbeddingResult), (jsonData, postData, req) => EmbeddingResult.Deserialize(LLmProviders.Cohere, jsonData, postData) },
        { typeof(RetrievedModelsResult), (jsonData, postData, req) => RetrievedModelsResult.Deserialize(LLmProviders.Cohere, jsonData, postData) }
    };
    
    public override T? InboundMessage<T>(string jsonData, string? postData, object? requestObject) where T : default
    {
        if (inboundMessageHandlers.TryGetValue(typeof(T), out Func<string, string?, object?, object?>? fn))
        {
            object? result = fn.Invoke(jsonData, postData, requestObject);

            if (result is null)
            {
                return default;
            }

            return (dynamic)result;
        }

        return default;
    }
    
    public override object? InboundMessage(Type type, string jsonData, string? postData, object? requestObject)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}