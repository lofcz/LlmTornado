using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Mcp;
using LlmTornado.Cli.Memory;
using LlmTornado.Cli.Agents;
using LlmTornado.Cli.Skills;
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
        CliSettings settings = new();
        CliSkillManager skillManager = new(settings);
        skillManager.LoadSkills(_tempDir); // Empty dir → no skills
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings);

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager);
        LlmTornado.Agents.ChatRuntime.ChatRuntime runtime = builder.Build();

        Assert.That(builder.Agent, Is.Not.Null);
        Assert.That(builder.Runtime, Is.Not.Null);
        Assert.That(builder.ActiveModel, Is.EqualTo(model));
        Assert.That(runtime, Is.Not.Null);

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task Build_Includes_BuiltIn_Tools()
    {
        string? key = TestHelpers.GetOpenAiKey();
        if (key is null) { Assert.Ignore("OPENAI_API_KEY not set."); return; }

        TornadoApi api = new(key);
        ChatModel model = TestHelpers.CheapModel;
        CliSettings settings = new();
        CliSkillManager skillManager = new(settings);
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings);

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager);
        builder.Build();

        // ToolList should contain load_skill, list_skills, read_reference
        var toolNames = builder.Agent.ToolList?.Select(t => t.Key).ToList() ?? [];
        Assert.That(toolNames, Does.Contain("load_skill"));
        Assert.That(toolNames, Does.Contain("list_skills"));
        Assert.That(toolNames, Does.Contain("read_reference"));

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
        CliSettings settings = new();
        CliSkillManager skillManager = new(settings);
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings);

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager);
        builder.Build();

        var toolNames = builder.Agent.ToolList?.Select(t => t.Key).ToList() ?? [];
        Assert.That(toolNames, Does.Contain("test-skill:hello"));

        await mcpLoader.DisposeAsync();
    }

    [Test]
    public async Task SetModel_Changes_ActiveModel()
    {
        string? key = TestHelpers.GetOpenAiKey();
        if (key is null) { Assert.Ignore("OPENAI_API_KEY not set."); return; }

        TornadoApi api = new(key);
        ChatModel initialModel = ChatModel.OpenAi.Gpt41.V41Nano;
        CliSettings settings = new();
        CliSkillManager skillManager = new(settings);
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, initialModel, initialModel.ContextTokens);
        AgentDefinitionManager agentManager = new(settings);

        CliAgentBuilder builder = new(api, initialModel, skillManager, mcpLoader, toolApproval, memoryManager, agentManager);
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
        CliSettings settings = new();
        CliSkillManager skillManager = new(settings);
        skillManager.LoadSkills(_tempDir);
        McpConfigLoader mcpLoader = new();
        ConsoleRenderer renderer = new();
        ToolApprovalManager toolApproval = new(renderer);
        ConversationMemoryManager memoryManager = new(api, model, model.ContextTokens);
        AgentDefinitionManager agentManager = new(settings);

        CliAgentBuilder builder = new(api, model, skillManager, mcpLoader, toolApproval, memoryManager, agentManager);

        Assert.Throws<InvalidOperationException>(() => _ = builder.Agent);
        Assert.Throws<InvalidOperationException>(() => _ = builder.Runtime);
    }

    #endregion
}
