using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Serialization and integration tests for the GA Responses API <c>computer</c> built-in tool (Mar 2026).
/// </summary>
[TestFixture]
public class ComputerToolTests
{
    private static readonly JsonSerializer Serializer = JsonSerializer.Create(EndpointBase.NullSettings);

    [Test]
    public void ResponseComputerTool_SerializesAsComputerType()
    {
        ResponseComputerTool tool = ResponseComputerTool.Default;

        JObject json = JObject.FromObject(tool, Serializer);

        Assert.That(json["type"]?.ToString(), Is.EqualTo("computer"));
        Assert.That(json.Properties().Count(), Is.EqualTo(1));
    }

    [Test]
    public void ResponseComputerUseTool_SerializesPreviewFields()
    {
        ResponseComputerUseTool tool = new ResponseComputerUseTool
        {
            DisplayWidth = 1440,
            DisplayHeight = 900,
            Environment = ResponseComputerEnvironment.Browser
        };

        JObject json = JObject.FromObject(tool, Serializer);

        Assert.That(json["type"]?.ToString(), Is.EqualTo("computer_use_preview"));
        Assert.That(json["display_width"]?.Value<int>(), Is.EqualTo(1440));
        Assert.That(json["display_height"]?.Value<int>(), Is.EqualTo(900));
        Assert.That(json["environment"]?.ToString(), Is.EqualTo("browser"));
    }

    [Test]
    public void ResponseToolConverter_DeserializesComputerAndPreviewTools()
    {
        ResponseTool gaTool = JsonConvert.DeserializeObject<ResponseTool>("{\"type\":\"computer\"}", EndpointBase.NullSettings)!;
        ResponseTool previewTool = JsonConvert.DeserializeObject<ResponseTool>(
            "{\"type\":\"computer_use_preview\",\"display_width\":1024,\"display_height\":768,\"environment\":\"mac\"}",
            EndpointBase.NullSettings)!;

        Assert.That(gaTool, Is.InstanceOf<ResponseComputerTool>());
        Assert.That(gaTool.Type, Is.EqualTo("computer"));

        Assert.That(previewTool, Is.InstanceOf<ResponseComputerUseTool>());
        ResponseComputerUseTool preview = (ResponseComputerUseTool)previewTool;
        Assert.That(preview.DisplayWidth, Is.EqualTo(1024));
        Assert.That(preview.Environment, Is.EqualTo(ResponseComputerEnvironment.Mac));
    }

    [Test]
    public void ResponseComputerToolCallItem_DeserializesBatchedActions()
    {
        const string json = """
            {
              "type": "computer_call",
              "id": "cu_001",
              "call_id": "call_001",
              "status": "completed",
              "pending_safety_checks": [],
              "actions": [
                { "type": "screenshot" },
                { "type": "click", "button": "left", "x": 120, "y": 240, "keys": ["CTRL"] },
                { "type": "type", "text": "hello" },
                { "type": "wait" }
              ]
            }
            """;

        ResponseComputerToolCallItem call = JsonConvert.DeserializeObject<ResponseComputerToolCallItem>(json, EndpointBase.NullSettings)!;

        Assert.That(call.Type, Is.EqualTo("computer_call"));
        Assert.That(call.Action, Is.Null);
        Assert.That(call.Actions, Has.Count.EqualTo(4));
        Assert.That(call.GetExecutableActions(), Has.Count.EqualTo(4));
        Assert.That(call.GetExecutableActions()[0], Is.InstanceOf<ScreenshotAction>());
        Assert.That(call.GetExecutableActions()[1], Is.InstanceOf<ClickAction>());

        ClickAction click = (ClickAction)call.GetExecutableActions()[1];
        Assert.That(click.X, Is.EqualTo(120));
        Assert.That(click.Y, Is.EqualTo(240));
        Assert.That(click.Keys, Does.Contain("CTRL"));
    }

    [Test]
    public void ResponseComputerToolCallItem_DeserializesLegacySingleAction()
    {
        const string json = """
            {
              "type": "computer_call",
              "id": "cu_002",
              "call_id": "call_002",
              "status": "completed",
              "pending_safety_checks": [],
              "action": { "type": "screenshot" }
            }
            """;

        ResponseComputerToolCallItem call = JsonConvert.DeserializeObject<ResponseComputerToolCallItem>(json, EndpointBase.NullSettings)!;

        Assert.That(call.Actions, Is.Null.Or.Empty);
        Assert.That(call.Action, Is.InstanceOf<ScreenshotAction>());
        Assert.That(call.GetExecutableActions(), Has.Count.EqualTo(1));
    }

