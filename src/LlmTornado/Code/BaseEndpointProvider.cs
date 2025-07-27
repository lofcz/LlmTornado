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
using LlmTornado.Code.Models;
using LlmTornado.Code.Sse;
using LlmTornado.Threads;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Code;

/// <summary>
/// Shared base used by built-in providers. Custom providers can either inherit this and override the required methods or implement <see cref="IEndpointProvider"/> directly.
/// </summary>
public abstract class BaseEndpointProvider : IEndpointProviderExtended
{
    public TornadoApi? Api { get; set; }
    public LLmProviders Provider { get; set; } = LLmProviders.Unknown;
    public JsonSchemaCapabilities JsonSchemaCapabilities { get; set; }
    
    internal static readonly JsonSerializerSettings NullSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
    
    public BaseEndpointProvider()
    {
        JsonSchemaCapabilities = GetJsonSchemaCapabilities();
    }

    public virtual JsonSchemaCapabilities GetJsonSchemaCapabilities()
    {
        return new JsonSchemaCapabilities();
    }

    public void StoreApiAuth()
    {
        if (Api?.Authentications.TryGetValue(Provider, out ProviderAuthentication? auth) ?? false)
        {
            Auth = auth;
        }
    }

    public abstract string ApiUrl(CapabilityEndpoints endpoint, string? url, IModel? model = null);
    public abstract T? InboundMessage<T>(string jsonData, string? postData, object? request);
    public abstract object? InboundMessage(Type type, string jsonData, string? postData, object? request);
    public abstract void ParseInboundHeaders<T>(T res, HttpResponseMessage response) where T : ApiResultBase;
    public abstract void ParseInboundHeaders(object? res, HttpResponseMessage response);
    public abstract IAsyncEnumerable<object?> InboundStream(Type type, StreamReader streamReader);
    public abstract IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : class;
    public abstract IAsyncEnumerable<ChatResult?> InboundStream(StreamReader streamReader, ChatRequest request, ChatStreamEventHandler? eventHandler);
    public abstract HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);
    public ProviderAuthentication? Auth { get; set; }
    
#if MODERN
    public static Version OutboundDefaultVersion { get; set; } = HttpVersion.Version20;
#else
    public static Version OutboundDefaultVersion { get; set; } = HttpVersion.Version11;
#endif

    #if MODERN
    public Version OutboundVersion { get; set; } = HttpVersion.Version20;
    #else 
    public Version OutboundVersion { get; set; } = HttpVersion.Version11;
    #endif

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
    
    /// <summary>
    /// Basic SSE stream.
    /// </summary>
    public async IAsyncEnumerable<ServerSentEvent> InboundStream(StreamReader reader)
    {
        await foreach (SseItem<string> item in SseParser.Create(reader.BaseStream).EnumerateAsync())
        {
            yield return new ServerSentEvent
            {
                Data = item.Data,
                EventType = item.EventType
            };
        }
    }
}