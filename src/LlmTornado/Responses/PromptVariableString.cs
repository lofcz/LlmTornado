using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Wrapper for string values in prompt variables.
/// </summary>
[JsonConverter(typeof(PromptVariableStringConverter))]
public class PromptVariableString : IPromptVariable
{
    /// <summary>
    /// Value of the variable.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Creates a string prompt variable.
    /// </summary>
    public PromptVariableString(string value)
    {
        Value = value;
    }
    
    public static implicit operator PromptVariableString(string value) => new PromptVariableString(value);
    public static implicit operator string(PromptVariableString wrapper) => wrapper.Value;
} 