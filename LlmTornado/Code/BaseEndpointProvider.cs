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
using LlmTornado.Chat;
using LlmTornado.Vendor.Anthropic;

namespace LlmTornado.Code;

/// <summary>
/// Shared base used by built-in providers. Custom providers can either inherit this and override the required methods or implement <see cref="IEndpointProvider"/> directly.
/// </summary>
public abstract class BaseEndpointProvider : IEndpointProviderExtended
{
    public TornadoApi Api { get; set; }
    public LLmProviders Provider { get; set; } = LLmProviders.Unknown;
    
    internal static readonly JsonSerializerSettings NullSettings = new() { NullValueHandling = NullValueHandling.Ignore };
    
    public BaseEndpointProvider(TornadoApi api)
    {
        Api = api;
    }

    public void StoreApiAuth()
    {
        if (Api.Authentications.TryGetValue(Provider, out ProviderAuthentication? auth))
        {
            Auth = auth;
        }
    }

    public abstract string ApiUrl(CapabilityEndpoints endpoint, string? url);
    public abstract T? InboundMessage<T>(string jsonData, string? postData);
    public abstract object? InboundMessage(Type type, string jsonData, string? postData);
    public abstract void ParseInboundHeaders<T>(T res, HttpResponseMessage response) where T : ApiResultBase;
    public abstract void ParseInboundHeaders(object? res, HttpResponseMessage response);
    public abstract IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : class;
    public abstract IAsyncEnumerable<ChatResult?> InboundStream(StreamReader streamReader, ChatRequest request);
    public abstract HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);
    public abstract HashSet<string> ToolFinishReasons { get;  }
    public ProviderAuthentication? Auth { get; set; }
    static Version IEndpointProviderExtended.OutboundVersion { get; set; } = HttpVersion.Version20;

    private static Dictionary<Type, StreamRequestTypes> StreamTypes = new Dictionary<Type, StreamRequestTypes> {
        { typeof(ChatResult), StreamRequestTypes.Chat }
    };
    
    /// <summary>
    /// Returns stream kind based on the expected yield type.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static StreamRequestTypes GetStreamType(Type t)
    {
        return StreamTypes.GetValueOrDefault(t, StreamRequestTypes.Unknown);
    }
}