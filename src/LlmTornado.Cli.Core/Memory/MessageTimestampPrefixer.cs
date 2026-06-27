using System.Globalization;
using System.Text.RegularExpressions;
using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Memory;

public static partial class MessageTimestampPrefixer
{
    private const string TimestampPrefixStart = "[timestamp role=";

    public static bool Prefix(
        ChatMessage message,
        string role,
        DateTimeOffset? timestamp = null,
        int? contextUsedPercent = null)
    {
        if (message.Role == ChatMessageRoles.Tool)
            return false;

        string prefix = BuildPrefix(role, timestamp ?? DateTimeOffset.Now, contextUsedPercent);

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            if (HasTimestampPrefix(message.Content))
                return false;

            message.Content = $"{prefix}\n{message.Content}";
            return true;
        }

        ChatMessagePart? textPart = message.Parts?.FirstOrDefault(part =>
            part.Type == ChatMessageTypes.Text && !string.IsNullOrWhiteSpace(part.Text));
        if (textPart is null)
            return false;

        if (HasTimestampPrefix(textPart.Text))
            return false;

        textPart.Text = $"{prefix}\n{textPart.Text}";
        return true;
    }

    public static int PrefixAssistantMessages(IEnumerable<ChatMessage> messages, DateTimeOffset? timestamp = null)
    {
        DateTimeOffset resolvedTimestamp = timestamp ?? DateTimeOffset.Now;
        int changed = 0;

        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatMessageRoles.Assistant && Prefix(message, "assistant", resolvedTimestamp))
                changed++;
        }

        return changed;
    }

    public static bool HasTimestampPrefix(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return TimestampPrefixRegex().IsMatch(content);
    }

    private static string BuildPrefix(string role, DateTimeOffset timestamp, int? contextUsedPercent)
    {
        DateTimeOffset utc = timestamp.ToUniversalTime();
        string budget = contextUsedPercent is not null
            ? string.Create(CultureInfo.InvariantCulture,
                $" context_used={contextUsedPercent.Value}%")
            : "";

        return string.Create(CultureInfo.InvariantCulture,
            $"[timestamp role={role} local={timestamp:yyyy-MM-ddTHH:mm:sszzz} utc={utc:yyyy-MM-ddTHH:mm:ss'Z'}{budget}]");
    }

    [GeneratedRegex(@"^\s*\[timestamp role=[^\]\r\n]+ local=\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2} utc=\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z(?: context_used=\d+%)?\]")]
    private static partial Regex TimestampPrefixRegex();
}
