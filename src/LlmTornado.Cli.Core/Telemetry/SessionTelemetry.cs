using LlmTornado.Agents.DataModels;

namespace LlmTornado.Cli.Core.Telemetry;

/// <summary>
/// Folds the runner's token telemetry events into per-turn and per-session figures.
/// A single agent turn issues one model request per tool-loop iteration, so
/// <see cref="AgentRunnerUsageReceivedEvent"/> fires multiple times per turn: the last
/// prompt-token figure is the current context size; completion tokens accumulate.
/// </summary>
public sealed class SessionTelemetry
{
    private readonly object _sync = new();

    /// <summary>Preflight telemetry for the most recent request (estimate or provider tokenizer).</summary>
    public AgentRequestTokenTelemetry? LastRequest { get; private set; }

    /// <summary>Real usage from the most recent model response, when the provider reports it.</summary>
    public AgentUsageTelemetry? LastUsage { get; private set; }

    /// <summary>Completion tokens accumulated across all requests of the current/last turn.</summary>
    public int TurnCompletionTokens { get; private set; }

    /// <summary>Reasoning tokens accumulated across the current/last turn (0 when not reported).</summary>
    public int TurnReasoningTokens { get; private set; }

    /// <summary>Number of usage events observed this turn (= model requests in the tool loop).</summary>
    public int TurnRequestCount { get; private set; }

    /// <summary>Compression/trim events observed this session.</summary>
    public int CompressionEvents { get; private set; }

    /// <summary>True once at least one real usage report has been received.</summary>
    public bool HasRealUsage => LastUsage is not null;

    /// <summary>
    /// Best real-data estimate of what the next request's prompt will contain: the last prompt
    /// plus the completion that followed it (which is now part of history). Null until real
    /// usage arrives or after <see cref="InvalidateUsage"/>.
    /// </summary>
    public int? EstimatedNextPromptTokens
    {
        get
        {
            lock (_sync)
            {
                return LastUsage is null ? null : LastUsage.PromptTokens + LastUsage.CompletionTokens;
            }
        }
    }

    /// <summary>Reset per-turn accumulators. Call before each agent invocation.</summary>
    public void BeginTurn()
    {
        lock (_sync)
        {
            TurnCompletionTokens = 0;
            TurnReasoningTokens = 0;
            TurnRequestCount = 0;
        }
    }

    public void OnRequestPrepared(AgentRequestTokenTelemetry telemetry)
    {
        lock (_sync)
        {
            LastRequest = telemetry;
        }
    }

    public void OnUsageReceived(AgentUsageTelemetry usage)
    {
        lock (_sync)
        {
            LastUsage = usage;
            TurnRequestCount++;
            TurnCompletionTokens += usage.CompletionTokens;
            TurnReasoningTokens += usage.CompletionReasoningTokens ?? 0;
        }
    }

    /// <summary>
    /// History was rewritten (compression or trim): the last real prompt figure no longer
    /// describes the next request. Real numbers resume with the next usage event.
    /// </summary>
    public void InvalidateUsage()
    {
        lock (_sync)
        {
            LastUsage = null;
            CompressionEvents++;
        }
    }
}
