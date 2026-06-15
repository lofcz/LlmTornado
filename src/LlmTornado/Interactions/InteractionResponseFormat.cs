using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

/// <summary>
/// Polymorphic output format for the May 2026 Interactions schema (<c>response_format</c>).
/// </summary>
[JsonConverter(typeof(InteractionResponseFormatConverter))]
public class InteractionResponseFormat
{
    /// <summary>Format discriminator: <c>text</c>, <c>image</c>, or <c>audio</c>.</summary>
    public string? Type { get; set; }

    /// <summary>MIME type for text or image output.</summary>
    public string? MimeType { get; set; }

    /// <summary>JSON schema when <see cref="MimeType"/> is <c>application/json</c>.</summary>
    public JObject? Schema { get; set; }

    /// <summary>Image aspect ratio (image format only).</summary>
    public string? AspectRatio { get; set; }

    /// <summary>Image size (image format only).</summary>
    public string? ImageSize { get; set; }

    /// <summary>Audio sample rate in Hz (audio format only).</summary>
    public int? SampleRate { get; set; }

    /// <summary>Structured JSON text output.</summary>
    public static InteractionResponseFormat Json(JObject schema) => new()
    {
        Type = "text",
        MimeType = "application/json",
        Schema = schema
    };

    /// <summary>Plain text output.</summary>
    public static InteractionResponseFormat PlainText() => new()
    {
        Type = "text",
        MimeType = "text/plain"
    };

    /// <summary>Image output configuration (replaces legacy <c>generation_config.image_config</c>).</summary>
    public static InteractionResponseFormat Image(string mimeType = "image/jpeg", string? aspectRatio = null, string? imageSize = null) => new()
    {
        Type = "image",
        MimeType = mimeType,
        AspectRatio = aspectRatio,
        ImageSize = imageSize
    };

    /// <summary>Audio output configuration.</summary>
    public static InteractionResponseFormat Audio(int sampleRate = 24000) => new()
    {
        Type = "audio",
        SampleRate = sampleRate
    };
}

internal sealed class InteractionResponseFormatConverter : JsonConverter<InteractionResponseFormat>
{
    public override InteractionResponseFormat? ReadJson(JsonReader reader, Type objectType, InteractionResponseFormat? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null)
        {
            return null;
        }

        JObject obj = JObject.Load(reader);
        string? type = obj["type"]?.ToString();

        if (type is "text" or "image" or "audio")
        {
            return new InteractionResponseFormat
            {
                Type = type,
                MimeType = obj["mime_type"]?.ToString(),
                Schema = obj["schema"] as JObject,
                AspectRatio = obj["aspect_ratio"]?.ToString(),
                ImageSize = obj["image_size"]?.ToString(),
                SampleRate = obj["sample_rate"]?.Value<int?>()
            };
        }

        // Legacy schema: bare JSON schema object without type discriminator.
        return new InteractionResponseFormat
        {
            Schema = obj
        };
    }

    public override void WriteJson(JsonWriter writer, InteractionResponseFormat? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        JObject obj = new JObject();

        if (value.Type is not null)
        {
            obj["type"] = value.Type;
        }

        if (value.MimeType is not null)
        {
            obj["mime_type"] = value.MimeType;
        }

        if (value.Schema is not null)
        {
            obj["schema"] = value.Schema;
        }

        if (value.AspectRatio is not null)
        {
            obj["aspect_ratio"] = value.AspectRatio;
        }

        if (value.ImageSize is not null)
        {
            obj["image_size"] = value.ImageSize;
        }

        if (value.SampleRate is not null)
        {
            obj["sample_rate"] = value.SampleRate;
        }

        obj.WriteTo(writer);
    }
}

/// <summary>
/// Model behavior settings for an interaction request.
/// </summary>
public class InteractionGenerationConfig
{
    [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Temperature { get; set; }

    [JsonProperty("top_p", NullValueHandling = NullValueHandling.Ignore)]
    public double? TopP { get; set; }

    [JsonProperty("max_output_tokens", NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxOutputTokens { get; set; }

    [JsonProperty("stop_sequences", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? StopSequences { get; set; }

    [JsonProperty("thinking_level", NullValueHandling = NullValueHandling.Ignore)]
    public string? ThinkingLevel { get; set; }

    [JsonProperty("thinking_summaries", NullValueHandling = NullValueHandling.Ignore)]
    public string? ThinkingSummaries { get; set; }

    /// <summary>
    /// Legacy image settings (May 2026 schema moves these to <see cref="InteractionResponseFormat"/>).
    /// </summary>
    [JsonProperty("image_config", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionLegacyImageConfig? ImageConfig { get; set; }
}

/// <summary>
/// Legacy image configuration nested under <c>generation_config.image_config</c>.
/// </summary>
public class InteractionLegacyImageConfig
{
    [JsonProperty("aspect_ratio", NullValueHandling = NullValueHandling.Ignore)]
    public string? AspectRatio { get; set; }

    [JsonProperty("image_size", NullValueHandling = NullValueHandling.Ignore)]
    public string? ImageSize { get; set; }
}
