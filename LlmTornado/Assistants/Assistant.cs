using System.Collections.Generic;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Assistants;

/// <summary>
///     Purpose-built AI that uses OpenAI's models and calls tools.
/// </summary>
public sealed class Assistant : ApiResultBase
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    
    /// <summary>
    ///     The Unix timestamp (in seconds) for when the assistant was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt
    {
        get => CreatedUnixTime ?? 0;
        set => CreatedUnixTime = value;
    }

    /// <summary>
    ///     The name of the assistant.
    ///     The maximum length is 256 characters.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; private set; } = null!;

    /// <summary>
    ///     The description of the assistant.
    ///     The maximum length is 512 characters.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; private set; } = null!;

    /// <summary>
    ///     The system instructions that the assistant uses.
    ///     The maximum length is 256000 characters.
    /// </summary>
    [JsonProperty("instructions")]
    public string Instructions { get; private set; } = null!;

    /// <summary>
    ///     A list of tool enabled on the assistant.
    ///     There can be a maximum of 128 tools per assistant.
    ///     Tools can be of types 'code_interpreter', 'file_search', or 'function'.
    /// </summary>
    [JsonProperty("tools"), JsonConverter(typeof(AssistantToolConverter))]
    public IReadOnlyList<AssistantTool>? Tools { get; private set; }

    /// <summary>
    ///     A set of resources that are used by the assistant's tools.
    ///     The resources are specific to the type of tool. For example,
    ///     the code_interpreter tool requires a list of file IDs, while the file_search tool requires a list of vector store IDs.
    /// </summary>
    [JsonProperty("file_ids")]
    public ToolResources? ToolResources { get; private set; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; private set; } = null!;
    
    /// <summary>
    ///     What sampling temperature to use, between 0 and 2.
    ///     Higher values like 0.8 will make the output more random,
    ///     while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    [JsonProperty("temperature")]
    public double? Temperature { get; private set; }

    /// <summary>
    ///     An alternative to sampling with temperature, called nucleus sampling,
    ///     where the model considers the results of the tokens with top_p probability mass.
    ///     So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    ///     We generally recommend altering this or temperature but not both.
    /// </summary>
    [JsonProperty("top_p")]
    public double? TopP { get; private set; }
    
    /// <summary>
    ///     Specifies the format that the model must output.
    ///     Compatible with GPT-4, GPT-4 Turbo, and all GPT-3.5 Turbo models since `gpt-3.5-turbo-1106`.
    /// </summary>
    [JsonProperty("response_format"), JsonConverter(typeof(ResponseFormatConverter))]
    public ResponseFormat? ResponseFormat { get; private set; }

    /// <summary>
    ///     Implicit conversion of Assistant object to its id
    /// </summary>
    /// <param name="assistant"></param>
    /// <returns></returns>
    public static implicit operator string(Assistant assistant)
    {
        return assistant.Id;
    }

    /// <summary>
    ///     Return id of the assistant
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Id;
    }
}