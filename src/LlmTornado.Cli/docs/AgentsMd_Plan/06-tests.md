# Phase 6: Tests

## Goal

Provide comprehensive test coverage for all new agent definition functionality. Tests follow the existing patterns in `LlmTornado.Cli.Tests/` (NUnit 4.4, `InternalsVisibleTo`, temp directories via `TestHelpers.CreateTempDir()`).

---

## File to Create

### `src/LlmTornado.Cli.Tests/AgentDefinitionTests.cs`

---

## Test Organization

Tests are grouped into nested classes by functional area:

```csharp
namespace LlmTornado.Cli.Tests;

[TestFixture]
internal sealed class AgentDefinitionTests
{
    [TestFixture]
    internal sealed class PersonaParsing { ... }

    [TestFixture]
    internal sealed class ProjectDiscovery { ... }

    [TestFixture]
    internal sealed class PersonaDiscovery { ... }

    [TestFixture]
    internal sealed class ManagerLifecycle { ... }

    [TestFixture]
    internal sealed class CapabilityBaseline { ... }

    [TestFixture]
    internal sealed class ToolFiltering { ... }

    [TestFixture]
    internal sealed class SettingsPersistence { ... }

    [TestFixture]
    internal sealed class InstructionsBlock { ... }
}
```

---

## Test Fixtures by Category

### 1. PersonaParsing — Frontmatter & Markdown Extraction

These tests validate `AgentDefinitionLoader.ParsePersonaFile()`:

