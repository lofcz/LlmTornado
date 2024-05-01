using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;

namespace LlmTornado.Common;

public class RestDataOrException<T>
{
    public RestDataOrException(T data, IHttpCallResult? httpRequest)
    {
        Data = data;
        HttpResult = httpRequest;
    }
    
    public RestDataOrException(T data, HttpRequestMessage httpRequest)
    {
        Data = data;
        ParseRawRequest(httpRequest);
    }
    
    public RestDataOrException(Exception e, HttpRequestMessage httpRequest)
    {
        Exception = e;
        ParseRawRequest(httpRequest);
    }

    public RestDataOrException(Exception e, IHttpCallResult? httpRequest)
    {
        Exception = e;
        HttpResult = httpRequest;
    }
    
    public RestDataOrException(IHttpCallResult httpRequest)
    {
        Exception = httpRequest.Exception;
        HttpResult = httpRequest;
    }

    internal void ParseRawRequest(HttpRequestMessage httpRequest)
    {
        HttpRequest = new HttpCallRequest
        {
            Method = httpRequest.Method,
            Url = httpRequest.RequestUri?.AbsolutePath ?? string.Empty,
            Headers = httpRequest.Headers.ToDictionary()
        };
    }

    private bool exceptionIsNull => Exception is null;
    private bool dataIsNull => Data is null;

    [MemberNotNullWhen(true, nameof(exceptionIsNull))]
    public T? Data { get; set; }

    [MemberNotNullWhen(true, nameof(dataIsNull))]
    public Exception? Exception { get; set; }
    
    public IHttpCallResult? HttpResult { get; set; }
    public HttpCallRequest? HttpRequest { get; set; }
}