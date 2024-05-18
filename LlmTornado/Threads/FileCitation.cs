using System.Text.Json.Serialization;
using Argon;

namespace LlmTornado.Threads;

public sealed class FileCitation
{
    /// <summary>
    ///     The ID of the specific File the citation is from.
    /// </summary>
    [JsonInclude]
    [JsonProperty("file_id")]
    public string FileId { get; private set; }

    /// <summary>
    ///     The specific quote in the file.
    /// </summary>
    [JsonInclude]
    [JsonProperty("quote")]
    public string Quote { get; private set; }
}