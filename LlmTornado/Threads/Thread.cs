using System.Collections.Generic;
using LlmTornado.Assistants;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     A conversation session between an Assistant and a user.
///     Threads store Messages and automatically handle truncation to fit content into a model's context.
/// </summary>
public sealed class Thread : ApiResultBase
{
    /// <summary>
    ///     The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the thread was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt
    {
        get => CreatedUnixTime ?? 0;
        set => CreatedUnixTime = value;
    }
    /// <summary>
    ///     A set of resources that are used by the assistant's tools.
    ///     The resources are specific to the type of tool. For example,
    ///     the code_interpreter tool requires a list of file IDs, while the file_search tool requires a list of vector store IDs.
    /// </summary>
    [JsonProperty("tool_resources")]
    public ToolResources? ToolResources { get; set; }

    /// <summary>
    ///     Set of up to 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = null!;

    /// <summary>
    ///     Implicit conversion of Thread object to its id
    /// </summary>
    public static implicit operator string(Thread thread)
    {
        return thread.ToString();
    }

    /// <summary>
    /// </summary>
    /// <returns>
    ///     Returns the id of the thread
    /// </returns>
    public override string ToString()
    {
        return Id;
    }
}