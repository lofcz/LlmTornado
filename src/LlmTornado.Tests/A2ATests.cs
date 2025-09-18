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
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Tests;

internal class A2ATests
{
    private string? _apiKey;
    private DockerDispatchService dockerService;
    private string[] _availableAgents = Array.Empty<string>();

    private static A2AClient CreateA2AClient(object result, Action<HttpRequestMessage>? onRequest = null, bool isSse = false)
    {
        var response = new JsonRpcResponse
        {
            Id = "test-id",
            Result = JsonSerializer.SerializeToNode(result)
        };

        return CreateA2AClient(response, onRequest, isSse);
    }

    private static A2AClient CreateA2AClient(JsonRpcResponse jsonResponse, Action<HttpRequestMessage>? onRequest = null, bool isSse = false)
    {
        var responseContent = JsonSerializer.Serialize(jsonResponse);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                isSse ? $"event: message\ndata: {responseContent}\n\n" : responseContent,
                Encoding.UTF8,
                isSse ? "text/event-stream" : "application/json")
        };

        var handler = new MockHttpMessageHandler(response, onRequest);

        var httpClient = new HttpClient(handler);

        return new A2AClient(new Uri("http://localhost"), httpClient);
    }
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        private readonly Action<HttpRequestMessage>? _capture;

        public MockHttpMessageHandler(HttpResponseMessage response, Action<HttpRequestMessage>? capture = null)
        {
            _response = response;
            _capture = capture;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _capture?.Invoke(request);
            return Task.FromResult(_response);
        }
    }


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
        var response = await a2aService.SendMessageAsync(createResult.Endpoint!, new List<Part> { new TextPart { Text = "Hello, how are you?" } });

        var message = (AgentTask)response;   
        // Assert
        Assert.That(message, Is.Not.Null);
        Assert.That(message.Status.Message, Is.Not.Null);
        Assert.That(message.Status.Message.Parts.Count, Is.GreaterThan(0));
        Assert.That(message.Status.Message.Parts[0], Is.TypeOf<TextPart>());
        var textPart = (TextPart)message.Status.Message.Parts[0];
        Console.WriteLine($"Response: {textPart.Text}");
        // Cleanup
        var deleteResult = await dockerService.RemoveContainerAsync(createResult.ServerId!);
        Assert.That(deleteResult, Is.True);
    }

    [Test]
    public async Task A2AClientSendStreamingMessageTest()
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
        await a2aService.SendStreamingMessageAsync(createResult.Endpoint, new List<Part> { new TextPart { Text = "Hello, how are you?" } },async (message) =>
        {
            // Assert
            if(message.Data.Kind == A2AEventKind.Message)
            {
                var msg = (AgentMessage)message.Data;
                var textPart = (TextPart)msg.Parts.FirstOrDefault(p => p is TextPart);
                Console.WriteLine($"Response: {textPart.Text}");
            }
            else if(message.Data.Kind == A2AEventKind.Task)
            {
                var task = (AgentTask)message.Data;
                Console.WriteLine($"Task Completed: {task.Status.State}");
            }
            else if(message.Data.Kind == A2AEventKind.ArtifactUpdate)
            {
                var artifact = (TaskArtifactUpdateEvent)message.Data;
                if(!artifact.Artifact.Description.Contains("Orch"))
                    Console.WriteLine($"Artifact Update: {artifact.Artifact.ArtifactId}, Parts: {artifact.Artifact.Parts.OfType<TextPart>().Last().Text}");
            }
        });

        // Cleanup
        var deleteResult = await dockerService.RemoveContainerAsync(createResult.ServerId!);
        Assert.That(deleteResult, Is.True);
    }
}
