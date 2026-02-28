# Stage 3: Skill-Level Persistent Memory

## Overview

Add **per-skill persistent memory** — the LLM can save learnings, workarounds, and preferences as Markdown files in each skill's `memories/` subdirectory. These memories are automatically loaded when a skill is activated and persist across conversations.

**Source of inspiration:** VisualErp's `update_skill_memory` tool and `LoadSkillMemories()` in `UnifiedAgentRunnable`.

## Problem Solved

When the LLM discovers something useful while executing a skill (a workaround for a flaky API, a user preference, a domain insight), that knowledge is lost when the conversation ends. Skill memory creates a lightweight learning loop:

1. LLM activates a skill, receives instructions + existing memories
2. LLM discovers an insight while working
3. LLM calls `update_skill_memory` to persist it
4. Next time the skill is activated (even in a different conversation), the memory is included

This follows the agentskills.io convention — memories live as Markdown files in `Skills/{name}/memories/{topic}.md`, making them version-controllable and human-readable.

## Files Changed

| File | Action | Lines Affected |
|------|--------|----------------|
| `AgentBuilder.cs` | **Modify** | `CollectTools()` + `BuildLoadSkillTool()` + 2 new methods |
| `Skills/SkillManager.cs` | **Modify** | `ActivateSkill()` return value enriched |

## Detailed Changes

### 1. Modify: `AgentBuilder.cs`

#### 1a. Add `update_skill_memory` tool to `CollectTools()`

**In `CollectTools()` (line ~277)**, add the new tool after the existing three built-in tools:

Current code:
```csharp
private List<Tool> CollectTools()
{
    List<Tool> tools = [];

    // Built-in skill management tools
    tools.Add(BuildLoadSkillTool());
    tools.Add(BuildListSkillsTool());
    tools.Add(BuildReadReferenceTool());
    // ...
```

Updated:
```csharp
private List<Tool> CollectTools()
{
    List<Tool> tools = [];

    // Built-in skill management tools
    tools.Add(BuildLoadSkillTool());
    tools.Add(BuildListSkillsTool());
    tools.Add(BuildReadReferenceTool());
    tools.Add(BuildUpdateSkillMemoryTool());
    // ...
```

#### 1b. Add `BuildUpdateSkillMemoryTool()` method

**Add after `BuildReadReferenceTool()` (after line ~345):**

```csharp
private Tool BuildUpdateSkillMemoryTool()
{
    return new Tool(
        new Func<string, string, string, string>((skillName, topic, content) =>
        {
            // Validate skill exists
            Skill? skill = _skillManager.GetSkill(skillName);
            if (skill is null)
                return $"Error: Skill '{skillName}' not found.";

            // Validate topic name (prevent path traversal)
            if (string.IsNullOrWhiteSpace(topic) 
                || topic.Contains('.') 
                || topic.Contains('/') 
                || topic.Contains('\\')
                || topic.Contains(':'))
                return "Error: Topic must be a simple name (letters, numbers, hyphens). No dots, slashes, or special characters.";

            if (string.IsNullOrWhiteSpace(content))
                return "Error: Content cannot be empty.";

            try
            {
                string memoriesDir = Path.Combine(skill.DirectoryPath, "memories");
                Directory.CreateDirectory(memoriesDir);

                string filePath = Path.Combine(memoriesDir, $"{topic}.md");
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
                string entry = $"\n\n---\n**{timestamp}**\n\n{content}\n";

                File.AppendAllText(filePath, entry);

                return $"Memory saved to {skill.Name}/memories/{topic}.md";
            }
            catch (Exception ex)
            {
                return $"Error saving memory: {ex.Message}";
            }
        }),
        "update_skill_memory",
        "Save a new insight, workaround, or preference for a skill. " +
        "Writes to the skill's memories/ directory as a Markdown file. " +
        "Parameters: skill_name (the skill's name), topic (a simple name like 'workarounds' or 'preferences'), " +
        "content (the insight to save).");
}
```

**Security considerations:**
- Topic name validated: no dots (prevents `..`), no slashes, no colons
- Content is appended (not overwritten) — append-only log
- Timestamp included for audit trail
- File path constructed from `skill.DirectoryPath` which is already validated by `SkillLoader`

#### 1c. Modify `BuildLoadSkillTool()` to include memories

**Replace the existing `BuildLoadSkillTool()` method** (lines 288-296):

Current code:
```csharp
private Tool BuildLoadSkillTool()
{
    return new Tool(
        new Func<string, string>(skillName =>
        {
            string? instructions = _skillManager.ActivateSkill(skillName);
            return instructions ?? $"Skill '{skillName}' not found or not enabled. Use list_skills to see available skills.";
        }),
        "load_skill",
        "Load and activate a skill by name. Returns the skill's full instructions. " +
        "Use this when a user's task matches a skill from the available_skills list.");
}
```