    [Test]
    public void ComputerToolCallOutput_RoundTripsScreenshot()
    {
        ComputerToolCallOutput output = new ComputerToolCallOutput("call_123", new ComputerScreenshot
        {
            ImageUrl = "data:image/png;base64,abc123"
        })
        {
            Id = "output_123",
            Status = ResponseMessageStatuses.Completed
        };

        string json = JsonConvert.SerializeObject(output, EndpointBase.NullSettings);
        ResponseInputItem? roundTripped = JsonConvert.DeserializeObject<ResponseInputItem>(json, EndpointBase.NullSettings);

        Assert.That(roundTripped, Is.InstanceOf<ComputerToolCallOutput>());
        ComputerToolCallOutput restored = (ComputerToolCallOutput)roundTripped!;
        Assert.That(restored.CallId, Is.EqualTo("call_123"));
        Assert.That(restored.Output.ImageUrl, Is.EqualTo("data:image/png;base64,abc123"));
    }

    [Test]
    public void ResponseRequest_SerializesComputerToolInToolsArray()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY not set.");
        }

        TornadoApi api = new TornadoApi(LLmProviders.OpenAi, apiKey);
        IEndpointProvider provider = api.GetProvider(LLmProviders.OpenAi);

        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Take a screenshot using the computer tool.")
            ],
            Tools = [ResponseComputerTool.Default]
        };

        TornadoRequestContent serialized = request.Serialize(provider);
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["tools"]?[0]?["type"]?.ToString(), Is.EqualTo("computer"));
    }
}

/// <summary>
/// Live API tests for the GA computer tool. Requires OPENAI_API_KEY.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ComputerToolIntegrationTests
{
    private TornadoApi? _api;

    [SetUp]
    public void Setup()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping computer tool integration tests.");
        }

        _api = new TornadoApi(LLmProviders.OpenAi, apiKey);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt54_ComputerTool_ReturnsComputerCall()
    {
        ResponseResult result = await _api!.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            Instructions = "You control a computer through the built-in computer tool. When asked to inspect the UI, request a screenshot first.",
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Use the computer tool to take a screenshot of the current screen.")
            ],
            Tools = [ResponseComputerTool.Default],
            MaxOutputTokens = 512
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Output, Is.Not.Empty);

        ResponseComputerToolCallItem? computerCall = result.Output
            .OfType<ResponseComputerToolCallItem>()
            .FirstOrDefault();

        Assert.That(computerCall, Is.Not.Null, "Expected a computer_call output item.");
        Assert.That(computerCall!.CallId, Is.Not.Empty);

        IReadOnlyList<IComputerAction> actions = computerCall.GetExecutableActions();
        Assert.That(actions, Is.Not.Empty, "Expected at least one computer action.");

        bool hasScreenshot = actions.Any(a => a is ScreenshotAction);
        Assert.That(hasScreenshot, Is.True, "Expected the first turn to include a screenshot action.");
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task Gpt54_ComputerTool_ScreenshotLoop_AcceptsCallOutput()
    {
        ResponseResult firstTurn = await _api!.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            Instructions = "Use the computer tool for UI interaction. Request a screenshot when you need visual context.",
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Open example.com in the browser. Start by taking a screenshot.")
            ],
            Tools = [ResponseComputerTool.Default],
            MaxOutputTokens = 512
        });

        ResponseComputerToolCallItem? computerCall = firstTurn.Output
            .OfType<ResponseComputerToolCallItem>()
            .FirstOrDefault();

        Assert.That(computerCall, Is.Not.Null);

        byte[] placeholder = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        ResponseResult secondTurn = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            PreviousResponseId = firstTurn.Id,
            InputItems =
            [
                new ComputerToolCallOutput(computerCall!.CallId, new ComputerScreenshot
                {
                    ImageUrl = $"data:image/png;base64,{Convert.ToBase64String(placeholder)}"
                })
            ],
            Tools = [ResponseComputerTool.Default],
            MaxOutputTokens = 512
        });

        Assert.That(secondTurn, Is.Not.Null);
        Assert.That(secondTurn.Id, Is.Not.Empty);
        Assert.That(secondTurn.Output, Is.Not.Empty);
    }
}
