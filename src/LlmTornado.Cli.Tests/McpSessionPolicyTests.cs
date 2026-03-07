using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Mcp;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class McpSessionPolicyTests
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

    [Test]
    public void FromSettings_Allows_Cwd_And_Subdirectories()
    {
        string subDir = Path.Combine(_tempDir, "src", "nested");
        Directory.CreateDirectory(subDir);

        McpSessionPolicy policy = McpSessionPolicy.FromSettings(new AgentSettings(), _tempDir);

        Assert.That(policy.IsFilesystemPathAllowed(_tempDir), Is.True);
        Assert.That(policy.IsFilesystemPathAllowed(subDir), Is.True);
    }

    [Test]
    public void FromSettings_Blocks_Sibling_Directory_Without_Whitelist()
    {
        string parent = Directory.GetParent(_tempDir)!.FullName;
        string sibling = Path.Combine(parent, Path.GetFileName(_tempDir) + "-sibling");
        Directory.CreateDirectory(sibling);

        McpSessionPolicy policy = McpSessionPolicy.FromSettings(new AgentSettings(), _tempDir);

        Assert.That(policy.IsFilesystemPathAllowed(sibling), Is.False);
    }

    [Test]
    public void FromSettings_Allows_Explicit_Whitelist_Outside_Cwd()
    {
        string parent = Directory.GetParent(_tempDir)!.FullName;
        string sibling = Path.Combine(parent, Path.GetFileName(_tempDir) + "-shared");
        Directory.CreateDirectory(sibling);

        AgentSettings settings = new()
        {
            FilesystemWhitelist = [sibling],
            TerminalDirectoryWhitelist = [sibling]
        };

        McpSessionPolicy policy = McpSessionPolicy.FromSettings(settings, _tempDir);

        Assert.That(policy.IsFilesystemPathAllowed(sibling), Is.True);
        Assert.That(policy.IsTerminalDirectoryAllowed(sibling), Is.True);
    }

    [Test]
    public void Command_Blocklist_Takes_Precedence_Over_Allowlist()
    {
        AgentSettings settings = new()
        {
            AllowedCommands = ["dotnet", "npm"],
            BlockedCommands = ["npm"]
        };

        McpSessionPolicy policy = McpSessionPolicy.FromSettings(settings, _tempDir);

        Assert.That(policy.IsCommandAllowed("dotnet build"), Is.True);
        Assert.That(policy.IsCommandAllowed("npm test"), Is.False);
        Assert.That(policy.IsCommandAllowed("pwsh -c dir"), Is.False);
    }
}