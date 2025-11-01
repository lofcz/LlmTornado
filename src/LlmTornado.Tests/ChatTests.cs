using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Zai;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Files;

namespace LlmTornado.Tests;

[TestFixture]
public class ChatTests
{
    [SetUp]
    public async Task Setup()
    {
        await Program.SetupApi();
    }

    [Test]
    public void StreamChatEnumerable_Cancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        TornadoApi api = Program.Connect();

        CancellationTokenSource cts = new CancellationTokenSource();
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt5.V5Nano,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Tell me a long story.")],
            Stream = true,
            CancellationToken = cts.Token
        };

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            cts.CancelAfter(100);

            await foreach (ChatResult res in api.Chat.StreamChatEnumerable(request))
            {
                Console.Write(res.Choices?.FirstOrDefault()?.Delta?.Content);
            }
        });
    }

    [Test]
    public void ZaiProvider_ModelRegistration_Works()
    {
        // Arrange & Act
        var zaiModels = ChatModel.Zai;

        // Assert
        Assert.That(zaiModels, Is.Not.Null);
        Assert.That(zaiModels.Provider, Is.EqualTo(LLmProviders.Zai));
        Assert.That(zaiModels.Glm.Glm46.Name, Is.EqualTo("glm-4.6"));
        Assert.That(zaiModels.Glm.Glm45.Name, Is.EqualTo("glm-4.5"));
        Assert.That(zaiModels.Glm.Glm45V.Name, Is.EqualTo("glm-4.5v"));
    }

    [Test]
    public void ZaiProvider_VendorExtensions_Work()
    {
        // Arrange
        var extensions = new ChatRequestVendorZaiExtensions
        {
            DoSample = true,
            RequestId = "test-request-id",
            ToolStream = false
        };

        var request = new ChatRequest
        {
            Model = ChatModel.Zai.Glm.Glm46,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            User = "test-user-id",
            ReasoningEffort = ChatReasoningEfforts.Medium, // This should enable thinking
            VendorExtensions = new ChatRequestVendorExtensions(extensions)
        };

        // Act & Assert - Test that vendor extensions are properly set
        Assert.That(request.VendorExtensions, Is.Not.Null);
        Assert.That(request.VendorExtensions.Zai, Is.Not.Null);
        Assert.That(request.VendorExtensions.Zai.DoSample, Is.True);
        Assert.That(request.VendorExtensions.Zai.RequestId, Is.EqualTo("test-request-id"));
        Assert.That(request.VendorExtensions.Zai.ToolStream, Is.False);
        Assert.That(request.User, Is.EqualTo("test-user-id"));
        Assert.That(request.ReasoningEffort, Is.EqualTo(ChatReasoningEfforts.Medium));
    }

    [Test]
    public void ZaiProvider_ReasoningBudget_Works()
    {
        // Arrange
        var api = new TornadoApi("test-key");
        var request = new ChatRequest
        {
            Model = ChatModel.Zai.Glm.Glm46,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            ReasoningBudget = 1000 // This should enable thinking
        };

        // Act
        var serialized = request.Serialize(api.GetProvider(LLmProviders.Zai));

        // Assert
        Assert.That(serialized, Is.Not.Null);
        Assert.That(serialized.Body, Is.Not.Empty);
        Assert.That(serialized.Model.Name, Is.EqualTo("glm-4.6"));
    }

    [Test]
    public void ZaiProvider_Serialization_Works()
    {
        // Arrange
        var api = new TornadoApi("test-key");
        var request = new ChatRequest
        {
            Model = ChatModel.Zai.Glm.Glm46,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            Temperature = 0.7,
            MaxTokens = 100
        };

        // Act
        var serialized = request.Serialize(api.GetProvider(LLmProviders.Zai));

        // Assert
        Assert.That(serialized, Is.Not.Null);
        Assert.That(serialized.Body, Is.Not.Empty);
        Assert.That(serialized.Model.Name, Is.EqualTo("glm-4.6"));
    }

    [Test]
    public void ZaiProvider_WebSearchTool_Works()
    {
        // Arrange
        var api = new TornadoApi("test-key");
        var webSearchTool = new VendorZaiWebSearchTool
        {
            WebSearch = new VendorZaiWebSearchObject
            {
                Enable = true,
                SearchQuery = "recent AI news",
                Count = 10
            }
        };

        var request = new ChatRequest
        {
            Model = ChatModel.Zai.Glm.Glm46,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Search for recent AI news")],
            VendorExtensions = new ChatRequestVendorExtensions(
                new ChatRequestVendorZaiExtensions
                {
                    BuiltInTools = [webSearchTool]
                }
            )
        };

        // Act
        var serialized = request.Serialize(api.GetProvider(LLmProviders.Zai));

        // Assert
        Assert.That(serialized, Is.Not.Null);
        Assert.That(serialized.Body, Is.Not.Empty);
        Assert.That(serialized.Model.Name, Is.EqualTo("glm-4.6"));

        // Verify web search tool is included in serialized request
        string bodyJson = serialized.Body.ToString();
        Assert.That(bodyJson, Does.Contain("web_search"));
        Assert.That(bodyJson, Does.Contain("\"enable\":true"));
        Assert.That(bodyJson, Does.Contain("\"search_query\":\"recent AI news\""));
        Assert.That(bodyJson, Does.Contain("\"count\":10"));
    }

    [Test]
    public void ZaiProvider_FunctionTool_Works()
    {
        // Arrange
        var api = new TornadoApi("test-key");
        var functionTool = new Tool
        {
            Function = new ToolFunction("get_weather", "Get weather information")
        };

        var request = new ChatRequest
        {
            Model = ChatModel.Zai.Glm.Glm46,
            Messages = [new ChatMessage(ChatMessageRoles.User, "What's the weather like?")],
            Tools = [functionTool]
        };

        // Act
        var serialized = request.Serialize(api.GetProvider(LLmProviders.Zai));

        // Assert
        Assert.That(serialized, Is.Not.Null);
        Assert.That(serialized.Body, Is.Not.Empty);
        Assert.That(serialized.Model.Name, Is.EqualTo("glm-4.6"));

        // Verify function tool is included in serialized request
        string bodyJson = serialized.Body.ToString();
        Assert.That(bodyJson, Does.Contain("function"));
        Assert.That(bodyJson, Does.Contain("get_weather"));
    }

    [Test]
    public void ZaiProvider_FileUpload_Works()
    {
        // Arrange
        var api = new TornadoApi("test-key");
        var testFileBytes = System.Text.Encoding.UTF8.GetBytes("test file content");

        // Act
        var result = api.Files.Upload(testFileBytes, "test.txt", FilePurpose.Agent, "text/plain", LLmProviders.Zai).Result;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Name, Is.EqualTo("test.txt"));
        Assert.That(result.Data.Bytes, Is.EqualTo(testFileBytes.Length));
    }

}
