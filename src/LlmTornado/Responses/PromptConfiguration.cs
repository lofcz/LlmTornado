using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Reference to a prompt template and its variables
/// </summary>
public class PromptConfiguration
{
    /// <summary>
    /// The unique identifier of the prompt template to use.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Optional version of the prompt template.
    /// </summary>
    [JsonProperty("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Optional map of values to substitute in for variables in your prompt.
    /// The substitution values can either be strings, or other Response input types like images or files.
    /// </summary>
    [JsonProperty("variables")]
    public Dictionary<string, object>? Variables { get; set; }

    public PromptConfiguration() { }

    public PromptConfiguration(string id, string? version = null, Dictionary<string, object>? variables = null)
    {
        Id = id;
        Version = version;
        Variables = variables;
    }
} 