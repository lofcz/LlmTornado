using System;
using Argon;

namespace LlmTornado.Audio;

/// <summary>
///     Represents available sizes for image generation endpoints
/// </summary>
public class SpeechVoice
{
    private SpeechVoice(string? value)
    {
        Value = value ?? "";
    }

    private string Value { get; }

    /// <summary>
    ///     Requests a voice named Alloy
    /// </summary>
    public static SpeechVoice Alloy => new("alloy");

    /// <summary>
    ///     Requests a voice named Echo
    /// </summary>
    public static SpeechVoice Echo => new("echo");

    /// <summary>
    ///     Requests a voice named Fabled
    /// </summary>
    public static SpeechVoice Fable => new("fable");

    /// <summary>
    ///     Requests a voice named Onyx
    /// </summary>
    public static SpeechVoice Onyx => new("onyx");

    /// <summary>
    ///     Requests a voice named Nova
    /// </summary>
    public static SpeechVoice Nova => new("nova");

    /// <summary>
    ///     Requests a voice named Shimmer
    /// </summary>
    public static SpeechVoice Shimmer => new("shimmer");

    /// <summary>
    ///     Gets the string value for this size to pass to the API
    /// </summary>
    /// <returns>The size as a string</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Gets the string value for this size to pass to the API
    /// </summary>
    /// <param name="value">The SpeechVoice to convert</param>
    public static implicit operator string(SpeechVoice value)
    {
        return value.Value;
    }

    internal class SpeechVoiceJsonConverter : JsonConverter<SpeechVoice>
    {
        public override void WriteJson(JsonWriter writer, SpeechVoice value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }

        public override SpeechVoice ReadJson(JsonReader reader, Type objectType, SpeechVoice existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new SpeechVoice(reader.ReadAsString());
        }
    }
}