using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace LlmTornado.Code;

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

    public T? InboundMessage<T>(string jsonData, string? postData);
    public IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : class;
    public TornadoApi Api { get; set; }
    public LLmProviders Provider { get; set; }
    public string ApiUrl(CapabilityEndpoints endpoint, string? url);
    public HashSet<string> ToolFinishReasons { get; }
    public ProviderAuthentication? Auth { get; set; }
    public static abstract Version OutboundVersion { get; set; }

    private static HashSet<string> toolFinishReasons;
}