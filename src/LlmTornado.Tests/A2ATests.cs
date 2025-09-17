using A2A;
using LlmTornado.A2A.Hosting;
using LlmTornado.A2A.Hosting.Services;
using LlmTornado.Code;
using LlmTornado.Demo.ExampleAgents.ChatBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Testing.Platform.Extensions.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Tests;

internal class A2ATests
{
    private string? _apiKey;
    private DockerDispatchService dockerService;
    private string[] _availableAgents = Array.Empty<string>();
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        dockerService = new DockerDispatchService(configuration);
        _availableAgents = dockerService.GetAvailableAgents();
        Assert.That(_availableAgents, Is.Not.Empty);
    }

    [SetUp]
    public void SetUp()
    {
        
    }

    #region Docker Service Tests
    [Test]
    public async Task CreateContainerTest()
    {
        // Act
        var result = await dockerService.CreateContainerAsync(_availableAgents[0], $"OPENAI_API_KEY={_apiKey}");

        // Assert
        Assert.That(result.Success);
        Assert.That(result.ServerId, Is.Not.Null);
        Assert.That(result.Endpoint, Is.Not.Null);

        Console.WriteLine($"Server ID: {result.ServerId}, Endpoint: {result.Endpoint}");
    }

    [Test]
    public async Task DeleteContainerTest()
    {
        var createResult = await dockerService.CreateContainerAsync(_availableAgents[0], $"OPENAI_API_KEY={_apiKey}");
        // Assert
        Assert.That(createResult.Success);
        Assert.That(createResult.ServerId, Is.Not.Null);
        Assert.That(createResult.Endpoint, Is.Not.Null);

        Console.WriteLine($"Server ID: {createResult.ServerId}, Endpoint: {createResult.Endpoint}");
        // Act
        var deleteResult = await dockerService.RemoveContainerAsync(createResult.ServerId);

        // Assert
        Assert.That(deleteResult, Is.True);
    }

    [Test]
    public async Task GetContainerStatusTest()
    {
        // Arrange
        var createResult = await dockerService.CreateContainerAsync(_availableAgents[0], $"OPENAI_API_KEY={_apiKey}");
        // Assert
        Assert.That(createResult.Success);
        Assert.That(createResult.ServerId, Is.Not.Null);
        Assert.That(createResult.Endpoint, Is.Not.Null);
        Console.WriteLine($"Server ID: {createResult.ServerId}, Endpoint: {createResult.Endpoint}");
        // Act
        var statusResult = await dockerService.GetContainerStatusAsync(createResult.ServerId);

        // Assert
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.ServerId, Is.EqualTo(createResult.ServerId));

        Console.WriteLine($"Server Status: {statusResult.Status}");
    }

    [Test]
    public async Task GetActiveContainers()
    {
        var containers = dockerService.GetActiveContainers();
        Assert.That(containers, Is.Not.Null);
        Console.WriteLine($"Active Containers: {string.Join("\n",containers)}");
    }
    #endregion

    [Test]
    public async Task A2AClientSendMessageTest()
    {
        // Arrange
        var createResult = await dockerService.CreateContainerAsync(_availableAgents[0], $"OPENAI_API_KEY={_apiKey}");

        // Assert
        Assert.That(createResult.Success);
        Assert.That(createResult.ServerId, Is.Not.Null);
        Assert.That(createResult.Endpoint, Is.Not.Null);
        Console.WriteLine($"Server ID: {createResult.ServerId}, Endpoint: {createResult.Endpoint}");
        var a2aService = new A2AContainerService();
        // Act
        var message = await a2aService.SendMessageAsync(createResult.Endpoint!, new List<Part> { new TextPart { Text = "Hello, how are you?" } });
        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.Parts, Is.Not.Null);
        Assert.That(message.Parts.Count, Is.GreaterThan(0));
        Assert.That(message.Parts[0], Is.TypeOf<TextPart>());
        var textPart = (TextPart)message.Parts[0];
        Console.WriteLine($"Response: {textPart.Text}");
        // Cleanup
        var deleteResult = await dockerService.RemoveContainerAsync(createResult.ServerId!);
        Assert.That(deleteResult, Is.True);
    }
}
