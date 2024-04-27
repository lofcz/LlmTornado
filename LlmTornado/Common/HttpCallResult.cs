using System;
using System.Net;

namespace LlmTornado.Common;

public class HttpCallResult<T>
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
    ///     Status code recieved
    /// </summary>
    public HttpStatusCode Code { get; set; }

    /// <summary>
    ///     Raw response from the endpoint
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    ///     Deserialized data, can be null even if <see cref="Ok" /> is true.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    ///     Whether the call succeeded, this is true every time <see cref="Code" /> is in range of 200-299 and depending on the
    ///     call also in range of 400-499
    /// </summary>
    public bool Ok { get; set; }

    /// <summary>
    ///     Network exception
    /// </summary>
    public Exception? Exception { get; set; }
}