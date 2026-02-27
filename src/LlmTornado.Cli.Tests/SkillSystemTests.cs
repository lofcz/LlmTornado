using LlmTornado.Cli;
using LlmTornado.Cli.Skills;

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

    #region CliSkillLoader — ParseSkillMetadata

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

        CliSkill? skill = CliSkillLoader.ParseSkillMetadata(skillDir);

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

        CliSkill? skill = CliSkillLoader.ParseSkillMetadata(emptyDir);
        Assert.That(skill, Is.Null);
    }

    [Test]
    public void ParseSkillMetadata_Rejects_Name_With_Uppercase()
    {
        string dir = CreateSkillWithFrontmatter("BadName", "name: BadName\ndescription: test");
        CliSkill? skill = CliSkillLoader.ParseSkillMetadata(dir);
        Assert.That(skill, Is.Null);
    }

    [Test]
    public void ParseSkillMetadata_Rejects_Name_With_Consecutive_Hyphens()
    {
        string dir = CreateSkillWithFrontmatter("bad--name", "name: bad--name\ndescription: test");
        CliSkill? skill = CliSkillLoader.ParseSkillMetadata(dir);
        Assert.That(skill, Is.Null);
    }

    [Test]
    public void ParseSkillMetadata_Rejects_Mismatched_Name_And_Directory()
    {
        string dir = CreateSkillWithFrontmatter("actual-dir", "name: different-name\ndescription: test");
        CliSkill? skill = CliSkillLoader.ParseSkillMetadata(dir);
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

        CliSkill? skill = CliSkillLoader.ParseSkillMetadata(dir);
        Assert.That(skill, Is.Not.Null);
        Assert.That(skill!.Name, Is.EqualTo("minimal"));
        Assert.That(skill.Description, Is.EqualTo("Minimal skill"));
        Assert.That(skill.AllowedTools, Is.Empty);
        Assert.That(skill.Metadata, Is.Empty);
    }

    #endregion

    #region CliSkillLoader — LoadInstructions

    [Test]
    public void LoadInstructions_Extracts_Body_After_Frontmatter()
    {
        string dir = CreateValidSkill("body-test", @"---
name: body-test
description: test
---
Line one.
Line two.");

        CliSkill skill = CliSkillLoader.ParseSkillMetadata(dir)!;
        Assert.That(skill.Instructions, Is.Null); // Not loaded yet

        CliSkillLoader.LoadInstructions(skill);
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

        CliSkill skill = CliSkillLoader.ParseSkillMetadata(dir)!;
        CliSkillLoader.LoadInstructions(skill);
        string? firstLoad = skill.Instructions;

        CliSkillLoader.LoadInstructions(skill);
        Assert.That(skill.Instructions, Is.EqualTo(firstLoad));
    }

    #endregion

    #region CliSkillLoader — DiscoverSkills

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

        List<CliSkill> skills = CliSkillLoader.DiscoverSkills(_tempDir);

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

        List<CliSkill> skills = CliSkillLoader.DiscoverSkills(_tempDir);
        Assert.That(skills, Has.Count.EqualTo(1));
        Assert.That(skills[0].Name, Is.EqualTo("good-skill"));
    }

    [Test]
    public void DiscoverSkills_Returns_Empty_For_NonexistentDir()
    {
        List<CliSkill> skills = CliSkillLoader.DiscoverSkills(Path.Combine(_tempDir, "nope"));
        Assert.That(skills, Is.Empty);
    }

    #endregion

    #region CliSkillLoader — Script Discovery

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

        CliSkill skill = CliSkillLoader.ParseSkillMetadata(dir)!;
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

        CliSkill skill = CliSkillLoader.ParseSkillMetadata(dir)!;
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

        CliSkill skill = CliSkillLoader.ParseSkillMetadata(dir)!;
        Assert.That(skill.References, Has.Count.EqualTo(1));
        Assert.That(skill.Assets, Has.Count.EqualTo(1));
    }

    #endregion

    #region CliSkillManager

    [Test]
    public void LoadSkills_Populates_Manager()
    {
        CreateValidSkill("skill-a", @"---
name: skill-a
description: First
---
First body.");

        CliSettings settings = new();
        CliSkillManager manager = new(settings);
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

        CliSettings settings = new();
        CliSkillManager manager = new(settings);
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

        CliSettings settings = new();
        CliSkillManager manager = new(settings);
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
        CliSettings settings = new();
        CliSkillManager manager = new(settings);
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

        CliSettings settings = new();
        CliSkillManager manager = new(settings);
        manager.LoadSkills(_tempDir);

        string? instructions = manager.ActivateSkill("activatable");
        Assert.That(instructions, Is.Not.Null);
        Assert.That(instructions, Does.Contain("These are the instructions."));
    }

    [Test]
    public void ActivateSkill_Returns_Null_For_Unknown()
    {
        CliSettings settings = new();
        CliSkillManager manager = new(settings);
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

        CliSettings settings = new();
        CliSkillManager manager = new(settings);
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

        CliSettings settings = new() { DisabledSkills = ["pre-disabled"] };
        CliSkillManager manager = new(settings);
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

        CliSkill skill = CliSkillLoader.ParseSkillMetadata(dir)!;
        skill.Enabled = true;

        var tools = ScriptToolBuilder.BuildScriptTools([skill]);
        Assert.That(tools, Has.Count.EqualTo(2));

        var toolNames = tools.Select(t => t.ResolvedName).ToList();
        Assert.That(toolNames, Does.Contain("tooled:run"));
        Assert.That(toolNames, Does.Contain("tooled:build"));
    }

    [Test]
    public void BuildScriptTools_Returns_Empty_For_No_Scripts()
    {
        string dir = CreateValidSkill("no-scripts", @"---
name: no-scripts
description: test
---
Body.");

        CliSkill skill = CliSkillLoader.ParseSkillMetadata(dir)!;
        skill.Enabled = true;

        var tools = ScriptToolBuilder.BuildScriptTools([skill]);
        Assert.That(tools, Is.Empty);
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
