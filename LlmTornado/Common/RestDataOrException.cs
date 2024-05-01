using System;
using System.Diagnostics.CodeAnalysis;

namespace LlmTornado.Common;

public class RestDataOrException<T>
{
    public RestDataOrException(T data, IHttpCallResult? httpCall)
    {
        Data = data;
        HttpCall = httpCall;
    }

    public RestDataOrException(Exception e, IHttpCallResult? httpCall)
    {
        Exception = e;
        HttpCall = httpCall;
    }
    
    public RestDataOrException(IHttpCallResult httpCall)
    {
        Exception = httpCall.Exception;
        HttpCall = httpCall;
    }

    private bool exceptionIsNull => Exception is null;
    private bool dataIsNull => Data is null;


    [MemberNotNullWhen(true, nameof(exceptionIsNull))]
    public T? Data { get; set; }

    [MemberNotNullWhen(true, nameof(dataIsNull))]
    public Exception? Exception { get; set; }
    
    public IHttpCallResult? HttpCall { get; set; }
}