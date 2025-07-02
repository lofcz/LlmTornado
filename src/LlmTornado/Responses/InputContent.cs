using System;
using System.Linq;
using LlmTornado.Images;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Base class for input content types
/// </summary>
[JsonConverter(typeof(InputContentJsonConverter))]
public abstract class InputContent
{
    /// <summary>
    /// The type of the input content
    /// </summary>
    [JsonProperty("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Text input content
/// </summary>
public class InputTextContent : InputContent
{
    /// <summary>
    /// The type of the input item. Always "input_text".
    /// </summary>
    public override string Type => "input_text";

    /// <summary>
    /// The text input to the model.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    public InputTextContent() { }

    public InputTextContent(string text)
    {
        Text = text;
    }
}

/// <summary>
/// Image input content
/// </summary>
public class InputImageContent : InputContent
{
    /// <summary>
    /// The type of the input item. Always "input_image".
    /// </summary>
    public override string Type => "input_image";

    /// <summary>
    /// The URL of the image to be sent to the model. A fully qualified URL or base64 encoded image in a data URL.
    /// </summary>
    [JsonProperty("image_url")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The ID of the file to be sent to the model.
    /// </summary>
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    /// <summary>
    /// The detail level of the image to be sent to the model.
    /// </summary>
    [JsonProperty("detail")]
    public ImageDetail Detail { get; set; } = ImageDetail.Auto;

    public InputImageContent() { }

    public InputImageContent(string imageUrl, ImageDetail detail = ImageDetail.Auto)
    {
        ImageUrl = imageUrl;
        Detail = detail;
    }

    public InputImageContent(string fileId, ImageDetail detail = ImageDetail.Auto, bool isFileId = true)
    {
        if (isFileId)
        {
            FileId = fileId;
        }
        else
        {
            ImageUrl = fileId;
        }
        Detail = detail;
    }
}

/// <summary>
/// File input content
/// </summary>
public class InputFileContent : InputContent
{
    /// <summary>
    /// The type of the input item. Always "input_file".
    /// </summary>
    public override string Type => "input_file";

    /// <summary>
    /// The ID of the file to be sent to the model.
    /// </summary>
    [JsonProperty("file_id")]
    public string? FileId { get; set; }

    /// <summary>
    /// The name of the file to be sent to the model.
    /// </summary>
    [JsonProperty("filename")]
    public string? Filename { get; set; }

    /// <summary>
    /// The content of the file to be sent to the model.
    /// </summary>
    [JsonProperty("file_data")]
    public string? FileData { get; set; }

    public InputFileContent() { }

    public InputFileContent(string fileId)
    {
        FileId = fileId;
    }

    public InputFileContent(string filename, string fileData)
    {
        Filename = filename;
        FileData = fileData;
    }
}

/// <summary>
/// JSON converter for InputContent types
/// </summary>
internal class InputContentJsonConverter : JsonConverter<InputContent>
{
    public override void WriteJson(JsonWriter writer, InputContent? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteValue(value.Type);

        switch (value)
        {
            case InputTextContent textContent:
            {
                writer.WritePropertyName("text");
                writer.WriteValue(textContent.Text);
                break;
            }
            case InputImageContent imageContent:
            {
                if (!string.IsNullOrEmpty(imageContent.ImageUrl))
                {
                    writer.WritePropertyName("image_url");
                    writer.WriteValue(imageContent.ImageUrl);
                }
                if (!string.IsNullOrEmpty(imageContent.FileId))
                {
                    writer.WritePropertyName("file_id");
                    writer.WriteValue(imageContent.FileId);
                }
                writer.WritePropertyName("detail");
                serializer.Serialize(writer, imageContent.Detail);
                break;
            }
            case InputFileContent fileContent:
            {
                if (!string.IsNullOrEmpty(fileContent.FileId))
                {
                    writer.WritePropertyName("file_id");
                    writer.WriteValue(fileContent.FileId);
                }
                if (!string.IsNullOrEmpty(fileContent.Filename))
                {
                    writer.WritePropertyName("filename");
                    writer.WriteValue(fileContent.Filename);
                }
                if (!string.IsNullOrEmpty(fileContent.FileData))
                {
                    writer.WritePropertyName("file_data");
                    writer.WriteValue(fileContent.FileData);
                }
                break;
            }
            default:
            {
                throw new JsonSerializationException($"Unknown InputContent type: {value.GetType()}");
            }
        }

        writer.WriteEndObject();
    }

    public override InputContent? ReadJson(JsonReader reader, Type objectType, InputContent? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();

        return type switch
        {
            "input_text" => jo.ToObject<InputTextContent>(serializer),
            "input_image" => jo.ToObject<InputImageContent>(serializer),
            "input_file" => jo.ToObject<InputFileContent>(serializer),
            _ => throw new JsonSerializationException($"Unknown input content type: {type}")
        };
    }
} 