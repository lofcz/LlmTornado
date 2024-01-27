// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenAiNg.Threads;

public sealed class Annotation
{
    [JsonInclude]
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter<AnnotationType>))]
    public AnnotationType Type { get; private set; }

    /// <summary>
    /// The text in the message content that needs to be replaced.
    /// </summary>
    [JsonInclude]
    [JsonProperty("text")]
    public string Text { get; private set; }

    /// <summary>
    /// A citation within the message that points to a specific quote from a
    /// specific File associated with the assistant or the message.
    /// Generated when the assistant uses the 'retrieval' tool to search files.
    /// </summary>
    [JsonInclude]
    [JsonProperty("file_citation")]
    public FileCitation FileCitation { get; private set; }

    /// <summary>
    /// A URL for the file that's generated when the assistant used the 'code_interpreter' tool to generate a file.
    /// </summary>
    [JsonInclude]
    [JsonProperty("file_path")]
    public FilePath FilePath { get; private set; }

    [JsonInclude]
    [JsonProperty("start_index")]
    public int StartIndex { get; private set; }

    [JsonInclude]
    [JsonProperty("end_index")]
    public int EndIndex { get; private set; }
}
