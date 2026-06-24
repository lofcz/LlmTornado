using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Human- and machine-readable export helpers for inspecting the exact conversation payload
/// that will be supplied to the model after compression and hard-budget enforcement.
/// </summary>
public static class ContextExportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static ContextExportSnapshot CreateSnapshot(
        IReadOnlyList<ChatMessage> modelContext,
        string? conversationId = null,
        IReadOnlyList<ChatMessage>? fullHistory = null,
        string? latestSummary = null,
        int? latestSummaryCoversThrough = null,
        IReadOnlyList<ContextSnapshotInfo>? snapshots = null,
        DateTimeOffset? exportedAt = null)
    {
        List<ContextMessageExport> contextMessages = BuildMessages(modelContext, isModelContext: true);
        List<ContextMessageExport>? fullMessages = fullHistory is null
            ? null
            : BuildMessages(fullHistory, isModelContext: false);

        int totalTokens = contextMessages.Sum(m => m.EstimatedTokens);

        return new ContextExportSnapshot
        {
            ExportedAt = exportedAt ?? DateTimeOffset.UtcNow,
            ConversationId = conversationId,
            ModelContextMessageCount = contextMessages.Count,
            EstimatedModelContextTokens = totalTokens,
            ModelContext = contextMessages,
            FullHistoryMessageCount = fullMessages?.Count,
            FullHistory = fullMessages,
            LatestSummary = latestSummary,
            LatestSummaryCoversThrough = latestSummaryCoversThrough,
            Snapshots = snapshots?.ToList() ?? [],
        };
    }

    public static string ToMarkdown(ContextExportSnapshot snapshot)
    {
        StringBuilder sb = new();
        sb.AppendLine("# LlmTornado Context Export");
        sb.AppendLine();
        sb.AppendLine($"- Exported at: `{snapshot.ExportedAt:O}`");
        sb.AppendLine($"- Conversation id: `{snapshot.ConversationId ?? "<none>"}`");
        sb.AppendLine($"- Model context messages: `{snapshot.ModelContextMessageCount}`");
        sb.AppendLine($"- Estimated model context tokens: `{snapshot.EstimatedModelContextTokens}`");
        if (snapshot.FullHistoryMessageCount is not null)
            sb.AppendLine($"- Full stored history messages: `{snapshot.FullHistoryMessageCount}`");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(snapshot.LatestSummary))
        {
            sb.AppendLine("## Latest Stored Summary");
            sb.AppendLine();
            if (snapshot.LatestSummaryCoversThrough is not null)
                sb.AppendLine($"Covers through sequence: `{snapshot.LatestSummaryCoversThrough}`");
            sb.AppendLine();
            sb.AppendLine("```text");
            sb.AppendLine(snapshot.LatestSummary);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (snapshot.Snapshots.Count > 0)
        {
            sb.AppendLine("## Restore Points");
            sb.AppendLine();
            sb.AppendLine("| Id | Created At | Label | Visible Messages |");
            sb.AppendLine("|---:|---|---|---:|");
            foreach (ContextSnapshotInfo snap in snapshot.Snapshots)
                sb.AppendLine($"| {snap.Id} | `{snap.CreatedAt:O}` | {EscapeTable(snap.Label ?? "")} | {snap.MessageCount} |");
            sb.AppendLine();
        }

        sb.AppendLine("## Model Context Sent Next Turn");
        sb.AppendLine();
        AppendMessages(sb, snapshot.ModelContext);

        if (snapshot.FullHistory is not null)
        {
            sb.AppendLine("## Full Stored History");
            sb.AppendLine();
            sb.AppendLine("Includes messages that may no longer be visible in the active model context.");
            sb.AppendLine();
            AppendMessages(sb, snapshot.FullHistory);
        }

        return sb.ToString();
    }

    public static string ToJson(ContextExportSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, JsonOptions);

    private static List<ContextMessageExport> BuildMessages(IReadOnlyList<ChatMessage> messages, bool isModelContext)
    {
        List<ContextMessageExport> result = [];
        for (int i = 0; i < messages.Count; i++)
        {
            ChatMessage msg = messages[i];
            result.Add(new ContextMessageExport
            {
                Index = i,
                Id = msg.Id,
                Role = msg.Role?.ToString() ?? "unknown",
                EstimatedTokens = CompressionStrategy.EstimateTokens(msg),
                IsModelContext = isModelContext,
                Content = msg.Content,
                Parts = msg.Parts?.Select(DescribePart).ToList() ?? [],
                ToolCallId = msg.ToolCallId,
                ToolCallCount = msg.ToolCalls?.Count,
                HasReasoning = !string.IsNullOrEmpty(msg.ReasoningTokens),
                HasAudio = msg.Audio is not null,
                ImageCount = msg.Images?.Count,
            });
        }

        return result;
    }

    private static ContextPartExport DescribePart(ChatMessagePart part)
    {
        string? text = part.Text;
        return new ContextPartExport
        {
            Type = part.Type.ToString(),
            Text = text,
            TextLength = text?.Length,
            HasImage = part.Image is not null,
            HasAudio = part.Audio is not null,
            HasVideo = part.Video is not null,
            HasDocument = part.Document is not null,
            HasReasoning = part.Reasoning is not null,
            HasEncryptedContent = !string.IsNullOrEmpty(part.EncryptedContent),
            CitationCount = part.Citations?.Count,
        };
    }

    private static void AppendMessages(StringBuilder sb, IReadOnlyList<ContextMessageExport> messages)
    {
        foreach (ContextMessageExport msg in messages)
        {
            sb.AppendLine($"### {msg.Index + 1}. {msg.Role} — {msg.EstimatedTokens} estimated tokens");
            sb.AppendLine();
            sb.AppendLine($"- Id: `{msg.Id}`");
            if (!string.IsNullOrWhiteSpace(msg.ToolCallId))
                sb.AppendLine($"- Tool call id: `{msg.ToolCallId}`");
            if (msg.ToolCallCount is > 0)
                sb.AppendLine($"- Tool calls: `{msg.ToolCallCount}`");
            if (msg.HasReasoning)
                sb.AppendLine("- Has reasoning content: `true`");
            if (msg.HasAudio)
                sb.AppendLine("- Has audio output: `true`");
            if (msg.ImageCount is > 0)
                sb.AppendLine($"- Output images: `{msg.ImageCount}`");

            if (msg.Parts.Count > 0)
            {
                sb.AppendLine("- Parts:");
                foreach (ContextPartExport part in msg.Parts)
                {
                    List<string> flags = [];
                    if (part.HasImage) flags.Add("image");
                    if (part.HasAudio) flags.Add("audio");
                    if (part.HasVideo) flags.Add("video");
                    if (part.HasDocument) flags.Add("document");
                    if (part.HasReasoning) flags.Add("reasoning");
                    if (part.HasEncryptedContent) flags.Add("encrypted");
                    if (part.CitationCount is > 0) flags.Add($"citations={part.CitationCount}");
                    string suffix = flags.Count > 0 ? $" ({string.Join(", ", flags)})" : "";
                    sb.AppendLine($"  - `{part.Type}`{suffix}");
                }
            }

            string content = msg.Content ?? string.Join("\n", msg.Parts.Select(p => p.Text).Where(t => !string.IsNullOrEmpty(t)));
            if (!string.IsNullOrEmpty(content))
            {
                sb.AppendLine();
                sb.AppendLine("```text");
                sb.AppendLine(content);
                sb.AppendLine("```");
            }
            else if (msg.Parts.Count > 0)
            {
                foreach (ContextPartExport part in msg.Parts.Where(p => !string.IsNullOrEmpty(p.Text)))
                {
                    sb.AppendLine();
                    sb.AppendLine($"Part `{part.Type}` text:");
                    sb.AppendLine("```text");
                    sb.AppendLine(part.Text);
                    sb.AppendLine("```");
                }
            }

            sb.AppendLine();
        }
    }

    private static string EscapeTable(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);
}

