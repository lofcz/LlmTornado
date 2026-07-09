using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Telemetry;

namespace LlmTornado.Cli.Commands;

internal sealed class ContextCommand : ICliCommand
{
    private readonly ConversationMemoryManager _memoryManager;
    private readonly SqliteConversationStore _store;
    private readonly string _defaultExportDirectory;
    private readonly AgentSettings _settings;
    private readonly ISettingsPersistence _persistence;
    private readonly SessionTelemetry? _telemetry;

    public string Name => "context";
    public string Description => "Inspect or export the active model context";
    public string Usage => "/context [stats | export [path] [--format markdown|json] [--full] | compress | cap [status|off|<tokens>]]";

    public ContextCommand(
        ConversationMemoryManager memoryManager,
        SqliteConversationStore store,
        string defaultExportDirectory,
        AgentSettings? settings = null,
        ISettingsPersistence? persistence = null,
        SessionTelemetry? telemetry = null)
    {
        _memoryManager = memoryManager;
        _store = store;
        _defaultExportDirectory = defaultExportDirectory;
        _settings = settings ?? new AgentSettings();
        _persistence = persistence ?? NoOpSettingsPersistence.Instance;
        _telemetry = telemetry;
    }

    public async Task<bool> ExecuteAsync(string[] args)
    {
        string action = args.Length == 0 ? "stats" : args[0].ToLowerInvariant();
        string[] rest = args.Length > 1 ? args[1..] : [];

        switch (action)
        {
            case "stats":
                WriteStats();
                break;

            case "export":
            case "dump":
                Export(rest);
                break;

            case "compress":
            case "summarize":
                await Compress();
                break;

            case "cap":
                HandleCap(rest);
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return true;
    }

    private void WriteStats()
    {
        List<Chat.ChatMessage> messages = _memoryManager.GetMessagesForAgent();
        int tokens = messages.Sum(CompressionStrategy.EstimateTokens);
        ConsoleRenderer.WriteInfo($"Context messages: {messages.Count}");
        if (_telemetry?.EstimatedNextPromptTokens is int real)
        {
            ConsoleRenderer.WriteInfo($"Context tokens: {real:N0} (measured — last prompt + completion)");
            ConsoleRenderer.WriteInfo($"Estimated tokens: {tokens:N0} (chars/4, for comparison)");
        }
        else
        {
            ConsoleRenderer.WriteInfo($"Estimated tokens: {tokens:N0} (~chars/4; no provider usage yet)");
        }

        if (_telemetry is not null)
        {
            if (_telemetry.TurnRequestCount > 0)
            {
                ConsoleRenderer.WriteInfo(
                    $"Last turn: {_telemetry.TurnRequestCount} request(s), out {_telemetry.TurnCompletionTokens:N0}"
                    + (_telemetry.TurnReasoningTokens > 0 ? $", reasoning {_telemetry.TurnReasoningTokens:N0}" : ""));
            }

            if (_telemetry.CompressionEvents > 0)
                ConsoleRenderer.WriteInfo($"Compression events this session: {_telemetry.CompressionEvents}");
        }
        ConsoleRenderer.WriteInfo($"Compression context window: {_memoryManager.EffectiveCompressionContextTokens:N0}");
        ConsoleRenderer.WriteInfo(_memoryManager.CompressionContextTokenCap is > 0
            ? $"Compression cap: {_memoryManager.CompressionContextTokenCap.Value:N0}"
            : "Compression cap: none (uses model context)");
        ConsoleRenderer.WriteInfo($"Conversation id: {_memoryManager.ConversationId ?? "<none>"}");
        ConsoleRenderer.WriteInfo("Use /context export to review the full context in a file.");
    }

    private void HandleCap(string[] args)
    {
        if (args.Length == 0 || args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            WriteCapStatus();
            return;
        }

        string value = args[0].ToLowerInvariant();
        if (value is "off" or "none" or "0")
        {
            ApplyCap(null);
            return;
        }

        if (!int.TryParse(args[0], out int capTokens) || capTokens < 4096)
        {
            ConsoleRenderer.WriteError("Usage: /context cap [status|off|<tokens>=4096+]");
            return;
        }

        ApplyCap(capTokens);
    }

    private void ApplyCap(int? capTokens)
    {
        _memoryManager.SetCompressionContextTokenCap(capTokens);
        _settings.CompressionContextTokenCap = capTokens;
        _persistence.SaveSettings(_settings);

        ConsoleRenderer.WriteSuccess(capTokens is > 0
            ? $"Compression context cap set to {capTokens.Value:N0} tokens."
            : "Compression context cap disabled.");

        WriteCapStatus();
    }

    private void WriteCapStatus()
    {
        string cap = _memoryManager.CompressionContextTokenCap is > 0
            ? _memoryManager.CompressionContextTokenCap.Value.ToString("N0")
            : "none";

        ConsoleRenderer.WriteInfo($"Model context window: {_memoryManager.ModelContextWindowTokens:N0} tokens");
        ConsoleRenderer.WriteInfo($"Compression cap: {cap}");
        ConsoleRenderer.WriteInfo($"Effective compression window: {_memoryManager.EffectiveCompressionContextTokens:N0} tokens");
    }

    private void Export(string[] args)
    {
        ContextExportFormat format = ContextExportFormat.Markdown;
        bool includeFull = false;
        string? path = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.Equals("--full", StringComparison.OrdinalIgnoreCase))
            {
                includeFull = true;
            }
            else if (arg.Equals("--json", StringComparison.OrdinalIgnoreCase))
            {
                format = ContextExportFormat.Json;
            }
            else if (arg.Equals("--markdown", StringComparison.OrdinalIgnoreCase) || arg.Equals("--md", StringComparison.OrdinalIgnoreCase))
            {
                format = ContextExportFormat.Markdown;
            }
            else if (arg.Equals("--format", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                format = ParseFormat(args[++i]);
            }
            else if (arg.StartsWith("--format=", StringComparison.OrdinalIgnoreCase))
            {
                format = ParseFormat(arg["--format=".Length..]);
            }
            else if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                path = path is null ? arg : $"{path} {arg}";
            }
        }