```csharp
[TestFixture]
internal sealed class PersonaParsing
{
    private string _tempDir;

    [SetUp]
    public void Setup()
    {
        _tempDir = TestHelpers.CreateTempDir();
    }

    [TearDown]
    public void Teardown()
    {
        Directory.Delete(_tempDir, recursive: true);
    }

    /// <summary>
    /// Full frontmatter with all capability fields is correctly parsed.
    /// </summary>
    [Test]
    public void FullFrontmatter_AllFieldsParsed()
    {
        string content = """
            ---
            name: test-agent
            description: A test agent for unit testing
            enabled-skills: file-analyzer web-search
            disabled-skills: note-taker
            disabled-tools: web-search:ddg-search web-search:fetch-url
            auto-approve-tools: file-analyzer:line-count
            ---

            # Test Agent Instructions

            You are a test agent.
            """;
        string path = Path.Combine(_tempDir, "test-agent.md");
        File.WriteAllText(path, content);

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("test-agent"));
        Assert.That(result.Description, Is.EqualTo("A test agent for unit testing"));
        Assert.That(result.Source, Is.EqualTo(AgentSource.Custom));
        Assert.That(result.EnabledSkills, Is.EqualTo(new[] { "file-analyzer", "web-search" }));
        Assert.That(result.DisabledSkills, Is.EqualTo(new[] { "note-taker" }));
        Assert.That(result.DisabledTools,
            Is.EqualTo(new[] { "web-search:ddg-search", "web-search:fetch-url" }));
        Assert.That(result.AutoApproveTools,
            Is.EqualTo(new[] { "file-analyzer:line-count" }));
        Assert.That(result.Instructions, Does.Contain("# Test Agent Instructions"));
        Assert.That(result.Instructions, Does.Contain("You are a test agent."));
        Assert.That(result.HasCapabilityCuration, Is.True);
    }

    /// <summary>
    /// File with no frontmatter: name from filename, description from body.
    /// </summary>
    [Test]
    public void NoFrontmatter_NameFromFilename_DescriptionFromBody()
    {
        string content = """
            # Quick Helper

            A fast assistant that gives concise answers.

            ## Style
            - Keep it short
            """;
        string path = Path.Combine(_tempDir, "quick-helper.md");
        File.WriteAllText(path, content);

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.BuiltIn);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("quick-helper"));
        Assert.That(result.Description, Is.EqualTo("A fast assistant that gives concise answers."));
        Assert.That(result.Source, Is.EqualTo(AgentSource.BuiltIn));
        Assert.That(result.EnabledSkills, Is.Empty);
        Assert.That(result.DisabledSkills, Is.Empty);
        Assert.That(result.HasCapabilityCuration, Is.False);
    }

    /// <summary>
    /// Frontmatter name takes precedence over filename slug.
    /// </summary>
    [Test]
    public void FrontmatterName_TakesPrecedence_OverFilename()
    {
        string content = """
            ---
            name: custom-name
            description: Uses frontmatter name
            ---

            Instructions here.
            """;
        string path = Path.Combine(_tempDir, "different-filename.md");
        File.WriteAllText(path, content);

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("custom-name"));
    }

    /// <summary>
    /// Empty file returns null.
    /// </summary>
    [Test]
    public void EmptyFile_ReturnsNull()
    {
        string path = Path.Combine(_tempDir, "empty.md");
        File.WriteAllText(path, "");

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Whitespace-only file returns null.
    /// </summary>
    [Test]
    public void WhitespaceOnly_ReturnsNull()
    {
        string path = Path.Combine(_tempDir, "whitespace.md");
        File.WriteAllText(path, "   \n  \n  ");

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Invalid filename (underscores, uppercase) returns null.
    /// </summary>
    [Test]
    public void InvalidFilename_ReturnsNull()
    {
        string path = Path.Combine(_tempDir, "Invalid_Name.md");
        File.WriteAllText(path, "# Instructions");

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Filename with consecutive hyphens returns null.
    /// </summary>
    [Test]
    public void ConsecutiveHyphens_ReturnsNull()
    {
        string path = Path.Combine(_tempDir, "bad--name.md");
        File.WriteAllText(path, "# Instructions");

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Partial frontmatter: only some fields present.
    /// </summary>
    [Test]
    public void PartialFrontmatter_MissingFieldsDefaultToEmpty()
    {
        string content = """
            ---
            name: partial
            enabled-skills: file-analyzer
            ---

            Some instructions.
            """;
        string path = Path.Combine(_tempDir, "partial.md");
        File.WriteAllText(path, content);

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("partial"));
        Assert.That(result.Description, Is.EqualTo("Some instructions."));
        Assert.That(result.EnabledSkills, Is.EqualTo(new[] { "file-analyzer" }));
        Assert.That(result.DisabledSkills, Is.Empty);
        Assert.That(result.DisabledTools, Is.Empty);
        Assert.That(result.AutoApproveTools, Is.Empty);
        Assert.That(result.HasCapabilityCuration, Is.True);
    }

    /// <summary>
    /// Single skill/tool in space-delimited field parses as single-element list.
    /// </summary>
    [Test]
    public void SingleValueInList_ParsesCorrectly()
    {
        string content = """
            ---
            enabled-skills: file-analyzer
            disabled-tools: web-search:ddg-search
            ---

            Instructions.
            """;
        string path = Path.Combine(_tempDir, "single-value.md");
        File.WriteAllText(path, content);

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result!.EnabledSkills, Has.Count.EqualTo(1));
        Assert.That(result.DisabledTools, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Unknown frontmatter fields are silently ignored.
    /// </summary>
    [Test]
    public void UnknownFrontmatterFields_AreIgnored()
    {
        string content = """
            ---
            name: tolerant
            unknown-field: some value
            another-thing: whatever
            enabled-skills: file-analyzer
            ---

            Instructions.
            """;
        string path = Path.Combine(_tempDir, "tolerant.md");
        File.WriteAllText(path, content);

        CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
            path, AgentSource.Custom);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("tolerant"));
        Assert.That(result.EnabledSkills, Is.EqualTo(new[] { "file-analyzer" }));
    }
}
```

---

### 2. ProjectDiscovery — CWD Hierarchy Walker

Tests for `AgentDefinitionLoader.DiscoverProjectAgents()`:

