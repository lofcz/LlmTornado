using LlmTornado.A2A.Hosting;
using LlmTornado.Code;
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
    private DockerDispatchService dockerService = new DockerDispatchService();
    private string[] _availableAgents = Array.Empty<string>();
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        dockerService = new DockerDispatchService();
        _availableAgents = dockerService.GetAvailableAgents();
        Assert.That(_availableAgents, Is.Not.Empty);
    }

    [SetUp]
    public void SetUp()
    {
        
    }

    [Test]
    public async Task CreateContainerTest()
    {
        // Act
        var result = await dockerService.CreateContainerAsync(_availableAgents[0], $"OPENAI_API_KEY={_apiKey}");

        // Assert
        Assert.That(result.Success);
        Assert.That(result.ContainerId, Is.Not.Null);
        Assert.That(result.Endpoint, Is.Not.Null);

        Console.WriteLine($"Container ID: {result.ContainerId}, Endpoint: {result.Endpoint}");
    }

    [Test]
    public async Task DeleteContainerTest()
    {
        // Arrange
        var dockerService = new DockerDispatchService();

        var createResult = await dockerService.CreateContainerAsync(_availableAgents[0], $"OPENAI_API_KEY={_apiKey}");
        // Assert
        Assert.That(createResult.Success);
        Assert.That(createResult.ContainerId, Is.Not.Null);
        Assert.That(createResult.Endpoint, Is.Not.Null);

        Console.WriteLine($"Container ID: {createResult.ContainerId}, Endpoint: {createResult.Endpoint}");
        // Act
        var deleteResult = await dockerService.RemoveContainerAsync(createResult.ContainerId);

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
        Assert.That(createResult.ContainerId, Is.Not.Null);
        Assert.That(createResult.Endpoint, Is.Not.Null);
        Console.WriteLine($"Container ID: {createResult.ContainerId}, Endpoint: {createResult.Endpoint}");
        // Act
        var statusResult = await dockerService.GetContainerStatusAsync(createResult.ContainerId);

        // Assert
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.ContainerId, Is.EqualTo(createResult.ContainerId));

        Console.WriteLine($"Container Status: {statusResult.Status}");
    }

    [Test]
    public async Task GetActiveContainers()
    {
        var containers = dockerService.GetActiveContainers();
        Assert.That(containers, Is.Not.Null);
        Console.WriteLine($"Active Containers: {string.Join("\n",containers)}");
    }
}