public sealed class ContextExportSnapshot
{
    public required DateTimeOffset ExportedAt { get; init; }
    public string? ConversationId { get; init; }
    public required int ModelContextMessageCount { get; init; }
    public required int EstimatedModelContextTokens { get; init; }
    public required List<ContextMessageExport> ModelContext { get; init; }
    public int? FullHistoryMessageCount { get; init; }
    public List<ContextMessageExport>? FullHistory { get; init; }
    public string? LatestSummary { get; init; }
    public int? LatestSummaryCoversThrough { get; init; }
    public List<ContextSnapshotInfo> Snapshots { get; init; } = [];
}

public sealed class ContextSnapshotInfo
{
    public required long Id { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? Label { get; init; }
    public required int MessageCount { get; init; }
}

public sealed class ContextMessageExport
{
    public required int Index { get; init; }
    public required Guid Id { get; init; }
    public required string Role { get; init; }
    public required int EstimatedTokens { get; init; }
    public required bool IsModelContext { get; init; }
    public string? Content { get; init; }
    public List<ContextPartExport> Parts { get; init; } = [];
    public string? ToolCallId { get; init; }
    public int? ToolCallCount { get; init; }
    public bool HasReasoning { get; init; }
    public bool HasAudio { get; init; }
    public int? ImageCount { get; init; }
}

public sealed class ContextPartExport
{
    public required string Type { get; init; }
    public string? Text { get; init; }
    public int? TextLength { get; init; }
    public bool HasImage { get; init; }
    public bool HasAudio { get; init; }
    public bool HasVideo { get; init; }
    public bool HasDocument { get; init; }
    public bool HasReasoning { get; init; }
    public bool HasEncryptedContent { get; init; }
    public int? CitationCount { get; init; }
}