Replacement:
```csharp
private Tool BuildLoadSkillTool()
{
    return new Tool(
        new Func<string, string>(skillName =>
        {
            string? instructions = _skillManager.ActivateSkill(skillName);
            if (instructions is null)
                return $"Skill '{skillName}' not found or not enabled. Use list_skills to see available skills.";

            Skill? skill = _skillManager.GetSkill(skillName);
            if (skill is null)
                return instructions;

            // Build rich activation response with memories
            return BuildSkillActivationResponse(skill, instructions);
        }),
        "load_skill",
        "Load and activate a skill by name. Returns the skill's full instructions, " +
        "accumulated memories, and available resources. " +
        "Use this when a user's task matches a skill from the available_skills list.");
}
```

#### 1d. Add `BuildSkillActivationResponse()` and `LoadSkillMemories()` helper methods

**Add after `BuildUpdateSkillMemoryTool()`:**

```csharp
/// <summary>
/// Build a rich JSON response for skill activation that includes instructions,
/// available resources, accumulated memories, and a usage guide.
/// </summary>
private static string BuildSkillActivationResponse(Skill skill, string instructions)
{
    List<SkillMemoryEntry> memories = LoadSkillMemories(skill);

    var response = new
    {
        skill = skill.Name,
        metadata = new
        {
            description = skill.Description,
            license = skill.License,
            allowed_tools = skill.AllowedTools,
            extra = skill.Metadata
        },
        resources = skill.References.Select(r => new { path = r }).ToList(),
        scripts = skill.Scripts.Select(s => new { name = s.FileName }).ToList(),
        memories = memories.Select(m => new { m.Topic, m.Content }).ToList(),
        instructions,
        usage = """
            HOW TO USE THIS SKILL:
            
            1. READ the instructions carefully — they contain specialized guidance.
            2. UNDERSTAND the context:
               - The 'metadata.allowed_tools' list specifies tool constraints
               - The 'resources' array lists available reference files
               - The 'scripts' array lists available executable scripts
               - The 'memories' section contains past learnings for this skill
            3. APPLY the skill instructions to complete the task.
            4. ACCESS resources when needed using read_reference(skill_name, path).
            5. SAVE new insights using update_skill_memory(skill_name, topic, content).
            6. RESPECT constraints from metadata.allowed_tools if specified.
            """
    };

    return System.Text.Json.JsonSerializer.Serialize(response,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
}

/// <summary>
/// Load all memory files from a skill's memories/ subdirectory.
/// </summary>
private static List<SkillMemoryEntry> LoadSkillMemories(Skill skill)
{
    string memoriesDir = Path.Combine(skill.DirectoryPath, "memories");
    if (!Directory.Exists(memoriesDir))
        return [];

    try
    {
        return Directory.GetFiles(memoriesDir, "*.md")
            .Select(f => new SkillMemoryEntry
            {
                Topic = Path.GetFileNameWithoutExtension(f),
                Content = File.ReadAllText(f)
            })
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .ToList();
    }
    catch
    {
        return [];
    }
}
```

#### 1e. Add `SkillMemoryEntry` helper class

**Add at the bottom of `AgentBuilder.cs` (inside the namespace, after the `AgentBuilder` class):**

```csharp
/// <summary>
/// A memory entry loaded from a skill's memories/ directory.
/// </summary>
internal sealed class SkillMemoryEntry
{
    public string Topic { get; set; } = "";
    public string Content { get; set; } = "";
}
```

**Alternatively**, this could be a private nested class inside `AgentBuilder` since it's only used there. Either is fine — going with `internal` at the namespace level for testability.

### 2. Modify: `Skills/SkillManager.cs` — No Changes Needed

Originally I considered modifying `ActivateSkill()` to return richer data, but on reflection the simpler approach is to keep `ActivateSkill()` returning `string?` (just instructions) and have `AgentBuilder.BuildLoadSkillTool()` do the enrichment by calling `GetSkill()` separately and building the full response. This:

- Keeps `SkillManager` focused on lifecycle (load/enable/disable/activate)
- Avoids coupling `SkillManager` to the memory file format
- Keeps the JSON response construction in `AgentBuilder` where the other tool handlers live

**No changes to `SkillManager.cs`.**

## File System Layout

After a skill has accumulated memories:

```
skills/
└── my-skill/
    ├── SKILL.md              # Skill definition (existing)
    ├── scripts/              # Executable scripts (existing)
    ├── references/           # Reference docs (existing)
    ├── assets/               # Static assets (existing)
    └── memories/             # NEW — auto-created by update_skill_memory
        ├── workarounds.md    # Append-only Markdown
        ├── preferences.md   # Each entry timestamped
        └── insights.md
```

