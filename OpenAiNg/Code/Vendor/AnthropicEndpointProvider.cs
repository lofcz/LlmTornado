using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using OpenAiNg.Chat;
using OpenAiNg.Vendor.Anthropic;

namespace OpenAiNg.Code.Vendor;

/// <summary>
/// 
/// </summary>
public class AnthropicEndpointProvider : BaseEndpointProvider
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
    
    public override HashSet<string> ToolFinishReasons => toolFinishReasons;
    
    private enum StreamNextAction
    {
        Read,
        BlockStart,
        BlockDelta,
        BlockStop,
        Skip,
        MsgStart
    }
    
    public AnthropicEndpointProvider(OpenAiApi api) : base(api)
    {
        Provider = LLmProviders.Anthropic;
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
            CapabilityEndpoints.Chat => "messages",
            CapabilityEndpoints.Completions => "complete",
            _ => throw new Exception($"Anthropic doesn't support endpoint {endpoint}")
        };

        return $"https://api.anthropic.com/v1/{eStr}{url}";
    }

    public override async IAsyncEnumerable<T?> InboundStream<T>(StreamReader reader) where T : class
    {
        StreamNextAction nextAction = StreamNextAction.Read;
        
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.IsNullOrWhiteSpace())
            {
                continue;
            }
            
            line = line.TrimStart();
            
            switch (nextAction)
            {
                case StreamNextAction.Read when line.StartsWith(StreamContentBlockStart):
                    nextAction = StreamNextAction.BlockStart;
                    continue;
                case StreamNextAction.Read when line.StartsWith(StreamMsgStart):
                    nextAction = StreamNextAction.MsgStart;
                    continue;
                case StreamNextAction.Read when line.StartsWith(StreamPing) || line.StartsWith(StreamMsgStop):
                    nextAction = StreamNextAction.Skip;
                    continue;
                case StreamNextAction.Read when line.StartsWith(StreamContentBlockStart):
                    nextAction = StreamNextAction.BlockStart;
                    continue;
                case StreamNextAction.Read when line.StartsWith(StreamContentBlockStop):
                    nextAction = StreamNextAction.BlockStop;
                    continue;
                case StreamNextAction.Read:
                {
                    if (line.StartsWith(StreamContentBlockDelta))
                    {
                        nextAction = StreamNextAction.BlockDelta;
                    }

                    break;
                }
                case StreamNextAction.Skip:
                    nextAction = StreamNextAction.Read;
                    break;
                case StreamNextAction.BlockStart:
                {
                    line = line.Substring(Data.Length);
                    AnthropicStreamBlockStart? res = JsonConvert.DeserializeObject<AnthropicStreamBlockStart>(line);

                    if (!res?.ContentBlock.Text.IsNullOrWhiteSpace() ?? false)
                    {
                        //yield return (T)(dynamic)res.ContentBlock.Text;
                    }
                    
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.BlockDelta:
                {
                    line = line.Substring(Data.Length);
                    AnthropicStreamBlockDelta? res = JsonConvert.DeserializeObject<AnthropicStreamBlockDelta>(line);
                    
                    if (!res?.Delta.Text.IsNullOrWhiteSpace() ?? false)
                    {
                        if (typeof(T) == typeof(ChatResult))
                        {
                            yield return (T)(dynamic) new ChatResult
                            {
                                Choices = [
                                    new ChatChoice { Delta = new ChatMessage(ChatMessageRole.Assistant, res.Delta.Text) }
                                ]
                            };
                        }
                    }
                    
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.BlockStop:
                {
                    line = line.Substring(Data.Length);
                    AnthropicStreamBlockDelta? res = JsonConvert.DeserializeObject<AnthropicStreamBlockDelta>(line);
                    nextAction = StreamNextAction.Read;
                    break;
                }
                case StreamNextAction.MsgStart:
                {
                    line = line.Substring(Data.Length);
                    AnthropicStreamMsgStart? res = JsonConvert.DeserializeObject<AnthropicStreamMsgStart>(line);
                    nextAction = StreamNextAction.Read;
                    break;
                }
            }
        }
    }

    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming)
    {
        HttpRequestMessage req = new(verb, url);
        req.Headers.Add("User-Agent", EndpointBase.GetUserAgent());
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Headers.Add("anthropic-beta", "tools-2024-04-04");

        if (Api.Auth is not null)
        {
            if (Api.Auth.ApiKey is not null)
            {
                req.Headers.Add("x-api-key", Api.Auth.ApiKey);
            } 
        }

        return req;
    }

    public override T? InboundMessage<T>(string jsonData) where T : default
    {
        if (typeof(T) == typeof(ChatResult))
        {
            return (T?)(dynamic)ChatResult.Deserialize(LLmProviders.Anthropic, jsonData);
        }
        
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
}