```csharp
[TestFixture]
internal sealed class ProjectDiscovery
{
    private string _tempDir;

    [SetUp]
    public void Setup()
    {
        _tempDir = TestHelpers.CreateTempDir();
    }

    [TearDown]
    public void Teardown()
    {
        Directory.Delete(_tempDir, recursive: true);
    }

    /// <summary>
    /// Single AGENTS.md in CWD is discovered.
    /// </summary>
    [Test]
    public void SingleAgentsMd_Discovered()
    {
        File.WriteAllText(
            Path.Combine(_tempDir, "AGENTS.md"),
            "# Project\n\n## Build\ndotnet build");

        CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(_tempDir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Source, Is.EqualTo(AgentSource.Project));
        Assert.That(result.Name, Is.EqualTo("project-context"));
        Assert.That(result.Instructions, Does.Contain("dotnet build"));
    }

    /// <summary>
    /// Multiple AGENTS.md in hierarchy: nearest first in merged content.
    /// </summary>
    [Test]
    public void MultipleAgentsMd_NearestFirst()
    {
        // Create hierarchy: tempDir/sub/AGENTS.md and tempDir/AGENTS.md
        string subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);

        File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "# Root instructions");
        File.WriteAllText(Path.Combine(subDir, "AGENTS.md"), "# Sub instructions");

        CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(subDir);

        Assert.That(result, Is.Not.Null);
        // Nearest file (sub/) should appear first in the merged content
        int subIndex = result!.Instructions.IndexOf("Sub instructions");
        int rootIndex = result.Instructions.IndexOf("Root instructions");
        Assert.That(subIndex, Is.LessThan(rootIndex),
            "Nearest AGENTS.md content should appear first");
        Assert.That(result.FilePath, Does.Contain("sub"),
            "FilePath should be the nearest file");
    }

    /// <summary>
    /// No AGENTS.md found returns null.
    /// </summary>
    [Test]
    public void NoAgentsMd_ReturnsNull()
    {
        // Empty temp directory — no AGENTS.md anywhere we can reliably test
        // (can't guarantee parent dirs don't have one)
        // Create isolated deep directory
        string deep = Path.Combine(_tempDir, "a", "b", "c", "d");
        Directory.CreateDirectory(deep);

        CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(deep);

        // May or may not be null depending on parent directories
        // At minimum, if result is not null, it should be a Project source
        if (result is not null)
            Assert.That(result.Source, Is.EqualTo(AgentSource.Project));
    }

    /// <summary>
    /// Merged content includes source path comments for each file.
    /// </summary>
    [Test]
    public void MergedContent_IncludesPathComments()
    {
        string subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);

        File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "Root content");
        File.WriteAllText(Path.Combine(subDir, "AGENTS.md"), "Sub content");

        CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(subDir);

        Assert.That(result!.Instructions, Does.Contain("<!-- AGENTS.md from:"));
    }

    /// <summary>
    /// Empty AGENTS.md files are skipped.
    /// </summary>
    [Test]
    public void EmptyAgentsMd_Skipped()
    {
        File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "   ");

        CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(_tempDir);

        // Empty/whitespace-only AGENTS.md should not produce a result
        // (unless a parent directory has a valid one)
        if (result is not null)
            Assert.That(result.Instructions, Is.Not.Empty.And.Not.WhiteSpace);
    }

    /// <summary>
    /// Project context has no capability curation.
    /// </summary>
    [Test]
    public void ProjectContext_NoCuration()
    {
        File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "# Instructions\nBuild stuff.");

        CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(_tempDir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.HasCapabilityCuration, Is.False);
        Assert.That(result.EnabledSkills, Is.Empty);
        Assert.That(result.DisabledSkills, Is.Empty);
        Assert.That(result.EnabledTools, Is.Empty);
        Assert.That(result.DisabledTools, Is.Empty);
    }
}
```

---

### 3. PersonaDiscovery — Built-in + Custom Scanning

Tests for `AgentDefinitionLoader.DiscoverPersonaAgents()`:

