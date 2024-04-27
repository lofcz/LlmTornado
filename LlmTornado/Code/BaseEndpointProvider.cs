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

public abstract class BaseEndpointProvider : IEndpointProvider
{
    public TornadoApi Api { get; set; }
    public LLmProviders Provider { get; set; } = LLmProviders.Unknown;
    
    internal static readonly JsonSerializerSettings NullSettings = new() { NullValueHandling = NullValueHandling.Ignore };
    
    public BaseEndpointProvider(TornadoApi api)
    {
        Api = api;
    }

    public abstract string ApiUrl(CapabilityEndpoints endpoint, string? url);
    public abstract T? InboundMessage<T>(string jsonData, string? postData);
    public abstract IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : ApiResultBase;
    public abstract HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);
    public abstract HashSet<string> ToolFinishReasons { get;  }
}