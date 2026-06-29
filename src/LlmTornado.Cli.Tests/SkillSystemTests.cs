using LlmTornado.Cli;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class SkillSystemTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTempDir();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.CleanupTempDir(_tempDir);
    }

    #region SkillLoader — ParseSkillMetadata

    [Test]
    public void ParseSkillMetadata_Valid_Skill_Returns_Parsed_Skill()
    {
        string skillDir = CreateValidSkill("my-skill", @"---
name: my-skill
description: A test skill
license: MIT
compatibility: "">=1.0""
allowed-tools: tool-a tool-b
metadata:
  author: tester
  version: 1.0.0
---
## Instructions

This is the body.");

        Skill? skill = SkillLoader.ParseSkillMetadata(skillDir);

        Assert.That(skill, Is.Not.Null);
        Assert.That(skill!.Name, Is.EqualTo("my-skill"));
        Assert.That(skill.Description, Is.EqualTo("A test skill"));
        Assert.That(skill.License, Is.EqualTo("MIT"));
        Assert.That(skill.AllowedTools, Has.Count.EqualTo(2));
        Assert.That(skill.AllowedTools, Does.Contain("tool-a"));
        Assert.That(skill.AllowedTools, Does.Contain("tool-b"));
        Assert.That(skill.Metadata, Does.ContainKey("author"));
        Assert.That(skill.Metadata["author"], Is.EqualTo("tester"));
        Assert.That(skill.Metadata["version"], Is.EqualTo("1.0.0"));
    }

    [Test]
    public void ParseSkillMetadata_Returns_Null_When_No_SkillMd()
    {
        string emptyDir = Path.Combine(_tempDir, "empty-skill");
        Directory.CreateDirectory(emptyDir);

        Skill? skill = SkillLoader.ParseSkillMetadata(emptyDir);
        Assert.That(skill, Is.Null);
    }

    [Test]
    public void ParseSkillMetadata_Rejects_Name_With_Uppercase()
    {
        string dir = CreateSkillWithFrontmatter("BadName", "name: BadName\ndescription: test");
        Skill? skill = SkillLoader.ParseSkillMetadata(dir);
        Assert.That(skill, Is.Null);
    }

    [Test]
    public void ParseSkillMetadata_Rejects_Name_With_Consecutive_Hyphens()
    {
        string dir = CreateSkillWithFrontmatter("bad--name", "name: bad--name\ndescription: test");
        Skill? skill = SkillLoader.ParseSkillMetadata(dir);
        Assert.That(skill, Is.Null);
    }

    [Test]
    public void ParseSkillMetadata_Rejects_Mismatched_Name_And_Directory()
    {
        string dir = CreateSkillWithFrontmatter("actual-dir", "name: different-name\ndescription: test");
        Skill? skill = SkillLoader.ParseSkillMetadata(dir);
        Assert.That(skill, Is.Null);
    }

    [Test]
    public void ParseSkillMetadata_Accepts_MinimalFrontmatter()
    {
        string dir = CreateValidSkill("minimal", @"---
name: minimal
description: Minimal skill
---
Body here.");

        Skill? skill = SkillLoader.ParseSkillMetadata(dir);
        Assert.That(skill, Is.Not.Null);
        Assert.That(skill!.Name, Is.EqualTo("minimal"));
        Assert.That(skill.Description, Is.EqualTo("Minimal skill"));
        Assert.That(skill.AllowedTools, Is.Empty);
        Assert.That(skill.Metadata, Is.Empty);
    }

    #endregion

    #region SkillLoader — LoadInstructions

    [Test]
    public void LoadInstructions_Extracts_Body_After_Frontmatter()
    {
        string dir = CreateValidSkill("body-test", @"---
name: body-test
description: test
---
Line one.
Line two.");

        Skill skill = SkillLoader.ParseSkillMetadata(dir)!;
        Assert.That(skill.Instructions, Is.Null); // Not loaded yet

        SkillLoader.LoadInstructions(skill);
        Assert.That(skill.Instructions, Is.Not.Null);
        Assert.That(skill.Instructions, Does.Contain("Line one."));
        Assert.That(skill.Instructions, Does.Contain("Line two."));
    }

    [Test]
    public void LoadInstructions_Is_Idempotent()
    {
        string dir = CreateValidSkill("idem", @"---
name: idem
description: test
---
Body.");

        Skill skill = SkillLoader.ParseSkillMetadata(dir)!;
        SkillLoader.LoadInstructions(skill);
        string? firstLoad = skill.Instructions;

        SkillLoader.LoadInstructions(skill);
        Assert.That(skill.Instructions, Is.EqualTo(firstLoad));
    }

    #endregion

    #region SkillLoader — DiscoverSkills

    [Test]
    public void DiscoverSkills_Finds_Multiple_Skills()
    {
        CreateValidSkill("alpha", @"---
name: alpha
description: First
---
Alpha body.");

        CreateValidSkill("beta", @"---
name: beta
description: Second
---
Beta body.");

        List<Skill> skills = SkillLoader.DiscoverSkills(_tempDir);

        Assert.That(skills, Has.Count.EqualTo(2));
        Assert.That(skills.Select(s => s.Name), Does.Contain("alpha"));
        Assert.That(skills.Select(s => s.Name), Does.Contain("beta"));
    }

    [Test]
    public void DiscoverSkills_Skips_Invalid_Skills()
    {
        CreateValidSkill("good-skill", @"---
name: good-skill
description: Valid
---
Body.");

        // Create invalid skill (no SKILL.md)
        Directory.CreateDirectory(Path.Combine(_tempDir, "no-skill-md"));

        List<Skill> skills = SkillLoader.DiscoverSkills(_tempDir);
        Assert.That(skills, Has.Count.EqualTo(1));
        Assert.That(skills[0].Name, Is.EqualTo("good-skill"));
    }

    [Test]
    public void DiscoverSkills_Returns_Empty_For_NonexistentDir()
    {
        List<Skill> skills = SkillLoader.DiscoverSkills(Path.Combine(_tempDir, "nope"));
        Assert.That(skills, Is.Empty);
    }

    #endregion

    #region SkillLoader — Script Discovery

    [Test]
    public void DiscoverSkills_Finds_Scripts()
    {
        string dir = CreateValidSkill("scripted", @"---
name: scripted
description: Has scripts
---
Body.");

        string scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "run.py"), "print('hello')");
        File.WriteAllText(Path.Combine(scriptsDir, "build.sh"), "echo hello");

        Skill skill = SkillLoader.ParseSkillMetadata(dir)!;
        Assert.That(skill.Scripts, Has.Count.EqualTo(2));
        Assert.That(skill.Scripts.Select(s => s.Extension), Does.Contain("py"));
        Assert.That(skill.Scripts.Select(s => s.Extension), Does.Contain("sh"));
    }

    [Test]
    public void Script_Command_Detection_Python()
    {
        string dir = CreateValidSkill("py-skill", @"---
name: py-skill
description: test
---
Body.");

        string scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "main.py"), "pass");

        Skill skill = SkillLoader.ParseSkillMetadata(dir)!;
        SkillScript pyScript = skill.Scripts.First(s => s.Extension == "py");

        string expected = OperatingSystem.IsWindows() ? "python" : "python3";
        Assert.That(pyScript.Command, Is.EqualTo(expected));
    }

    [Test]
    public void DiscoverSkills_Finds_References_And_Assets()
    {
        string dir = CreateValidSkill("resources", @"---
name: resources
description: Has resources
---
Body.");

        string refsDir = Path.Combine(dir, "references");
        Directory.CreateDirectory(refsDir);
        File.WriteAllText(Path.Combine(refsDir, "api.md"), "# API");

        string assetsDir = Path.Combine(dir, "assets");
        Directory.CreateDirectory(assetsDir);
        File.WriteAllText(Path.Combine(assetsDir, "icon.png"), "fake-png");

        Skill skill = SkillLoader.ParseSkillMetadata(dir)!;
        Assert.That(skill.References, Has.Count.EqualTo(1));
        Assert.That(skill.Assets, Has.Count.EqualTo(1));
    }

    #endregion

    #region SkillManager

    [Test]
    public void LoadSkills_Populates_Manager()
    {
        CreateValidSkill("skill-a", @"---
name: skill-a
description: First
---
First body.");

        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        Assert.That(manager.GetAllSkills(), Has.Count.EqualTo(1));
        Assert.That(manager.GetEnabledSkills(), Has.Count.EqualTo(1));
    }

    [Test]
    public void DisableSkill_Removes_From_Enabled()
    {
        CreateValidSkill("disableable", @"---
name: disableable
description: Can be disabled
---
Body.");

        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);
        Assert.That(manager.GetEnabledSkills(), Has.Count.EqualTo(1));

        bool result = manager.DisableSkill("disableable");
        Assert.That(result, Is.True);
        Assert.That(manager.GetEnabledSkills(), Is.Empty);
        Assert.That(manager.GetAllSkills(), Has.Count.EqualTo(1)); // Still exists
    }

    [Test]
    public void EnableSkill_Restores_Disabled_Skill()
    {
        CreateValidSkill("toggleable", @"---
name: toggleable
description: test
---
Body.");

        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        manager.DisableSkill("toggleable");
        Assert.That(manager.GetEnabledSkills(), Is.Empty);

        bool result = manager.EnableSkill("toggleable");
        Assert.That(result, Is.True);
        Assert.That(manager.GetEnabledSkills(), Has.Count.EqualTo(1));
    }

    [Test]
    public void DisableSkill_Returns_False_For_Unknown()
    {
        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        Assert.That(manager.DisableSkill("nonexistent"), Is.False);
    }

    [Test]
    public void ActivateSkill_Loads_Instructions()
    {
        CreateValidSkill("activatable", @"---
name: activatable
description: test
---
These are the instructions.");

        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        string? instructions = manager.ActivateSkill("activatable");
        Assert.That(instructions, Is.Not.Null);
        Assert.That(instructions, Does.Contain("These are the instructions."));
    }

    [Test]
    public void ActivateSkill_Returns_Null_For_Unknown()
    {
        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        Assert.That(manager.ActivateSkill("nope"), Is.Null);
    }

    [Test]
    public void BuildSkillsContextXml_Contains_AvailableSkills_Tag()
    {
        CreateValidSkill("xml-skill", @"---
name: xml-skill
description: For XML test
---
Body.");

        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        string xml = manager.BuildSkillsContextXml();
        Assert.That(xml, Does.Contain("<available_skills>"));
        Assert.That(xml, Does.Contain("</available_skills>"));
        Assert.That(xml, Does.Contain("xml-skill"));
        Assert.That(xml, Does.Contain("For XML test"));
    }

    [Test]
    public void Settings_DisabledSkills_Respected_On_Load()
    {
        CreateValidSkill("pre-disabled", @"---
name: pre-disabled
description: test
---
Body.");

        AgentSettings settings = new() { DisabledSkills = ["pre-disabled"] };
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        Assert.That(manager.GetAllSkills(), Has.Count.EqualTo(1));
        Assert.That(manager.GetEnabledSkills(), Is.Empty);
    }

    #endregion

    #region ScriptToolBuilder

    [Test]
    public void BuildScriptTools_Creates_Tools_For_Scripts()
    {
        string dir = CreateValidSkill("tooled", @"---
name: tooled
description: test
---
Body.");

        string scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "run.py"), "print('hi')");
        File.WriteAllText(Path.Combine(scriptsDir, "build.sh"), "echo hi");

        Skill skill = SkillLoader.ParseSkillMetadata(dir)!;
        skill.Enabled = true;

        var tools = ScriptToolBuilder.BuildScriptTools([skill]);
        Assert.That(tools, Has.Count.EqualTo(2));

        var toolNames = tools.Select(t => t.ResolvedName).ToList();
        Assert.That(toolNames, Does.Contain("tooled__run"));
        Assert.That(toolNames, Does.Contain("tooled__build"));
    }

    [Test]
    public void BuildScriptTools_Returns_Empty_For_No_Scripts()
    {
        string dir = CreateValidSkill("no-scripts", @"---
name: no-scripts
description: test
---
Body.");

        Skill skill = SkillLoader.ParseSkillMetadata(dir)!;
        skill.Enabled = true;

        var tools = ScriptToolBuilder.BuildScriptTools([skill]);
        Assert.That(tools, Is.Empty);
    }

    #endregion

    #region Bundled Skills — file-analyzer, web-search, note-taker

    private static string GetBundledSkillsDir()
    {
        // Walk up from bin/Debug/net8.0 to find the skills directory
        string dir = AppContext.BaseDirectory;
        // Try successive parent directories looking for LlmTornado.Cli/Skills (or skills)
        for (int i = 0; i < 8; i++)
        {
            dir = Path.GetDirectoryName(dir)!;
            string candidate = Path.Combine(dir, "LlmTornado.Cli", "skills");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "file-analyzer", "SKILL.md")))
                return candidate;
            // Also check src/ subfolder
            candidate = Path.Combine(dir, "src", "LlmTornado.Cli", "skills");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "file-analyzer", "SKILL.md")))
                return candidate;
        }
        Assert.Ignore("Bundled skills directory not found — skipping.");
        return null!; // unreachable
    }

    [Test]
    public void BundledSkills_AllThreeDiscovered()
    {
        string skillsDir = GetBundledSkillsDir();
        List<Skill> skills = SkillLoader.DiscoverSkills(skillsDir);

        var names = skills.Select(s => s.Name).OrderBy(n => n).ToList();
        Assert.That(names, Does.Contain("file-analyzer"));
        Assert.That(names, Does.Contain("web-search"));
        Assert.That(names, Does.Contain("note-taker"));
    }

    [Test]
    public void BundledSkills_FileAnalyzer_HasExpectedScripts()
    {
        string skillsDir = GetBundledSkillsDir();
        Skill? skill = SkillLoader.ParseSkillMetadata(Path.Combine(skillsDir, "file-analyzer"));
        Assert.That(skill, Is.Not.Null);
        Assert.That(skill!.Name, Is.EqualTo("file-analyzer"));
        Assert.That(skill.Description, Does.Contain("Analyze"));

        var scriptNames = skill.Scripts.Select(s => Path.GetFileNameWithoutExtension(s.FileName)).ToList();
        Assert.That(scriptNames, Does.Contain("line-count"));
        Assert.That(scriptNames, Does.Contain("find-todos"));
        Assert.That(scriptNames, Does.Contain("detect-encoding"));
        Assert.That(scriptNames, Does.Contain("find-duplicates"));
        Assert.That(scriptNames, Does.Contain("tree-summary"));
        Assert.That(skill.Scripts.Count, Is.EqualTo(5));

        Assert.That(skill.References, Is.Not.Empty, "Should have references");
        Assert.That(skill.AllowedTools, Has.Count.EqualTo(5));
    }

    [Test]
    public void BundledSkills_WebSearch_HasExpectedScripts()
    {
        string skillsDir = GetBundledSkillsDir();
        Skill? skill = SkillLoader.ParseSkillMetadata(Path.Combine(skillsDir, "web-search"));
        Assert.That(skill, Is.Not.Null);
        Assert.That(skill!.Name, Is.EqualTo("web-search"));

        var scriptNames = skill.Scripts.Select(s => Path.GetFileNameWithoutExtension(s.FileName)).ToList();
        Assert.That(scriptNames, Does.Contain("ddg-search"));
        Assert.That(scriptNames, Does.Contain("fetch-url"));
        Assert.That(scriptNames, Does.Contain("extract-text"));
        Assert.That(skill.Scripts.Count, Is.EqualTo(3));
    }

    [Test]
    public void BundledSkills_NoteTaker_HasExpectedScripts()
    {
        string skillsDir = GetBundledSkillsDir();
        Skill? skill = SkillLoader.ParseSkillMetadata(Path.Combine(skillsDir, "note-taker"));
        Assert.That(skill, Is.Not.Null);
        Assert.That(skill!.Name, Is.EqualTo("note-taker"));

        var scriptNames = skill.Scripts.Select(s => Path.GetFileNameWithoutExtension(s.FileName)).ToList();
        Assert.That(scriptNames, Does.Contain("add-note"));
        Assert.That(scriptNames, Does.Contain("search-notes"));
        Assert.That(scriptNames, Does.Contain("list-notes"));
        Assert.That(scriptNames, Does.Contain("view-note"));
        Assert.That(scriptNames, Does.Contain("delete-note"));
        Assert.That(skill.Scripts.Count, Is.EqualTo(5));
    }

    [Test]
    public void BundledSkills_InstructionsLoad()
    {
        string skillsDir = GetBundledSkillsDir();
        List<Skill> skills = SkillLoader.DiscoverSkills(skillsDir);

        foreach (Skill skill in skills)
        {
            Assert.That(skill.Instructions, Is.Null, $"{skill.Name} instructions should be lazy-loaded");
            SkillLoader.LoadInstructions(skill);
            Assert.That(skill.Instructions, Is.Not.Null.And.Not.Empty, $"{skill.Name} should have instructions");
            Assert.That(skill.Instructions, Does.Contain("##"), $"{skill.Name} instructions should have markdown headings");
        }
    }

    [Test]
    public void BundledSkills_ScriptToolBuilder_CreatesAllTools()
    {
        string skillsDir = GetBundledSkillsDir();
        List<Skill> skills = SkillLoader.DiscoverSkills(skillsDir);
        foreach (Skill s in skills) s.Enabled = true;

        var tools = ScriptToolBuilder.BuildScriptTools(skills);
        var toolNames = tools.Select(t => t.ResolvedName).OrderBy(n => n).ToList();

        // file-analyzer: 5, web-search: 3, note-taker: 5 = 13 total
        Assert.That(tools, Has.Count.EqualTo(13));

        // Spot-check naming convention {skill}:{script}
        Assert.That(toolNames, Does.Contain("file-analyzer__line-count"));
        Assert.That(toolNames, Does.Contain("web-search__ddg-search"));
        Assert.That(toolNames, Does.Contain("note-taker__add-note"));
    }

    [Test]
    public void BundledSkills_SkillManager_LoadsAll()
    {
        string skillsDir = GetBundledSkillsDir();
        AgentSettings settings = new() { SkillsDirectory = skillsDir };
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(skillsDir);

        Assert.That(manager.GetEnabledSkills().Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(manager.GetEnabledSkills().Select(s => s.Name), Does.Contain("file-analyzer"));
        Assert.That(manager.GetEnabledSkills().Select(s => s.Name), Does.Contain("web-search"));
        Assert.That(manager.GetEnabledSkills().Select(s => s.Name), Does.Contain("note-taker"));

        string xml = manager.BuildSkillsContextXml();
        Assert.That(xml, Does.Contain("file-analyzer"));
        Assert.That(xml, Does.Contain("web-search"));
        Assert.That(xml, Does.Contain("note-taker"));
    }

    #endregion

    #region Global directory resolution

    [Test]
    public void ResolveGlobalSkillsDirectory_Uses_TornadoHome_Subfolder_When_Set()
    {
        string? original = Environment.GetEnvironmentVariable(TornadoPaths.HomeEnvVar);
        try
        {
            string customRoot = Path.Combine(_tempDir, "custom-home");
            Environment.SetEnvironmentVariable(TornadoPaths.HomeEnvVar, customRoot);

            Assert.That(SkillLoader.ResolveGlobalSkillsDirectory(),
                Is.EqualTo(Path.Combine(Path.GetFullPath(customRoot), "skills")));
            Assert.That(AgentDefinitionLoader.ResolveGlobalAgentsDirectory(),
                Is.EqualTo(Path.Combine(Path.GetFullPath(customRoot), "agents")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(TornadoPaths.HomeEnvVar, original);
        }
    }

    [Test]
    public void ResolveGlobalDirectories_Default_To_AppData_Llmtornado()
    {
        string? original = Environment.GetEnvironmentVariable(TornadoPaths.HomeEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(TornadoPaths.HomeEnvVar, null);

            Assert.That(SkillLoader.ResolveGlobalSkillsDirectory(),
                Does.EndWith(Path.Combine("llmtornado", "skills")));
            Assert.That(AgentDefinitionLoader.ResolveGlobalAgentsDirectory(),
                Does.EndWith(Path.Combine("llmtornado", "agents")));
            // Never resolves into the source tree.
            Assert.That(SkillLoader.ResolveGlobalSkillsDirectory(),
                Does.Not.Contain(Path.Combine("LlmTornado.Cli", "skills")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(TornadoPaths.HomeEnvVar, original);
        }
    }

    [Test]
    public void ResolveProjectDirectories_Use_Cwd_Llmtornado_Subfolder()
    {
        // Override is honored verbatim.
        string overridden = Path.Combine(_tempDir, "explicit");
        Assert.That(SkillLoader.ResolveSkillsDirectory(overridden), Is.EqualTo(Path.GetFullPath(overridden)));

        // No override → <cwd>/llmtornado/{skills,agents}.
        string cwd = Directory.GetCurrentDirectory();
        Assert.That(SkillLoader.ResolveSkillsDirectory(null),
            Is.EqualTo(Path.Combine(cwd, "llmtornado", "skills")));
        Assert.That(AgentDefinitionLoader.ResolveAgentsDirectory(null),
            Is.EqualTo(Path.Combine(cwd, "llmtornado", "agents")));
    }

    #endregion

    #region Seeding built-in skills

    [Test]
    public void SeedBuiltInSkills_Copies_When_Absent()
    {
        string bundled = Path.Combine(_tempDir, "bundled");
        string skillDir = Path.Combine(bundled, "seeded-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "---\nname: seeded-skill\ndescription: test\n---\nBody.");

        string global = Path.Combine(_tempDir, "global");
        List<string> seeded = [];
        SkillLoader.SeedBuiltInSkills(bundled, global, seeded.Add);

        Assert.That(seeded, Does.Contain("seeded-skill"));
        Assert.That(File.Exists(Path.Combine(global, "seeded-skill", "SKILL.md")), Is.True);
    }

    [Test]
    public void SeedBuiltInSkills_Preserves_Existing_User_Edits()
    {
        string bundled = Path.Combine(_tempDir, "bundled");
        string skillDir = Path.Combine(bundled, "shared");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "---\nname: shared\ndescription: bundled\n---\nBundled body.");

        string global = Path.Combine(_tempDir, "global");
        string destSkill = Path.Combine(global, "shared");
        Directory.CreateDirectory(destSkill);
        File.WriteAllText(Path.Combine(destSkill, "SKILL.md"), "user-edited");

        List<string> seeded = [];
        SkillLoader.SeedBuiltInSkills(bundled, global, seeded.Add);

        Assert.That(seeded, Is.Empty, "Existing skill folders must not be re-seeded.");
        Assert.That(File.ReadAllText(Path.Combine(destSkill, "SKILL.md")), Is.EqualTo("user-edited"));
    }

    [Test]
    public void SeedBuiltInSkills_NoOp_When_No_Bundled_Dir()
    {
        string global = Path.Combine(_tempDir, "global");
        Assert.DoesNotThrow(() =>
            SkillLoader.SeedBuiltInSkills(Path.Combine(_tempDir, "missing"), global, null));
    }

    #endregion

    #region Discovery diagnostics

    [Test]
    public void ParseSkillMetadata_Warns_On_Name_Directory_Mismatch()
    {
        string dir = CreateSkillWithFrontmatter("actual-dir", "name: different-name\ndescription: test");

        List<string> warnings = [];
        Skill? skill = SkillLoader.ParseSkillMetadata(dir, warnings.Add);

        Assert.That(skill, Is.Null);
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.Contain("different-name").And.Contain("actual-dir"));
    }

    [Test]
    public void ParseSkillMetadata_Warns_On_Missing_Description()
    {
        string dir = CreateValidSkill("nodesc", "---\nname: nodesc\n---\nBody.");

        List<string> warnings = [];
        Skill? skill = SkillLoader.ParseSkillMetadata(dir, warnings.Add);

        Assert.That(skill, Is.Null);
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.Contain("description"));
    }

    #endregion

    #region Helpers

    private string CreateValidSkill(string name, string skillMdContent)
    {
        string skillDir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), skillMdContent);
        return skillDir;
    }

    private string CreateSkillWithFrontmatter(string dirName, string frontmatterContent)
    {
        string dir = Path.Combine(_tempDir, dirName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"---\n{frontmatterContent}\n---\nBody.");
        return dir;
    }

    #endregion
}
