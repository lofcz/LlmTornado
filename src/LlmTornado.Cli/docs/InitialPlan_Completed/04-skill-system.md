# Stage 4: Skill System

## Goal

Implement skill discovery, parsing, and management following the open [Agent Skills standard](https://agentskills.io/specification). Skills are directories containing a `SKILL.md` file with YAML frontmatter and markdown instructions, plus optional `scripts/`, `references/`, and `assets/` directories. The agent uses progressive disclosure: only skill metadata is loaded at startup; full instructions and resources are loaded on-demand when a skill is activated.

---

## Files to Create

### `src/LlmTornado.Cli/Skills/CliSkill.cs`
### `src/LlmTornado.Cli/Skills/CliSkillLoader.cs`
### `src/LlmTornado.Cli/Skills/CliSkillManager.cs`
### `src/LlmTornado.Cli/Skills/ScriptToolBuilder.cs`

---

## Agent Skills Standard — Key Points

From the [specification](https://agentskills.io/specification):

### Directory Structure
```
skill-name/
├── SKILL.md          # Required: metadata + instructions
├── scripts/          # Optional: executable code
├── references/       # Optional: additional documentation
└── assets/           # Optional: templates, resources
```

### SKILL.md Format
```yaml
---
name: skill-name
description: A description of what this skill does and when to use it.
license: Apache-2.0
compatibility: Requires Python 3.10+
metadata:
  author: example-org
  version: "1.0"
allowed-tools: Bash(git:*) Read
---

# Skill Instructions

Step-by-step instructions for the agent...

## Available scripts

- **`scripts/validate.sh`** — Validates configuration files
- **`scripts/process.py`** — Processes input data

## Workflow
1. Run the validation script:
   ```bash
   bash scripts/validate.sh "$INPUT_FILE"
   ```
```

### Name Validation Rules
- 1-64 characters
- Lowercase letters, numbers, and hyphens only (`a-z`, `0-9`, `-`)
- Must not start or end with `-`
- Must not contain consecutive hyphens (`--`)
- Must match the parent directory name

### Progressive Disclosure
1. **Metadata** (~100 tokens): `name` + `description` loaded at startup for all skills
2. **Instructions** (<5000 tokens): Full `SKILL.md` body loaded when skill is activated
3. **Resources** (as needed): `scripts/`, `references/`, `assets/` loaded only when required

---

## CliSkill — Data Model

```csharp
namespace LlmTornado.Cli.Skills;

internal sealed class CliSkill
{
    // --- Frontmatter (always loaded) ---
    
    /// <summary>
    /// Unique skill identifier (lowercase, hyphens, matches directory name).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// What the skill does and when to use it (1-1024 chars).
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Optional license identifier or reference.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Optional environment requirements description.
    /// </summary>
    public string? Compatibility { get; init; }

    /// <summary>
    /// Optional key-value metadata (author, version, etc.).
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Space-delimited list of pre-approved tools (experimental).
    /// Parsed into individual tool names for integration with tool approval.
    /// </summary>
    public List<string> AllowedTools { get; init; } = [];

    // --- Paths ---

    /// <summary>
    /// Absolute path to the skill directory.
    /// </summary>
    public required string DirectoryPath { get; init; }

    /// <summary>
    /// Absolute path to the SKILL.md file.
    /// </summary>
    public required string SkillMdPath { get; init; }

    // --- Body (loaded on demand) ---

    /// <summary>
    /// Full markdown body of SKILL.md (everything after frontmatter).
    /// Null until activated via load_skill tool or /skill info.
    /// </summary>
    public string? Instructions { get; set; }

    // --- Discovered Resources ---

    /// <summary>
    /// Scripts found in scripts/ directory.
    /// </summary>
    public List<SkillScript> Scripts { get; init; } = [];

    /// <summary>
    /// Reference documents found in references/ directory.
    /// </summary>
    public List<string> References { get; init; } = [];

    /// <summary>
    /// Asset files found in assets/ directory.
    /// </summary>
    public List<string> Assets { get; init; } = [];

    // --- Runtime State ---

    /// <summary>
    /// Whether this skill is currently enabled (can be toggled via /skill enable|disable).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this skill's full instructions have been loaded into context.
    /// </summary>
    public bool Activated { get; set; }
}

internal sealed class SkillScript
{
    /// <summary>
    /// Filename (e.g., "validate.sh", "process.py").
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Absolute path to the script file.
    /// </summary>
    public required string AbsolutePath { get; init; }

    /// <summary>
    /// File extension without dot (e.g., "sh", "py", "ps1", "js").
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// The command to run this script (e.g., "python", "bash", "node", "pwsh").
    /// Auto-detected from extension.
    /// </summary>
    public required string Command { get; init; }
}
```

---

## CliSkillLoader — Parser

```csharp
namespace LlmTornado.Cli.Skills;

internal static partial class CliSkillLoader
{
    // Matches the Agent Skills standard name validation
    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex ValidSkillNameRegex();

    // Must not contain consecutive hyphens
    [GeneratedRegex(@"--")]
    private static partial Regex ConsecutiveHyphensRegex();

    /// <summary>
    /// Discover all skill directories under the given path.
    /// A valid skill directory contains a SKILL.md file.
    /// </summary>
    public static List<CliSkill> DiscoverSkills(string skillsRootDirectory);

    /// <summary>
    /// Parse a SKILL.md file and return a CliSkill with metadata loaded.
    /// Does NOT load the body (instructions) — only frontmatter.
    /// </summary>
    public static CliSkill? ParseSkillMetadata(string skillDirectory);

    /// <summary>
    /// Load the full SKILL.md body (instructions) for a skill.
    /// Called when the skill is activated.
    /// </summary>
    public static void LoadInstructions(CliSkill skill);

    /// <summary>
    /// Discover scripts in the scripts/ subdirectory.
    /// </summary>
    private static List<SkillScript> DiscoverScripts(string skillDirectory);

    /// <summary>
    /// Discover reference files in the references/ subdirectory.
    /// </summary>
    private static List<string> DiscoverReferences(string skillDirectory);

    /// <summary>
    /// Discover asset files in the assets/ subdirectory.
    /// </summary>
    private static List<string> DiscoverAssets(string skillDirectory);

    /// <summary>
    /// Auto-detect the command to run a script based on extension.
    /// </summary>
    private static string DetectScriptCommand(string extension);
}
```

### Frontmatter Parsing

Parse YAML between `---` delimiters. Minimal YAML parsing (no full YAML library dependency):

```csharp
// Simple line-by-line YAML frontmatter parser
// Handles: name, description, license, compatibility, metadata (map), allowed-tools (string)
// Does NOT need a full YAML library — the frontmatter is flat key-value pairs
// with only the metadata field being a nested map (one level deep)

private static Dictionary<string, object> ParseFrontmatter(string content)
{
    // Find first "---" and second "---"
    // Parse lines between them as key: value pairs
    // For "metadata:", read indented sub-lines as nested key-value pairs
    // For "allowed-tools:", split by space into list
}
```

### Script Command Detection

```csharp
private static string DetectScriptCommand(string extension) => extension.ToLower() switch
{
    "py"  => OperatingSystem.IsWindows() ? "python" : "python3",
    "sh"  => "bash",
    "ps1" => "pwsh",
    "js"  => "node",
    "ts"  => "npx tsx",
    "rb"  => "ruby",
    _     => throw new NotSupportedException($"Unsupported script extension: .{extension}")
};
```

### Skill Directory Discovery

```csharp
public static List<CliSkill> DiscoverSkills(string skillsRootDirectory)
{
    var skills = new List<CliSkill>();
    
    if (!Directory.Exists(skillsRootDirectory))
        return skills;

    foreach (string dir in Directory.GetDirectories(skillsRootDirectory))
    {
        string skillMdPath = Path.Combine(dir, "SKILL.md");
        if (!File.Exists(skillMdPath))
            continue;

        var skill = ParseSkillMetadata(dir);
        if (skill is null)
            continue;

        // Validate: directory name must match skill name
        string dirName = Path.GetFileName(dir);
        if (dirName != skill.Name)
        {
            // Log warning: directory name doesn't match skill name
            continue;
        }

        // Validate: name length <= 64
        if (skill.Name.Length > 64)
            continue;

        // Validate: no consecutive hyphens
        if (ConsecutiveHyphensRegex().IsMatch(skill.Name))
            continue;

        skills.Add(skill);
    }

    return skills;
}
```

---

## CliSkillManager — Runtime Skill Management

```csharp
namespace LlmTornado.Cli.Skills;

internal sealed class CliSkillManager
{
    private readonly Dictionary<string, CliSkill> _skills = new();
    private readonly CliSettings _settings;

    public CliSkillManager(CliSettings settings) { }

    /// <summary>
    /// Load skills from the skills directory. Applies disabled state from settings.
    /// </summary>
    public void LoadSkills(string skillsDirectory);

    /// <summary>
    /// Get all loaded skills.
    /// </summary>
    public IReadOnlyList<CliSkill> GetAllSkills();

    /// <summary>
    /// Get only enabled skills.
    /// </summary>
    public IReadOnlyList<CliSkill> GetEnabledSkills();

    /// <summary>
    /// Enable a skill by name. Returns false if not found.
    /// </summary>
    public bool EnableSkill(string name);

    /// <summary>
    /// Disable a skill by name. Returns false if not found.
    /// </summary>
    public bool DisableSkill(string name);

    /// <summary>
    /// Get a skill by name. Returns null if not found.
    /// </summary>
    public CliSkill? GetSkill(string name);

    /// <summary>
    /// Activate a skill: load its full instructions and mark as activated.
    /// Returns the instructions text.
    /// </summary>
    public string? ActivateSkill(string name);

    /// <summary>
    /// Generate the <available_skills> XML block for the system prompt.
    /// Only includes enabled skills. Uses name + description (progressive disclosure).
    /// </summary>
    public string BuildSkillsContextXml();

    /// <summary>
    /// Persist current enable/disable state to settings.
    /// </summary>
    public void SaveState();
}
```

### System Prompt Integration

The `BuildSkillsContextXml()` method generates the metadata block injected into the system prompt, following the Agent Skills integration guide:

```xml
<available_skills>
  <skill>
    <name>code-review</name>
    <description>Reviews code for bugs, security issues, and best practices.</description>
  </skill>
  <skill>
    <name>pdf-processing</name>
    <description>Extracts text and tables from PDF files, fills forms, merges documents.</description>
  </skill>
</available_skills>
```

The agent is also given a `load_skill` tool (see Stage 8: Agent Builder) that it can call when it decides a task matches a skill, which triggers `ActivateSkill()` and returns the full instructions.

---

## ScriptToolBuilder — Convert Scripts to Tools

```csharp
namespace LlmTornado.Cli.Skills;

internal static class ScriptToolBuilder
{
    /// <summary>
    /// Build Tool objects for all scripts in enabled skills.
    /// Each script becomes a callable tool.
    /// </summary>
    public static List<Tool> BuildScriptTools(IReadOnlyList<CliSkill> enabledSkills);

    /// <summary>
    /// Build a single Tool for a script.
    /// </summary>
    private static Tool BuildScriptTool(CliSkill skill, SkillScript script);

    /// <summary>
    /// Execute a script, capturing stdout/stderr.
    /// </summary>
    internal static async Task<string> ExecuteScript(
        SkillScript script, string arguments, string workingDirectory,
        CancellationToken cancellationToken = default);
}
```

### Tool Naming Convention

Scripts are registered as tools with namespaced names to avoid collisions:

```
{skill-name}:{script-filename-without-extension}
```

Examples:
- `code-review:lint` (from `code-review/scripts/lint.sh`)
- `pdf-processing:extract` (from `pdf-processing/scripts/extract.py`)
- `data-analysis:process` (from `data-analysis/scripts/process.py`)

### Tool Definition

Each script tool has:
- **Name**: `{skill}:{script}` as above
- **Description**: `"Execute the {script.FileName} script from the {skill.Name} skill. Run with --help to see usage."`
- **Parameters**:
  - `arguments` (string, required): Command-line arguments to pass to the script

```csharp
private static Tool BuildScriptTool(CliSkill skill, SkillScript script)
{
    string toolName = $"{skill.Name}:{Path.GetFileNameWithoutExtension(script.FileName)}";
    string description = $"Execute the {script.FileName} script from the {skill.Name} skill. " +
                         $"Run with --help to see usage.";

    return new Tool(
        (string arguments) => ExecuteScript(script, arguments, skill.DirectoryPath),
        toolName,
        description
    );
}
```

### Script Execution

```csharp
internal static async Task<string> ExecuteScript(
    SkillScript script, string arguments, string workingDirectory,
    CancellationToken cancellationToken = default)
{
    var psi = new ProcessStartInfo
    {
        FileName = script.Command,
        Arguments = script.Extension switch
        {
            "py" => $"{script.AbsolutePath} {arguments}",
            "sh" => $"{script.AbsolutePath} {arguments}",
            "ps1" => $"-File {script.AbsolutePath} {arguments}",
            "js" => $"{script.AbsolutePath} {arguments}",
            _ => $"{script.AbsolutePath} {arguments}"
        },
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi)!;
    string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
    string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
    await process.WaitForExitAsync(cancellationToken);

    // Format output for the agent
    var result = new StringBuilder();
    if (!string.IsNullOrEmpty(stdout))
        result.AppendLine(stdout.TrimEnd());
    if (!string.IsNullOrEmpty(stderr))
        result.AppendLine($"[stderr]: {stderr.TrimEnd()}");
    result.AppendLine($"[exit code]: {process.ExitCode}");

    // Truncate if output is too large (>30k chars)
    string output = result.ToString();
    if (output.Length > 30_000)
    {
        output = output[..30_000] + "\n[output truncated at 30000 characters]";
    }

    return output;
}
```

### Security Considerations

From the Agent Skills integration guide:
- **Sandboxing**: Scripts run in the skill directory as working directory, but are not sandboxed further. The tool approval system (Stage 6) provides the user control layer.
- **Allowlisting**: Skills' `allowed-tools` frontmatter field can pre-approve certain tools.
- **Confirmation**: All script tools require first-use approval (Stage 6) unless the skill's `allowed-tools` pre-approves them.
- **Logging**: Script executions are logged to stderr with timestamps.

---

## Skills Directory Resolution

Priority order for the skills directory:

1. `TORNADO_SKILLS_DIR` environment variable (absolute path)
2. `settings.json` → `skills_directory` field (absolute or relative to CWD)
3. `./skills/` relative to CWD (default)

```csharp
internal static string ResolveSkillsDirectory(CliSettings settings)
{
    string? envDir = Environment.GetEnvironmentVariable("TORNADO_SKILLS_DIR");
    if (!string.IsNullOrEmpty(envDir) && Directory.Exists(envDir))
        return Path.GetFullPath(envDir);

    if (!string.IsNullOrEmpty(settings.SkillsDirectory) && Directory.Exists(settings.SkillsDirectory))
        return Path.GetFullPath(settings.SkillsDirectory);

    return Path.GetFullPath("skills");
}
```

---

## Example Skill

For reference, here's what a complete skill looks like:

```
code-review/
├── SKILL.md
├── scripts/
│   ├── lint.sh
│   └── check-types.py
├── references/
│   └── STYLE-GUIDE.md
└── assets/
    └── review-template.md
```

**`code-review/SKILL.md`:**
```yaml
---
name: code-review
description: Reviews code for bugs, security issues, and adherence to best practices. Use when the user asks for a code review or mentions reviewing code quality.
metadata:
  author: my-team
  version: "1.2"
allowed-tools: code-review:lint code-review:check-types
---

# Code Review

## When to use
Use this skill when the user needs code reviewed for quality, security, or best practices.

## Available scripts
- **`scripts/lint.sh`** — Runs ESLint/Ruff on the target files
- **`scripts/check-types.py`** — Runs type checking with mypy/pyright

## Workflow
1. Ask the user which files or directories to review
2. Run the lint script: `scripts/lint.sh <path>`
3. Run type checking if Python: `scripts/check-types.py <path>`
4. Review the output and provide a structured report

## Review checklist
- [ ] Security vulnerabilities (SQL injection, XSS, etc.)
- [ ] Error handling (uncaught exceptions, missing error paths)
- [ ] Performance (N+1 queries, unnecessary allocations)
- [ ] Code style (naming, formatting, comments)

See [STYLE-GUIDE.md](references/STYLE-GUIDE.md) for team-specific conventions.
```

---

## Comparison: Open Standard vs. Existing SkillLoader

| Aspect | Existing `SkillLoader` (Acp.Server) | New `CliSkillLoader` |
|--------|--------------------------------------|----------------------|
| File format | `*.skill.md` anywhere in dir | `SKILL.md` in named subdirectory |
| Name validation | `^[a-z0-9][a-z0-9_-]{0,63}$` (allows `_`) | `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$` (no `_`, no `--`) |
| Fields | `name`, `display_name`, `description`, `use_tools`, `orchestrated` | `name`, `description`, `license`, `compatibility`, `metadata`, `allowed-tools` |
| Scripts | Not supported (tools are built-in) | `scripts/` directory with auto-detected executables |
| References | Not supported | `references/` directory for on-demand loading |
| Assets | Not supported | `assets/` directory for templates/resources |
| Progressive disclosure | Full load at startup | Metadata only at startup; body on activation |
| Embedded skills | Yes (`BuiltInSkills.cs`) | No — all skills are from filesystem |
