using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class CliAgentBuilderTests
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

    #region Build

    [Test]
    public async Task Build_Creates_Agent_And_Runtime()
    {
        string? key = TestHelpers.GetOpenAiKey();
        if (key is null) { Assert.Ignore("OPENAI_API_KEY not set."); return; }

        TornadoApi api = new(key);
        ChatModel model = TestHelpers.CheapModel;
        AgentSettings settings = new();
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir); // Empty dir → no skills
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager, settings, null);
        LlmTornado.Agents.ChatRuntime.ChatRuntime runtime = builder.Build();

        Assert.That(builder.Agent, Is.Not.Null);
        Assert.That(builder.Runtime, Is.Not.Null);
        Assert.That(builder.ActiveModel, Is.EqualTo(model));
        Assert.That(runtime, Is.Not.Null);

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task Build_SystemPromptIsStable_EnvMessageCarriesCwd()
    {
        // No API key needed: nothing here touches the network.
        TornadoApi api = new(new Uri("http://127.0.0.1:9"), "unused");
        ChatModel model = new("local-model", LLmProviders.Custom);
        AgentSettings settings = new();
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager, settings, null);
        builder.Build();

        // KV-cache invariant: the system prompt must not embed the (volatile) working directory —
        // that lives in the pinned <env> message at position 0 of the conversation.
        string cwd = builder.WorkingDirectory;
        Assert.That(builder.Agent.Instructions, Does.Not.Contain(cwd));

        List<ChatMessage> messages = builder.ConversationConfig!.GetMessages();
        Assert.That(messages, Is.Not.Empty);
        Assert.That(messages[0].Content, Does.StartWith("<env>"));
        Assert.That(messages[0].Content, Does.Contain(cwd));

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task Build_Includes_BuiltIn_Tools()
    {
        string? key = TestHelpers.GetOpenAiKey();
        if (key is null) { Assert.Ignore("OPENAI_API_KEY not set."); return; }

        TornadoApi api = new(key);
        ChatModel model = TestHelpers.CheapModel;
        AgentSettings settings = new();
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager, settings, null);
        builder.Build();

        // ToolList should contain the built-in management + tool-discovery tools
        var toolNames = builder.Agent.ToolList?.Select(t => t.Key).ToList() ?? [];
        Assert.That(toolNames, Does.Contain("load_skill"));
        Assert.That(toolNames, Does.Contain("list_skills"));
        Assert.That(toolNames, Does.Contain("read_reference"));
        Assert.That(toolNames, Does.Contain("list_all_tools"));
        Assert.That(toolNames, Does.Contain("select_tools"));

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task Build_With_Skills_Includes_ScriptTools()
    {
        string? key = TestHelpers.GetOpenAiKey();
        if (key is null) { Assert.Ignore("OPENAI_API_KEY not set."); return; }

        // Create a skill with a script
        string skillDir = Path.Combine(_tempDir, "test-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), @"---
name: test-skill
description: Test
---
Instructions.");

        string scriptsDir = Path.Combine(skillDir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "hello.py"), "print('hello')");

        TornadoApi api = new(key);
        ChatModel model = TestHelpers.CheapModel;
        AgentSettings settings = new();
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager, settings, null);
        builder.Build();

        var toolNames = builder.Agent.ToolList?.Select(t => t.Key).ToList() ?? [];
        Assert.That(toolNames, Does.Contain("test-skill__hello"));

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task SetModel_Changes_ActiveModel()
    {
        string? key = TestHelpers.GetOpenAiKey();
        if (key is null) { Assert.Ignore("OPENAI_API_KEY not set."); return; }

        TornadoApi api = new(key);
        ChatModel initialModel = ChatModel.OpenAi.Gpt41.V41Nano;
        AgentSettings settings = new();
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, initialModel, initialModel.ContextTokens);
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());

        CliAgentBuilder builder = new(api, initialModel, skillManager, mcpLoader, toolApproval, memoryManager, agentManager, settings, null);
        builder.Build();

        ChatModel newModel = ChatModel.OpenAi.Gpt41.V41Mini;
        builder.SetModel(newModel);

        Assert.That(builder.ActiveModel.Name, Is.EqualTo(newModel.Name));

        await mcpLoader.DisposeAsync();
    }

    #endregion

    #region Agent/Runtime Access Before Build

    [Test]
    public void Agent_Throws_Before_Build()
    {
        string? key = TestHelpers.GetOpenAiKey();
        if (key is null) { Assert.Ignore("OPENAI_API_KEY not set."); return; }

        TornadoApi api = new(key);
        ChatModel model = TestHelpers.CheapModel;
        AgentSettings settings = new();
        SkillManager skillManager = new(settings, new NoOpPersistence());
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings, new NoOpPersistence());

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager, settings, null);

        Assert.Throws<InvalidOperationException>(() => _ = builder.Agent);
        Assert.Throws<InvalidOperationException>(() => _ = builder.Runtime);
    }

    #endregion
}
