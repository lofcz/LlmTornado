using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Interface for all output content annotation types.
/// </summary>
public interface IResponseOutputContentAnnotation
{
    /// <summary>
    /// The type of the annotation.
    /// </summary>
    string Type { get; }
}

/// <summary>
/// A citation to a file.
/// </summary>
public class FileCitationAnnotation : IResponseOutputContentAnnotation
{
    [JsonProperty("type")]
    public string Type { get; set; } = "file_citation";

    [JsonProperty("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("filename")]
    public string Filename { get; set; } = string.Empty;
}

/// <summary>
/// A citation for a web resource used to generate a model response.
/// </summary>
public class UrlCitationAnnotation : IResponseOutputContentAnnotation
{
    [JsonProperty("type")]
    public string Type { get; set; } = "url_citation";

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("start_index")]
    public int StartIndex { get; set; }

    [JsonProperty("end_index")]
    public int EndIndex { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// A citation for a container file used to generate a model response.
/// </summary>
public class ContainerFileCitationAnnotation : IResponseOutputContentAnnotation
{
    [JsonProperty("type")]
    public string Type { get; set; } = "container_file_citation";

    [JsonProperty("container_id")]
    public string ContainerId { get; set; } = string.Empty;

    [JsonProperty("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonProperty("start_index")]
    public int StartIndex { get; set; }

    [JsonProperty("end_index")]
    public int EndIndex { get; set; }

    [JsonProperty("filename")]
    public string Filename { get; set; } = string.Empty;
}

/// <summary>
/// A path to a file.
/// </summary>
public class FilePathAnnotation : IResponseOutputContentAnnotation
{
    [JsonProperty("type")]
    public string Type { get; set; } = "file_path";

    [JsonProperty("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonProperty("index")]
    public int Index { get; set; }
}

internal class OutputContentAnnotationListConverter : JsonConverter<List<IResponseOutputContentAnnotation>>
{
    public override void WriteJson(JsonWriter writer, List<IResponseOutputContentAnnotation>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override List<IResponseOutputContentAnnotation>? ReadJson(JsonReader reader, Type objectType, List<IResponseOutputContentAnnotation>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JArray array = JArray.Load(reader);
        List<IResponseOutputContentAnnotation> result = new List<IResponseOutputContentAnnotation>();
        foreach (JToken? token in array)
        {
            string? type = token["type"]?.ToString();
            IResponseOutputContentAnnotation? annotation = type switch
            {
                "file_citation" => token.ToObject<FileCitationAnnotation>(serializer),
                "url_citation" => token.ToObject<UrlCitationAnnotation>(serializer),
                "container_file_citation" => token.ToObject<ContainerFileCitationAnnotation>(serializer),
                "file_path" => token.ToObject<FilePathAnnotation>(serializer),
                _ => null
            };
            if (annotation != null)
                result.Add(annotation);
        }
        return result;
    }
} 