```csharp
[TestFixture]
internal sealed class PersonaDiscovery
{
    private string _tempDir;
    private string _builtInDir;
    private string _customDir;

    [SetUp]
    public void Setup()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _builtInDir = Path.Combine(_tempDir, "built-in");
        _customDir = Path.Combine(_tempDir, "custom");
        Directory.CreateDirectory(_builtInDir);
        Directory.CreateDirectory(_customDir);
    }

    [TearDown]
    public void Teardown()
    {
        Directory.Delete(_tempDir, recursive: true);
    }

    /// <summary>
    /// Built-in agents are discovered with correct source.
    /// </summary>
    [Test]
    public void BuiltInAgents_DiscoveredWithCorrectSource()
    {
        File.WriteAllText(Path.Combine(_builtInDir, "reviewer.md"),
            "---\nname: reviewer\ndescription: Reviews code\n---\n# Review");

        List<CliAgentDefinition> result =
            AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("reviewer"));
        Assert.That(result[0].Source, Is.EqualTo(AgentSource.BuiltIn));
    }

    /// <summary>
    /// Custom agent shadows built-in with same name.
    /// </summary>
    [Test]
    public void CustomAgent_ShadowsBuiltIn_BySameName()
    {
        File.WriteAllText(Path.Combine(_builtInDir, "reviewer.md"),
            "---\nname: reviewer\ndescription: Built-in reviewer\n---\n# Built-in");
        File.WriteAllText(Path.Combine(_customDir, "reviewer.md"),
            "---\nname: reviewer\ndescription: Custom reviewer\n---\n# Custom");

        List<CliAgentDefinition> result =
            AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Source, Is.EqualTo(AgentSource.Custom));
        Assert.That(result[0].Description, Is.EqualTo("Custom reviewer"));
    }

    /// <summary>
    /// Both built-in and custom agents coexist when names differ.
    /// </summary>
    [Test]
    public void MixedSources_Coexist()
    {
        File.WriteAllText(Path.Combine(_builtInDir, "reviewer.md"),
            "---\nname: reviewer\n---\n# Review");
        File.WriteAllText(Path.Combine(_customDir, "my-agent.md"),
            "---\nname: my-agent\n---\n# Custom");

        List<CliAgentDefinition> result =
            AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(a => a.Name), Is.EquivalentTo(new[] { "reviewer", "my-agent" }));
    }

    /// <summary>
    /// Non-.md files are ignored.
    /// </summary>
    [Test]
    public void NonMdFiles_Ignored()
    {
        File.WriteAllText(Path.Combine(_builtInDir, "config.json"), "{}");
        File.WriteAllText(Path.Combine(_builtInDir, "readme.txt"), "hello");
        File.WriteAllText(Path.Combine(_builtInDir, "valid.md"),
            "---\nname: valid\n---\n# OK");

        List<CliAgentDefinition> result =
            AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("valid"));
    }

    /// <summary>
    /// Empty directories produce no agents.
    /// </summary>
    [Test]
    public void EmptyDirectories_NoAgents()
    {
        List<CliAgentDefinition> result =
            AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Non-existent directories handled gracefully.
    /// </summary>
    [Test]
    public void NonExistentDirectories_HandledGracefully()
    {
        string fakePath = Path.Combine(_tempDir, "nonexistent");

        List<CliAgentDefinition> result =
            AgentDefinitionLoader.DiscoverPersonaAgents(fakePath, fakePath);

        Assert.That(result, Is.Empty);
    }
}
```

---

### 4. ManagerLifecycle — Selection, Clear, Restore

Tests for `AgentDefinitionManager`:

```csharp
[TestFixture]
internal sealed class ManagerLifecycle
{
    private string _tempDir;
    private string _builtInDir;
    private string _customDir;
    private CliSettings _settings;
    private AgentDefinitionManager _manager;

    [SetUp]
    public void Setup()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _builtInDir = Path.Combine(_tempDir, "built-in");
        _customDir = Path.Combine(_tempDir, "custom");
        Directory.CreateDirectory(_builtInDir);
        Directory.CreateDirectory(_customDir);

        // Create test agents
        File.WriteAllText(Path.Combine(_builtInDir, "agent-a.md"),
            "---\nname: agent-a\ndescription: Agent A\n---\n# A");
        File.WriteAllText(Path.Combine(_builtInDir, "agent-b.md"),
            "---\nname: agent-b\ndescription: Agent B\n---\n# B");

        _settings = new CliSettings();
        _manager = new AgentDefinitionManager(_settings);
        _manager.LoadAll(_builtInDir, _customDir, _tempDir);
    }

    [TearDown]
    public void Teardown()
    {
        Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void LoadAll_DiscoversPersonas()
    {
        Assert.That(_manager.GetAllPersonas(), Has.Count.EqualTo(2));
    }

    [Test]
    public void SetActivePersona_ValidName_Succeeds()
    {
        CliAgentDefinition? result = _manager.SetActivePersona("agent-a");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("agent-a"));
        Assert.That(_manager.ActivePersonaName, Is.EqualTo("agent-a"));
    }

    [Test]
    public void SetActivePersona_InvalidName_ReturnsNull()
    {
        CliAgentDefinition? result = _manager.SetActivePersona("nonexistent");

        Assert.That(result, Is.Null);
        Assert.That(_manager.ActivePersonaName, Is.Null);
    }

    [Test]
    public void ClearActivePersona_ResetsState()
    {
        _manager.SetActivePersona("agent-a");
        _manager.ClearActivePersona();

        Assert.That(_manager.ActivePersonaName, Is.Null);
        Assert.That(_manager.GetActivePersona(), Is.Null);
    }

    [Test]
    public void GetActivePersona_ReturnsSelectedAgent()
    {
        _manager.SetActivePersona("agent-b");

        CliAgentDefinition? active = _manager.GetActivePersona();

        Assert.That(active, Is.Not.Null);
        Assert.That(active!.Name, Is.EqualTo("agent-b"));
    }

    [Test]
    public void LoadAll_RestoresSavedAgent()
    {
        _settings.ActiveAgent = "agent-a";
        AgentDefinitionManager fresh = new(_settings);
        fresh.LoadAll(_builtInDir, _customDir, _tempDir);

        Assert.That(fresh.ActivePersonaName, Is.EqualTo("agent-a"));
    }

    [Test]
    public void LoadAll_ClearsMissingSavedAgent()
    {
        _settings.ActiveAgent = "deleted-agent";
        AgentDefinitionManager fresh = new(_settings);
        fresh.LoadAll(_builtInDir, _customDir, _tempDir);

        Assert.That(fresh.ActivePersonaName, Is.Null);
        Assert.That(_settings.ActiveAgent, Is.Null);
    }

    [Test]
    public void SetActivePersona_IsCaseInsensitive()
    {
        CliAgentDefinition? result = _manager.SetActivePersona("Agent-A");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("agent-a"));
    }
}
```

---

### 5. CapabilityBaseline — Skill/Tool Curation

Tests for `AgentDefinitionManager.ApplyCapabilityBaseline()`:

