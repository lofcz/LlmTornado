using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from OpenAI.
/// </summary>
public class ChatModelOpenAi : IVendorModelProvider
{
    /// <summary>
    /// GPT 3.5 (Turbo) models.
    /// </summary>
    public readonly ChatModelOpenAiGpt35 Gpt35 = new ChatModelOpenAiGpt35();

    /// <summary>
    /// GPT 4 (Turbo) models.
    /// </summary>
    public readonly ChatModelOpenAiGpt4 Gpt4 = new ChatModelOpenAiGpt4();

    /// <summary>
    /// All known chat models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ..ChatModelOpenAiGpt35.ModelsAll,
        ..ChatModelOpenAiGpt4.ModelsAll
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAi()
    {
        
    }
}