using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Interactions;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Core.Tools;
using LlmTornado.Common;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class AgentBuilderToolOptimizationTests
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
    public async Task OptimizeToolsForTurn_RunsOnce_AndKeepsSelectedToolsActive()
    {
        AgentSettings settings = new() { MaxTools = 8 };
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ToolApprovalManager toolApproval = new(new ConsoleRenderer());
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());
        FakeToolOptimizer optimizer = new();

        List<Tool> extraTools = Enumerable.Range(0, 20)
            .Select(i => new Tool(new Func<string>(() => "ok"), $"extra_tool_{i:00}", $"Extra tool {i}"))
            .ToList();

        AgentBuilder builder = new(
            new TornadoApi("test-key"),
            ChatModel.OpenAi.Gpt41.V41Nano,
            skillManager,
            mcpLoader,
            toolApproval,
            userInteraction: null,
            agentManager,
            settings,
            optimizerModel: null,
            additionalTools: extraTools,
            toolOptimizer: optimizer);

        builder.Build();

        Assert.That(builder.NeedsOptimization, Is.True);

        ToolOptimizationResult? first = await builder.OptimizeToolsForTurn("first request");
        List<string> activeAfterFirst = builder.Agent.ToolList.Keys.OrderBy(x => x).ToList();

        ToolOptimizationResult? second = await builder.OptimizeToolsForTurn("second request");
        List<string> activeAfterSecond = builder.Agent.ToolList.Keys.OrderBy(x => x).ToList();

        Assert.That(first?.WasOptimized, Is.True);
        Assert.That(second, Is.Null);
        Assert.That(optimizer.CallCount, Is.EqualTo(1));
        Assert.That(builder.NeedsOptimization, Is.False);
        Assert.That(activeAfterSecond, Is.EqualTo(activeAfterFirst));
        Assert.That(activeAfterFirst, Does.Contain("list_all_tools"));
        Assert.That(activeAfterFirst, Does.Contain("select_tools"));
        Assert.That(activeAfterFirst, Does.Contain("extra_tool_00"));
        Assert.That(activeAfterFirst.Count, Is.LessThan(builder.TotalToolCount));

        await mcpLoader.DisposeAsync();
    }

    private sealed class FakeToolOptimizer : IToolOptimizer
    {
        public int CallCount { get; private set; }

        public Task<ToolOptimizationResult> OptimizeAsync(
            List<Tool> allTools,
            string userMessage,
            CancellationToken ct = default)
        {
            CallCount++;

            HashSet<string> keep = new(StringComparer.OrdinalIgnoreCase)
            {
                "load_skill",
                "list_skills",
                "read_reference",
                "list_all_tools",
                "select_tools",
                "extra_tool_00",
            };

            List<Tool> selected = allTools
                .Where(t => keep.Contains(t.ResolvedName))
                .ToList();

            return Task.FromResult(new ToolOptimizationResult
            {
                Tools = selected,
                WasOptimized = true,
                OriginalCount = allTools.Count,
                SelectedCount = selected.Count,
            });
        }
    }
}