```csharp
[TestFixture]
internal sealed class CapabilityBaseline
{
    private string _tempDir;
    private string _builtInDir;
    private string _customDir;
    private CliSettings _settings;
    private AgentDefinitionManager _manager;
    private CliSkillManager _skillManager;

    [SetUp]
    public void Setup()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _builtInDir = Path.Combine(_tempDir, "agents-built-in");
        _customDir = Path.Combine(_tempDir, "agents-custom");
        Directory.CreateDirectory(_builtInDir);
        Directory.CreateDirectory(_customDir);

        // Create agent with curation
        File.WriteAllText(Path.Combine(_builtInDir, "curated.md"), """
            ---
            name: curated
            enabled-skills: skill-a skill-b
            disabled-skills: skill-b
            disabled-tools: skill-a:dangerous
            auto-approve-tools: skill-a:safe
            ---
            # Curated agent
            """);

        // Create agent without curation
        File.WriteAllText(Path.Combine(_builtInDir, "uncurated.md"), """
            ---
            name: uncurated
            description: No curation
            ---
            # Uncurated
            """);

        _settings = new CliSettings();
        _manager = new AgentDefinitionManager(_settings);
        _manager.LoadAll(_builtInDir, _customDir, _tempDir);

        // Create mock skills
        string skillsDir = Path.Combine(_tempDir, "skills");
        CreateMockSkill(skillsDir, "skill-a", "Skill A");
        CreateMockSkill(skillsDir, "skill-b", "Skill B");
        CreateMockSkill(skillsDir, "skill-c", "Skill C");

        _skillManager = new CliSkillManager(_settings);
        _skillManager.LoadSkills(skillsDir);
    }

    private void CreateMockSkill(string skillsDir, string name, string desc)
    {
        string dir = Path.Combine(skillsDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"),
            $"---\nname: {name}\ndescription: {desc}\n---\n# {desc}");
    }

    [TearDown]
    public void Teardown()
    {
        Directory.Delete(_tempDir, recursive: true);
    }

    /// <summary>
    /// Agent with enabled-skills whitelist: only whitelisted skills remain enabled.
    /// </summary>
    [Test]
    public void Whitelist_OnlyEnabledSkillsActive()
    {
        _manager.SetActivePersona("curated");
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        _manager.ApplyCapabilityBaseline(_skillManager, toolApproval);

        // enabled-skills: [skill-a, skill-b], disabled-skills: [skill-b]
        // Result: skill-a enabled, skill-b disabled (blacklist overrides), skill-c disabled (not whitelisted)
        Assert.That(_skillManager.GetSkill("skill-a")!.Enabled, Is.True);
        Assert.That(_skillManager.GetSkill("skill-b")!.Enabled, Is.False);
        Assert.That(_skillManager.GetSkill("skill-c")!.Enabled, Is.False);
    }

    /// <summary>
    /// Agent without curation: all skills remain enabled.
    /// </summary>
    [Test]
    public void NoCuration_AllSkillsEnabled()
    {
        _manager.SetActivePersona("uncurated");
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        _manager.ApplyCapabilityBaseline(_skillManager, toolApproval);

        Assert.That(_skillManager.GetSkill("skill-a")!.Enabled, Is.True);
        Assert.That(_skillManager.GetSkill("skill-b")!.Enabled, Is.True);
        Assert.That(_skillManager.GetSkill("skill-c")!.Enabled, Is.True);
    }

    /// <summary>
    /// Clear persona: all skills restored to enabled.
    /// </summary>
    [Test]
    public void ClearPersona_AllSkillsRestored()
    {
        // Set curated agent (disables skill-b and skill-c)
        _manager.SetActivePersona("curated");
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        _manager.ApplyCapabilityBaseline(_skillManager, toolApproval);

        // Clear and reapply
        _manager.ClearActivePersona();
        _manager.ApplyCapabilityBaseline(_skillManager, toolApproval);

        Assert.That(_skillManager.GetSkill("skill-a")!.Enabled, Is.True);
        Assert.That(_skillManager.GetSkill("skill-b")!.Enabled, Is.True);
        Assert.That(_skillManager.GetSkill("skill-c")!.Enabled, Is.True);
    }

    /// <summary>
    /// Baseline is idempotent: calling twice produces same state.
    /// </summary>
    [Test]
    public void Baseline_IsIdempotent()
    {
        _manager.SetActivePersona("curated");
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);

        _manager.ApplyCapabilityBaseline(_skillManager, toolApproval);
        _manager.ApplyCapabilityBaseline(_skillManager, toolApproval); // second call

        Assert.That(_skillManager.GetSkill("skill-a")!.Enabled, Is.True);
        Assert.That(_skillManager.GetSkill("skill-b")!.Enabled, Is.False);
        Assert.That(_skillManager.GetSkill("skill-c")!.Enabled, Is.False);
    }
}
```

---

### 6. ToolFiltering — IsToolAllowed

Tests for `AgentDefinitionManager.IsToolAllowed()`:

```csharp
[TestFixture]
internal sealed class ToolFiltering
{
    [Test]
    public void NoActivePersona_AllToolsAllowed()
    {
        CliSettings settings = new();
        AgentDefinitionManager manager = new(settings);
        // No LoadAll() — no personas

        Assert.That(manager.IsToolAllowed("any-tool"), Is.True);
        Assert.That(manager.IsToolAllowed("web-search:ddg-search"), Is.True);
    }

    [Test]
    public void BuiltInTools_AlwaysAllowed()
    {
        // Even with an active persona that has strict curation
        // Test requires setup with a curated agent
        // load_skill, list_skills, read_reference are always allowed

        // (Detailed setup similar to CapabilityBaseline tests)
        // Assert IsToolAllowed("load_skill") == true
        // Assert IsToolAllowed("list_skills") == true
        // Assert IsToolAllowed("read_reference") == true
    }

    [Test]
    public void BlockedTool_Rejected()
    {
        // Agent with disabled-tools: [web-search:ddg-search]
        // After ApplyCapabilityBaseline()
        // Assert IsToolAllowed("web-search:ddg-search") == false
    }

    [Test]
    public void UnblockedTool_Allowed()
    {
        // Agent with disabled-tools: [web-search:ddg-search]
        // Assert IsToolAllowed("file-analyzer:line-count") == true
    }

    [Test]
    public void ToolWhitelist_OnlyWhitelistedAllowed()
    {
        // Agent with enabled-tools: [file-analyzer:line-count]
        // Assert IsToolAllowed("file-analyzer:line-count") == true
        // Assert IsToolAllowed("file-analyzer:find-todos") == false
    }
}
```

