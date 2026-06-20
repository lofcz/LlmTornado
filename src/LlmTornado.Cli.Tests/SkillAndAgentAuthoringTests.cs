using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Authoring;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class SkillAndAgentAuthoringTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp() => _tempDir = TestHelpers.CreateTempDir();

    [TearDown]
    public void TearDown() => TestHelpers.CleanupTempDir(_tempDir);

    #region Slugify / validation

    [TestCase("My Cool Skill", "my-cool-skill")]
    [TestCase("  weird__name!! ", "weird-name")]
    [TestCase("already-good", "already-good")]
    [TestCase("UPPER", "upper")]
    public void Slugify_Produces_Valid_Names(string input, string expected)
    {
        string slug = SkillLoader.Slugify(input);
        Assert.That(slug, Is.EqualTo(expected));
        Assert.That(SkillLoader.IsValidSkillName(slug), Is.True);
    }

    #endregion

    #region WriteSkillMd round-trip

    [Test]
    public void WriteSkillMd_RoundTrips_Through_ParseSkillMetadata()
    {
        string path = SkillLoader.WriteSkillMd(
            _tempDir,
            name: "My Test Skill",
            description: "Does a thing when the user asks for \"a thing\"",
            instructions: "## Steps\n\nDo the thing.",
            license: "MIT",
            allowedTools: ["my-test-skill:run", "my-test-skill:check"],
            fullSkeleton: true);

        Assert.That(File.Exists(path), Is.True);

        string skillDir = Path.GetDirectoryName(path)!;
        Assert.That(Path.GetFileName(skillDir), Is.EqualTo("my-test-skill"));
        Assert.That(Directory.Exists(Path.Combine(skillDir, "scripts")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(skillDir, "references")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(skillDir, "assets")), Is.True);

        Skill? parsed = SkillLoader.ParseSkillMetadata(skillDir);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Name, Is.EqualTo("my-test-skill"));
        Assert.That(parsed.Description, Is.EqualTo("Does a thing when the user asks for \"a thing\""));
        Assert.That(parsed.License, Is.EqualTo("MIT"));
        Assert.That(parsed.AllowedTools, Does.Contain("my-test-skill:run"));
        Assert.That(parsed.AllowedTools, Does.Contain("my-test-skill:check"));
    }

    [Test]
    public void WriteSkillMd_Throws_On_Unusable_Name()
    {
        Assert.Throws<ArgumentException>(() =>
            SkillLoader.WriteSkillMd(_tempDir, "!!!", "desc", "body"));
    }

    #endregion

    #region WriteAgentMd round-trip

    [Test]
    public void WriteAgentMd_RoundTrips_Through_DiscoverPersonaAgents()
    {
        string filePath = Path.Combine(_tempDir, "my-reviewer.md");
        AgentDefinitionLoader.WriteAgentMd(
            filePath,
            name: "my-reviewer",
            description: "Reviews code carefully",
            instructions: "You are a careful reviewer.",
            enabledSkills: ["file-analyzer"],
            disabledTools: ["note-taker:delete"]);

        Assert.That(File.Exists(filePath), Is.True);

        List<AgentDefinition> agents = AgentDefinitionLoader.DiscoverPersonaAgents(
            builtInDirectory: Path.Combine(_tempDir, "no-builtin"),
            globalDirectory: null,
            customDirectory: _tempDir);

        AgentDefinition? agent = agents.FirstOrDefault(a => a.Name == "my-reviewer");
        Assert.That(agent, Is.Not.Null);
        Assert.That(agent!.Description, Is.EqualTo("Reviews code carefully"));
        Assert.That(agent.EnabledSkills, Does.Contain("file-analyzer"));
        Assert.That(agent.DisabledTools, Does.Contain("note-taker:delete"));
        Assert.That(agent.Instructions, Does.Contain("careful reviewer"));
    }

    #endregion

    #region SkillManager create / delete round-trip

    [Test]
    public void SkillManager_Creates_Then_Deletes_Skill()
    {
        AgentSettings settings = new();
        SkillManager manager = new(settings, new NoOpPersistence());
        manager.LoadSkills(_tempDir);

        manager.CreateSkill(_tempDir, "Temp Skill", "A throwaway skill", "## Body");
        manager.LoadSkills(_tempDir);
        Assert.That(manager.GetSkill("temp-skill"), Is.Not.Null);

        string skillDir = Path.Combine(_tempDir, "temp-skill");
        Assert.That(Directory.Exists(skillDir), Is.True);

        bool removed = manager.DeleteSkill("temp-skill");
        Assert.That(removed, Is.True);
        Assert.That(Directory.Exists(skillDir), Is.False);
        Assert.That(manager.GetSkill("temp-skill"), Is.Null);
    }

    [Test]
    public void SkillManager_DeleteSkill_Returns_False_When_Missing()
    {
        SkillManager manager = new(new AgentSettings(), new NoOpPersistence());
        manager.LoadSkills(_tempDir);
        Assert.That(manager.DeleteSkill("does-not-exist"), Is.False);
    }

    #endregion

    #region AuthoringAssistant.CleanBody

    [Test]
    public void CleanBody_Strips_Wrapping_Code_Fence()
    {
        string cleaned = AuthoringAssistant.CleanBody("```markdown\n## Title\n\nBody text.\n```");
        Assert.That(cleaned, Is.EqualTo("## Title\n\nBody text."));
    }

    [Test]
    public void CleanBody_Strips_Leading_Frontmatter()
    {
        string cleaned = AuthoringAssistant.CleanBody("---\nname: x\ndescription: y\n---\n## Real Body");
        Assert.That(cleaned, Is.EqualTo("## Real Body"));
    }

    [Test]
    public void CleanBody_Leaves_Plain_Markdown_Untouched()
    {
        string cleaned = AuthoringAssistant.CleanBody("## Just markdown\n\nNo wrappers here.");
        Assert.That(cleaned, Is.EqualTo("## Just markdown\n\nNo wrappers here."));
    }

    #endregion
}
