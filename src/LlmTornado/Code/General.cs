using System.Net.Http;

namespace LlmTornado.Code;

internal class General
{
    public static string IIID()
    {
        return $"_{Nanoid.Generate("0123456789abcdefghijklmnopqrstuvwxzyABCDEFGHCIJKLMNOPQRSTUVWXYZ", 23)}";
    }
}

internal enum HttpVerbs
{
    Get,
    Head,
    Post,
    Put,
    Delete,
    Connect,
    Options,
    Trace,
    Patch
}

internal static class HttpVerbsCls
{
    public static HttpMethod Get = HttpMethod.Get;
    public static HttpMethod Head = HttpMethod.Head;
    public static HttpMethod Post = HttpMethod.Post;
    public static HttpMethod Put = HttpMethod.Put;
    public static HttpMethod Delete = HttpMethod.Delete;
    public static HttpMethod Connect = new HttpMethod("CONNECT");
    public static HttpMethod Options = HttpMethod.Options;
    public static HttpMethod Trace = HttpMethod.Trace;
    public static HttpMethod Patch = new HttpMethod("PATCH");
}