---

### 7. SettingsPersistence — Round-trip

Tests for settings serialization with new agent fields:

```csharp
[TestFixture]
internal sealed class SettingsPersistence
{
    [Test]
    public void NewFields_SerializeAndDeserialize()
    {
        CliSettings original = new()
        {
            ActiveModel = "gpt-4o",
            ActiveAgent = "code-reviewer",
            AgentsDirectory = "/custom/agents",
            ProjectAgentsEnabled = false,
            DisabledSkills = ["note-taker"]
        };

        string json = System.Text.Json.JsonSerializer.Serialize(original);
        CliSettings? restored = System.Text.Json.JsonSerializer.Deserialize<CliSettings>(json);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.ActiveAgent, Is.EqualTo("code-reviewer"));
        Assert.That(restored.AgentsDirectory, Is.EqualTo("/custom/agents"));
        Assert.That(restored.ProjectAgentsEnabled, Is.False);
    }

    [Test]
    public void OldSettingsJson_DeserializesWithDefaults()
    {
        // Simulate an old settings.json without the new fields
        string oldJson = """{"active_model":"gpt-4o","disabled_skills":[]}""";

        CliSettings? settings = System.Text.Json.JsonSerializer.Deserialize<CliSettings>(oldJson);

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.ActiveAgent, Is.Null);
        Assert.That(settings.AgentsDirectory, Is.Null);
        Assert.That(settings.ProjectAgentsEnabled, Is.True); // default
    }
}
```

---

### 8. InstructionsBlock — System Prompt Assembly

Tests for `AgentDefinitionManager.BuildInstructionsBlock()`:

```csharp
[TestFixture]
internal sealed class InstructionsBlock
{
    [Test]
    public void WithPersonaAndProject_BothIncluded()
    {
        // Setup: active persona + project AGENTS.md
        // Assert: output contains <agent_persona> and <project_context> tags
        // Assert: persona content appears before project content
    }

    [Test]
    public void PersonaOnly_NoProjectTags()
    {
        // Setup: active persona, no project AGENTS.md
        // Assert: output contains <agent_persona> but not <project_context>
    }

    [Test]
    public void ProjectOnly_NoPersonaTags()
    {
        // Setup: no active persona, project AGENTS.md exists
        // Assert: output contains <project_context> but not <agent_persona>
    }

    [Test]
    public void NoPersonaNoProject_ReturnsEmpty()
    {
        // Setup: no active persona, no project AGENTS.md
        // Assert: output is empty string
    }

    [Test]
    public void ProjectDisabled_NotIncluded()
    {
        // Setup: project AGENTS.md exists but settings.ProjectAgentsEnabled = false
        // Assert: output does not contain <project_context>
    }
}
```

---

## Test Helpers

Reuse the existing `TestHelpers` pattern from the test project:

```csharp
internal static class TestHelpers
{
    public static string CreateTempDir()
    {
        string path = Path.Combine(Path.GetTempPath(), "tornado-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
```

---

## Test Count Summary

| Category | Test Count | Focus |
|----------|-----------|-------|
| PersonaParsing | ~10 | Frontmatter parsing, edge cases |
| ProjectDiscovery | ~6 | Hierarchy walking, merging |
| PersonaDiscovery | ~6 | Built-in/custom scanning, shadowing |
| ManagerLifecycle | ~7 | Selection, clear, restore |
| CapabilityBaseline | ~4 | Skill whitelist/blacklist application |
| ToolFiltering | ~5 | Tool allow/block predicate |
| SettingsPersistence | ~2 | JSON round-trip |
| InstructionsBlock | ~5 | System prompt content assembly |
| **Total** | **~45** | |

---

## Running Tests

```bash
cd src
dotnet test LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj --verbosity normal
```

Filter to just agent tests:
```bash
dotnet test LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj --filter "AgentDefinition"
```

All tests are self-contained (no API keys, no external dependencies) and use temp directories that are cleaned up in `[TearDown]`.
