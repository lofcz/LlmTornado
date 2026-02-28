using System.Text.Json;
using LlmTornado.Cli;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class CliStorageTests
{
    private string _tempDir = null!;
    private string _originalRoot = null!;

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

    #region SaveJson / LoadJson round-trip

    [Test]
    public void SaveJson_LoadJson_RoundTrips_SimpleObject()
    {
        string path = Path.Combine(_tempDir, "test.json");
        var data = new TestData { Name = "hello", Value = 42 };

        CliStorage.SaveJson(path, data);

        Assert.That(File.Exists(path), Is.True);

        TestData? loaded = CliStorage.LoadJson<TestData>(path);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Name, Is.EqualTo("hello"));
        Assert.That(loaded.Value, Is.EqualTo(42));
    }

    [Test]
    public void SaveJson_Overwrites_ExistingFile()
    {
        string path = Path.Combine(_tempDir, "overwrite.json");
        CliStorage.SaveJson(path, new TestData { Name = "first", Value = 1 });
        CliStorage.SaveJson(path, new TestData { Name = "second", Value = 2 });

        TestData? loaded = CliStorage.LoadJson<TestData>(path);
        Assert.That(loaded!.Name, Is.EqualTo("second"));
        Assert.That(loaded.Value, Is.EqualTo(2));
    }

    [Test]
    public void LoadJson_Returns_Null_For_MissingFile()
    {
        string path = Path.Combine(_tempDir, "nonexistent.json");
        TestData? result = CliStorage.LoadJson<TestData>(path);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void LoadJson_Returns_Null_For_InvalidJson()
    {
        string path = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(path, "this is {not valid} json!!!}");

        TestData? result = CliStorage.LoadJson<TestData>(path);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SaveJson_AtomicWrite_NoTmpFileLeftBehind()
    {
        string path = Path.Combine(_tempDir, "atomic.json");
        CliStorage.SaveJson(path, new TestData { Name = "test" });

        Assert.That(File.Exists(path + ".tmp"), Is.False);
    }

    #endregion

    #region AgentSettings serialization

    [Test]
    public void CliSettings_RoundTrip_AllFields()
    {
        string path = Path.Combine(_tempDir, "settings.json");
        AgentSettings settings = new()
        {
            ActiveModel = "gpt-4.1-nano",
            DisabledSkills = ["skill-a", "skill-b"],
            SkillsDirectory = "/custom/skills",
            McpConfigPath = "/custom/mcp.json",
            MaxTurnsBeforeSummary = 10,
        };

        CliStorage.SaveJson(path, settings);
        AgentSettings? loaded = CliStorage.LoadJson<AgentSettings>(path);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.ActiveModel, Is.EqualTo("gpt-4.1-nano"));
        Assert.That(loaded.DisabledSkills, Has.Count.EqualTo(2));
        Assert.That(loaded.DisabledSkills, Does.Contain("skill-a"));
        Assert.That(loaded.SkillsDirectory, Is.EqualTo("/custom/skills"));
        Assert.That(loaded.McpConfigPath, Is.EqualTo("/custom/mcp.json"));
        Assert.That(loaded.MaxTurnsBeforeSummary, Is.EqualTo(10));
    }

    [Test]
    public void CliSettings_Defaults_AreCorrect()
    {
        AgentSettings settings = new();

        Assert.That(settings.ActiveModel, Is.Null);
        Assert.That(settings.DisabledSkills, Is.Empty);
        Assert.That(settings.SkillsDirectory, Is.Null);
        Assert.That(settings.McpConfigPath, Is.Null);
        Assert.That(settings.MaxTurnsBeforeSummary, Is.EqualTo(0));
    }

    [Test]
    public void CliSettings_JsonPropertyNames_AreCamelSnakeCase()
    {
        string path = Path.Combine(_tempDir, "props.json");
        AgentSettings settings = new() { ActiveModel = "test", MaxTurnsBeforeSummary = 5 };
        CliStorage.SaveJson(path, settings);

        string json = File.ReadAllText(path);
        Assert.That(json, Does.Contain("active_model"));
        Assert.That(json, Does.Contain("disabled_skills"));
        Assert.That(json, Does.Contain("max_turns_before_summary"));
    }

    #endregion

    #region Dictionary serialization (tool approvals)

    [Test]
    public void SaveJson_LoadJson_Dictionary_RoundTrip()
    {
        string path = Path.Combine(_tempDir, "approvals.json");
        var data = new Dictionary<string, string>
        {
            ["my-tool"] = "allow",
            ["other-tool"] = "deny",
        };

        CliStorage.SaveJson(path, data);
        var loaded = CliStorage.LoadJson<Dictionary<string, string>>(path);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded, Has.Count.EqualTo(2));
        Assert.That(loaded!["my-tool"], Is.EqualTo("allow"));
        Assert.That(loaded["other-tool"], Is.EqualTo("deny"));
    }

    #endregion

    private class TestData
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
