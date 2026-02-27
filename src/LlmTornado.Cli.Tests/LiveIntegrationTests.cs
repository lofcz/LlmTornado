using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Mcp;
using LlmTornado.Cli.Memory;
using LlmTornado.Cli.Skills;
using LlmTornado.Code;
using ChatRuntime = LlmTornado.Agents.ChatRuntime.ChatRuntime;

namespace LlmTornado.Cli.Tests;

/// <summary>
/// Live integration tests using OpenAI API.
/// All tests use gpt-4.1-nano for minimal token costs.
/// Tests are skipped when OPENAI_API_KEY is not set.
/// </summary>
[TestFixture]
public class LiveIntegrationTests
{
    private TornadoApi? _api;
    private ChatModel _model = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _api = TestHelpers.CreateOpenAiApi();
        _model = TestHelpers.CheapModel;
    }

    private void RequireApi()
    {
        if (_api is null) Assert.Ignore("OPENAI_API_KEY not set — skipping live test.");
    }

    #region Live: Basic ChatRuntime Invoke

    [Test]
    public async Task Live_ChatRuntime_SimpleInvoke_Returns_Response()
    {
        RequireApi();

        TornadoAgent agent = new(
            client: _api!,
            model: _model,
            name: "test-agent",
            instructions: "Reply with exactly 'OK' and nothing else.",
            streaming: false);

        SingletonRuntimeConfiguration config = new(agent);
        ChatRuntime runtime = new(config);

        ChatMessage response = await runtime.InvokeAsync(
            new ChatMessage(ChatMessageRoles.User, "Say OK"));

        Assert.That(response, Is.Not.Null);
        Assert.That(response.Content ?? response.ToString(), Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region Live: Streaming

    [Test]
    public async Task Live_ChatRuntime_Streaming_ReceivesTokens()
    {
        RequireApi();

        TornadoAgent agent = new(
            client: _api!,
            model: _model,
            name: "stream-test",
            instructions: "Reply with exactly the word 'hello' and nothing else.",
            streaming: true);

        List<string> receivedTokens = [];
        SingletonRuntimeConfiguration config = new(agent);
        config.OnRuntimeEvent = evt =>
        {
            if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
            {
                if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt &&
                    streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent delta)
                {
                    if (delta.DeltaText is not null)
                        receivedTokens.Add(delta.DeltaText);
                }
            }
            return ValueTask.CompletedTask;
        };

        ChatRuntime runtime = new(config);
        ChatMessage response = await runtime.InvokeAsync(
            new ChatMessage(ChatMessageRoles.User, "Say hello"));

        Assert.That(receivedTokens, Is.Not.Empty, "Should have received streaming tokens");
        Assert.That(string.Join("", receivedTokens).Trim().ToLower(), Does.Contain("hello"));
    }

    #endregion

    #region Live: Tool Invocation

    [Test]
    public async Task Live_ChatRuntime_ToolCall_InvokesTool()
    {
        RequireApi();

        bool toolCalled = false;
        TornadoAgent agent = new(
            client: _api!,
            model: _model,
            name: "tool-test",
            instructions: "When the user asks for the time, call the get_time tool and return its result.",
            streaming: false);

        agent.AddTool(new LlmTornado.Common.Tool(
            new Func<string>(() =>
            {
                toolCalled = true;
                return "The time is 12:00 PM.";
            }),
            "get_time",
            "Returns the current time."));

        SingletonRuntimeConfiguration config = new(agent);
        ChatRuntime runtime = new(config);

        ChatMessage response = await runtime.InvokeAsync(
            new ChatMessage(ChatMessageRoles.User, "What time is it?"));

        Assert.That(toolCalled, Is.True, "Tool should have been called");
        Assert.That(response.Content ?? response.ToString(), Does.Contain("12:00").IgnoreCase);
    }

    #endregion

    #region Live: Tool Permission Required

    [Test]
    public async Task Live_ChatRuntime_ToolPermission_AutoApproved()
    {
        RequireApi();

        bool toolCalled = false;
        TornadoAgent agent = new(
            client: _api!,
            model: _model,
            name: "perm-test",
            instructions: "Call the test_tool when asked.",
            streaming: false);

        agent.AddTool(new LlmTornado.Common.Tool(
            new Func<string>(() => { toolCalled = true; return "done"; }),
            "test_tool",
            "A test tool."));
        agent.ToolPermissionRequired["test_tool"] = true;

        SingletonRuntimeConfiguration config = new(agent);
        // Auto-approve all tool permission requests
        config.OnRuntimeRequestEvent = _ => ValueTask.FromResult(true);

        ChatRuntime runtime = new(config);
        ChatMessage response = await runtime.InvokeAsync(
            new ChatMessage(ChatMessageRoles.User, "Please call test_tool"));

        Assert.That(toolCalled, Is.True);
    }

    [Test]
    public async Task Live_ChatRuntime_ToolPermission_Denied()
    {
        RequireApi();

        bool toolCalled = false;
        TornadoAgent agent = new(
            client: _api!,
            model: _model,
            name: "deny-test",
            instructions: "Call the denied_tool when asked. If the tool is denied, say 'DENIED'.",
            streaming: false);

        agent.AddTool(new LlmTornado.Common.Tool(
            new Func<string>(() => { toolCalled = true; return "done"; }),
            "denied_tool",
            "A tool that will be denied."));
        agent.ToolPermissionRequired["denied_tool"] = true;

        SingletonRuntimeConfiguration config = new(agent);
        // Deny all tool permission requests
        config.OnRuntimeRequestEvent = _ => ValueTask.FromResult(false);

        ChatRuntime runtime = new(config);
        ChatMessage response = await runtime.InvokeAsync(
            new ChatMessage(ChatMessageRoles.User, "Call denied_tool"));

        Assert.That(toolCalled, Is.False, "Tool should NOT have been called when denied");
    }

    #endregion

    #region Live: Full Agent Builder Pipeline

    [Test]
    public async Task Live_AgentBuilder_Build_And_Invoke()
    {
        RequireApi();

        string tempDir = TestHelpers.CreateTempDir();
        try
        {
            CliSettings settings = new();
            CliSkillManager skillManager = new(settings);
            skillManager.LoadSkills(tempDir);
            McpConfigLoader mcpLoader = new();
            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            // Auto-approve all
            toolApproval.PreApproveSkillTools(["load_skill", "list_skills", "read_reference"]);
            ConversationMemoryManager memoryManager = new(_api!, _model, _model.ContextTokens);

            CliAgentBuilder builder = new(_api!, _model, skillManager, mcpLoader, toolApproval, memoryManager);

            List<string> streamedTokens = [];
            Func<ChatRuntimeEvents, ValueTask> handler = evt =>
            {
                if (evt is ChatRuntimeAgentRunnerEvents runner &&
                    runner.AgentRunnerEvent is AgentRunnerStreamingEvent stream &&
                    stream.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent delta &&
                    delta.DeltaText is not null)
                {
                    streamedTokens.Add(delta.DeltaText);
                }
                return ValueTask.CompletedTask;
            };

            ChatRuntime runtime = builder.Build(handler);

            ChatMessage userMsg = new(ChatMessageRoles.User, "Reply with exactly 'OK'");
            memoryManager.AddMessage(userMsg);
            ChatMessage response = await runtime.InvokeAsync(userMsg);
            memoryManager.AddMessage(response);

            Assert.That(response, Is.Not.Null);
            Assert.That(streamedTokens, Is.Not.Empty);
            Assert.That(memoryManager.Messages.Count, Is.GreaterThanOrEqualTo(2));

            await mcpLoader.DisposeAsync();
        }
        finally
        {
            TestHelpers.CleanupTempDir(tempDir);
        }
    }

    #endregion

    #region Live: Conversation Memory — Summary (minimal)

    [Test]
    public async Task Live_MessageSummarizer_GeneratesSummary()
    {
        RequireApi();

        MessageSummarizer summarizer = new(_api!, _model);
        MessageMetadataTracker tracker = new();

        // Create a conversation that exceeds "keep" threshold
        // Need > 10 uncompressed messages for summarizeUpTo > 0 (keepCount = max(2, n/5))
        List<ChatMessage> messages = [];
        for (int i = 0; i < 12; i++)
        {
            ChatMessage msg = new(i % 2 == 0 ? ChatMessageRoles.User : ChatMessageRoles.Assistant,
                $"Message {i}: This is test content number {i}.");
            messages.Add(msg);
            tracker.Track(msg);
        }

        CompressionAnalysis analysis = new()
        {
            ShouldCompress = true,
            IsReCompression = false,
            TotalTokens = 500,
            Utilization = 0.8,
            LargeMessageIndices = [],
            UncompressedIndices = Enumerable.Range(0, 12).ToList(),
            CompressedIndices = [],
            TargetTokens = 200,
        };

        List<ChatMessage> result = await summarizer.Summarize(messages, analysis, tracker, CancellationToken.None);

        // Should have fewer messages after summarization
        Assert.That(result.Count, Is.LessThan(messages.Count));
        // Should contain a summary system message
        Assert.That(result.Any(m => m.Role == ChatMessageRoles.System && m.Content?.Contains("Summary") == true),
            Is.True, "Should contain a summary message");
    }

    #endregion

    #region Live: ProviderDetector with Real Key

    [Test]
    public void Live_ProviderDetector_DetectsOpenAI()
    {
        RequireApi();

        ProviderDetectionResult? result = ProviderDetector.Detect();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Providers.Any(p => p.Provider == LLmProviders.OpenAi), Is.True);

        DetectedProvider openAi = result.Providers.First(p => p.Provider == LLmProviders.OpenAi);
        Assert.That(openAi.Models, Is.Not.Empty);
        Assert.That(openAi.DefaultModel.Name, Does.Contain("gpt").IgnoreCase);
    }

    #endregion

    #region Live: Multi-turn Conversation

    [Test]
    public async Task Live_MultiTurn_MemoryPreserved()
    {
        RequireApi();

        TornadoAgent agent = new(
            client: _api!,
            model: _model,
            name: "memory-test",
            instructions: "Remember what the user tells you. Be very brief.",
            streaming: false);

        SingletonRuntimeConfiguration config = new(agent);
        ChatRuntime runtime = new(config);

        // Turn 1: Tell it something
        ChatMessage r1 = await runtime.InvokeAsync(
            new ChatMessage(ChatMessageRoles.User, "My favorite color is blue. Just say 'noted'."));
        Assert.That(r1.Content ?? r1.ToString(), Is.Not.Null);

        // Turn 2: Ask about it
        ChatMessage r2 = await runtime.InvokeAsync(
            new ChatMessage(ChatMessageRoles.User, "What is my favorite color? Reply with just the color."));
        string response2 = r2.Content ?? r2.ToString() ?? "";
        Assert.That(response2.ToLower(), Does.Contain("blue"));
    }

    #endregion
}
