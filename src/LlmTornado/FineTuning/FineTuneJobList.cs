using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.FineTuning;

[Obsolete("Use ListResponse<FineTuneJobResponse>")]
public sealed class FineTuneJobList
{
    [JsonProperty("object")] 
    public string Object { get; private set; }

    [JsonProperty("data")] 
    public IReadOnlyList<FineTuneJob> Jobs { get; private set; }
    
    [JsonProperty("has_more")]
    public bool HasMore { get; private set; }
}