// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenAiNg.Threads;

public sealed class ContentText
{
    public ContentText(string value) => Value = value;

    /// <summary>
    /// The data that makes up the text.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; private set; }

    /// <summary>
    /// Annotations.
    /// </summary>
    [JsonProperty("annotations")]
    public IReadOnlyList<Annotation> Annotations { get; private set; }

    public static implicit operator ContentText(string value) => new ContentText(value);

    public static implicit operator string(ContentText text) => text?.ToString();

    public override string ToString() => Value;
}
