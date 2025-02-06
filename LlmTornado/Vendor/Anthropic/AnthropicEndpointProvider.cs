using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code.Sse;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// 
/// </summary>
internal class AnthropicEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
    private const string StreamMsgStart = $"message_start";
    private const string StreamMsgStop = $"message_stop";
    private const string StreamMsgDelta = $"message_delta";
    private const string StreamError = $"error";
    private const string StreamPing = $"ping";
    private const string StreamContentBlockDelta = $"content_block_delta";
    private const string StreamContentBlockStart = $"content_block_start";
    private const string StreamContentBlockStop = $"content_block_stop";
    private static readonly HashSet<string> toolFinishReasons = [ "tool_use" ];

    private static readonly Dictionary<string, StreamRawActions> StreamEventsMap = new Dictionary<string, StreamRawActions>
    {
        { StreamMsgStart, StreamRawActions.MsgStart },
        { StreamMsgStop, StreamRawActions.MsgStop },
        { StreamMsgDelta, StreamRawActions.MsgDelta },
        { StreamError, StreamRawActions.Error },
        { StreamPing, StreamRawActions.Ping },
        { StreamContentBlockDelta, StreamRawActions.ContentBlockDelta },
        { StreamContentBlockStart, StreamRawActions.ContentBlockStart },
        { StreamContentBlockStop, StreamRawActions.ContentBlockEnd }
    };
    
    public static Version OutboundVersion { get; set; } = HttpVersion.Version20;
    
    public Func<CapabilityEndpoints, string?, string>? UrlResolver { get; set; } 
    public override HashSet<string> ToolFinishReasons => toolFinishReasons;

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
        ContentBlockEnd
    }
    
    public AnthropicEndpointProvider(TornadoApi api) : base(api)
    {
        Provider = LLmProviders.Anthropic;
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
            [JsonProperty("partial_json")]
            public string? PartialJson { get; set; }
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
        public string StopReason { get; set; }
        
        [JsonProperty("stop_sequence")]
        public string? StopSequence { get; set; }
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
            CapabilityEndpoints.Chat => "messages",
            CapabilityEndpoints.Completions => "complete",
            _ => throw new Exception($"Anthropic doesn't support endpoint {endpoint}")
        };

        return UrlResolver is not null ? UrlResolver.Invoke(endpoint, url) : $"https://api.anthropic.com/v1/{eStr}{url}";
    }
    
    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request)
    {
        ChatMessage? accuToolsMessage = null;
        ChatMessage? accuPlaintext = null;
        ChatUsage? plaintextUsage = null;
        
        await foreach (SseItem<string> item in SseParser.Create(reader.BaseStream).EnumerateAsync(request.CancellationToken))
        {
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

                    if (res?.ContentBlock.Type is "tool_use")
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
                    }                    
                    
                    break;
                }
                case StreamRawActions.ContentBlockDelta:
                {
                    AnthropicStreamBlockDelta? res = JsonConvert.DeserializeObject<AnthropicStreamBlockDelta>(line);

                    if (accuToolsMessage is not null)
                    {
                        accuToolsMessage.ContentBuilder ??= new StringBuilder();
                        accuToolsMessage.ContentBuilder.Append(res?.Delta.PartialJson);
                    }
                    else if (res?.Delta.Text is not null)
                    {
                        accuPlaintext ??= new ChatMessage(ChatMessageRoles.Assistant);
                        accuPlaintext.ContentBuilder ??= new StringBuilder();
                        accuPlaintext.ContentBuilder.Append(res.Delta.Text);
                        
                        yield return new ChatResult
                        {
                            Choices = [
                                new ChatChoice
                                {
                                    Delta = new ChatMessage(ChatMessageRoles.Assistant, res.Delta.Text)
                                }
                            ]
                        };
                    }
                    
                    break;
                }
                case StreamRawActions.ContentBlockEnd:
                {
                    AnthropicStreamBlockStop? res = JsonConvert.DeserializeObject<AnthropicStreamBlockStop>(line);

                    if (accuToolsMessage is not null)
                    {
                        ToolCall? lastCall = accuToolsMessage.ToolCalls?.FirstOrDefault(x => x.Index == res?.Index);

                        if (lastCall is not null)
                        {
                            lastCall.FunctionCall.Arguments = accuToolsMessage.ContentBuilder?.ToString() ?? string.Empty;
                        }

                        accuToolsMessage.ContentBuilder?.Clear();
                    }
                    
                    break;
                }
                case StreamRawActions.MsgDelta:
                {
                    AnthropicStreamMsgDelta? res = JsonConvert.DeserializeObject<AnthropicStreamMsgDelta>(line);

                    if (res is not null)
                    {
                        plaintextUsage ??= new ChatUsage();
                        plaintextUsage.CompletionTokens = res.Usage.OutputTokens;
                        
                        // todo: propagate data from res.Delta
                    }
                    
                    break;
                }
                case StreamRawActions.MsgStart:
                {
                    AnthropicStreamMsgStart? res = JsonConvert.DeserializeObject<AnthropicStreamMsgStart>(line);
  
                    if (res is not null && res.Message.Usage.InputTokens + res.Message.Usage.OutputTokens > 0)
                    {
                        plaintextUsage = new ChatUsage
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
                    if (accuToolsMessage is not null)
                    {
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

                    if (accuPlaintext is not null)
                    {
                        accuPlaintext.Content = accuPlaintext.ContentBuilder?.ToString() ?? string.Empty;
                        
                        yield return new ChatResult
                        {
                            Choices =
                            [
                                new ChatChoice
                                {
                                    Delta = accuPlaintext
                                }
                            ],
                            StreamInternalKind = ChatResultStreamInternalKinds.AppendAssistantMessage,
                            Usage = plaintextUsage
                        };
                    }
                    
                    break;
                }
            }
        }
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
        req.Headers.Add("anthropic-beta", "prompt-caching-2024-07-31"); // 8k output tokens for Sonnet 3.5

        ProviderAuthentication? auth = Api.GetProvider(LLmProviders.Anthropic).Auth;

        if (auth?.ApiKey is not null)
        {
            req.Headers.Add("x-api-key", auth.ApiKey);
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
    
    public override T? InboundMessage<T>(string jsonData, string? postData) where T : default
    {
        if (typeof(T) == typeof(ChatResult))
        {
            return (T?)(dynamic)ChatResult.Deserialize(LLmProviders.Anthropic, jsonData, postData);
        }
        
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
    
    public override object? InboundMessage(Type type, string jsonData, string? postData)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}