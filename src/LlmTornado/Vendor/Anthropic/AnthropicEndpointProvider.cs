using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code.Models;
using LlmTornado.Code.Sse;
using LlmTornado.Threads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FunctionCall = LlmTornado.ChatFunctions.FunctionCall;
using ToolCall = LlmTornado.ChatFunctions.ToolCall;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// Built-in Anthropic provider.
/// </summary>
public class AnthropicEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
    private const string StreamMsgStart = $"message_start";
    private const string StreamMsgStop = $"message_stop";
    private const string StreamMsgDelta = $"message_delta";
    private const string StreamError = $"error";
    private const string StreamPing = $"ping";
    private const string StreamContentBlockDelta = $"content_block_delta";
    private const string StreamContentBlockStart = $"content_block_start";
    private const string StreamContentBlockStop = $"content_block_stop";

    private static readonly Dictionary<string, StreamRawActions> StreamEventsMap = new Dictionary<string, StreamRawActions>
    {
        { StreamMsgStart, StreamRawActions.MsgStart },
        { StreamMsgStop, StreamRawActions.MsgStop },
        { StreamMsgDelta, StreamRawActions.MsgDelta },
        { StreamError, StreamRawActions.Error },
        { StreamPing, StreamRawActions.Ping },
        { StreamContentBlockDelta, StreamRawActions.ContentBlockDelta },
        { StreamContentBlockStart, StreamRawActions.ContentBlockStart },
        { StreamContentBlockStop, StreamRawActions.ContentBlockStop }
    };


    public static Version OutboundVersion { get; set; } = OutboundDefaultVersion;

    public Func<CapabilityEndpoints, string?, RequestUrlContext, string>? UrlResolver { get; set; } 
    
    public Action<HttpRequestMessage, object?, bool>? RequestResolver { get; set; }
    
    public Action<JObject, RequestSerializerContext>? RequestSerializer { get; set; }
    
    private enum StreamRawActions
    {
        Unknown,
        MsgStart,
        MsgStop,
        MsgDelta,
        Error,
        Ping,
        ContentBlockDelta,
        ContentBlockStart,
        ContentBlockStop
    }
    
    public AnthropicEndpointProvider()
    {
        Provider = LLmProviders.Anthropic;
        StoreApiAuth();
    }

    private enum AnthropicStreamBlockStartTypes
    {
        Unknown,
        Text,
        ToolUse,
        RedactedThinking
    }

    private class AnthropicStreamBlockStart
    {
        public static readonly FrozenDictionary<string, AnthropicStreamBlockStartTypes> Map = new Dictionary<string, AnthropicStreamBlockStartTypes>
        {
            { "text", AnthropicStreamBlockStartTypes.Text },
            { "tool_use", AnthropicStreamBlockStartTypes.ToolUse },
            { "redacted_thinking", AnthropicStreamBlockStartTypes.RedactedThinking }
        }.ToFrozenDictionary();
        
        public class AnthropicStreamBlockStartData
        {
            [JsonProperty("type")]
            public string Type { get; set; }
            
            [JsonProperty("text")]
            public string Text { get; set; }
            
            /// <summary>
            /// For tools
            /// </summary>
            [JsonProperty("name")]
            public string? Name { get; set; }
            
            /// <summary>
            /// For tools
            /// </summary>
            [JsonProperty("id")]
            public string? Id { get; set; }
            
            /// <summary>
            /// For redacted thinking blocks
            /// </summary>
            public string? Data { get; set; }
        }
        
        [JsonProperty("index")]
        public int Index { get; set; }
        
        [JsonProperty("content_block")]
        public AnthropicStreamBlockStartData ContentBlock { get; set; }
    }

    private enum AnthropicStreamBlockDeltaTypes
    {
        Unknown,
        TextDelta,
        ThinkingDelta,
        SignatureDelta,
        InputJsonDelta,
        CitationDelta
    }

    private class AnthropicStreamBlockDelta
    {
        public static readonly FrozenDictionary<string, AnthropicStreamBlockDeltaTypes> Map = new Dictionary<string, AnthropicStreamBlockDeltaTypes>
        {
            { "text_delta", AnthropicStreamBlockDeltaTypes.TextDelta },
            { "citations_delta", AnthropicStreamBlockDeltaTypes.CitationDelta },
            { "thinking_delta", AnthropicStreamBlockDeltaTypes.ThinkingDelta },
            { "signature_delta", AnthropicStreamBlockDeltaTypes.SignatureDelta },
            { "input_json_delta", AnthropicStreamBlockDeltaTypes.InputJsonDelta }
        }.ToFrozenDictionary();
        
        public class AnthropicStreamBlockDeltaData
        {
            [JsonProperty("type")]
            public string Type { get; set; }
            
            [JsonProperty("text")]
            public string Text { get; set; }
            
            [JsonProperty("partial_json")]
            public string? PartialJson { get; set; }
            
            [JsonProperty("thinking")]
            public string? Thinking { get; set; }
            
            [JsonProperty("signature")]
            public string? Signature { get; set; }
            
            [JsonProperty("citation")]
            public IChatMessagePartCitation? Citation { get; set; }
        }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("index")]
        public int Index { get; set; }
        
        [JsonProperty("delta")]
        public AnthropicStreamBlockDeltaData? Delta { get; set; }
        
        [JsonProperty("citation")]
        public IChatMessagePartCitation? Citation { get; set; }
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
    
    private class AnthropicStreamMsgDelta
    {
        [JsonProperty("delta")]
        public AnthropicStreamMsgDeltaData Delta { get; set; }
        
        [JsonProperty("usage")]
        public AnthropicStreamMsgDeltaUsage Usage { get; set; }
    }
    
    private class AnthropicStreamMsgDeltaUsage
    {
        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }

    private class AnthropicStreamMsgDeltaData
    {
        [JsonProperty("stop_reason")]
        public string? StopReason { get; set; }
        
        [JsonProperty("stop_sequence")]
        public string? StopSequence { get; set; }
    }
    
    /// <summary>
    /// Gets endpoint url for a given capability.
    /// </summary>
    public static string GetEndpointUrlFragment(CapabilityEndpoints endpoint)
    {
        return endpoint switch
        {
            CapabilityEndpoints.Chat => "messages",
            CapabilityEndpoints.Completions => "complete",
            CapabilityEndpoints.Models => "models",
            CapabilityEndpoints.Files => "files",
            CapabilityEndpoints.Skills => "skills",
            _ => throw new Exception($"Anthropic doesn't support endpoint {endpoint}")
        };
    }
    
    public override string ApiUrl(CapabilityEndpoints endpoint, string? url, IModel? model = null)
    {
        string eStr = GetEndpointUrlFragment(endpoint);
        return UrlResolver is not null ? string.Format(UrlResolver.Invoke(endpoint, url, new RequestUrlContext(eStr, url, model)), eStr, url, model?.Name) : $"https://api.anthropic.com/v1/{eStr}{url}";
    }
    
    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request, ChatStreamEventHandler? eventHandler)
    {
        ChatMessage? accuToolsMessage = null;
        ChatMessage? accuMessage = null;
        ChatUsage? plaintextUsage = null;
        ChatMessage? accuThinking = null;
        List<ChatMessagePart>? thinkingParts = null;
        ChatMessageFinishReasons finishReason = ChatMessageFinishReasons.Unknown;
        ChatMessagePart? currentPart = null;
        StringBuilder currentPartTextBuilder = new StringBuilder();
        
        #if DEBUG
        List<string> items = [];
        #endif
        
        await foreach (SseItem<string> item in SseParser.Create(reader.BaseStream).EnumerateAsync(request.CancellationToken))
        {
            #if DEBUG
            items.Add(item.Data);
            #endif

            if (eventHandler?.OnSse is not null)
            {
                await eventHandler.OnSse.Invoke(new ServerSentEvent
                {
                    Data = item.Data,
                    EventType = item.EventType
                });
            }
            
            if (request.CancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            string line = item.Data;

            StreamRawActions rawAction = StreamEventsMap.GetValueOrDefault(item.EventType, StreamRawActions.Unknown);
            
            if (rawAction is StreamRawActions.Error)
            {
                continue;
            }
            
            switch (rawAction)
            {
                case StreamRawActions.ContentBlockStart:
                {
                    AnthropicStreamBlockStart? res = JsonConvert.DeserializeObject<AnthropicStreamBlockStart>(line);

                    if (res is null)
                    {
                        continue;
                    }

                    AnthropicStreamBlockStartTypes type = AnthropicStreamBlockStart.Map.GetValueOrDefault(res.ContentBlock.Type, AnthropicStreamBlockStartTypes.Unknown);

                    if (type is AnthropicStreamBlockStartTypes.Text)
                    {
                        currentPart = new ChatMessagePart
                        {
                            Type = ChatMessageTypes.Text
                        };

                        accuMessage ??= new ChatMessage(ChatMessageRoles.Assistant);
                        accuMessage.Parts ??= [];
                        accuMessage.Parts.Add(currentPart);
                        currentPartTextBuilder.Clear();
                    }
                    
                    switch (type)
                    {
                        case AnthropicStreamBlockStartTypes.ToolUse:
                        {
                            ToolCall tc = new ToolCall
                            {
                                Id = res.ContentBlock.Id,
                                Index = res.Index
                            };

                            FunctionCall fc = new FunctionCall
                            {
                                Name = res.ContentBlock.Name ?? string.Empty,
                                ToolCall = tc
                            };

                            tc.FunctionCall = fc;
                        
                            accuToolsMessage ??= new ChatMessage(ChatMessageRoles.Tool)
                            {
                                ToolCalls = []
                            };

                            accuToolsMessage.ToolCalls?.Add(tc);
                        
                            accuToolsMessage.ContentBuilder ??= new StringBuilder();
                            accuToolsMessage.ContentBuilder.Clear();
                            break;
                        }
                        case AnthropicStreamBlockStartTypes.RedactedThinking:
                        {
                            string token = res.ContentBlock.Data ?? string.Empty;
                        
                            thinkingParts ??= [];
                            thinkingParts.Add(new ChatMessagePart(ChatMessageTypes.Reasoning)
                            {
                                Reasoning = new ChatMessageReasoningData
                                {
                                    Signature = token,
                                    Provider = LLmProviders.Anthropic
                                }
                            });
                        
                            yield return new ChatResult
                            {
                                Choices =
                                [
                                    new ChatChoice
                                    {
                                        Delta = new ChatMessage(ChatMessageRoles.Assistant, [
                                            new ChatMessagePart(ChatMessageTypes.Reasoning)
                                            {
                                                Reasoning = new ChatMessageReasoningData
                                                {
                                                    Signature = token,
                                                    Provider = LLmProviders.Anthropic
                                                }
                                            }
                                        ])
                                    }
                                ]
                            };
                            break;
                        }
                    }
                    
                    break;
                }
                case StreamRawActions.ContentBlockDelta:
                {
                    AnthropicStreamBlockDelta? res = JsonConvert.DeserializeObject<AnthropicStreamBlockDelta>(line);

                    if (accuToolsMessage is not null)
                    {
                        accuToolsMessage.ContentBuilder ??= new StringBuilder();
                        accuToolsMessage.ContentBuilder.Append(res?.Delta?.PartialJson);
                    }
                    else if (res?.Delta is not null)
                    {
                        AnthropicStreamBlockDeltaTypes type = AnthropicStreamBlockDelta.Map.GetValueOrDefault(res.Delta.Type, AnthropicStreamBlockDeltaTypes.Unknown);

                        switch (type)
                        {
                            case AnthropicStreamBlockDeltaTypes.TextDelta:
                            {
                                accuMessage ??= new ChatMessage(ChatMessageRoles.Assistant);
                                accuMessage.ContentBuilder ??= new StringBuilder();
                                accuMessage.ContentBuilder.Append(res.Delta.Text);

                                currentPartTextBuilder.Append(res.Delta.Text);
                                
                                yield return new ChatResult
                                {
                                    Choices =
                                    [
                                        new ChatChoice
                                        {
                                            Delta = new ChatMessage(ChatMessageRoles.Assistant, res.Delta.Text)
                                        }
                                    ]
                                };
                                break;
                            }
                            case AnthropicStreamBlockDeltaTypes.CitationDelta:
                            {
                                IChatMessagePartCitation? cit = res.Delta?.Citation;

                                if (cit is not null)
                                {
                                    if (currentPart is not null)
                                    {
                                        currentPart.Citations ??= [];
                                        currentPart.Citations.Add(cit);
                                    }

                                    yield return new ChatResult
                                    {
                                        Choices =
                                        [
                                            new ChatChoice
                                            {
                                                Delta = new ChatMessage(ChatMessageRoles.Assistant)
                                                {
                                                    Parts = [
                                                        new ChatMessagePart
                                                        {
                                                            Type = ChatMessageTypes.Text,
                                                            Citations = [ cit ]
                                                        }
                                                    ]
                                                }
                                            }
                                        ]
                                    };
                                }

                                break;
                            }
                            case AnthropicStreamBlockDeltaTypes.SignatureDelta:
                            {
                                accuThinking ??= new ChatMessage(ChatMessageRoles.Assistant);
                                accuThinking.VendorExtensions = new ChatMessageVendorExtensionsAnthropic
                                {
                                    Signature = res.Delta.Signature
                                };

                                break;
                            }
                            case AnthropicStreamBlockDeltaTypes.ThinkingDelta:
                            {
                                accuThinking ??= new ChatMessage(ChatMessageRoles.Assistant);
                                accuThinking.ContentBuilder ??= new StringBuilder();
                                accuThinking.ContentBuilder.Append(res.Delta.Thinking);

                                yield return new ChatResult
                                {
                                    Choices =
                                    [
                                        new ChatChoice
                                        {
                                            Delta = new ChatMessage(ChatMessageRoles.Assistant, [
                                                new ChatMessagePart(ChatMessageTypes.Reasoning)
                                                {
                                                    Reasoning = new ChatMessageReasoningData
                                                    {
                                                        Content = res.Delta.Thinking ?? string.Empty,
                                                        Signature = accuThinking.VendorExtensions is ChatMessageVendorExtensionsAnthropic sigData ? sigData.Signature : null,
                                                        Provider = LLmProviders.Anthropic
                                                    }
                                                }
                                            ])
                                        }
                                    ]
                                };

                                break;
                            }
                        }
                    }

                    break;
                }
                case StreamRawActions.ContentBlockStop:
                {
                    AnthropicStreamBlockStop? res = JsonConvert.DeserializeObject<AnthropicStreamBlockStop>(line);

                    if (currentPart is not null)
                    {
                        currentPart.Text = currentPartTextBuilder.ToString();
                    }

                    if (accuToolsMessage is not null)
                    {
                        ToolCall? lastCall = accuToolsMessage.ToolCalls?.FirstOrDefault(x => x.Index == res?.Index);

                        if (lastCall is not null)
                        {
                            lastCall.FunctionCall.Arguments = accuToolsMessage.ContentBuilder?.ToString() ?? string.Empty;
                        }

                        accuToolsMessage.ContentBuilder?.Clear();
                        
                        yield return new ChatResult
                        {
                            Choices = [
                                new ChatChoice
                                {
                                    Delta = accuToolsMessage
                                }
                            ],
                            Usage = plaintextUsage
                        };
                    }
                    
                    if (accuThinking is not null)
                    {
                        accuThinking.Parts =
                        [
                            ..thinkingParts ?? [],
                            new ChatMessagePart(ChatMessageTypes.Reasoning)
                            {
                                Reasoning = new ChatMessageReasoningData
                                {
                                    Content = accuThinking.ContentBuilder?.ToString() ?? string.Empty,
                                    Signature = accuThinking.VendorExtensions is ChatMessageVendorExtensionsAnthropic sigData ? sigData.Signature : null,
                                    Provider = LLmProviders.Anthropic
                                }
                            }
                        ];
                        
                        yield return new ChatResult
                        {
                            Choices =
                            [
                                new ChatChoice
                                {
                                    Delta = accuThinking
                                }
                            ],
                            StreamInternalKind = ChatResultStreamInternalKinds.AssistantMessageTransientBlock
                        };
                    }

                    if (accuMessage is not null)
                    {
                        accuMessage.Parts ??= [];
                        
                        if (accuThinking?.Parts?.Count > 0)
                        {
                            foreach (ChatMessagePart reasoningPart in accuThinking.Parts)
                            {
                                accuMessage.Parts.Add(reasoningPart);
                            }
                        }
                    }
                    
                    break;
                }
                case StreamRawActions.MsgDelta:
                {
                    AnthropicStreamMsgDelta? res = JsonConvert.DeserializeObject<AnthropicStreamMsgDelta>(line);

                    if (res is not null)
                    {
                        plaintextUsage ??= new ChatUsage(LLmProviders.Anthropic);
                        plaintextUsage.CompletionTokens = res.Usage.OutputTokens;

                        if (res.Delta.StopReason is not null)
                        {
                            finishReason = ChatMessageFinishReasonsConverter.Map.GetValueOrDefault(res.Delta.StopReason, ChatMessageFinishReasons.Unknown);
                        }
                        
                        // todo: propagate stop_sequence from res.Delta
                    }
                    
                    break;
                }
                case StreamRawActions.MsgStart:
                {
                    AnthropicStreamMsgStart? res = JsonConvert.DeserializeObject<AnthropicStreamMsgStart>(line);
  
                    if (res is not null && res.Message.Usage.InputTokens + res.Message.Usage.OutputTokens > 0)
                    {
                        plaintextUsage = new ChatUsage(LLmProviders.Anthropic)
                        {
                            TotalTokens = res.Message.Usage.InputTokens + res.Message.Usage.OutputTokens,
                            CompletionTokens = res.Message.Usage.OutputTokens,
                            PromptTokens = res.Message.Usage.InputTokens,
                            CacheCreationTokens = res.Message.Usage.CacheCreationInputTokens,
                            CacheReadTokens = res.Message.Usage.CacheReadInputTokens
                        };
                    }
                    
                    break;
                }
                case StreamRawActions.MsgStop:
                {
                    break;
                }
            }
        }

        plaintextUsage ??= new ChatUsage(LLmProviders.Anthropic);
        plaintextUsage.TotalTokens = plaintextUsage.CompletionTokens + plaintextUsage.PromptTokens;

        if (accuMessage is not null)
        {
            accuMessage.Content = accuMessage.ContentBuilder?.ToString();   
        }
        
        yield return new ChatResult
        {
            Choices =
            [
                new ChatChoice
                {
                    Delta = accuMessage
                }
            ],
            StreamInternalKind = ChatResultStreamInternalKinds.AppendAssistantMessage,
            Usage = plaintextUsage
        };
        
        yield return new ChatResult
        {
            Usage = plaintextUsage,
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
        req.Headers.Add("anthropic-version", "2023-06-01");

        ProviderAuthentication? auth = Api?.GetProvider(LLmProviders.Anthropic).Auth;

        if (auth?.ApiKey is not null)
        {
            req.Headers.Add("x-api-key", auth.ApiKey);
        }

        if (RequestResolver is not null)
        {
            RequestResolver.Invoke(req, data, streaming);
        }
        else
        {
            req.Headers.Add("anthropic-beta", ["interleaved-thinking-2025-05-14", "files-api-2025-04-14", "code-execution-2025-08-25", "search-results-2025-06-09"]);
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
    
    public override T? InboundMessage<T>(string jsonData, string? postData, object? requestObject) where T : default
    {
        if (typeof(T) == typeof(ChatResult))
        {
            return (T?)(dynamic)ChatResult.Deserialize(LLmProviders.Anthropic, jsonData, postData, requestObject);
        }
        
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
    
    public override object? InboundMessage(Type type, string jsonData, string? postData, object? requestObject)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}