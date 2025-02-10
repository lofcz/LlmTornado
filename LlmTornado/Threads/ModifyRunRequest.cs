using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a request to modify the properties of an existing run in the system.
/// </summary>
public class ModifyRunRequest
{
    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}