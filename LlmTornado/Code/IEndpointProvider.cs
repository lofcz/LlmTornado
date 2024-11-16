using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using LlmTornado.Chat;

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
    public object? InboundMessage(Type type, string jsonData, string? postData);
    public void ParseInboundHeaders<T>(T res, HttpResponseMessage response) where T : ApiResultBase;
    public void ParseInboundHeaders(object? res, HttpResponseMessage response);
    public IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : class;
    IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request);
    public TornadoApi Api { get; set; }
    public LLmProviders Provider { get; set; }
    public string ApiUrl(CapabilityEndpoints endpoint, string? url);
    public HashSet<string> ToolFinishReasons { get; }
    public ProviderAuthentication? Auth { get; set; }
    private static HashSet<string> toolFinishReasons;
    public Func<CapabilityEndpoints, string?, string>? UrlResolver { get; set; }
}

/// <summary>
/// 
/// </summary>
public interface IEndpointProviderExtended
{
    public static abstract Version OutboundVersion { get; set; }
}