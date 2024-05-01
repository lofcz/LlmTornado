using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

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
    public HttpCallResult(HttpStatusCode code, string? response, T? data, bool ok)
    {
        Code = code;
        Response = response;
        Data = data;
        Ok = ok;
    }

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