using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    ///     Body of the request.
    /// </summary>
    public string? Body { get; set; }
}

/// <summary>
/// Failed HTTP call.
/// </summary>
public class ErrorHttpCallResult : IHttpCallResult
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
    ///     Exception.
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Creates new error result.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="response"></param>
    /// <param name="exception"></param>
    public ErrorHttpCallResult(HttpStatusCode code, string? response, Exception? exception)
    {
        Code = code;
        Response = response;
        Exception = exception;
    }
}

public class HttpResponseData
{
    public Dictionary<string, string>? Headers { get; set; }

    internal static HttpResponseData Instantiate(HttpResponseMessage message)
    {
        HttpResponseData data = new HttpResponseData
        {
            Headers = []
        };

        foreach (KeyValuePair<string, IEnumerable<string>> x in message.Headers)
        {
            data.Headers[x.Key] = x.Value.FirstOrDefault() ?? string.Empty;
        }
        
        return data;
    }
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
    public HttpCallResult(HttpStatusCode code, string? response, T? data, bool ok, RestDataOrException<HttpResponseData> request)
    {
        Code = code;
        Response = response;
        Data = data;
        Ok = ok;
        Request = request;
    }
    
    /// <summary>
    /// Raw request data.
    /// </summary>
    public RestDataOrException<HttpResponseData> Request { get; set; }

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