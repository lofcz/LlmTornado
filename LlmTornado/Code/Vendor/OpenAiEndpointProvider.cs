using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// 
/// </summary>
internal class OpenAiEndpointProvider : BaseEndpointProvider
{
    private const string DataString = "data:";
    private const string DoneString = "[DONE]";
    private static readonly HashSet<string> toolFinishReasons = [ "function_call", "tool_calls" ];

    public override HashSet<string> ToolFinishReasons => toolFinishReasons;
    
    public OpenAiEndpointProvider(TornadoApi api) : base(api)
    {
        Provider = LLmProviders.OpenAi;
        StoreApiAuth();
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
        req.Headers.Add("OpenAI-Beta", "assistants=v2");

        ProviderAuthentication? auth = Api.GetProvider(LLmProviders.OpenAi).Auth;
        
        if (auth is not null)
        {
            if (auth.ApiKey is not null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.ApiKey.Trim());
                req.Headers.Add("api-key", auth.ApiKey.Trim());
            }

            if (auth.Organization is not null)
            {
                req.Headers.Add("OpenAI-Organization", auth.Organization.Trim());
            }
        }
        
        return req;
    }
    
    public override T? InboundMessage<T>(string jsonData, string? postData) where T : default
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

            yield return res;
        }
    }
}