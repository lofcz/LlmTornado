using System.IO;
using System.Net.Http;
using LlmTornado.Images;
using LlmTornado;
using Newtonsoft.Json;

namespace LlmTornado.Code;

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
///     Represents a chat image
/// </summary>
public class ChatImage
{
    /// <summary>
    ///     Creates a new chat image
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    public ChatImage(string content)
    {
        Url = content;
    }

    /// <summary>
    ///     Creates a new chat image
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    /// <param name="detail">The detail level to use, defaults to <see cref="ImageDetail.Auto" /></param>
    public ChatImage(string content, ImageDetail? detail)
    {
        Url = content;
        Detail = detail;
    }

    /// <summary>
    ///     Publicly available URL to the image or base64 encoded content
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; }

    /// <summary>
    ///     Publicly available URL to the image or base64 encoded content
    /// </summary>
    [JsonProperty("detail")]
    public ImageDetail? Detail { get; set; }
}

/// <summary>
/// 
/// </summary>
public enum LLmProviders
{
    Unknown,
    OpenAi,
    Anthropic,
    AzureOpenAi
}

/// <summary>
/// 
/// </summary>
public enum CapabilityEndpoints
{
    Chat,
    Moderation,
    Completions,
    Embeddings,
    Models,
    Files,
    ImageGeneration,
    Audio,
    Assistants,
    ImageEdit,
    Threads,
    FineTuning
}

internal class StreamToken<T>
{
    public T? Data { get; set; }
    public bool Break { get; set; }

    public StreamToken(T? data, bool brk)
    {
        Data = data;
        Break = brk;
    } 
}

public class StreamChoicesBase
{
    
}