using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.FineTuning;

[Obsolete("Use ListResponse<FineTuneJobResponse>")]
public sealed class FineTuneJobList
{
    [JsonInclude] [JsonProperty("object")] public string Object { get; private set; }

    [JsonInclude] [JsonProperty("data")] public IReadOnlyList<FineTuneJob> Jobs { get; private set; }

    [JsonInclude]
    [JsonProperty("has_more")]
    public bool HasMore { get; private set; }
}