using System;
using System.Diagnostics.CodeAnalysis;

namespace OpenAiNg.Common;

public class DataOrException<T>
{
    public DataOrException(T data)
    {
        Data = data;
    }

    public DataOrException(Exception e)
    {
        Exception = e;
    }

    private bool exceptionIsNull => Exception is null;
    private bool dataIsNull => Data is null;


    [MemberNotNullWhen(true, nameof(exceptionIsNull))]
    public T? Data { get; set; }

    [MemberNotNullWhen(true, nameof(dataIsNull))]
    public Exception? Exception { get; set; }
}