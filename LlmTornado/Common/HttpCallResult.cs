using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace LlmTornado.Common;

/// <summary>
/// REST call result.
/// </summary>
public interface IHttpCallResult
{
    /// <summary>
    ///     Status code received.
    /// </summary>
    public HttpStatusCode Code { get; set; }

    /// <summary>
    ///     Raw response from the endpoint.
    /// </summary>
    public string? Response { get; set; }
    
    /// <summary>
    ///     Network exception.
    /// </summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// REST call request.
/// </summary>
public class HttpCallRequest
{
    /// <summary>
    ///     URL of the request.
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    ///     Method used to perform the request.
    /// </summary>
    public HttpMethod Method { get; set; }
    
    /// <summary>
    ///     Outbound headers.
    /// </summary>
    public Dictionary<string, IEnumerable<string>> Headers { get; set; } = [];
    
    /// <summary>
    ///     Content of the request.
    /// </summary>
    [JsonIgnore]
    public HttpContent? Content { get; set; }
}

/// <summary>
/// REST call result.
/// </summary>
/// <typeparam name="T"></typeparam>
public class HttpCallResult<T> : IHttpCallResult
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="response"></param>
    /// <param name="data"></param>
    /// <param name="ok"></param>
    /// <param name="request"></param>
    public HttpCallResult(HttpStatusCode code, string? response, T? data, bool ok, RestDataOrException<HttpResponseMessage> request)
    {
        Code = code;
        Response = response;
        Data = data;
        Ok = ok;
        Request = request;
    }
    
    /// <summary>
    ///     The raw request.
    /// </summary>
    public RestDataOrException<HttpResponseMessage> Request { get; set; }

    /// <summary>
    ///     Status code received.
    /// </summary>
    public HttpStatusCode Code { get; set; }

    /// <summary>
    ///     Raw response from the endpoint.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    ///     Deserialized data, can be null even if <see cref="Ok" /> is true.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    ///     Whether the call succeeded, this is true every time <see cref="Code" /> is in range of 200-299 and depending on the
    ///     call also in range of 400-499.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Data))]
    [MemberNotNullWhen(false, nameof(Exception))]
    public bool Ok { get; set; }

    /// <summary>
    ///     Network exception.
    /// </summary>
    public Exception? Exception { get; set; }
}