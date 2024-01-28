using System;
using Newtonsoft.Json;

namespace OpenAiNg.Audio;

/// <summary>
///     Represents available sizes for image generation endpoints
/// </summary>
public class SpeechResponseFormat
{
    private SpeechResponseFormat(string? value)
    {
        Value = value ?? "";
    }

    private string Value { get; }

    /// <summary>
    ///     Requests response in mp3 format
    /// </summary>
    public static SpeechResponseFormat Mp3 => new("mp3");

    /// <summary>
    ///     Requests response in opus format
    /// </summary>
    public static SpeechResponseFormat Opus => new("opus");

    /// <summary>
    ///     Requests a response in aac format
    /// </summary>
    public static SpeechResponseFormat Aac => new("aac");

    /// <summary>
    ///     Requests a response in flac format
    /// </summary>
    public static SpeechResponseFormat Flac => new("flac");

    /// <summary>
    ///     Gets the string value for this size to pass to the API
    /// </summary>
    /// <returns>The format as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this format to pass to the API
    /// </summary>
    /// <param name="value">The SpeechResponseFormat to convert</param>
    public static implicit operator string(SpeechResponseFormat value)
    {
        return value.Value;
    }

    internal class SpeechResponseFormatJsonConverter : JsonConverter<SpeechResponseFormat>
    {
        public override void WriteJson(JsonWriter writer, SpeechResponseFormat value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }

        public override SpeechResponseFormat ReadJson(JsonReader reader, Type objectType, SpeechResponseFormat existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new SpeechResponseFormat(reader.ReadAsString());
        }
    }
}