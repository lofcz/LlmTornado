namespace LlmTornado.Responses;

/// <summary>
/// Values used in <see cref="PromptConfiguration.Variables"/> dictionary.
/// Implementing types:
/// - <see cref="PromptVariableString"/>
/// - any <see cref="ResponseInputContent"/> subtype (images, files, text).
/// </summary>
public interface IPromptVariable
{
    
}