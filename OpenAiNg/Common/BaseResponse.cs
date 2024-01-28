using System.Text.Json.Serialization;

namespace OpenAiNg.Common;

public abstract class BaseResponse
{
    /// <summary>
    ///     The <see cref="OpenAiApi" /> this response was generated from.
    /// </summary>
    [JsonIgnore]
    public OpenAiApi Api { get; internal set; }
}