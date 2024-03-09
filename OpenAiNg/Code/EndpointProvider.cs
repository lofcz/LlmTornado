using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        if (data is not null)
        {
            if (data is HttpContent hData)
            {
                req.Content = hData;
            }
            else
            {
                string jsonContent = JsonConvert.SerializeObject(data, NullSettings);
                StringContent stringContent = new(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        return req;
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
            CapabilityEndpoints.Chat => "messages",
            CapabilityEndpoints.Completions => "complete",
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

        if (data is not null)
        {
            if (data is HttpContent hData)
            {
                req.Content = hData;
            }
            else
            {
                string jsonContent = JsonConvert.SerializeObject(data, NullSettings);
                StringContent stringContent = new(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        return req;
    }
}