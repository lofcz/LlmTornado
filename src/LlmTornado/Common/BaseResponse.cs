using Newtonsoft.Json;

namespace LlmTornado.Common;

public abstract class BaseResponse
{
    /// <summary>
    ///     The <see cref="TornadoApi" /> this response was generated from.
    /// </summary>
    [JsonIgnore]
    public TornadoApi Api { get; internal set; }
}