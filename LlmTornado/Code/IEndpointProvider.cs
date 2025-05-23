using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using LlmTornado.Chat;

namespace LlmTornado.Code;

/// <summary>
/// Interface for all endpoint providers.
/// </summary>
public interface IEndpointProvider
{
    /// <summary>
    /// Invoked to construct outbound messages.
    /// </summary>
    public HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);

    /// <summary>
    /// Invoked to parse inbound messages.
    /// </summary>
    public T? InboundMessage<T>(string jsonData, string? postData);
    
    /// <summary>
    /// Invoked to parse inbound messages.
    /// </summary>
    public object? InboundMessage(Type type, string jsonData, string? postData);

    /// <summary>
    /// Invoked to parse headers of inbound requests.
    /// </summary>
    public void ParseInboundHeaders<T>(T res, HttpResponseMessage response) where T : ApiResultBase;
    
    /// <summary>
    /// Invoked to parse headers of inbound requests.
    /// </summary>
    public void ParseInboundHeaders(object? res, HttpResponseMessage response);
    
    /// <summary>
    /// Invoked to process any inbound streams where type conditionally changes.
    /// </summary>
    public IAsyncEnumerable<object?> InboundStream(Type type, StreamReader streamReader);
    
    /// <summary>
    /// Invoked to process any inbound streams where type is known ahead of time.
    /// </summary>
    public IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : class;
    
    /// <summary>
    /// Streaming for chat requests.
    /// </summary>
    IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request);
    
    /// <summary>
    /// API instance owning this provider. Note that this is not null only after the provider is enlisted in the API.
    /// </summary>
    public TornadoApi? Api { get; set; }
    
    /// <summary>
    /// Known/custom provider.
    /// </summary>
    public LLmProviders Provider { get; set; }
    
    /// <summary>
    /// API url resolver.
    /// </summary>
    public string ApiUrl(CapabilityEndpoints endpoint, string? url);

    /// <summary>
    /// Authentication.
    /// </summary>
    public ProviderAuthentication? Auth { get; set; }
    
    /// <summary>
    /// Invoked when resolving concrete API url.<br/>
    /// Arguments: endpoint, url (if any). This function can return deferred fragments <c>{0}</c>, <c>{1}</c> which are resolved to endpoint and url fragment, for example:<br/><c>https://api.anthropic.com/v1/{0}{1}</c>.<br/>Alternatively, fully resolved url can be returned.
    /// </summary>
    public Func<CapabilityEndpoints, string?, string>? UrlResolver { get; set; }
    
    /// <summary>
    /// Invoked when outbound request is constructed. Can be used to customize headers/url/content.<br/>
    /// Arguments: the request, outbound data, whether the request is streaming or not
    /// </summary>
    public Action<HttpRequestMessage, object?, bool>? RequestResolver { get; set; }
}

/// <summary>
/// Extended capabilities providers.
/// </summary>
public interface IEndpointProviderExtended
{
    /// <summary>
    /// Gets version of the protocol.
    /// </summary>
    public static abstract Version OutboundVersion { get; set; }
}