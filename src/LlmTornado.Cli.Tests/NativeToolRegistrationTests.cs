using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Common;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class NativeToolRegistrationTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp() => _tempDir = TestHelpers.CreateTempDir();

    [TearDown]
    public void TearDown() => TestHelpers.CleanupTempDir(_tempDir);

    [Test]
    public async Task Build_RegistersNativeTools_ByDefault()
    {
        McpConfigLoader mcpLoader = new();
        AgentBuilder builder = CreateBuilder(mcpLoader, new AgentSettings());

        builder.Build();

        List<string> toolNames = builder.FullToolList.Select(t => t.ResolvedName).ToList();
        Assert.That(toolNames, Does.Contain("read_file"));
        Assert.That(toolNames, Does.Contain("write_file"));
        Assert.That(toolNames, Does.Contain("edit_file"));
        Assert.That(toolNames, Does.Contain("glob"));
        Assert.That(toolNames, Does.Contain("grep"));
        Assert.That(toolNames, Does.Contain("list_dir"));
        Assert.That(toolNames, Does.Contain("shell"));

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task Build_OmitsNativeTools_WhenDisabled()
    {
        McpConfigLoader mcpLoader = new();
        AgentBuilder builder = CreateBuilder(mcpLoader, new AgentSettings { NativeToolsEnabled = false });

        builder.Build();

        List<string> toolNames = builder.FullToolList.Select(t => t.ResolvedName).ToList();
        Assert.That(toolNames, Does.Not.Contain("read_file"));
        Assert.That(toolNames, Does.Not.Contain("shell"));

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task Build_DedupsToolNames_FirstRegistrationWins()
    {
        McpConfigLoader mcpLoader = new();
        // A later-registered tool with a native tool's name (simulates an MCP server
        // exposing read_file, e.g. Desktop Commander) must be shadowed.
        Tool duplicate = new(new Func<string>(() => "mcp-version"), "read_file", "duplicate read_file");
        AgentBuilder builder = CreateBuilder(mcpLoader, new AgentSettings(), additionalTools: [duplicate]);

        builder.Build();

        List<Tool> readFileTools = builder.FullToolList.Where(t => t.ResolvedName == "read_file").ToList();
        Assert.That(readFileTools, Has.Count.EqualTo(1));
        // The surviving tool is the native one (registered first), not the later duplicate.
        Assert.That(ReferenceEquals(readFileTools[0], duplicate), Is.False);

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public void SystemPrompt_MentionsNativeTools_OnlyWhenEnabled()
    {
        McpConfigLoader mcpLoader = new();

        AgentBuilder withTools = CreateBuilder(mcpLoader, new AgentSettings());
        withTools.Build();
        Assert.That(withTools.Agent.Instructions, Does.Contain("native file and shell tools"));

        AgentBuilder without = CreateBuilder(mcpLoader, new AgentSettings { NativeToolsEnabled = false });
        without.Build();
        Assert.That(without.Agent.Instructions, Does.Not.Contain("native file and shell tools"));
    }

    private AgentBuilder CreateBuilder(McpConfigLoader mcpLoader, AgentSettings settings, List<Tool>? additionalTools = null)
    {
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir);
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());
        TornadoApi api = new("test-key");
        ChatModel model = TestHelpers.CheapModel;
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);

        return new AgentBuilder(
            api,
            model,
            skillManager,
            mcpLoader,
            new NoOpToolApproval(),
            null,
            agentManager,
            settings,
            optimizerModel: null,
            additionalTools: additionalTools,
            memoryManager: memoryManager);
    }

    private sealed class NoOpToolApproval : IToolApproval
    {
        public ValueTask<bool> HandleToolPermissionRequest(string requestMessage) => ValueTask.FromResult(true);
        public void PreApproveSkillTools(IEnumerable<string> toolNames) { }
        public bool IsAutoApproved(string toolName) => true;
    }

    private sealed class NoOpPersistence : ISettingsPersistence
    {
        public void SaveSettings(AgentSettings settings) { }
    }
}
