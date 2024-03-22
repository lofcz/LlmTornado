using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAiNg.Chat;
using OpenAiNg.Vendor.Anthropic;

namespace OpenAiNg.Code;

/// <summary>
/// 
/// </summary>
public interface IEndpointProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="verb"></param>
    /// <param name="data"></param>
    /// <param name="streaming"></param>
    /// <returns></returns>
    public HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);

    public T? InboundMessage<T>(string jsonData);
    public IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : ApiResultBase;
    public OpenAiApi Api { get; set; }
    public LLmProviders Provider { get; set; }
    public string ApiUrl(CapabilityEndpoints endpoint, string? url);
}

public abstract class BaseEndpointProvider : IEndpointProvider
{
    public OpenAiApi Api { get; set; }
    public LLmProviders Provider { get; set; } = LLmProviders.Unknown;
    internal static readonly JsonSerializerSettings NullSettings = new() { NullValueHandling = NullValueHandling.Ignore };
    
    public BaseEndpointProvider(OpenAiApi api)
    {
        Api = api;
    }

    public abstract string ApiUrl(CapabilityEndpoints endpoint, string? url);
    public abstract T? InboundMessage<T>(string jsonData);
    public abstract IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : ApiResultBase;
    public abstract HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);
}

internal static class EndpointProviderConverter
{
    public static IEndpointProvider CreateProvider(LLmProviders provider, OpenAiApi api)
    {
        return provider switch
        {
            LLmProviders.OpenAi => new OpenAiEndpointProvider(api),
            LLmProviders.Anthropic => new AnthropicEndpointProvider(api),
            _ => new OpenAiEndpointProvider(api)
        };
    }
}

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

/// <summary>
/// 
/// </summary>
public class OpenAiEndpointProvider : BaseEndpointProvider
{
    private const string DataString = "data:";
    private const string DoneString = "[DONE]";
    
    public OpenAiEndpointProvider(OpenAiApi api) : base(api)
    {
        Provider = LLmProviders.OpenAi;
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
            CapabilityEndpoints.Audio => "audio",
            CapabilityEndpoints.Chat => "chat/completions",
            CapabilityEndpoints.Completions => "completions",
            CapabilityEndpoints.Embeddings => "embeddings",
            CapabilityEndpoints.FineTuning => "fine_tuning",
            CapabilityEndpoints.Files => "files",
            CapabilityEndpoints.ImageGeneration => "images/generations",
            CapabilityEndpoints.ImageEdit => "images/edits",
            CapabilityEndpoints.Models => "models",
            CapabilityEndpoints.Moderation => "moderations",
            CapabilityEndpoints.Assistants => "assistants",
            CapabilityEndpoints.Threads => "threads",
            _ => throw new Exception($"OpenAI doesn't support endpoint {endpoint}")
        };

        return $"{string.Format(Api.ApiUrlFormat ?? "https://api.openai.com/{0}/{1}", Api.ApiVersion, eStr)}{url}";
    }
    
    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming)
    {
        HttpRequestMessage req = new(verb, url);
        req.Headers.Add("User-Agent", EndpointBase.GetUserAgent().Trim());
        req.Headers.Add("OpenAI-Beta", "assistants=v1");

        if (Api.Auth is not null)
        {
            if (Api.Auth.ApiKey is not null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Api.Auth.ApiKey.Trim());
                req.Headers.Add("api-key", Api.Auth.ApiKey);
            }

            if (Api.Auth.Organization is not null) req.Headers.Add("OpenAI-Organization", Api.Auth.Organization.Trim());
        }
        
        return req;
    }
    
    public override T? InboundMessage<T>(string jsonData) where T : default
    {
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
    
    public override async IAsyncEnumerable<T?> InboundStream<T>(StreamReader reader) where T : class
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith(DataString))
            {
                line = line[DataString.Length..];
            }

            line = line.TrimStart();

            if (line is DoneString)
            {
                yield break;
            }

            if (line.StartsWith(':') || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            T? res = JsonConvert.DeserializeObject<T>(line);

            if (res is null)
            {
                continue;
            }

            /*res.Organization = organization;
            res.RequestId = requestId;
            res.ProcessingTime = processingTime;
            res.OpenaiVersion = openaiVersion;

            if (res.Model != null && string.IsNullOrEmpty(res.Model))
                if (modelFromHeaders != null)
                    res.Model = modelFromHeaders;*/

            yield return res;
        }
    }
}