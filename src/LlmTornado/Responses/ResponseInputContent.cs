using System;
using System.Linq;
using LlmTornado.Images;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.Serialization;
using LlmTornado.Chat;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Responses;

/// <summary>
/// Base class for input content types
/// </summary>
[JsonConverter(typeof(InputContentJsonConverter))]
public abstract class ResponseInputContent : IPromptVariable
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
public class ResponseInputContentText : ResponseInputContent
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

    /// <summary>
    /// Creates a new, empty <see cref="ResponseInputContentText"/>.
    /// </summary>
    public ResponseInputContentText() { }

    /// <summary>
    /// Creates a new <see cref="ResponseInputContentText"/> with the specified text.
    /// </summary>
    /// <param name="text"></param>
    public ResponseInputContentText(string text)
    {
        Text = text;
    }
}


/// <summary>
/// Image input content
/// </summary>
public class ResponseInputContentImage : ResponseInputContent
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
    public ImageDetail? Detail { get; set; }

    /// <summary>
    /// Creates a new, empty <see cref="ResponseInputContentImage"/>.
    /// </summary>
    public ResponseInputContentImage()
    {
        
    }

    /// <summary>
    /// Creates image content from the URL of the image to be sent to the model. A fully qualified URL or base64 encoded image in a data URL.
    /// </summary>
    public static ResponseInputContentImage CreateImageUrl(string imageUrl)
    {
        return new ResponseInputContentImage
        {
            ImageUrl = imageUrl,
        };
    }
    
    /// <summary>
    /// Creates image content from the file id.
    /// </summary>
    public static ResponseInputContentImage CreateFileId(string imageUrl)
    {
        return new ResponseInputContentImage
        {
            FileId = imageUrl
        };
    }
}

/// <summary>
/// An audio input to the model.
/// </summary>
public class ResponseInputContentAudio : ResponseInputContent
{
    /// <summary>
    /// The type of the input item. Always "input_audio".
    /// </summary>
    public override string Type => "input_audio";

    /// <summary>
    /// The audio data.
    /// </summary>
    [JsonProperty("input_audio")]
    public InputAudioData InputAudio { get; set; } = new InputAudioData();
}

/// <summary>
/// Audio data for the input audio content.
/// </summary>
public class InputAudioData
{
    /// <summary>
    /// Base64-encoded audio data.
    /// </summary>
    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// The format of the audio data.
    /// </summary>
    [JsonProperty("format")]
    public InputAudioDataFormat Format { get; set; }
}

/// <summary>
/// Supported audio formats.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum InputAudioDataFormat
{
    /// <summary>
    /// MP3 format.
    /// </summary>
    [EnumMember(Value = "mp3")]
    Mp3,
    /// <summary>
    /// WAV format.
    /// </summary>
    [EnumMember(Value = "wav")]
    Wav
}

/// <summary>
/// A file input to the model.
/// </summary>
public class ResponseInputContentFile : ResponseInputContent
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

    /// <summary>
    /// Creates a new, empty <see cref="ResponseInputContentFile"/>.
    /// </summary>
    public ResponseInputContentFile() { }

    /// <summary>
    /// Creates a new <see cref="ResponseInputContentFile"/> with the specified file ID.
    /// </summary>
    /// <param name="fileId"></param>
    public ResponseInputContentFile(string fileId)
    {
        FileId = fileId;
    }

    /// <summary>
    /// Creates a new <see cref="ResponseInputContentFile"/> with the specified filename and file data.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="fileData"></param>
    public ResponseInputContentFile(string filename, string fileData)
    {
        Filename = filename;
        FileData = fileData;
    }
}

/// <summary>
/// JSON converter for InputContent types
/// </summary>
internal class InputContentJsonConverter : JsonConverter<ResponseInputContent>
{
    public override void WriteJson(JsonWriter writer, ResponseInputContent? value, JsonSerializer serializer)
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
            case ResponseInputContentText textContent:
            {
                writer.WritePropertyName("text");
                writer.WriteValue(textContent.Text);
                break;
            }
            case ResponseInputContentImage imageContent:
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
            case ResponseInputContentAudio audioContent:
            {
                writer.WritePropertyName("input_audio");
                serializer.Serialize(writer, audioContent.InputAudio);
                break;
            }
            case ResponseInputContentFile fileContent:
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

    public override ResponseInputContent? ReadJson(JsonReader reader, Type objectType, ResponseInputContent? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();

        switch (type)
        {
            case "input_text":
            {
                ResponseInputContentText textContent = new ResponseInputContentText
                {
                    Text = jo["text"]?.ToString() ?? string.Empty
                };
                return textContent;
            }
            case "input_image":
            {
                ResponseInputContentImage imageContent = new ResponseInputContentImage
                {
                    ImageUrl = jo["image_url"]?.ToString(),
                    FileId = jo["file_id"]?.ToString(),
                    Detail = jo["detail"]?.ToObject<ImageDetail>(serializer)
                };
                return imageContent;
            }
            case "input_file":
            {
                ResponseInputContentFile fileContent = new ResponseInputContentFile
                {
                    FileId = jo["file_id"]?.ToString(),
                    Filename = jo["filename"]?.ToString(),
                    FileData = jo["file_data"]?.ToString()
                };
                return fileContent;
            }
            case "input_audio":
            {
                ResponseInputContentAudio audioContent = new ResponseInputContentAudio
                {
                    InputAudio = jo["input_audio"]?.ToObject<InputAudioData>(serializer) ?? new InputAudioData()
                };
                return audioContent;
            }
            default:
            {
                throw new JsonSerializationException($"Unknown input content type: {type}");
            }
        }
    }
} 