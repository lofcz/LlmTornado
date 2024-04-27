using System;
using System.Collections.Generic;
using LlmTornado.Common;
using LlmTornado;
using Newtonsoft.Json;

namespace LlmTornado.Assistants;

/// <summary>
///     Purpose-built AI that uses OpenAI's models and calls tools.
/// </summary>
public sealed class AssistantResponse : ApiResultBase
{
    /// <summary>
    ///     The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; private set; }

    /// <summary>
    ///     The object type, which is always assistant.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; private set; }

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the assistant was created.
    /// </summary>
    [JsonProperty("created_at")]
    public int CreatedAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    /// <summary>
    ///     The name of the assistant.
    ///     The maximum length is 256 characters.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; private set; }

    /// <summary>
    ///     The description of the assistant.
    ///     The maximum length is 512 characters.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; private set; }

    /// <summary>
    ///     ID of the model to use.
    ///     You can use the List models API to see all of your available models,
    ///     or see our Model overview for descriptions of them.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; private set; }

    /// <summary>
    ///     The system instructions that the assistant uses.
    ///     The maximum length is 32768 characters.
    /// </summary>
    [JsonProperty("instructions")]
    public string Instructions { get; private set; }

    /// <summary>
    ///     A list of tool enabled on the assistant.
    ///     There can be a maximum of 128 tools per assistant.
    ///     Tools can be of types 'code_interpreter', 'retrieval', or 'function'.
    /// </summary>
    [JsonProperty("tools")]
    public IReadOnlyList<Tool> Tools { get; private set; }

    /// <summary>
    ///     A list of file IDs attached to this assistant.
    ///     There can be a maximum of 20 files attached to the assistant.
    ///     Files are ordered by their creation date in ascending order.
    /// </summary>
    [JsonProperty("file_ids")]
    public IReadOnlyList<string> FileIds { get; private set; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; private set; }

    public static implicit operator string(AssistantResponse? assistant)
    {
        return assistant?.Id;
    }

    public override string ToString()
    {
        return Id;
    }
}