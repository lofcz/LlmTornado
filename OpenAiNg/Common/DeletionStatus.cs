// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace OpenAiNg.Common;

internal sealed class DeletionStatus
{
    [JsonProperty("id")]
    public string Id { get; private set; }
    
    [JsonProperty("object")]
    public string Object { get; private set; }
    
    [JsonProperty("deleted")]
    public bool Deleted { get; private set; }
}
