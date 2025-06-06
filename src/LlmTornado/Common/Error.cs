using Newtonsoft.Json;

namespace LlmTornado.Common;

public sealed class Error
{
    /// <summary>
    ///     One of server_error or rate_limit_exceeded.
    /// </summary>
    [JsonProperty("code")]
    public string Code { get; set; } = null!;

    /// <summary>
    ///     A human-readable description of the error.
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; } = null!;
}