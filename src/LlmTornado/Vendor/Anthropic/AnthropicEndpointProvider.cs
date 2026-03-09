using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using LlmTornado.Batch;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code.Models;
using LlmTornado.Code.Sse;
using LlmTornado.Threads;
using LlmTornado.Tokenize;
using LlmTornado.Vendor.Anthropic;
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
    
    public override JsonSchemaCapabilities GetJsonSchemaCapabilities()
    {
        return new JsonSchemaCapabilities
        {
            Const = true // Anthropic supports const keyword in JSON schemas
        };
    }

    private enum AnthropicStreamBlockStartTypes
    {
        Unknown,
        Text,
        ToolUse,
        RedactedThinking,
        Compaction
    }

    private class AnthropicStreamBlockStart
    {
        public static readonly FrozenDictionary<string, AnthropicStreamBlockStartTypes> Map = new Dictionary<string, AnthropicStreamBlockStartTypes>
        {
            { "text", AnthropicStreamBlockStartTypes.Text },
            { "tool_use", AnthropicStreamBlockStartTypes.ToolUse },
            { "redacted_thinking", AnthropicStreamBlockStartTypes.RedactedThinking },
            { "compaction", AnthropicStreamBlockStartTypes.Compaction }
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
        CitationDelta,
        CompactionDelta
    }

    private class AnthropicStreamBlockDelta
    {
        public static readonly FrozenDictionary<string, AnthropicStreamBlockDeltaTypes> Map = new Dictionary<string, AnthropicStreamBlockDeltaTypes>
        {
            { "text_delta", AnthropicStreamBlockDeltaTypes.TextDelta },
            { "citations_delta", AnthropicStreamBlockDeltaTypes.CitationDelta },
            { "thinking_delta", AnthropicStreamBlockDeltaTypes.ThinkingDelta },
            { "signature_delta", AnthropicStreamBlockDeltaTypes.SignatureDelta },
            { "input_json_delta", AnthropicStreamBlockDeltaTypes.InputJsonDelta },
            { "compaction_delta", AnthropicStreamBlockDeltaTypes.CompactionDelta }
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
            
            [JsonProperty("content")]
            public string? Content { get; set; }
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
    
    private static bool RequiresEffortHeader(object? data)
    {
        if (data is not ChatRequest chatRequest)
        {
            return false;
        }
        
        // Effort is GA on Claude Opus 4.6+ and Sonnet 4.6, no beta header needed
        string? modelName = chatRequest.Model?.Name;
        if (IsOpus46OrNewer(modelName) || IsSonnet46OrNewer(modelName))
        {
            return false;
        }
        
        // Effort parameter requires beta header for older models (Claude Opus 4.5)
        if (chatRequest.ReasoningEffort is null)
        {
            return false;
        }
        
        return modelName?.StartsWith("claude-opus-4-5", StringComparison.OrdinalIgnoreCase) == true;
    }
    
    private static bool IsSonnet46OrNewer(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }
        
        return modelName.StartsWith("claude-sonnet-4-6", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool RequiresCompactionHeader(object? data)
    {
        if (data is not ChatRequest chatRequest)
        {
            return false;
        }
        
        return chatRequest.VendorExtensions?.Anthropic?.ContextManagement is not null;
    }
    
    private static bool IsOpus46OrNewer(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }
        
        return modelName.StartsWith("claude-opus-4-6", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool RequiresFastModeHeader(object? data)
    {
        if (data is not ChatRequest chatRequest)
        {
            return false;
        }
        
        return chatRequest.Speed is ChatRequestSpeeds.Fast;
    }
    
    private static bool RequiresDynamicWebToolsHeader(object? data)
    {
        if (data is not ChatRequest chatRequest)
        {
            return false;
        }
        
        return chatRequest.VendorExtensions?.Anthropic?.BuiltInTools?.Any(x => 
            x.Type is VendorAnthropicChatRequestBuiltInToolTypes.WebSearch20260209 
                or VendorAnthropicChatRequestBuiltInToolTypes.WebFetch20260209) == true;
    }
    
    private static bool RequiresAdvancedToolUseHeader(object? data)
    {
        if (data is not ChatRequest chatRequest)
        {
            return false;
        }
        
        // Check if any tools have allowed_callers or defer_loading set
        if (chatRequest.Tools?.Any(x => x.AllowedCallers?.Count > 0 || x.DeferLoading == true) == true)
        {
            return true;
        }
        
        // Check if any built-in tools are tool search tools
        if (chatRequest.VendorExtensions?.Anthropic?.BuiltInTools?.Any(x => 
            x.Type is Chat.Vendors.Anthropic.VendorAnthropicChatRequestBuiltInToolTypes.ToolSearchRegex20251119 or 
                      Chat.Vendors.Anthropic.VendorAnthropicChatRequestBuiltInToolTypes.ToolSearchBm2520251119) == true)
        {
            return true;
        }
        
        return false;
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
            CapabilityEndpoints.Tokenize => "messages/count_tokens",
            CapabilityEndpoints.Batch => "messages/batches",
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
        ChatRequestServiceTiers? serviceTier = null;
        ChatRequestSpeeds? speed = null;
        
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
                        
                            accuToolsMessage ??= new ChatMessage(ChatMessageRoles.Assistant)
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
                        case AnthropicStreamBlockStartTypes.Compaction:
                        {
                            // Compaction block start - content arrives via compaction_delta
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
                            case AnthropicStreamBlockDeltaTypes.CompactionDelta:
                            {
                                // Compaction delta delivers the complete summary content in a single delta
                                string compactionContent = res.Delta.Content ?? string.Empty;
                                
                                yield return new ChatResult
                                {
                                    Choices =
                                    [
                                        new ChatChoice
                                        {
                                            Delta = new ChatMessage(ChatMessageRoles.Assistant, [
                                                new ChatMessagePart
                                                {
                                                    Type = ChatMessageTypes.Compaction,
                                                    Text = compactionContent
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
                        
                        serviceTier = res.Message.Usage.ServiceTier switch
                        {
                            "priority" => ChatRequestServiceTiers.Priority,
                            _ => null
                        };
                        
                        speed = res.Message.Usage.Speed switch
                        {
                            "fast" => ChatRequestSpeeds.Fast,
                            "standard" => ChatRequestSpeeds.Standard,
                            _ => null
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
            Usage = plaintextUsage,
            ServiceTier = serviceTier,
            Speed = speed
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
            StreamInternalKind = ChatResultStreamInternalKinds.FinishData,
            ServiceTier = serviceTier,
            Speed = speed
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
    
    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming, object? sourceObject)
    {
        HttpRequestMessage req = new HttpRequestMessage(verb, url)
        {
            Version = OutboundVersion
        };
        
        req.Headers.Add("User-Agent", EndpointBase.ResolveUserAgent(Api));
        req.Headers.Add("anthropic-version", "2023-06-01");
        
        if (IsDirectBrowserAccessEnabled())
        {
            req.Headers.Add("anthropic-dangerous-direct-browser-access", "true");
        }

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
            string? currentModel = (sourceObject as ChatRequest)?.Model?.Name;
            bool isOpus46Plus = IsOpus46OrNewer(currentModel);
            
            HashSet<string> betaHeaders = [
                "files-api-2025-04-14", 
                "code-execution-2025-08-25", 
                "search-results-2025-06-09"
            ];
            
            // Interleaved thinking header is deprecated on Opus 4.6+ (adaptive thinking auto-enables it)
            if (!isOpus46Plus)
            {
                betaHeaders.Add("interleaved-thinking-2025-05-14");
            }
            
            // Add effort beta header if applicable (Claude Opus 4.5 only; GA on 4.6+)
            if (RequiresEffortHeader(sourceObject))
            {
                betaHeaders.Add("effort-2025-11-24");
            }
            
            // Add compaction beta header if context management is configured
            if (RequiresCompactionHeader(sourceObject))
            {
                betaHeaders.Add("compact-2026-01-12");
            }
            
            // Add advanced tool use beta header if applicable (programmatic tool calling, tool search)
            if (RequiresAdvancedToolUseHeader(sourceObject))
            {
                betaHeaders.Add("advanced-tool-use-2025-11-20");
            }
            
            // Add fast mode beta header if applicable
            if (RequiresFastModeHeader(sourceObject))
            {
                betaHeaders.Add("fast-mode-2026-02-01");
            }
            
            // Add dynamic web tools beta header when web_search_20260209 or web_fetch_20260209 are used
            if (RequiresDynamicWebToolsHeader(sourceObject))
            {
                betaHeaders.Add("code-execution-web-tools-2026-02-09");
            }
            
            req.Headers.Add("anthropic-beta", AccumulateHeaders(betaHeaders, sourceObject));
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
        Type type = typeof(T);
    
        return type switch
        {
            _ when type == typeof(ChatResult) => 
                (T?)(dynamic)ChatResult.Deserialize(LLmProviders.Anthropic, jsonData, postData, requestObject),
            _ when type == typeof(TokenizeResult) => 
                (T?)(dynamic)TokenizeResult.Deserialize(LLmProviders.Anthropic, jsonData, postData),
            _ when type == typeof(BatchResult) => 
                (T?)(dynamic)BatchResult.Deserialize(LLmProviders.Anthropic, jsonData),
            _ => JsonConvert.DeserializeObject<T>(jsonData)
        };
    }
    
    public override object? InboundMessage(Type type, string jsonData, string? postData, object? requestObject)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}
