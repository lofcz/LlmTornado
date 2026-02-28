# Stage 3: Storage Layout

## Goal

Manage a persistent data directory for the CLI agent: tool approval whitelist, user settings, and saved conversations. The directory lives in the user's platform-appropriate application data folder.

---

## Files to Create

### `src/LlmTornado.Cli/CliStorage.cs`
### `src/LlmTornado.Cli/CliSettings.cs`

---

## Storage Root

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\LlmTornado\` (e.g., `C:\Users\john\AppData\Roaming\LlmTornado\`) |
| Linux/macOS | `~/.llmtornado/` (i.e., `$HOME/.llmtornado/`) |

Detection:

```csharp
internal static class CliStorage
{
    public static readonly string RootDirectory = OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlmTornado")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".llmtornado");
}
```

---

## Directory Structure

```
%APPDATA%\LlmTornado\           (or ~/.llmtornado/)
├── settings.json                # User preferences (active model, enabled skills, etc.)
├── tool-approvals.json          # Tool whitelist {"tool_name": true/false}
└── conversations/               # Saved conversation files
    ├── 20260227_143022.jsonl    # Auto-named by timestamp
    ├── 20260227_143022.meta.json # Metadata (label, model, skill used, message count)
    ├── 20260227_150511_my-project.jsonl
    ├── 20260227_150511_my-project.meta.json
    └── current.jsonl            # Active session (continuous save)
```

---

## CliStorage — Public API

```csharp
namespace LlmTornado.Cli;

internal static class CliStorage
{
    public static readonly string RootDirectory;
    public static readonly string ConversationsDirectory;
    public static readonly string SettingsPath;
    public static readonly string ToolApprovalsPath;
    public static readonly string CurrentConversationPath;

    /// <summary>
    /// Ensure all directories exist. Called once at startup.
    /// </summary>
    public static void Initialize();

    /// <summary>
    /// Read and deserialize a JSON file, or return default if not found.
    /// </summary>
    public static T? LoadJson<T>(string path) where T : class;

    /// <summary>
    /// Serialize and write a JSON file atomically (write to .tmp, then move).
    /// </summary>
    public static void SaveJson<T>(string path, T data);
}
```

**Atomic writes**: To prevent corruption on crash, `SaveJson` writes to `{path}.tmp` first, then `File.Move(tmp, path, overwrite: true)`. This ensures the file is never half-written.

---

## CliSettings — Data Model

```csharp
namespace LlmTornado.Cli;

using System.Text.Json.Serialization;

internal sealed class CliSettings
{
    /// <summary>
    /// The currently selected model name (e.g., "claude-3-7-sonnet").
    /// Null means use auto-detected default.
    /// </summary>
    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    /// <summary>
    /// Skills explicitly disabled by the user via /skill disable.
    /// All skills are enabled by default unless listed here.
    /// </summary>
    [JsonPropertyName("disabled_skills")]
    public HashSet<string> DisabledSkills { get; set; } = [];

    /// <summary>
    /// Custom skills directory path. Null = use default (./skills/ relative to CWD).
    /// </summary>
    [JsonPropertyName("skills_directory")]
    public string? SkillsDirectory { get; set; }

    /// <summary>
    /// Custom MCP config file path. Null = use default (./mcp.json relative to CWD).
    /// </summary>
    [JsonPropertyName("mcp_config_path")]
    public string? McpConfigPath { get; set; }

    /// <summary>
    /// Maximum conversation turns before auto-summarization is triggered.
    /// Default: 0 = use token-based threshold only.
    /// </summary>
    [JsonPropertyName("max_turns_before_summary")]
    public int MaxTurnsBeforeSummary { get; set; }
}
```

**Example `settings.json`:**

```json
{
    "active_model": "claude-3-7-sonnet",
    "disabled_skills": ["pdf-processing"],
    "skills_directory": null,
    "mcp_config_path": null,
    "max_turns_before_summary": 0
}
```

---

## Tool Approvals File

Managed by `ToolApprovalManager` (Stage 6), stored as simple JSON:

```json
{
    "run_script:validate.sh": true,
    "run_script:process.py": true,
    "mcp:filesystem:read_file": true,
    "mcp:github:create_issue": false,
    "list_dir": true
}
```

Keys use a namespaced format:
- `run_script:{script_name}` — skill script tools
- `mcp:{server}:{tool_name}` — MCP remote tools
- `{tool_name}` — built-in tools (if any)

Values: `true` = always allow, `false` = always deny.

Tools **not** in this file trigger the interactive approval prompt.

---

## Conversation Metadata

Each saved conversation has a companion `.meta.json` file:

```csharp
internal sealed class ConversationMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;    // Filename stem

    [JsonPropertyName("label")]
    public string? Label { get; set; }                  // User-provided label

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }                  // Model used for this conversation

    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    [JsonPropertyName("first_message_preview")]
    public string? FirstMessagePreview { get; set; }    // First 100 chars of first user message

    [JsonPropertyName("active_skills")]
    public List<string> ActiveSkills { get; set; } = [];
}
```

**Example `20260227_150511_my-project.meta.json`:**

```json
{
    "id": "20260227_150511_my-project",
    "label": "my-project",
    "created_at": "2026-02-27T15:05:11Z",
    "updated_at": "2026-02-27T15:32:44Z",
    "model": "claude-3-7-sonnet",
    "message_count": 24,
    "first_message_preview": "Help me refactor the authentication module...",
    "active_skills": ["code-review", "refactor"]
}
```

---

## Current Conversation

The file `conversations/current.jsonl` is the **active** session file. It uses `PersistentConversation` with `ContinuousSaving = true` for crash resilience. When the user runs `/conversation save`, the current file is copied to a timestamped filename and a metadata file is created alongside it.

When the user runs `/conversation new`, the current file is either:
1. Auto-saved (if it has content) to a timestamped file, or
2. Truncated (if the user declines saving)

---

## Initialization Flow

```csharp
// Called once at startup in Program.cs
CliStorage.Initialize();

// Load or create settings
var settings = CliStorage.LoadJson<CliSettings>(CliStorage.SettingsPath) ?? new CliSettings();

// Apply settings to provider detection, skill loading, etc.
```

---

## Concurrency Notes

- `settings.json` and `tool-approvals.json` are read at startup and written on change — no concurrent access expected (single-threaded CLI)
- `current.jsonl` uses `PersistentConversation.ContinuousSaving` which handles its own append-mode file access
- Atomic writes (`SaveJson`) protect against Ctrl+C during save

---

## Types Used from LlmTornado

| Type | Purpose |
|------|---------|
| `PersistentConversation` | JSONL conversation persistence (used in Stage 7) |
