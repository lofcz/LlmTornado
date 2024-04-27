using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace LlmTornado.Code;

/// <summary>
/// 
/// </summary>
public interface IEndpointProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="verb"></param>
    /// <param name="data"></param>
    /// <param name="streaming"></param>
    /// <returns></returns>
    public HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);

    public T? InboundMessage<T>(string jsonData);
    public IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : ApiResultBase;
    public TornadoApi Api { get; set; }
    public LLmProviders Provider { get; set; }
    public string ApiUrl(CapabilityEndpoints endpoint, string? url);
    public HashSet<string> ToolFinishReasons { get;  }

    private static HashSet<string> toolFinishReasons;
}