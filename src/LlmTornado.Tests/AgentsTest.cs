using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Mcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Tests;


[TestFixture]
public class AgentTests
{
    private TornadoApi? _modelProvider;
    private string? _apiKey;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }
            
    [SetUp]
    public void SetUp()
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _modelProvider = new (
                [new ProviderAuthentication(LLmProviders.OpenAi, _apiKey)]);
        }
    }

    #region Basic Agent Tests
    [Test]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

        // Act
        var agent = new TornadoAgent(_modelProvider,ChatModel.OpenAi.O4.V4Mini,instructions:"have fun");

        // Assert
        Assert.That(agent,Is.Not.Null);
        Assert.That(agent.Options.Model, Is.EqualTo(ChatModel.OpenAi.O4.V4Mini));
        Assert.That(agent.Instructions, Is.EqualTo("have fun"));
    }


    [Test]
    public void Constructor_WithOutputSchema_ShouldSetOutputFormat()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

        // Act
        var agent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, outputSchema: typeof(TestStructuredOutput));

        // Assert
        Assert.That(agent.OutputSchema, Is.EqualTo(typeof(TestStructuredOutput)));
    }

    [Test]
    public void Constructor_WithTools_ShouldSetupToolsCorrectly()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
        var tools = new List<Delegate> { TestFunction };

        // Act
        var agent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, tools: tools);

        // Assert
        Assert.That(agent.Tools, Is.Not.Null);
        Assert.That(agent.Tools, Has.Count.EqualTo(1));
        Assert.That(agent.Tools.First().Method.Name, Is.EqualTo("TestFunction"));
    }

    [Test]
    public void Constructor_WithAgentTool_ShouldSetupAgentToolsCorrectly()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
        var subAgent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, "I am a sub agent");
        var tools = new List<Delegate> { subAgent.AsTool };

        // Act
        var agent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, tools: tools);

        // Assert
        Assert.That(agent.AgentTools, Is.Not.Null);
        Assert.That(agent.AgentTools, Has.Count.EqualTo(1));
        Assert.That(agent.AgentTools.ContainsKey(subAgent.Id), Is.True);
    }

    [Test]
    public void Constructor_WithMCPServers_ShouldSetupMCPCorrectly()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
        string serverPath = Path.GetFullPath(Path.Join("..", "..", "..", "..", "LlmTornado.Mcp.Sample.Server"));
        var mcpServers = new List<MCPServer>{
        new MCPServer("test-server", serverPath)};

        // Act
        var agent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, mcpServers: mcpServers);

        // Assert
        Assert.That(agent.McpServers, Has.Count.EqualTo(1));
    }

    [Test]
    public void Constructor_WithEmptyInstructions_ShouldUseDefault()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

        // Act
        var agent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, instructions: "");

        // Assert
        Assert.That(agent.Instructions, Is.EqualTo("You are a helpful assistant"));
    }

    [Test]
    public void Constructor_WithNullInstructions_ShouldUseDefault()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");

        // Act
        var agent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, instructions: null!);

        // Assert
        Assert.That(agent.Instructions, Is.EqualTo("You are a helpful assistant"));
    }

    [Test]
    public void AsTool_ShouldReturnAgentTool()
    {
        // Arrange
        if (_modelProvider == null) Assert.Ignore("Model provider not initialized");
        var agent = new TornadoAgent(_modelProvider, ChatModel.OpenAi.O4.V4Mini, "Test instructions");

        // Act
        var agentTool = agent.AsTool();

        // Assert
        Assert.That(agentTool, Is.Not.Null);
        Assert.That(agentTool.ToolAgent, Is.TypeOf(typeof(TornadoAgent)));
        Assert.That(agentTool.Tool.Function.Name, Is.EqualTo(agent.Id));
    }

    // Helper method for testing
    [Description("A test function for testing purposes")]
    public static string TestFunction(string input)
    {
        return $"Processed: {input}";
    }
}

// Test structured output class
[System.ComponentModel.Description("Test structured output for testing")]
public class TestStructuredOutput
{
    [System.ComponentModel.Description("Test property")]
    public string TestProperty { get; set; } = string.Empty;

    [System.ComponentModel.Description("Test number")]
    public int TestNumber { get; set; }
}

#endregion
