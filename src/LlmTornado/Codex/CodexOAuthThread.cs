using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Codex;

/// <summary>
/// A text-only client-side thread using direct ChatGPT subscription OAuth.
/// </summary>
public sealed class CodexOAuthThread
{
    private readonly CodexOAuthSession session;
    private readonly string baseInstructions;
    private readonly List<JObject> history = [];
    private readonly SemaphoreSlim turnLock = new SemaphoreSlim(1, 1);

    internal CodexOAuthThread(
        CodexOAuthSession session,
        string id,
        string model,
        string baseInstructions,
        string? developerInstructions)
    {
        this.session = session;
        this.baseInstructions = baseInstructions;
        Id = id;
        Model = model;

        if (!string.IsNullOrWhiteSpace(developerInstructions))
        {
            history.Add(CreateMessage("developer", developerInstructions!));
        }
    }

    /// <summary>
    /// Client-side thread identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Default model selected for this thread.
    /// </summary>
    public string Model { get; }

    /// <summary>
    /// Runs one text-only turn and waits for the streamed response to complete.
    /// </summary>
    public async Task<CodexOAuthTurnResult> RunAsync(
        string input,
        CodexOAuthTurnOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Codex input cannot be empty.", nameof(input));
        }

        await turnLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            JObject userMessage = CreateMessage("user", input);
            List<JObject> turnInput = history
                .Select(item => (JObject)item.DeepClone())
                .ToList();
            turnInput.Add(userMessage);

            CodexOAuthTurnResult result = await session.RunTextTurnAsync(
                Id,
                Model,
                baseInstructions,
                turnInput,
                options ?? new CodexOAuthTurnOptions(),
                cancellationToken).ConfigureAwait(false);
            history.Add(userMessage);
            history.AddRange(result.OutputItems.Select(item => (JObject)item.DeepClone()));

            bool hasAssistantMessage = result.OutputItems.Any(item =>
                string.Equals(item.Value<string>("type"), "message", StringComparison.Ordinal)
                && string.Equals(item.Value<string>("role"), "assistant", StringComparison.Ordinal));
            if (!hasAssistantMessage)
            {
                history.Add(CreateMessage("assistant", result.FinalResponse, "output_text"));
            }

            return result;
        }
        finally
        {
            turnLock.Release();
        }
    }

    private static JObject CreateMessage(string role, string text, string contentType = "input_text")
        => new JObject
        {
            ["type"] = "message",
            ["role"] = role,
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = contentType,
                    ["text"] = text
                }
            }
        };
}
