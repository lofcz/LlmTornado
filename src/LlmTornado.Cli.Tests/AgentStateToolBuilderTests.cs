using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Core.State;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class AgentStateToolBuilderTests
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
    public async Task Build_Includes_StateTools_When_Store_Is_Supplied()
    {
        using SqliteAgentStateStore stateStore = new(Path.Combine(_tempDir, "state.db"));
        McpConfigLoader mcpLoader = new();
        AgentBuilder builder = CreateBuilder(mcpLoader, stateStore);

        builder.Build();

        List<string> toolNames = builder.FullToolList.Select(t => t.ResolvedName).ToList();
        Assert.That(toolNames, Does.Contain("memory_store"));
        Assert.That(toolNames, Does.Contain("memory_search"));
        Assert.That(toolNames, Does.Contain("state_set"));
        Assert.That(toolNames, Does.Contain("state_snapshot_restore"));

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task Build_Omits_StateTools_When_Store_Is_Not_Supplied()
    {
        McpConfigLoader mcpLoader = new();
        AgentBuilder builder = CreateBuilder(mcpLoader, null);

        builder.Build();

        List<string> toolNames = builder.FullToolList.Select(t => t.ResolvedName).ToList();
        Assert.That(toolNames, Does.Not.Contain("memory_store"));
        Assert.That(toolNames, Does.Not.Contain("state_set"));

        await mcpLoader.DisposeAsync();
    }

    private AgentBuilder CreateBuilder(McpConfigLoader mcpLoader, IAgentStateStore? stateStore)
    {
        AgentSettings settings = new();
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
            memoryManager: memoryManager,
            agentStateStore: stateStore);
    }

    private sealed class NoOpToolApproval : IToolApproval
    {
        public void PreApproveSkillTools(IEnumerable<string> toolNames) { }
        public bool IsAutoApproved(string toolName) => true;
        public ValueTask<bool> HandleToolPermissionRequest(string requestMessage) => ValueTask.FromResult(true);
    }
}
