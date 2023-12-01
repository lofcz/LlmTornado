using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace OpenAiNg.Code;

public class Ref<T>
{
    public T? Ptr { get; set; }
}

public class StreamResponse
{
    public Stream Stream { get; set; }
    public ApiResultBase Headers { get; set; }
    public HttpResponseMessage Response { get; set; }
}

/// <summary>
/// Represents a chat image
/// </summary>
public class ChatImage
{
    /// <summary>
    /// ChatImage URL
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; }

    /// <summary>
    /// Creates a new chat image
    /// </summary>
    /// <param name="url">Absolute uri to the image</param>
    public ChatImage(string url)
    {
        Url = url;
    }
}