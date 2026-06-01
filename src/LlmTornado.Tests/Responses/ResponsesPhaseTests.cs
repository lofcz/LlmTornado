using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Demo;
using LlmTornado.Responses;
using LlmTornado.Responses.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests.Responses;

[TestFixture]
public class ResponsesPhaseTests
{
    private static bool ApiConfigured;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        ApiConfigured = await Program.SetupApi();
    }

    [Test]
    public void DeserializeOutputMessage_ParsesCommentaryAndFinalAnswer()
    {
        const string commentaryJson = """
            {
              "type": "message",
              "role": "assistant",
              "phase": "commentary",
              "content": [{ "type": "output_text", "text": "Checking that now." }]
            }
            """;

        const string finalAnswerJson = """
            {
              "type": "message",
              "role": "assistant",
              "phase": "final_answer",
              "content": [{ "type": "output_text", "text": "Paris." }]
            }
            """;

        ResponseInputItem commentary = JsonConvert.DeserializeObject<ResponseInputItem>(commentaryJson)!;
        ResponseInputItem finalAnswer = JsonConvert.DeserializeObject<ResponseInputItem>(finalAnswerJson)!;

        Assert.That(commentary, Is.TypeOf<OutputMessageInput>());
        Assert.That(finalAnswer, Is.TypeOf<OutputMessageInput>());
        Assert.That(((OutputMessageInput)commentary).Phase, Is.EqualTo(ResponsePhases.Commentary));
        Assert.That(((OutputMessageInput)finalAnswer).Phase, Is.EqualTo(ResponsePhases.FinalAnswer));
    }

    [Test]
    public void DeserializeResponseOutput_ParsesPhaseOnMessageItem()
    {
        const string json = """
            {
              "type": "message",
              "id": "msg_test",
              "role": "assistant",
              "status": "completed",
              "phase": "final_answer",
              "content": [{ "type": "output_text", "text": "done", "annotations": [] }]
            }
            """;

        ResponseOutputMessageItem item = JsonConvert.DeserializeObject<ResponseOutputMessageItem>(json)!;

        Assert.That(item.Phase, Is.EqualTo(ResponsePhases.FinalAnswer));
    }

    [Test]
    public void SerializeOutputMessageInput_EmitsSnakeCasePhase()
    {
        OutputMessageInput message = new OutputMessageInput
        {
            Id = "msg_1",
            Phase = ResponsePhases.Commentary,
            Content =
            [
                new ResponseOutputTextContent { Text = "Working on it." }
            ]
        };

        JObject jo = JObject.Parse(JsonConvert.SerializeObject(message, Formatting.None));

        Assert.That(jo["phase"]?.ToString(), Is.EqualTo("commentary"));
    }

    [Test]
    public void ToOutputMessageInput_PreservesPhase()
    {
        ResponseOutputMessageItem output = new ResponseOutputMessageItem
        {
            Id = "msg_abc",
            Phase = ResponsePhases.FinalAnswer,
            Status = ResponseOutputItemStatus.Completed,
            Content = [new ResponseOutputTextContent { Text = "42" }]
        };

        OutputMessageInput input = ResponseHelpers.ToOutputMessageInput(output);

        Assert.That(input.Phase, Is.EqualTo(ResponsePhases.FinalAnswer));
        Assert.That(input.Id, Is.EqualTo("msg_abc"));
    }

    [Test]
    public async Task CreateResponse_AssistantMessagesIncludePhase()
    {
        if (!ApiConfigured)
        {
            Assert.Inconclusive("apiKey.json not configured");
            return;
        }

        TornadoApi api = Program.Connect();
        ResponseResult result = await api.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.Gpt53Codex,
            Reasoning = new ReasoningConfiguration(ResponseReasoningEfforts.Low),
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Reply with exactly: PHASE_OK")
            ],
            MaxOutputTokens = 256
        });

        List<ResponseOutputMessageItem> messages = result.Output
            .OfType<ResponseOutputMessageItem>()
            .ToList();

        Assert.That(messages, Is.Not.Empty, "Expected at least one assistant output message");
        Assert.That(messages.Any(m => m.Phase is not null),
            Is.True,
            $"Expected phase on assistant messages. Output types: {string.Join(", ", result.Output?.Select(o => o.Type) ?? [])}");
        Assert.That(messages.Any(m => m.Phase == ResponsePhases.FinalAnswer),
            Is.True,
            "Expected at least one final_answer phase on a simple completion");
    }

    [Test]
    public async Task ManualReplay_PreservesPhaseOnFollowUpRequest()
    {
        if (!ApiConfigured)
        {
            Assert.Inconclusive("apiKey.json not configured");
            return;
        }

        TornadoApi api = Program.Connect();
        ResponseResult first = await api.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.Gpt53Codex,
            Reasoning = new ReasoningConfiguration(ResponseReasoningEfforts.Low),
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Say hello in one short sentence.")
            ],
            MaxOutputTokens = 256
        });

        List<OutputMessageInput> replayMessages = first.Output
            .OfType<ResponseOutputMessageItem>()
            .Select(ResponseHelpers.ToOutputMessageInput)
            .ToList();

        Assert.That(replayMessages, Is.Not.Empty);
        Assert.That(replayMessages.All(m => m.Phase is not null), Is.True);

        ResponseResult second = await api.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.Gpt53Codex,
            Reasoning = new ReasoningConfiguration(ResponseReasoningEfforts.Low),
            InputItems =
            [
                ..replayMessages,
                new ResponseInputMessage(ChatMessageRoles.User, "Now say goodbye in one short sentence.")
            ],
            MaxOutputTokens = 256
        });

        Assert.That(second.Output.OfType<ResponseOutputMessageItem>().Any(m => m.Phase is not null), Is.True);
    }

    [Test]
    public async Task StreamOutputItemAdded_IncludesPhaseOnMessage()
    {
        if (!ApiConfigured)
        {
            Assert.Inconclusive("apiKey.json not configured");
            return;
        }

        List<ResponsePhases?> streamedPhases = [];

        await Program.Connect().Responses.StreamResponseRich(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.Gpt53Codex,
            Reasoning = new ReasoningConfiguration(ResponseReasoningEfforts.Low),
            Stream = true,
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "Reply with one word: OK")
            ],
            MaxOutputTokens = 256
        }, new ResponseStreamEventHandler
        {
            OnResponseOutputItemAdded = evt =>
            {
                if (evt.Item is ResponseOutputMessageItem msg && msg.Phase is not null)
                {
                    streamedPhases.Add(msg.Phase);
                }

                return ValueTask.CompletedTask;
            }
        });

        Assert.That(streamedPhases, Is.Not.Empty, "Expected phase on streamed response.output_item.added message events");
    }
}
