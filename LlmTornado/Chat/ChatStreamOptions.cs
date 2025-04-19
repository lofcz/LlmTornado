using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
///     Configuration of the stream.
/// </summary>
public class ChatStreamOptions
{
    /// <summary>
    ///     When streaming, pre 5/24 OpenAI wouldn't return the amount of tokens used in the request making counting them hard due to their
    ///     internal transformations of the input. The setting currently has no effect on the other vendors. It is recommended to use this unless you target an old
    ///     Azure instance without support of this field.
    /// </summary>
    [JsonProperty("include_usage")]
    public bool IncludeUsage { get; set; }

    /// <summary>
    ///     When streaming, pre 5/24 OpenAI wouldn't return the amount of tokens used in the request making counting them hard due to their
    ///     internal transformations of the input. The setting currently has no effect on the other vendors.
    /// </summary>
    public static readonly ChatStreamOptions KnownOptionsIncludeUsage = new ChatStreamOptions
    {
        IncludeUsage = true
    };
    
    /// <summary>
    ///     Using the "exclude" option omits stream_options from the request.
    /// </summary>
    public static readonly ChatStreamOptions KnownOptionsExcludeUsage = new ChatStreamOptions
    {
        IncludeUsage = false
    };

    /// <summary>
    /// Creates a copy of the options
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ChatStreamOptions Duplicate(ChatStreamOptions options)
    {
        return new ChatStreamOptions
        {
            IncludeUsage = options.IncludeUsage
        };
    }
}