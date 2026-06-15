using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Responses;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Integration tests for OpenAI Responses API compaction (server-side and /responses/compact).
/// </summary>
[TestFixture]
[Category("Integration")]
public class ResponsesCompactionIntegrationTests
{
    private TornadoApi _api = null!;
    private ChatModel _model = null!;

    [SetUp]
    public async Task Setup()
    {
        if (!await Program.SetupApi())
        {
            Assert.Ignore("apiKey.json not configured. Skipping Responses compaction integration tests.");
        }

        _api = Program.Connect();
        _model = ChatModel.OpenAi.Gpt5.V5Mini;
    }

    [Test]
    public void ContextManagement_SerializesCorrectly()
    {
        ResponseRequest request = new ResponseRequest(_model, "hello")
            .WithServerSideCompaction(50_000);

        string json = request.ToJson();
        JObject body = JObject.Parse(json);

        Assert.That(body["context_management"], Is.Not.Null);
        JArray contextManagement = (JArray)body["context_management"]!;
        Assert.That(contextManagement, Has.Count.EqualTo(1));
        Assert.That(contextManagement[0]!["type"]!.ToString(), Is.EqualTo("compaction"));
        Assert.That(contextManagement[0]!["compact_threshold"]!.Value<int>(), Is.EqualTo(50_000));
    }

    [Test]
    public void CompactionOutputItem_RoundTripsToInputItem()
    {
        ResponseCompactionOutputItem output = new ResponseCompactionOutputItem
        {
            Id = "cmp_123",
            EncryptedContent = "encrypted-summary-payload"
        };

        CompactionInputItem input = output.ToInputItem();

        Assert.That(input.Type, Is.EqualTo("compaction"));
        Assert.That(input.Id, Is.EqualTo("cmp_123"));
        Assert.That(input.EncryptedContent, Is.EqualTo("encrypted-summary-payload"));
    }

    [Test]
    [Explicit("Requires OpenAI API key and makes real API calls")]
    public async Task CompactEndpoint_ReturnsCompactionItem()
    {
        List<ResponseInputItem> conversation =
        [
            new ResponseInputMessage(ChatMessageRoles.User, "Let's begin a long coding task."),
            new ResponseInputMessage(ChatMessageRoles.User, BuildLargeContext(8_000))
        ];

        ResponseResult first = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = _model,
            InputItems = conversation,
            Store = false,
            MaxOutputTokens = 256
        });

        Assert.That(first.Output, Is.Not.Null.And.Not.Empty);

        conversation.AddRange(first.Output.ToInputItems());
        conversation.Add(new ResponseInputMessage(ChatMessageRoles.User, "Summarize the key themes in one sentence."));

        ResponseCompactResult compacted = await _api.Responses.CompactResponse(new ResponseCompactRequest(_model, conversation));

        Assert.That(compacted.Object, Is.EqualTo("response.compaction"));
        Assert.That(compacted.Output, Is.Not.Null.And.Not.Empty);

        ResponseCompactionOutputItem? compaction = compacted.GetLatestCompaction();
        Assert.That(compaction, Is.Not.Null);
        Assert.That(compaction!.EncryptedContent, Is.Not.Empty);
        Assert.That(compacted.Usage?.TotalTokens, Is.GreaterThan(0));

        List<ResponseInputItem> nextInput = compacted.Output.ToInputItems();
        nextInput.Add(new ResponseInputMessage(ChatMessageRoles.User, "What was the original task about?"));

        ResponseResult followUp = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = _model,
            InputItems = nextInput,
            Store = false,
            MaxOutputTokens = 256
        });

        Assert.That(followUp.OutputText, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Explicit("Requires OpenAI API key and makes real API calls")]
    public async Task ServerSideCompaction_AcceptsContextManagement()
    {
        List<ResponseInputItem> conversation =
        [
            new ResponseInputMessage(ChatMessageRoles.User, "Track a multi-step refactor for a billing service."),
            new ResponseInputMessage(ChatMessageRoles.User, BuildLargeContext(12_000))
        ];

        ResponseResult response = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = _model,
            InputItems = conversation,
            Store = false,
            MaxOutputTokens = 256,
            ContextManagement = [ResponseContextManagementItem.Compaction(1_000)]
        });

        Assert.That(response.Status, Is.EqualTo(ResponseMessageStatuses.Completed).Or.EqualTo(ResponseMessageStatuses.Incomplete));
        Assert.That(response.Output, Is.Not.Null.And.Not.Empty);
        Assert.That(response.OutputText, Is.Not.Null.And.Not.Empty);

        ResponseCompactionOutputItem? compaction = response.GetLatestCompaction();
        if (compaction is not null)
        {
            Assert.That(compaction.EncryptedContent, Is.Not.Empty);

            List<ResponseInputItem> pruned = conversation.ToList();
            pruned.AddRange(response.Output.ToInputItems());
            pruned = pruned.PruneBeforeLatestCompaction();

            Assert.That(pruned.Any(x => x is CompactionInputItem), Is.True);
            Assert.That(pruned.Count, Is.LessThan(conversation.Count + response.Output!.Count));
        }
    }

    [Test]
    [Explicit("Requires OpenAI API key and makes real API calls")]
    public async Task PreviousResponseIdChaining_WithContextManagement()
    {
        ResponseResult first = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = _model,
            InputString = "Remember the codename 'northstar' for this project.",
            Store = false,
            MaxOutputTokens = 128,
            ContextManagement = [ResponseContextManagementItem.Compaction(200_000)]
        });

        Assert.That(first.Id, Is.Not.Empty);

        ResponseResult second = await _api.Responses.CreateResponse(new ResponseRequest
        {
            Model = _model,
            InputString = "What codename did I give you?",
            PreviousResponseId = first.Id,
            Store = false,
            MaxOutputTokens = 128,
            ContextManagement = [ResponseContextManagementItem.Compaction(200_000)]
        });

        Assert.That(second.OutputText, Does.Contain("northstar").IgnoreCase);
    }

    private static string BuildLargeContext(int targetChars)
    {
        string seed = "Context line about API design, billing flows, and migration steps. ";
        return string.Concat(Enumerable.Repeat(seed, (targetChars / seed.Length) + 1)).Substring(0, targetChars);
    }
}
