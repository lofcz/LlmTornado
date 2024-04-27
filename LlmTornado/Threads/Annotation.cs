using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Threads;

public sealed class Annotation
{
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AnnotationType Type { get; private set; }

    /// <summary>
    ///     The text in the message content that needs to be replaced.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; private set; }

    /// <summary>
    ///     A citation within the message that points to a specific quote from a
    ///     specific File associated with the assistant or the message.
    ///     Generated when the assistant uses the 'retrieval' tool to search files.
    /// </summary>
    [JsonProperty("file_citation")]
    public FileCitation FileCitation { get; private set; }

    /// <summary>
    ///     A URL for the file that's generated when the assistant used the 'code_interpreter' tool to generate a file.
    /// </summary>
    [JsonProperty("file_path")]
    public FilePath FilePath { get; private set; }

    [JsonProperty("start_index")] 
    public int StartIndex { get; private set; }

    [JsonProperty("end_index")] 
    public int EndIndex { get; private set; }
}