        string outputPath = ResolveExportPath(path, format);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        ContextExportSnapshot snapshot = BuildSnapshot(includeFull);
        string output = format == ContextExportFormat.Json
            ? ContextExportFormatter.ToJson(snapshot)
            : ContextExportFormatter.ToMarkdown(snapshot);

        File.WriteAllText(outputPath, output);
        ConsoleRenderer.WriteSuccess($"Context exported: {outputPath}");
    }

    private async Task Compress()
    {
        bool changed = await _memoryManager.MaybeSummarize();
        if (changed)
            ConsoleRenderer.WriteInfo("[context compressed]");
        else
            ConsoleRenderer.WriteInfo("Context unchanged; compression was not needed.");
    }

    private ContextExportSnapshot BuildSnapshot(bool includeFull)
    {
        string? conversationId = _memoryManager.ConversationId;
        IReadOnlyList<Chat.ChatMessage>? fullHistory = null;
        string? latestSummaryText = null;
        int? latestSummaryCoversThrough = null;
        List<ContextSnapshotInfo> snapshots = [];

        if (!string.IsNullOrEmpty(conversationId))
        {
            if (includeFull)
                fullHistory = _store.LoadFull(conversationId) ?? [];

            var latestSummary = _store.GetLatestSummary(conversationId);
            if (latestSummary is not null)
            {
                latestSummaryText = latestSummary.Value.text;
                latestSummaryCoversThrough = latestSummary.Value.coversThrough;
            }

            snapshots = _store.ListSnapshots(conversationId)
                .Select(s => new ContextSnapshotInfo
                {
                    Id = s.Id,
                    CreatedAt = s.CreatedAt,
                    Label = s.Label,
                    MessageCount = s.MessageCount,
                })
                .ToList();
        }

        return ContextExportFormatter.CreateSnapshot(
            _memoryManager.GetMessagesForAgent(),
            conversationId,
            fullHistory,
            latestSummaryText,
            latestSummaryCoversThrough,
            snapshots);
    }

    private string ResolveExportPath(string? path, ContextExportFormat format)
    {
        string extension = format == ContextExportFormat.Json ? ".json" : ".md";

        if (string.IsNullOrWhiteSpace(path))
        {
            string fileName = $"context-{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            return Path.Combine(_defaultExportDirectory, fileName);
        }

        string resolved = Environment.ExpandEnvironmentVariables(path);
        if (!Path.IsPathRooted(resolved))
            resolved = Path.GetFullPath(resolved);

        bool looksLikeDirectory = Directory.Exists(resolved)
            || path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar);

        if (!looksLikeDirectory && string.IsNullOrEmpty(Path.GetExtension(resolved)))
        {
            string? fileName = Path.GetFileName(resolved);
            looksLikeDirectory = string.IsNullOrEmpty(fileName) || !fileName.Contains('.', StringComparison.Ordinal);
        }

        if (looksLikeDirectory)
        {
            string fileName = $"context-{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            return Path.Combine(resolved, fileName);
        }

        if (string.IsNullOrEmpty(Path.GetExtension(resolved)))
            resolved += extension;

        return resolved;
    }

    private static ContextExportFormat ParseFormat(string value) =>
        value.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? ContextExportFormat.Json
            : ContextExportFormat.Markdown;

    private enum ContextExportFormat
    {
        Markdown,
        Json,
    }

    private sealed class NoOpSettingsPersistence : ISettingsPersistence
    {
        public static readonly ISettingsPersistence Instance = new NoOpSettingsPersistence();

        public void SaveSettings(AgentSettings settings)
        {
            // no-op for hosts/tests that do not persist settings through this command.
        }
    }
}
