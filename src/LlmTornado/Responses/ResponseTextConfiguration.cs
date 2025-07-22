namespace LlmTornado.Responses;

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///    Represents the full "text" request/response configuration object. The <c>format</c> field is the
///    polymorphic part describing how the model should format the textual output (plain text, JSON schema, etc.).
///    Any additional top-level properties will be captured inside <see cref="AdditionalProperties"/> so that the
///    library remains forward-compatible when new fields are introduced by the vendor.
/// </summary>
public class ResponseTextConfiguration
{
    /// <summary>
    ///     The format definition that instructs the model how to structure the textual output. This is the part that
    ///     varies by <c>type</c> (e.g. <c>text</c>, <c>json_schema</c>, <c>json_object</c>).
    /// </summary>
    [JsonProperty("format")]
    public ResponseTextFormatConfiguration? Format { get; set; }

    /// <summary>
    ///     Any vendor-specific or future properties that we don't explicitly model yet are stored here to keep the raw data
    ///     round-trippable.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JToken>? AdditionalProperties { get; set; }

    /// <summary>
    ///     Creates a new instance with the given <paramref name="format"/>.
    /// </summary>
    public ResponseTextConfiguration(ResponseTextFormatConfiguration format)
    {
        Format = format;
    }

    /// <summary>
    ///     Parameter-less constructor for deserialisation.
    /// </summary>
    public ResponseTextConfiguration() { }

    /// <summary>
    /// Creates a new text configuration from the format.
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    public static implicit operator ResponseTextConfiguration(ResponseTextFormatConfiguration format) => new ResponseTextConfiguration(format);
} 