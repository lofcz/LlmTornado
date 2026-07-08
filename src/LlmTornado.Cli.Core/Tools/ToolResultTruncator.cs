using LlmTornado.ChatFunctions;
using LlmTornado.Common;

namespace LlmTornado.Cli.Core.Tools;

/// <summary>
/// Caps the size of tool results before they enter the model context (wired into
/// <c>TornadoAgent.ToolResultProcessor</c>). One oversized read/grep/shell result can otherwise
/// consume a small local model's whole window and force an immediate compression rewrite.
/// Keeps the head (70%) and tail (30%) with an explicit marker, so the model can see both the
/// beginning of the output and its final lines (errors, exit codes) and knows to narrow its query.
/// </summary>
public sealed class ToolResultTruncator
{
    private const double HeadFraction = 0.7;

    private readonly Func<int> _maxTokensProvider;
    private readonly HashSet<string> _exemptTools;

    /// <param name="maxTokensProvider">
    /// Effective per-result token cap, re-read on every call so /context cap and model switches
    /// apply immediately.
    /// </param>
    /// <param name="exemptTools">Tool names whose results are never truncated.</param>
    public ToolResultTruncator(Func<int> maxTokensProvider, IEnumerable<string>? exemptTools = null)
    {
        _maxTokensProvider = maxTokensProvider;
        _exemptTools = new HashSet<string>(exemptTools ?? [], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Signature-compatible with <c>TornadoAgent.ToolResultProcessor</c>.</summary>
    public ValueTask Process(string toolName, FunctionResult result, FunctionCall call)
    {
        if (!_exemptTools.Contains(toolName) && result.Content is { Length: > 0 } content)
        {
            string truncated = Truncate(content, _maxTokensProvider());
            if (!ReferenceEquals(truncated, content))
                result.Content = truncated;
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Returns the original string when it fits; otherwise head + marker + tail. The cap is in
    /// estimated tokens (chars/4, matching the compression estimator).
    /// </summary>
    internal static string Truncate(string content, int maxTokens)
    {
        if (maxTokens <= 0)
            return content;

        int maxChars = maxTokens * 4;
        if (content.Length <= maxChars)
            return content;

        int headChars = (int)(maxChars * HeadFraction);
        int tailChars = maxChars - headChars;

        int headEnd = AdjustBackwardToCharBoundary(content, headChars);
        int tailStart = AdjustForwardToCharBoundary(content, content.Length - tailChars);

        int removed = tailStart - headEnd;
        string marker = $"\n[... {removed:N0} characters truncated — call the tool again with a narrower scope for the full output ...]\n";

        return string.Concat(content.AsSpan(0, headEnd), marker, content.AsSpan(tailStart));
    }

    /// <summary>Never split a surrogate pair: move the cut left if it lands on a low surrogate.</summary>
    private static int AdjustBackwardToCharBoundary(string s, int index)
    {
        while (index > 0 && char.IsLowSurrogate(s[index]))
            index--;
        return index;
    }

    private static int AdjustForwardToCharBoundary(string s, int index)
    {
        while (index < s.Length && char.IsLowSurrogate(s[index]))
            index++;
        return index;
    }
}
