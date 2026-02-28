# Phase 1: Data Model & Loader

## Goal

Define the data model for agent definitions and implement the two discovery strategies: (1) walking the CWD hierarchy for project `AGENTS.md` files, and (2) scanning an `agents/` directory for persona `.md` files with YAML frontmatter. These are pure data types and stateless parsing — no lifecycle management yet.

---

## Files to Create

### `src/LlmTornado.Cli/Agents/CliAgentDefinition.cs`
### `src/LlmTornado.Cli/Agents/AgentDefinitionLoader.cs`

---

## AGENTS.md Specification — Key Points

From the [AGENTS.md website](https://agents.md) and [specification](https://github.com/agentsmd/agents.md):

### Format
- **Pure markdown** — no required fields, no frontmatter, no schema
- Standard markdown headings organize content
- Any structure the author chooses: build commands, code style, testing instructions, PR guidelines, security notes

### Discovery
- Place `AGENTS.md` at the repository root
- For monorepos, place additional `AGENTS.md` in subproject directories
- **The closest AGENTS.md to the edited file wins** — agents read the nearest file in the directory tree
- Explicit user chat prompts override AGENTS.md instructions

### Example AGENTS.md
```markdown
# Sample AGENTS.md file

## Dev environment tips
- Use `pnpm dlx turbo run where <project_name>` to jump to a package
- Run `pnpm install --filter <project_name>` to add the package to your workspace

## Testing instructions
- Run `pnpm turbo run test --filter <project_name>` for package tests
- Fix any test or type errors until the whole suite is green

## PR instructions
- Title format: [<project_name>] <Title>
- Always run `pnpm lint` and `pnpm test` before committing
```

### Compatibility
Used by 60k+ open-source projects. Compatible with GitHub Copilot coding agent, OpenAI Codex, Windsurf, Devin, Aider, RooCode, Amp, and many others. Stewarded by the Agentic AI Foundation under the Linux Foundation.

---

## Agent Persona Format — Our Extension

While project `AGENTS.md` files follow the open standard (pure markdown), **agent persona files** are our own format designed for the CLI's selectable agent system. They use YAML frontmatter (consistent with our existing SKILL.md pattern) plus a markdown instructions body.

### Persona File Format
```yaml
---
name: code-reviewer
description: Focuses on code quality, security vulnerabilities, and best practices
enabled-skills: file-analyzer
disabled-skills: note-taker
disabled-tools: web-search:ddg-search web-search:fetch-url
auto-approve-tools: file-analyzer:line-count file-analyzer:find-todos
---

# Code Reviewer Agent

You are a meticulous code reviewer. Your primary focus is on:
- Security vulnerabilities (SQL injection, XSS, CSRF, etc.)
- Error handling completeness
- Performance implications
- Naming and code style consistency

## Workflow
1. When asked to review code, first use file-analyzer to understand the codebase structure
2. Look for common vulnerability patterns
3. Check error handling paths
4. Verify naming conventions match the project's existing style
5. Provide a structured report with severity levels

## Response Style
- Be constructive, not just critical
- Suggest specific fixes, not just identify problems
- Reference line numbers when pointing out issues
- Group findings by severity: Critical > Warning > Info > Style
```

### Frontmatter Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | No* | Agent identifier. Defaults to filename slug if omitted. |
| `description` | string | No | One-line description shown in `/agent list`. Defaults to first paragraph if omitted. |
| `enabled-skills` | space-delimited string | No | Skill whitelist. If non-empty, **only** these skills are active. If empty/omitted, all skills are available. |
| `disabled-skills` | space-delimited string | No | Skill blacklist. These skills are force-disabled. Applied **after** the whitelist. |
| `enabled-tools` | space-delimited string | No | Tool whitelist. If non-empty, **only** these tools are registered. If empty/omitted, all tools from active skills are available. |
| `disabled-tools` | space-delimited string | No | Tool blacklist. These specific tools are excluded from registration. |
| `auto-approve-tools` | space-delimited string | No | Tools to pre-approve (skip the confirmation prompt). Passed to `ToolApprovalManager.PreApproveSkillTools()`. |

\* `name` is inferred from filename if omitted: `code-reviewer.md` → `"code-reviewer"`

### Whitelist + Blacklist Logic

```
Given:
  all_skills = [file-analyzer, web-search, note-taker]
  enabled-skills: file-analyzer web-search
  disabled-skills: web-search

Result:
  1. Whitelist applied: [file-analyzer, web-search]  (note-taker excluded)
  2. Blacklist applied: [file-analyzer]                (web-search removed)
  → Only file-analyzer is active

Given:
  all_skills = [file-analyzer, web-search, note-taker]
  enabled-skills: (empty)
  disabled-skills: note-taker

Result:
  1. Whitelist empty → all skills available: [file-analyzer, web-search, note-taker]
  2. Blacklist applied: [file-analyzer, web-search]  (note-taker removed)
  → Everything except note-taker

Same logic applies to tools.
```

---

## CliAgentDefinition — Data Model

```csharp
namespace LlmTornado.Cli.Agents;

/// <summary>
/// Source origin of an agent definition.
/// </summary>
internal enum AgentSource
{
    /// <summary>
    /// Shipped with the CLI binary (from Agents/built-in/ directory).
    /// </summary>
    BuiltIn,

    /// <summary>
    /// User-created agent from the agents/ directory.
    /// Custom agents shadow built-in agents with the same name.
    /// </summary>
    Custom,

    /// <summary>
    /// Auto-detected AGENTS.md from the project's directory hierarchy.
    /// These provide context only — no capability curation.
    /// </summary>
    Project
}

/// <summary>
/// An agent definition representing either a selectable persona (with capability curation)
/// or auto-detected project context (instructions only).
/// 
/// Persona agents use YAML frontmatter for structured configuration.
/// Project agents are pure markdown per the AGENTS.md specification.
/// </summary>
internal sealed class CliAgentDefinition
{
    // --- Identity ---

    /// <summary>
    /// Unique identifier for this agent. For personas, derived from frontmatter `name` field
    /// or filename slug. For project context, always "project-context".
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description. For personas, from frontmatter `description` field
    /// or first non-empty paragraph of the markdown body. For project context, 
    /// describes the source path(s).
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Where this agent definition came from.
    /// </summary>
    public required AgentSource Source { get; init; }

    /// <summary>
    /// Absolute path to the source file (.md for personas, AGENTS.md for project context).
    /// For merged project context (multiple AGENTS.md files), this is the nearest file's path.
    /// </summary>
    public required string FilePath { get; init; }

    // --- Instructions ---

    /// <summary>
    /// The markdown instructions body (everything after YAML frontmatter for personas,
    /// or the full file content for project AGENTS.md files).
    /// For merged project context, this contains all AGENTS.md contents concatenated
    /// with source path delimiters.
    /// </summary>
    public string Instructions { get; init; } = "";

    // --- Capability Curation (persona agents only, empty for project agents) ---

    /// <summary>
    /// Skill whitelist. If non-empty, only these skills should be enabled when this agent
    /// is active. If empty, all skills are available (subject to DisabledSkills blacklist).
    /// Values are skill names matching CliSkill.Name (e.g., "file-analyzer", "web-search").
    /// </summary>
    public List<string> EnabledSkills { get; init; } = [];

    /// <summary>
    /// Skill blacklist. These skills are force-disabled when this agent is active.
    /// Applied after EnabledSkills whitelist.
    /// </summary>
    public List<string> DisabledSkills { get; init; } = [];

    /// <summary>
    /// Tool whitelist. If non-empty, only these tools are registered when this agent is active.
    /// If empty, all tools from active skills are available (subject to DisabledTools blacklist).
    /// Values use the standard tool naming: "skill:script" for script tools, plain names for
    /// built-in tools, MCP tool names as-is.
    /// </summary>
    public List<string> EnabledTools { get; init; } = [];

    /// <summary>
    /// Tool blacklist. These specific tools are excluded from registration.
    /// Applied after EnabledTools whitelist.
    /// </summary>
    public List<string> DisabledTools { get; init; } = [];

    /// <summary>
    /// Tools to pre-approve (skip the confirmation prompt on first use).
    /// Passed to ToolApprovalManager.PreApproveSkillTools().
    /// </summary>
    public List<string> AutoApproveTools { get; init; } = [];

    // --- Derived Properties ---

    /// <summary>
    /// Whether this agent has any capability curation configured.
    /// Project agents always return false.
    /// </summary>
    public bool HasCapabilityCuration =>
        EnabledSkills.Count > 0 ||
        DisabledSkills.Count > 0 ||
        EnabledTools.Count > 0 ||
        DisabledTools.Count > 0 ||
        AutoApproveTools.Count > 0;

    /// <summary>
    /// Whether this is a persona agent (BuiltIn or Custom).
    /// </summary>
    public bool IsPersona => Source is AgentSource.BuiltIn or AgentSource.Custom;
}
```

### Design Notes

1. **Single class for both persona and project agents**: Rather than separate types, a single `CliAgentDefinition` serves both purposes. The `Source` enum and `IsPersona` property distinguish them. Project agents have empty capability lists (no curation). This simplifies the manager's API.

2. **Capability fields are `List<string>`, not `HashSet<string>`**: This preserves the ordering from the frontmatter, which is useful for display in `/agent info`. The manager converts to `HashSet` internally for O(1) lookups during filtering.

3. **No `Enabled`/`Activated` runtime state on the model**: Unlike `CliSkill`, agent definitions are stateless data objects. The active selection state lives in `AgentDefinitionManager`, not on the model. This keeps the model pure and simplifies serialization.

---

## AgentDefinitionLoader — Discovery & Parsing

```csharp
namespace LlmTornado.Cli.Agents;

/// <summary>
/// Stateless discovery and parsing of agent definitions from both
/// the project directory hierarchy (AGENTS.md files) and the agents
/// directory (persona .md files).
/// </summary>
internal static class AgentDefinitionLoader
{
    /// <summary>
    /// Maximum number of parent directories to walk when scanning for AGENTS.md files.
    /// Prevents pathological filesystem traversal.
    /// </summary>
    private const int MaxHierarchyDepth = 20;

    /// <summary>
    /// Resolve the agents directory path from settings with fallback to ./agents/.
    /// </summary>
    public static string ResolveAgentsDirectory(CliSettings settings);

    /// <summary>
    /// Walk from the given directory toward the filesystem root, collecting
    /// every AGENTS.md file found. Returns a merged CliAgentDefinition with
    /// Source = Project, or null if no AGENTS.md files exist.
    /// 
    /// Files are ordered nearest-first (closest to startDirectory takes precedence).
    /// Content is concatenated with source path delimiter comments.
    /// </summary>
    public static CliAgentDefinition? DiscoverProjectAgents(string startDirectory);

    /// <summary>
    /// Scan the built-in and custom agent directories for persona .md files.
    /// Custom agents shadow built-in agents with the same name.
    /// Returns a list of all discovered persona agents.
    /// </summary>
    public static List<CliAgentDefinition> DiscoverPersonaAgents(
        string builtInDirectory, string customDirectory);

    /// <summary>
    /// Parse a single persona .md file into a CliAgentDefinition.
    /// Extracts YAML frontmatter for capability curation and the markdown body
    /// for instructions.
    /// </summary>
    internal static CliAgentDefinition? ParsePersonaFile(
        string filePath, AgentSource source);

    /// <summary>
    /// Extract description from the markdown body when no frontmatter description exists.
    /// Uses the first non-empty, non-heading paragraph.
    /// </summary>
    private static string ExtractDescriptionFromMarkdown(string markdown);

    /// <summary>
    /// Convert a filename to a slug: "code-reviewer.md" → "code-reviewer".
    /// Strips extension, validates against skill-style naming rules.
    /// </summary>
    private static string? FileNameToSlug(string fileName);
}
```

---

### Project AGENTS.md Discovery — Hierarchy Walker

```csharp
public static CliAgentDefinition? DiscoverProjectAgents(string startDirectory)
{
    List<(string path, string content)> found = [];
    string? current = Path.GetFullPath(startDirectory);
    int depth = 0;

    while (current is not null && depth < MaxHierarchyDepth)
    {
        string agentsMdPath = Path.Combine(current, "AGENTS.md");
        if (File.Exists(agentsMdPath))
        {
            string content = File.ReadAllText(agentsMdPath);
            if (!string.IsNullOrWhiteSpace(content))
                found.Add((agentsMdPath, content));
        }

        // Move to parent directory
        string? parent = Directory.GetParent(current)?.FullName;
        if (parent == current) break; // filesystem root reached
        current = parent;
        depth++;
    }

    if (found.Count == 0) return null;

    // Merge: nearest file first (highest precedence per spec)
    StringBuilder merged = new();
    for (int i = 0; i < found.Count; i++)
    {
        if (i > 0) merged.AppendLine();
        merged.AppendLine($"<!-- AGENTS.md from: {found[i].path} -->");
        merged.AppendLine(found[i].content.TrimEnd());
    }

    return new CliAgentDefinition
    {
        Name = "project-context",
        Description = $"Project context from {found.Count} AGENTS.md file(s)",
        Source = AgentSource.Project,
        FilePath = found[0].path, // nearest file
        Instructions = merged.ToString()
    };
}
```

**Walking behavior:**
```
CWD: C:\repos\myapp\src\backend\

Scan order:
  C:\repos\myapp\src\backend\AGENTS.md    ← nearest (highest precedence)
  C:\repos\myapp\src\AGENTS.md
  C:\repos\myapp\AGENTS.md                ← repo root
  C:\repos\AGENTS.md
  C:\AGENTS.md
  (stop: filesystem root or depth=20)

Result: content concatenated in this order, with path comments separating each file.
```

**Why nearest-first**: The AGENTS.md spec states "the closest one takes precedence." By putting the nearest file's content first in the system prompt, we ensure the LLM gives it higher priority (models generally weight earlier context more heavily). The parent files provide broader context that the child file can override.

---

### Persona File Discovery

```csharp
public static List<CliAgentDefinition> DiscoverPersonaAgents(
    string builtInDirectory, string customDirectory)
{
    Dictionary<string, CliAgentDefinition> agents = new(StringComparer.OrdinalIgnoreCase);

    // 1. Load built-in agents first
    if (Directory.Exists(builtInDirectory))
    {
        foreach (string file in Directory.GetFiles(builtInDirectory, "*.md"))
        {
            CliAgentDefinition? agent = ParsePersonaFile(file, AgentSource.BuiltIn);
            if (agent is not null)
                agents[agent.Name] = agent;
        }
    }

    // 2. Load custom agents — shadow built-ins with same name
    if (Directory.Exists(customDirectory))
    {
        foreach (string file in Directory.GetFiles(customDirectory, "*.md"))
        {
            CliAgentDefinition? agent = ParsePersonaFile(file, AgentSource.Custom);
            if (agent is not null)
                agents[agent.Name] = agent; // overwrites built-in if same name
        }
    }

    return [.. agents.Values];
}
```

**Shadow behavior:**
```
Built-in directory:
  Agents/built-in/code-reviewer.md  → name="code-reviewer", source=BuiltIn
  Agents/built-in/debugger.md       → name="debugger", source=BuiltIn

Custom directory (./agents/):
  agents/code-reviewer.md           → name="code-reviewer", source=Custom
  agents/my-agent.md                → name="my-agent", source=Custom

Result:
  code-reviewer  [custom]     ← custom shadows built-in
  debugger       [built-in]
  my-agent       [custom]
```

---

### Persona File Parsing

```csharp
internal static CliAgentDefinition? ParsePersonaFile(string filePath, AgentSource source)
{
    string fileName = Path.GetFileName(filePath);
    string? slug = FileNameToSlug(fileName);
    if (slug is null) return null; // invalid filename

    string content = File.ReadAllText(filePath);
    if (string.IsNullOrWhiteSpace(content)) return null;

    // Try to parse YAML frontmatter
    Dictionary<string, object> frontmatter = ParseFrontmatter(content);
    string instructions = ExtractBody(content);

    // Name: frontmatter > filename slug
    string name = frontmatter.GetValueOrDefault("name") as string ?? slug;

    // Description: frontmatter > first paragraph of body
    string description = frontmatter.GetValueOrDefault("description") as string
                         ?? ExtractDescriptionFromMarkdown(instructions);

    return new CliAgentDefinition
    {
        Name = name,
        Description = description,
        Source = source,
        FilePath = Path.GetFullPath(filePath),
        Instructions = instructions,
        EnabledSkills = ParseSpaceDelimitedList(frontmatter, "enabled-skills"),
        DisabledSkills = ParseSpaceDelimitedList(frontmatter, "disabled-skills"),
        EnabledTools = ParseSpaceDelimitedList(frontmatter, "enabled-tools"),
        DisabledTools = ParseSpaceDelimitedList(frontmatter, "disabled-tools"),
        AutoApproveTools = ParseSpaceDelimitedList(frontmatter, "auto-approve-tools"),
    };
}
```

### Frontmatter Parsing — Reusing Existing Pattern

The existing `CliSkillLoader.ParseFrontmatter()` already handles the `---` delimited YAML parsing with one-level nested maps. We need to extend it slightly for space-delimited list fields. Rather than duplicating the parser, we'll extract the shared parsing logic:

```csharp
/// <summary>
/// Parse YAML-like frontmatter between --- delimiters.
/// Handles flat key: value pairs, nested maps (one level), and 
/// space-delimited list values.
/// 
/// Reuses the same approach as CliSkillLoader.ParseFrontmatter() 
/// with the addition of space-delimited list value support.
/// </summary>
private static Dictionary<string, object> ParseFrontmatter(string content)
{
    // Same implementation as CliSkillLoader.ParseFrontmatter()
    // Both classes use the same logic — could be extracted to a shared
    // FrontmatterParser utility if desired during implementation.
    
    // Find first "---" and second "---"
    int firstDash = content.IndexOf("---", StringComparison.Ordinal);
    if (firstDash < 0) return new();
    int secondDash = content.IndexOf("---", firstDash + 3, StringComparison.Ordinal);
    if (secondDash < 0) return new();

    string yaml = content[(firstDash + 3)..secondDash].Trim();
    // Parse key: value lines...
    // (same line-by-line approach as CliSkillLoader)
}

/// <summary>
/// Extract the markdown body after frontmatter (or the full content if no frontmatter).
/// </summary>
private static string ExtractBody(string content)
{
    int firstDash = content.IndexOf("---", StringComparison.Ordinal);
    if (firstDash < 0) return content;
    int secondDash = content.IndexOf("---", firstDash + 3, StringComparison.Ordinal);
    if (secondDash < 0) return content;
    return content[(secondDash + 3)..].Trim();
}

/// <summary>
/// Parse a space-delimited string value into a list.
/// Example: "file-analyzer web-search" → ["file-analyzer", "web-search"]
/// </summary>
private static List<string> ParseSpaceDelimitedList(
    Dictionary<string, object> frontmatter, string key)
{
    if (frontmatter.GetValueOrDefault(key) is not string value) return [];
    return [.. value.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
}
```

### Description Extraction from Markdown

When no `description` field exists in the frontmatter, we extract it from the markdown body:

```csharp
private static string ExtractDescriptionFromMarkdown(string markdown)
{
    // Find the first non-empty line that isn't a heading
    string[] lines = markdown.Split('\n');
    foreach (string rawLine in lines)
    {
        string line = rawLine.Trim();
        if (string.IsNullOrEmpty(line)) continue;
        if (line.StartsWith('#')) continue;
        // Found a content line — use it as description (capped at 200 chars)
        return line.Length > 200 ? line[..200] + "..." : line;
    }
    return "";
}
```

### Filename to Slug

```csharp
private static string? FileNameToSlug(string fileName)
{
    string slug = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
    
    // Validate: same rules as skill names (1-64 chars, a-z0-9 and hyphens,
    // no leading/trailing hyphens, no consecutive hyphens)
    if (slug.Length is < 1 or > 64) return null;
    if (!Regex.IsMatch(slug, @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")) return null;
    if (slug.Contains("--")) return null;
    
    return slug;
}
```

---

### Agents Directory Resolution

```csharp
public static string ResolveAgentsDirectory(CliSettings settings)
{
    // 1. Settings override
    if (!string.IsNullOrEmpty(settings.AgentsDirectory) && Directory.Exists(settings.AgentsDirectory))
        return Path.GetFullPath(settings.AgentsDirectory);

    // 2. Default: ./agents/ relative to CWD
    return Path.GetFullPath("agents");
}
```

The built-in agents directory is always resolved relative to the application binary:

```csharp
public static string ResolveBuiltInDirectory()
{
    string appDir = AppContext.BaseDirectory;
    return Path.Combine(appDir, "Agents", "built-in");
}
```

---

## Example Parsing Scenarios

### Scenario 1: Full Frontmatter Persona

**Input file**: `agents/code-reviewer.md`
```yaml
---
name: code-reviewer
description: Focuses on code quality, security, and best practices
enabled-skills: file-analyzer
disabled-tools: web-search:ddg-search web-search:fetch-url
auto-approve-tools: file-analyzer:line-count file-analyzer:find-todos
---

# Code Reviewer Agent

You are a meticulous code reviewer...
```

**Parsed result**:
```
Name = "code-reviewer"
Description = "Focuses on code quality, security, and best practices"
Source = Custom
FilePath = "C:\Users\john\projects\agents\code-reviewer.md"
Instructions = "# Code Reviewer Agent\n\nYou are a meticulous code reviewer..."
EnabledSkills = ["file-analyzer"]
DisabledSkills = []
EnabledTools = []
DisabledTools = ["web-search:ddg-search", "web-search:fetch-url"]
AutoApproveTools = ["file-analyzer:line-count", "file-analyzer:find-todos"]
```

### Scenario 2: Minimal Persona (No Frontmatter)

**Input file**: `agents/quick-helper.md`
```markdown
# Quick Helper

A fast, no-frills assistant that gives concise answers without ceremony.

## Style
- Keep responses under 3 sentences when possible
- Skip pleasantries and get to the point
- Use code blocks without lengthy explanations
```

**Parsed result**:
```
Name = "quick-helper"           (from filename slug)
Description = "A fast, no-frills assistant that gives concise answers without ceremony."
                                (from first non-heading paragraph)
Source = Custom
Instructions = "# Quick Helper\n\nA fast, no-frills assistant..."
EnabledSkills = []              (all skills available)
DisabledSkills = []
EnabledTools = []               (all tools available)
DisabledTools = []
AutoApproveTools = []
```

### Scenario 3: Project AGENTS.md Hierarchy

**Files found**:
```
C:\repos\myapp\src\backend\AGENTS.md (200 bytes)
C:\repos\myapp\AGENTS.md (500 bytes)
```

**Parsed result**:
```
Name = "project-context"
Description = "Project context from 2 AGENTS.md file(s)"
Source = Project
FilePath = "C:\repos\myapp\src\backend\AGENTS.md"  (nearest)
Instructions = """
<!-- AGENTS.md from: C:\repos\myapp\src\backend\AGENTS.md -->
[content of backend AGENTS.md]

<!-- AGENTS.md from: C:\repos\myapp\AGENTS.md -->
[content of root AGENTS.md]
"""
EnabledSkills = []   (project agents never curate capabilities)
DisabledSkills = []
EnabledTools = []
DisabledTools = []
AutoApproveTools = []
```

---

## Comparison with CliSkillLoader

| Aspect | CliSkillLoader (existing) | AgentDefinitionLoader (new) |
|--------|---------------------------|------------------------------|
| Discovery target | `SKILL.md` in subdirectories | `*.md` files in flat directory + `AGENTS.md` in hierarchy |
| Naming | Directory name must match `name` field | Filename slug, or explicit `name` in frontmatter |
| Frontmatter | Required (`name`, `description`) | Optional (`name`, `description`, capability fields) |
| Body loading | Progressive disclosure (on-demand) | Eager loading (instructions always loaded) |
| Subdirectories | `scripts/`, `references/`, `assets/` | None — agent personas are single-file |
| Hierarchy scanning | No — flat skills directory only | Yes — walks CWD to root for project AGENTS.md |
| Validation | Strict name regex, name==dirname | Same name regex, flexible naming |

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| `AGENTS.md` not found in CWD hierarchy | `DiscoverProjectAgents()` returns `null` — no project context injected |
| Persona file has invalid filename (e.g., `_bad_.md`) | `FileNameToSlug()` returns `null`, file skipped, warning logged |
| Persona file is empty | `ParsePersonaFile()` returns `null`, file skipped |
| Frontmatter has unknown fields | Silently ignored (forward-compatible extensibility) |
| `enabled-skills` references a skill that doesn't exist | Stored as-is; handled at baseline application time in `AgentDefinitionManager` |
| Filesystem permission error reading AGENTS.md | Catch `IOException`, log warning, continue scanning parent directories |
| Very large AGENTS.md file (>100KB) | Truncate content at 100KB with `[TRUNCATED]` marker |

---

## Testing Strategy

Tests for this phase focus on pure parsing and discovery logic. See [Phase 6: Tests](06-tests.md) for full test specifications.

Key test cases:
- Hierarchy walker finds AGENTS.md at multiple levels, orders nearest-first
- Hierarchy walker stops at filesystem root
- Hierarchy walker handles no AGENTS.md gracefully
- Persona parser extracts all frontmatter fields correctly
- Persona parser handles missing frontmatter (pure markdown)
- Filename-to-slug conversion and validation
- Custom agents shadow built-in agents by name
- Description extraction from markdown body
- Empty/whitespace-only files are skipped
- Space-delimited list parsing with various edge cases
