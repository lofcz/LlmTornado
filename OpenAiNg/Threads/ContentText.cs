using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenAiNg.Threads;

public sealed class ContentText
{
    public ContentText(string value)
    {
        Value = value;
    }

    /// <summary>
    ///     The data that makes up the text.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; private set; }

    /// <summary>
    ///     Annotations.
    /// </summary>
    [JsonProperty("annotations")]
    public IReadOnlyList<Annotation> Annotations { get; private set; }

    public static implicit operator ContentText(string value)
    {
        return new ContentText(value);
    }

    public static implicit operator string(ContentText text)
    {
        return text?.ToString();
    }

    public override string ToString()
    {
        return Value;
    }
}