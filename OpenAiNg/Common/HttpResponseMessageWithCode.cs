namespace OpenAiNg.Common;

internal class HttpResponseMessageWithCode<T>
{
    public T? Data { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}