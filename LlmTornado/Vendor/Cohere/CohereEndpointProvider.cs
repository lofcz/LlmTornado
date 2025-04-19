using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Embedding;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// 
/// </summary>
internal class CohereEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
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
    
    public static Version OutboundVersion { get; set; } = HttpVersion.Version20;
    public override HashSet<string> ToolFinishReasons => toolFinishReasons;
    public Func<CapabilityEndpoints, string?, string>? UrlResolver { get; set; } 
    
    private enum StreamNextAction
    {
        Read,
        BlockStart,
        BlockDelta,
        BlockStop,
        Skip,
        MsgStart
    }
    
    public CohereEndpointProvider(TornadoApi api) : base(api)
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
    /// 
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public override string ApiUrl(CapabilityEndpoints endpoint, string? url)
    {
        string eStr = endpoint switch
        {
            CapabilityEndpoints.Chat => "chat",
            CapabilityEndpoints.Embeddings => "embed",
            _ => throw new Exception($"Cohere doesn't support endpoint {endpoint}")
        };

        return UrlResolver is not null ? UrlResolver.Invoke(endpoint, url) : $"https://api.cohere.ai/v1/{eStr}{url}";
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

    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request)
    {
        ChatResult baseResult = new ChatResult();
        ChatMessage? plaintextAccu = null;
        ChatUsage? usage = null;
        ChatMessageFinishReasons finishReason = ChatMessageFinishReasons.Unknown;    
        
        while (true)
        {
            string? line = await reader.ReadLineAsync(); // note this is not sse like openai/anthropic

            if (line is null)
            {
                yield break;
            }
            
            if (line.IsNullOrWhiteSpace())
            {
                continue;
            }
            
            ChatStreamEventBase? baseEvent = JsonConvert.DeserializeObject<ChatStreamEventBase>(line);

            if (baseEvent is null)
            {
                continue;
            }

            if (!EventsMap.TryGetValue(baseEvent.EventType, out ChatStreamEventTypes eventType))
            {
                continue;
            }

            switch (eventType)
            {
                case ChatStreamEventTypes.TextGeneration:
                {
                    ChatTextGenerationEventData? data = JsonConvert.DeserializeObject<ChatTextGenerationEventData>(line);

                    if (data is null)
                    {
                        continue;
                    }

                    yield return new ChatResult
                    {
                        Choices =
                        [
                            new ChatChoice
                            {
                                Delta = new ChatMessage(ChatMessageRoles.Assistant, data.Text)
                            }
                        ]
                    };
                    
                    plaintextAccu ??= new ChatMessage();
                    plaintextAccu.ContentBuilder ??= new StringBuilder();
                    plaintextAccu.ContentBuilder.Append(data.Text);
                    break;
                }
                case ChatStreamEventTypes.StreamStart:
                {
                    ChatStreamStartEventData? data = JsonConvert.DeserializeObject<ChatStreamStartEventData>(line);

                    if (data is null)
                    {
                        continue;
                    }

                    baseResult.Id = data.GenerationId;
                    break;
                }
                case ChatStreamEventTypes.SearchQueriesGeneration:
                {
                    ChatSearchQueriesGenerationEventData? data = JsonConvert.DeserializeObject<ChatSearchQueriesGenerationEventData>(line);

                    if (data is null)
                    {
                        continue;
                    }

                    yield return new ChatResult
                    {
                        VendorExtensions = new ChatResponseVendorExtensions
                        {
                            Cohere = new ChatResponseVendorCohereExtensions(new VendorCohereChatResult
                            {
                                SearchQueries = data.SearchQueries
                            })
                        }
                    };
                    
                    break;
                }
                case ChatStreamEventTypes.SearchResults:
                {
                    ChatSearchResultsEventData? data = JsonConvert.DeserializeObject<ChatSearchResultsEventData>(line);

                    if (data is null)
                    {
                        continue;
                    }

                    yield return new ChatResult
                    {
                        VendorExtensions = new ChatResponseVendorExtensions
                        {
                            Cohere = new ChatResponseVendorCohereExtensions(new VendorCohereChatResult
                            {
                                SearchResults = data.SearchResults
                            })
                        }
                    };
                    
                    break;
                }
                case ChatStreamEventTypes.CitationGeneration:
                {
                    ChatCitationGenerationEventData? data = JsonConvert.DeserializeObject<ChatCitationGenerationEventData>(line);

                    if (data is null)
                    {
                        continue;
                    }

                    yield return new ChatResult
                    {
                        VendorExtensions = new ChatResponseVendorExtensions
                        {
                            Cohere = new ChatResponseVendorCohereExtensions(new VendorCohereChatResult
                            {
                                Citations = data.Citations
                            })
                        }
                    };
                    
                    break;
                }
                case ChatStreamEventTypes.ToolCallsGeneration:
                {
                    ToolCallsGenerationEventData? data = JsonConvert.DeserializeObject<ToolCallsGenerationEventData>(line);

                    if (data is null)
                    {
                        continue;
                    }

                    List<ToolCall> calls = [];

                    foreach (VendorCohereChatInboundTool x in data.ToolCalls)
                    {
                        ToolCall call = new ToolCall
                        {
                            Type = "function",
                            Id = $"{x.Name}{General.IIID()}", // cohere doesn't return a unique tool ID, so we make one up
                            FunctionCall = new FunctionCall
                            {
                                Name = x.Name,
                                Arguments = x.Parameters.ToJson()
                            }
                        };
                        
                        calls.Add(call);
                    }
                    
                    yield return new ChatResult
                    {
                        Choices = [
                            new ChatChoice
                            {
                                Delta = new ChatMessage(ChatMessageRoles.Tool)
                                {
                                    ToolCalls = calls
                                }
                            }
                        ]
                    };
                    
                    break;
                }
                case ChatStreamEventTypes.StreamEnd:
                {
                    ChatStreamEventComplete? result = JsonConvert.DeserializeObject<ChatStreamEventComplete>(line);

                    if (result?.Response is not null)
                    {
                        usage = new ChatUsage(LLmProviders.Cohere)
                        {
                            TotalTokens = result.Response.Meta.BilledUnits.InputTokens + result.Response.Meta.BilledUnits.OutputTokens,
                            PromptTokens = result.Response.Meta.BilledUnits.InputTokens,
                            CompletionTokens = result.Response.Meta.BilledUnits.OutputTokens
                        };

                        finishReason = ChatMessageFinishReasonsConverter.Map.GetValueOrDefault(result.Response.FinishReason, ChatMessageFinishReasons.Unknown);
                    }
                    
                    goto finalizer;
                }
            }
            
            if (baseEvent.IsFinished)
            {
                goto finalizer;
            }
        }
     
        finalizer:
        if (plaintextAccu is not null)
        {
            yield return new ChatResult
            {
                Choices =
                [
                    new ChatChoice
                    {
                        Delta = new ChatMessage
                        {
                            Content = plaintextAccu.ContentBuilder?.ToString()
                        }
                    }
                ],
                StreamInternalKind = ChatResultStreamInternalKinds.AppendAssistantMessage,
                Usage = usage
            };
        }
        
        yield return new ChatResult
        {
            Choices =
            [
                new ChatChoice
                {
                    FinishReason = finishReason
                }
            ],
            StreamInternalKind = ChatResultStreamInternalKinds.FinishData,
            Usage = usage
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

        ProviderAuthentication? auth = Api.GetProvider(LLmProviders.Cohere).Auth;
        
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

    private static readonly Dictionary<Type, Func<string, string?, object?>> inboundMessageHandlers = new Dictionary<Type, Func<string, string?, object?>>
    {
        { typeof(ChatResult), (jsonData, postData) => ChatResult.Deserialize(LLmProviders.Cohere, jsonData, postData) },
        { typeof(EmbeddingResult), (jsonData, postData) => EmbeddingResult.Deserialize(LLmProviders.Cohere, jsonData, postData) }
    };
    
    public override T? InboundMessage<T>(string jsonData, string? postData) where T : default
    {
        if (inboundMessageHandlers.TryGetValue(typeof(T), out Func<string, string?, object?>? fn))
        {
            object? result = fn.Invoke(jsonData, postData);

            if (result is null)
            {
                return default;
            }

            return (dynamic)result;
        }

        return default;
    }
    
    public override object? InboundMessage(Type type, string jsonData, string? postData)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}