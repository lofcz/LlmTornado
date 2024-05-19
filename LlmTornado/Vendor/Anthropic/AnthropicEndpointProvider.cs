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
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// 
/// </summary>
internal class AnthropicEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
    private const string Event = "event:";
    private const string Data = "data:";
    private const string StreamMsgStart = $"{Event} message_start";
    private const string StreamMsgStop = $"{Event} message_stop";
    private const string StreamError = $"{Event} error";
    private const string StreamPing = $"{Event} ping";
    private const string StreamContentBlockDelta = $"{Event} content_block_delta";
    private const string StreamContentBlockStart = $"{Event} content_block_start";
    private const string StreamContentBlockStop = $"{Event} content_block_stop";
    private static readonly HashSet<string> StreamSkip = [StreamMsgStart, StreamMsgStop, StreamPing];
    private static readonly HashSet<string> toolFinishReasons = [ "tool_use" ];
    
    public static Version OutboundVersion { get; set; } = HttpVersion.Version20;
    
    public override HashSet<string> ToolFinishReasons => toolFinishReasons;
    
    private enum StreamNextAction
    {
        Read,
        BlockStart,
        BlockDelta,
        BlockStop,
        Skip,
        MsgStart,
        MsgStop
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

        return $"https://api.anthropic.com/v1/{eStr}{url}";
    }
    
    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request)
    {
        StreamNextAction nextAction = StreamNextAction.Read;
        ChatMessage? accuToolsMessage = null;
        ChatMessage? accuPlaintext = null;
        ChatUsage? plaintextUsage = null;
      
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.IsNullOrWhiteSpace())
            {
                continue;
            }

            if (line.StartsWith(StreamError))
            {
                nextAction = StreamNextAction.Skip;
                continue;
            }
            
            switch (nextAction)
            {
                case StreamNextAction.Read when line.StartsWith(StreamContentBlockStart):
                {
                    nextAction = StreamNextAction.BlockStart;
                    continue;
                }
                case StreamNextAction.Read when line.StartsWith(StreamMsgStart):
                {
                    nextAction = StreamNextAction.MsgStart;
                    continue;
                }
                case StreamNextAction.Read when line.StartsWith(StreamPing):
                {
                    nextAction = StreamNextAction.Skip;
                    continue;
                }
                case StreamNextAction.Read when line.StartsWith(StreamMsgStop):
                {
                    nextAction = StreamNextAction.MsgStop;
                    continue;
                }
                case StreamNextAction.Read when line.StartsWith(StreamContentBlockStart):
                {
                    nextAction = StreamNextAction.BlockStart;
                    continue;
                }
                case StreamNextAction.Read when line.StartsWith(StreamContentBlockStop):
                {
                    nextAction = StreamNextAction.BlockStop;
                    continue;
                }
                case StreamNextAction.Read:
                {
                    if (line.StartsWith(StreamContentBlockDelta))
                    {
                        nextAction = StreamNextAction.BlockDelta;
                    }

                    break;
                }
                case StreamNextAction.Skip:
                {
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.BlockStart:
                {
                    line = line[Data.Length..];
                    AnthropicStreamBlockStart? res = JsonConvert.DeserializeObject<AnthropicStreamBlockStart>(line);

                    if (res?.ContentBlock.Type is "tool_use")
                    {
                        ToolCall tc = new ToolCall
                        {
                            Id = res.ContentBlock.Id,
                            Index = res.Index,
                        };

                        FunctionCall fc = new FunctionCall
                        {
                            Name = res.ContentBlock.Name,
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
                    
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.BlockDelta:
                {
                    line = line[Data.Length..];
                    AnthropicStreamBlockDelta? res = JsonConvert.DeserializeObject<AnthropicStreamBlockDelta>(line);

                    if (accuToolsMessage is not null)
                    {
                        accuToolsMessage.ContentBuilder ??= new StringBuilder();
                        accuToolsMessage.ContentBuilder.Append(res?.Delta.PartialJson);
                    }
                    else if (!res?.Delta.Text.IsNullOrWhiteSpace() ?? false)
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
                    
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.BlockStop:
                {
                    line = line[Data.Length..];
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
                    
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.MsgStart:
                {
                    line = line[Data.Length..];
                    AnthropicStreamMsgStart? res = JsonConvert.DeserializeObject<AnthropicStreamMsgStart>(line);
  
                    if (res is not null && res.Message.Usage.InputTokens + res.Message.Usage.OutputTokens > 0)
                    {
                        plaintextUsage = new ChatUsage
                        {
                            TotalTokens = res.Message.Usage.InputTokens + res.Message.Usage.OutputTokens,
                            CompletionTokens = res.Message.Usage.OutputTokens,
                            PromptTokens = res.Message.Usage.InputTokens
                        };
                    }
                    
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.MsgStop:
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
                            ]
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
        HttpRequestMessage req = new(verb, url)
        {
            Version = OutboundVersion
        };
        
        req.Headers.Add("User-Agent", EndpointBase.GetUserAgent());
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Headers.Add("anthropic-beta", "tools-2024-05-16");

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
    
    public override T? InboundMessage<T>(string jsonData, string? postData) where T : default
    {
        if (typeof(T) == typeof(ChatResult))
        {
            return (T?)(dynamic)ChatResult.Deserialize(LLmProviders.Anthropic, jsonData, postData);
        }
        
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
}