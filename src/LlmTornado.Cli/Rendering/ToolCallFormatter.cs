using System.Text;
using System.Text.Json;
using LlmTornado.Common;

namespace LlmTornado.Cli.Rendering;

/// <summary>Formats tool calls and results into compact single-line summaries.</summary>
internal static class ToolCallFormatter
{
    private const int MaxValueWidth = 40;
    private const int MaxPairs = 3;

    /// <summary>
    /// Renders up to three "key: value" pairs from the call's JSON arguments, clamped to
    /// <paramref name="maxWidth"/> display columns. Malformed or partial JSON degrades to "(…)".
    /// </summary>
    public static string SummarizeArguments(string? argumentsJson, int maxWidth)
    {
        string summary;
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            summary = "()";
        }
        else
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(argumentsJson);
                summary = doc.RootElement.ValueKind == JsonValueKind.Object
                    ? BuildPairSummary(doc.RootElement)
                    : "(" + Collapse(argumentsJson) + ")";
            }
            catch (JsonException)
            {
                summary = "(…)";
            }
        }

        if (DisplayWidth.Measure(summary) > maxWidth)
        {
            summary = DisplayWidth.TruncateToWidth(summary, Math.Max(3, maxWidth - 2)) + "…)";
        }
        return summary;
    }

    private static string BuildPairSummary(JsonElement obj)
    {
        StringBuilder sb = new("(");
        int count = 0;
        int total = 0;
        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            total++;
            if (count >= MaxPairs) continue;
            if (count > 0) sb.Append(", ");
            sb.Append(prop.Name).Append(": ").Append(RenderValue(prop.Value));
            count++;
        }
        if (total > count) sb.Append(", …");
        return sb.Append(')').ToString();
    }

    private static string RenderValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => "\"" + Truncate(Collapse(value.GetString() ?? "")) + "\"",
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
        JsonValueKind.Null => "null",
        JsonValueKind.Array => "[…]",
        JsonValueKind.Object => "{…}",
        _ => "…",
    };

    /// <summary>
    /// One-line preview of a tool result: the first meaningful line, whitespace-collapsed and
    /// truncated. Empty results become "done"; failures show the error's first line.
    /// </summary>
    public static string PreviewResult(FunctionResult result, int maxWidth)
    {
        string content = result.Content ?? "";
        string preview = Collapse(content);
        if (preview.Length == 0)
        {
            preview = result.InvocationSucceeded == false ? "failed" : "done";
        }
        return DisplayWidth.Measure(preview) > maxWidth
            ? DisplayWidth.TruncateToWidth(preview, Math.Max(1, maxWidth - 1)) + "…"
            : preview;
    }

    public static string FormatDuration(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 10) return $"{elapsed.TotalSeconds:0.0}s";
        if (elapsed.TotalMinutes < 1) return $"{elapsed.TotalSeconds:0}s";
        return $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
    }

    private static string Collapse(string text)
    {
        StringBuilder sb = new(Math.Min(text.Length, 256));
        bool lastWasSpace = true; // trims leading whitespace too
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace) sb.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                sb.Append(c);
                lastWasSpace = false;
            }
            if (sb.Length > 512) break; // previews never need more
        }
        return sb.ToString().TrimEnd();
    }

    private static string Truncate(string text) =>
        DisplayWidth.Measure(text) > MaxValueWidth
            ? DisplayWidth.TruncateToWidth(text, MaxValueWidth - 1) + "…"
            : text;
}