Each memory file looks like:

```markdown
---
**2026-02-28 14:30:00 UTC**

The API returns 404 for parts that have been soft-deleted.
Use the `include_deleted=true` parameter to find them.

---
**2026-02-28 15:45:00 UTC**

User prefers CSV output format for shortage reports.
```

## Tool Descriptors (for LLM)

### `update_skill_memory`

| Parameter | Type | Description |
|-----------|------|-------------|
| `skill_name` | string | The skill's name (as shown in `list_skills`) |
| `topic` | string | A simple topic name (e.g., "workarounds", "preferences"). Letters, numbers, hyphens only. |
| `content` | string | The insight, workaround, or preference to save. |

**Returns:** Confirmation message or error string.

### `load_skill` (modified response)

Previous return: just the instructions text.

New return: JSON object with:
```json
{
  "skill": "my-skill",
  "metadata": { "description": "...", "license": "...", "allowed_tools": [...], "extra": {...} },
  "resources": [{ "path": "references/guide.md" }],
  "scripts": [{ "name": "analyze.py" }],
  "memories": [
    { "topic": "workarounds", "content": "---\n**2026-02-28 14:30:00 UTC**\n\nThe API returns 404..." },
    { "topic": "preferences", "content": "---\n**2026-02-28 15:45:00 UTC**\n\nUser prefers CSV..." }
  ],
  "instructions": "Full SKILL.md body here...",
  "usage": "HOW TO USE THIS SKILL:\n\n1. READ the instructions..."
}
```

## Data Flow

### Writing a memory:
```
LLM activates skill → works on task → discovers insight
  → LLM calls update_skill_memory("my-skill", "workarounds", "The API returns 404...")
  → Handler validates skill exists + topic name is safe
  → Creates memories/ directory if needed
  → Appends timestamped entry to memories/workarounds.md
  → Returns "Memory saved to my-skill/memories/workarounds.md"
```

### Reading memories on activation:
```
LLM calls load_skill("my-skill")
  → SkillManager.ActivateSkill("my-skill") loads instructions
  → AgentBuilder.BuildSkillActivationResponse(skill, instructions)
    → LoadSkillMemories(skill) reads memories/*.md files
    → Builds JSON response with instructions + memories + resources + usage guide
  → LLM receives full context including past insights
```

### Cross-conversation persistence:
```
Conversation 1:
  LLM: update_skill_memory("my-skill", "workarounds", "API returns 404 for deleted parts")
  → File written to skills/my-skill/memories/workarounds.md

[User starts new conversation]

Conversation 2:
  LLM: load_skill("my-skill")
  → Response includes: memories: [{ topic: "workarounds", content: "...API returns 404..." }]
  → LLM has the insight from Conversation 1 without re-discovering it
```

## Verification

1. **Build:** Solution compiles with no errors
2. **Tool registration:** `list_skills` output is unchanged; `update_skill_memory` appears in the agent's tool list
3. **Memory creation:** Call `update_skill_memory("my-skill", "test-topic", "test content")` → verify `skills/my-skill/memories/test-topic.md` is created with timestamped entry
4. **Memory append:** Call `update_skill_memory` again with the same topic → verify content is appended, not overwritten
5. **Memory loading:** Call `load_skill("my-skill")` → verify the JSON response includes `memories` array with the saved entries
6. **Path traversal blocked:** Call `update_skill_memory("my-skill", "../evil", "hack")` → verify error response, no file created outside `memories/`
7. **Empty content blocked:** Call `update_skill_memory("my-skill", "test", "")` → verify error response
8. **Missing skill:** Call `update_skill_memory("nonexistent", "test", "content")` → verify error response
9. **No memories dir:** Call `load_skill` on a skill with no `memories/` directory → verify response has `memories: []` (not an error)
10. **Cross-conversation:** Save a memory, call `NewConversation()`, load the same skill → memories persist

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Topic name with spaces | Rejected (contains no special chars but spaces are fine since the topic is used as filename — actually let's allow hyphens and underscores but reject spaces too for clean filenames). **Decision:** Allow `[a-zA-Z0-9_-]` only. |
| Very large memory file | No explicit limit. The LLM's context window is the natural limit — if memories are too large, the `load_skill` response will be truncated by the LLM's context. Future: add a truncation cap (e.g., 10K chars per memory file). |
| Concurrent writes | `File.AppendAllText` is not atomic on all platforms. Acceptable risk for a CLI tool — unlikely to have concurrent processes writing to the same skill memory. |
| Skill in global vs project dir | Memories are written to whichever directory the skill lives in. Global skills get global memories, project skills get project-local memories. |

## Dependencies

- No new NuGet packages
- No new project references
- `System.Text.Json` (already available via `LlmTornado` dependency)
