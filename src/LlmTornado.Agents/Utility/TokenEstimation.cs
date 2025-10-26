using System;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;

namespace LlmTornado.Agents;

/// <summary>
/// Utility class for estimating token counts from text and messages.
/// Uses character-based approximation (4 characters ? 1 token).
/// </summary>
public static class TokenEstimator
{
    /// <summary>
    /// Average character count per token (approximation for GPT-style models)
    /// </summary>
    private const double CHARS_PER_TOKEN = 4.0;

    /// <summary>
    /// Default context window size if model doesn't specify
    /// </summary>
    private const int DEFAULT_CONTEXT_WINDOW = 128000;

    /// <summary>
    /// Estimates token count from text using character-based approximation.
    /// </summary>
    /// <param name="text">The text to estimate tokens for</param>
    /// <returns>Estimated token count</returns>
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return (int)Math.Ceiling(text.Length / CHARS_PER_TOKEN);
    }

    /// <summary>
    /// Estimates token count from a chat message.
    /// </summary>
    /// <param name="message">The message to estimate tokens for</param>
    /// <returns>Estimated token count</returns>
    public static int EstimateTokens(ChatMessage message)
    {
        if (message == null)
            return 0;

        string content = message.GetMessageContent() ?? string.Empty;
        return EstimateTokens(content);
    }

    /// <summary>
    /// Gets the context window size for a given model.
    /// </summary>
    /// <param name="model">The chat model</param>
    /// <returns>Context window size in tokens</returns>
    public static int GetContextWindowSize(ChatModel? model)
    {
        if (model == null)
            return DEFAULT_CONTEXT_WINDOW;

        // Return default if ContextTokens is not set or is 0
        int contextTokens = model.ContextTokens ?? 0;
        return contextTokens > 0 ? contextTokens : DEFAULT_CONTEXT_WINDOW;
    }

    /// <summary>
    /// Calculates the utilization percentage of the context window.
    /// </summary>
    /// <param name="usedTokens">Number of tokens currently used</param>
    /// <param name="totalTokens">Total context window size</param>
    /// <returns>Utilization as a decimal (0.0 to 1.0)</returns>
    public static double CalculateUtilization(int usedTokens, int totalTokens)
    {
        if (totalTokens <= 0)
            return 0.0;

        return (double)usedTokens / totalTokens;
    }

    /// <summary>
    /// Estimates total tokens used by a list of messages.
    /// </summary>
    /// <param name="messages">The messages to estimate</param>
    /// <returns>Total estimated token count</returns>
    public static int EstimateTotalTokens(System.Collections.Generic.List<ChatMessage> messages)
    {
        if (messages == null || messages.Count == 0)
            return 0;

        int total = 0;
        foreach (var message in messages)
        {
            total += EstimateTokens(message);
        }

        return total;
    }

    /// <summary>
    /// Checks if a message exceeds a token threshold.
    /// </summary>
    /// <param name="message">The message to check</param>
    /// <param name="threshold">Token threshold</param>
    /// <returns>True if message exceeds threshold</returns>
    public static bool ExceedsThreshold(ChatMessage message, int threshold)
    {
        return EstimateTokens(message) > threshold;
    }
}
