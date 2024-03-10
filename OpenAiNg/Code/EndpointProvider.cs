using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAiNg.Chat;

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
    public AnthropicEndpointProvider(OpenAiApi api) : base(api)
    {
        Provider = LLmProviders.Anthropic;
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

        return $"https://api.openai.com/v1/{eStr}{url}";
    }
    
    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming)
    {
        HttpRequestMessage req = new(verb, url);
        req.Headers.Add("User-Agent", EndpointBase.GetUserAgent());
        req.Headers.Add("OpenAI-Beta", "assistants=v1");

        if (Api.Auth is not null)
        {
            if (Api.Auth.ApiKey is not null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Api.Auth.ApiKey);
                req.Headers.Add("api-key", Api.Auth.ApiKey);
            }

            if (Api.Auth.Organization is not null) req.Headers.Add("OpenAI-Organization", Api.Auth.Organization);
        }
        
        return req;
    }
    
    public override T? InboundMessage<T>(string jsonData) where T : default
    {
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
}