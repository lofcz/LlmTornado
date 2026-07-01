using System.Text.Json;
using System.Text.Json.Serialization;
using LlmTornado.Chat;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;

namespace LlmTornado.Cli.Commands;

internal sealed class ConversationCommand : ICliCommand
{
    public string Name => "conversation";
    public string Description => "Save, load, list, delete conversations";
    public string Usage => "/conversation [save [label] [--path <file>] | load <id> | load --path <file> | list | delete <id> | new]";

    private readonly ConversationMemoryManager _memoryManager;
    private readonly SqliteConversationStore _store;
    private readonly CliAgentBuilder _builder;
    private static readonly JsonSerializerOptions ConversationFileJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ConversationCommand(ConversationMemoryManager memoryManager, SqliteConversationStore store, CliAgentBuilder builder)
    {
        _memoryManager = memoryManager;
        _store = store;
        _builder = builder;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleRenderer.WriteInfo($"Current conversation: {_memoryManager.Messages.Count} messages.");
            ConsoleRenderer.WriteInfo($"Use /conversation save, load, list, delete, or new.");
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "save":
                ParsedPathArgs saveArgs = ParsePathArgs(args[1..]);
                string? label = saveArgs.Remaining.Count > 0 ? string.Join(" ", saveArgs.Remaining) : null;
                string id = _store.Save(
                    [.. _memoryManager.Messages],
                    _builder.ActiveModel.Name,
                    null,
                    label);
                ConsoleRenderer.WriteSuccess($"Saved as: {id}");

                if (saveArgs.Path is not null)
                {
                    string exportPath = ExportToPath(saveArgs.Path, id, label);
                    ConsoleRenderer.WriteSuccess($"Exported conversation to: {exportPath}");
                }
                break;

            case "load":
                ParsedPathArgs loadArgs = ParsePathArgs(args[1..]);
                if (loadArgs.Path is not null)
                {
                    string? importedId = ImportFromPath(loadArgs.Path);
                    if (importedId is not null)
                        ConsoleRenderer.WriteSuccess($"Loaded conversation from: {ResolvePath(loadArgs.Path)} ({_memoryManager.Messages.Count} messages, id: {importedId})");
                    break;
                }

                if (loadArgs.Remaining.Count == 0)
                {
                    ConsoleRenderer.WriteError($"Usage: {Usage}");
                    break;
                }

                string loadId = loadArgs.Remaining[0];
                List<Chat.ChatMessage>? messages = _store.Load(loadId);
                if (messages is null)
                {
                    ConsoleRenderer.WriteError($"Conversation '{loadId}' not found.");
                    break;
                }
                // Delegate to the managed config so the id is bound (incremental persistence) and the
                // loaded history is pushed into the runtime conversation (so the model sees it).
                if (_builder.ConversationConfig is not null)
                    _builder.ConversationConfig.LoadConversation(loadId);
                else
                    _memoryManager.LoadConversation(loadId);
                ConsoleRenderer.WriteSuccess($"Loaded conversation: {loadId} ({messages.Count} messages)");
                break;

            case "list":
                List<ConversationMetadata> conversations = _store.List();
                if (conversations.Count == 0)
                {
                    ConsoleRenderer.WriteInfo("No saved conversations.");
                    break;
                }
                foreach (ConversationMetadata meta in conversations)
                {
                    string display = meta.Label ?? meta.Id;
                    string preview = meta.FirstMessagePreview is not null ? $" — {meta.FirstMessagePreview}" : "";
                    ConsoleRenderer.WriteInfo($"  {meta.Id}  [{meta.MessageCount} msgs] {display}{preview}");
                }
                break;

            case "delete" when args.Length >= 2:
                if (_store.Delete(args[1]))
                    ConsoleRenderer.WriteSuccess($"Deleted: {args[1]}");
                else
                    ConsoleRenderer.WriteError($"Conversation '{args[1]}' not found.");
                break;

            case "new":
                // Delegate to the managed config so the runtime conversation is cleared too.
                if (_builder.ConversationConfig is not null)
                    _builder.ConversationConfig.NewConversation();
                else
                    _memoryManager.NewConversation();
                ConsoleRenderer.WriteSuccess("Started new conversation.");
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }

    private string ExportToPath(string path, string id, string? label)
    {
        ConversationFile file = new()
        {
            Version = 1,
            Id = id,
            Label = label,
            Model = _builder.ActiveModel.Name,
            ExportedAt = DateTimeOffset.UtcNow,
            Messages = _memoryManager.Messages.Select(ToFileMessage).ToList(),
        };

        string resolved = ResolvePath(path);
        string? dir = Path.GetDirectoryName(resolved);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(resolved, JsonSerializer.Serialize(file, ConversationFileJsonOptions));
        return resolved;
    }

    private string? ImportFromPath(string path)
    {
        string resolved = ResolvePath(path);
        if (!File.Exists(resolved))
        {
            ConsoleRenderer.WriteError($"Conversation file not found: {resolved}");
            return null;
        }

        ConversationFile? file = JsonSerializer.Deserialize<ConversationFile>(
            File.ReadAllText(resolved), ConversationFileJsonOptions);
        if (file is null || file.Messages.Count == 0)
        {
            ConsoleRenderer.WriteError($"Conversation file is empty or invalid: {resolved}");
            return null;
        }

        List<ChatMessage> messages = file.Messages.Select(FromFileMessage).ToList();
        string id = _store.Save(
            messages,
            file.Model ?? _builder.ActiveModel.Name,
            null,
            file.Label);

        if (_builder.ConversationConfig is not null)
            _builder.ConversationConfig.LoadConversation(id);
        else
            _memoryManager.LoadConversation(id);

        return id;
    }

    private static ConversationFileMessage ToFileMessage(ChatMessage message)
    {
        SerializedMessage serialized = MessageSerializer.Serialize(message);
        return new ConversationFileMessage
        {
            Id = message.Id,
            Role = serialized.Role,
            Content = serialized.Content,
            PartsJson = serialized.PartsJson,
        };
    }

    private static ChatMessage FromFileMessage(ConversationFileMessage message)
    {
        Guid id = message.Id == Guid.Empty ? Guid.NewGuid() : message.Id;
        return MessageSerializer.DeserializeLightweight(message.Role, message.Content, message.PartsJson, id);
    }

    private static ParsedPathArgs ParsePathArgs(string[] args)
    {
        List<string> remaining = [];
        string? path = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.Equals("--path", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                    path = args[++i];
                continue;
            }

            const string prefix = "--path=";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                path = arg[prefix.Length..];
                continue;
            }

            remaining.Add(arg);
        }

        return new ParsedPathArgs(path, remaining);
    }

    private static string ResolvePath(string path)
        => Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(Environment.CurrentDirectory, path));

    private sealed record ParsedPathArgs(string? Path, List<string> Remaining);

    private sealed class ConversationFile
    {
        public int Version { get; set; }
        public string? Id { get; set; }
        public string? Label { get; set; }
        public string? Model { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
        public List<ConversationFileMessage> Messages { get; set; } = [];
    }

    private sealed class ConversationFileMessage
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = "user";
        public string? Content { get; set; }
        public string? PartsJson { get; set; }
    }
}
