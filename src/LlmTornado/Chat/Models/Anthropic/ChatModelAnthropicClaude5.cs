using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 5 class models from Anthropic (Claude Fable 5, Claude Sonnet 5).
/// </summary>
public class ChatModelAnthropicClaude5 : IVendorModelClassProvider
{
    /// <summary>
    /// Claude Fable 5 — Anthropic's most capable widely released model, built for the most demanding
    /// reasoning and long-horizon agentic work. 1M context window by default, 128K max output,
    /// adaptive thinking always on (no extended thinking, no <c>thinking.type = "disabled"</c>),
    /// effort parameter supported (defaults to <c>high</c>), high-res vision (2576px long edge).
    /// Raw chain of thought is never returned; use <c>thinking.display</c> to request summaries.
    /// API model ID: <c>claude-fable-5</c>.
    /// </summary>
    public static readonly ChatModel ModelFable = new ChatModel(Fable5ModelId, LLmProviders.Anthropic, Opus47PlusContextTokens)
    {
        ReasoningTokensSpecialValues = [-1]
    };

    /// <summary>
    /// <inheritdoc cref="ModelFable"/>
    /// </summary>
    public readonly ChatModel Fable = ModelFable;

    /// <summary>
    /// Claude Sonnet 5 — the best combination of speed and intelligence.
    /// 1M context window by default, 128K max output, adaptive thinking supported,
    /// effort parameter supported (defaults to <c>high</c> on the Claude API and Claude Code),
    /// high-res vision (2576px long edge).
    /// API model ID: <c>claude-sonnet-5</c>.
    /// </summary>
    public static readonly ChatModel ModelSonnet = new ChatModel(Sonnet5ModelId, LLmProviders.Anthropic, Opus47PlusContextTokens)
    {
        ReasoningTokensSpecialValues = [-1]
    };

    /// <summary>
    /// <inheritdoc cref="ModelSonnet"/>
    /// </summary>
    public readonly ChatModel Sonnet = ModelSonnet;

    /// <summary>
    /// All known Claude 5 models from Anthropic.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelFable, ModelSonnet
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude5()
    {

    }

    private const string Fable5ModelId = ChatModelAnthropicHelper.Fable5ModelId;
    private const string Sonnet5ModelId = ChatModelAnthropicHelper.Sonnet5ModelId;
    private const int Opus47PlusContextTokens = ChatModelAnthropicHelper.Opus47PlusContextTokens;
}
