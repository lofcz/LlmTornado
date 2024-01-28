using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenAiNg.Common;

public sealed class ListResponse<T> : ApiResultBase, IListResponse<T>
{
    [JsonProperty("object")] public string Object { get; private set; }

    [JsonProperty("has_more")] public bool HasMore { get; private set; }

    [JsonProperty("first_id")] public string FirstId { get; private set; }

    [JsonProperty("last_id")] public string LastId { get; private set; }

    [JsonProperty("data")] public IReadOnlyList<T> Items { get; private set; } = [];
}