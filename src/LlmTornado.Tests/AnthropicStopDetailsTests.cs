using System.IO;
using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using Newtonsoft.Json;

namespace LlmTornado.Tests;

[TestFixture]
public class AnthropicStopDetailsTests
{
    private const string RefusalWithCyberCategoryJson = """
        {
          "id": "msg_01234",
          "type": "message",
          "role": "assistant",
          "content": [
            {
              "type": "text",
              "text": "I can't help with that request."
            }
          ],
          "model": "claude-opus-4-8",
          "stop_reason": "refusal",
          "stop_sequence": null,
          "stop_details": {
            "type": "refusal",
            "category": "cyber",
            "explanation": "This request appears to involve unauthorized access techniques."
          },
          "usage": {
            "input_tokens": 42,
            "output_tokens": 12
          }
        }
        """;

    private const string RefusalWithoutCategoryJson = """
        {
          "id": "msg_56789",
          "type": "message",
          "role": "assistant",
          "content": [
            {
              "type": "text",
              "text": "I can't help with that request."
            }
          ],
          "model": "claude-opus-4-8",
          "stop_reason": "refusal",
          "stop_sequence": null,
          "stop_details": {
            "type": "refusal",
            "category": null,
            "explanation": null
          },
          "usage": {
            "input_tokens": 10,
            "output_tokens": 5
          }
        }
        """;

    private const string EndTurnWithoutStopDetailsJson = """
        {
          "id": "msg_99999",
          "type": "message",
          "role": "assistant",
          "content": [
            {
              "type": "text",
              "text": "Hello!"
            }
          ],
          "model": "claude-opus-4-8",
          "stop_reason": "end_turn",
          "stop_sequence": null,
          "usage": {
            "input_tokens": 8,
            "output_tokens": 4
          }
        }
        """;

    [Test]
    public void VendorAnthropicChatResult_DeserializesStopDetailsWithCyberCategory()
    {
        VendorAnthropicChatResult? result = JsonConvert.DeserializeObject<VendorAnthropicChatResult>(RefusalWithCyberCategoryJson);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StopReason, Is.EqualTo("refusal"));
        Assert.That(result.StopDetails, Is.Not.Null);
        Assert.That(result.StopDetails!.Type, Is.EqualTo("refusal"));
        Assert.That(result.StopDetails.Category, Is.EqualTo("cyber"));
        Assert.That(result.StopDetails.Explanation, Is.EqualTo("This request appears to involve unauthorized access techniques."));
    }

    [Test]
    public void VendorAnthropicStopDetails_ToChatStopDetails_MapsCyberCategory()
    {
        VendorAnthropicStopDetails details = new VendorAnthropicStopDetails
        {
            Type = "refusal",
            Category = "cyber",
            Explanation = "Cyber policy triggered."
        };

        ChatStopDetails? mapped = details.ToChatStopDetails();

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped!.Type, Is.EqualTo("refusal"));
        Assert.That(mapped.Category, Is.EqualTo(ChatRefusalCategory.Cyber));
        Assert.That(mapped.Explanation, Is.EqualTo("Cyber policy triggered."));
    }

    [Test]
    public void VendorAnthropicStopDetails_ToChatStopDetails_MapsBioCategory()
    {
        VendorAnthropicStopDetails details = new VendorAnthropicStopDetails
        {
            Type = "refusal",
            Category = "bio",
            Explanation = "Biological policy triggered."
        };

        ChatStopDetails? mapped = details.ToChatStopDetails();

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped!.Category, Is.EqualTo(ChatRefusalCategory.Bio));
    }

    [Test]
    public void VendorAnthropicStopDetails_ToChatStopDetails_AllowsNullCategory()
    {
        VendorAnthropicStopDetails details = new VendorAnthropicStopDetails
        {
            Type = "refusal",
            Category = null,
            Explanation = null
        };

        ChatStopDetails? mapped = details.ToChatStopDetails();

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped!.Type, Is.EqualTo("refusal"));
        Assert.That(mapped.Category, Is.Null);
        Assert.That(mapped.Explanation, Is.Null);
    }

    [Test]
    public void VendorAnthropicChatResult_ToChatResult_PropagatesStopDetailsToChoices()
    {
        VendorAnthropicChatResult? result = JsonConvert.DeserializeObject<VendorAnthropicChatResult>(RefusalWithCyberCategoryJson);
        Assert.That(result, Is.Not.Null);

        ChatResult chatResult = result!.ToChatResult(null, null);

        Assert.That(chatResult.Choices, Is.Not.Null);
        Assert.That(chatResult.Choices, Has.Count.GreaterThan(0));
        Assert.That(chatResult.Choices![0].FinishReason, Is.EqualTo(ChatMessageFinishReasons.Refusal));
        Assert.That(chatResult.Choices[0].StopDetails, Is.Not.Null);
        Assert.That(chatResult.Choices[0].StopDetails!.Category, Is.EqualTo(ChatRefusalCategory.Cyber));
        Assert.That(chatResult.Choices[0].StopDetails.Explanation, Is.EqualTo("This request appears to involve unauthorized access techniques."));
    }

    [Test]
    public void VendorAnthropicChatResult_ToChatResult_LeavesStopDetailsNullForNonRefusal()
    {
        VendorAnthropicChatResult? result = JsonConvert.DeserializeObject<VendorAnthropicChatResult>(EndTurnWithoutStopDetailsJson);
        Assert.That(result, Is.Not.Null);

        ChatResult chatResult = result!.ToChatResult(null, null);

        Assert.That(chatResult.Choices, Is.Not.Null);
        Assert.That(chatResult.Choices![0].FinishReason, Is.EqualTo(ChatMessageFinishReasons.EndTurn));
        Assert.That(chatResult.Choices[0].StopDetails, Is.Null);
    }

    [Test]
    public void VendorAnthropicChatResult_ToChatResult_AllowsNullRefusalCategory()
    {
        VendorAnthropicChatResult? result = JsonConvert.DeserializeObject<VendorAnthropicChatResult>(RefusalWithoutCategoryJson);
        Assert.That(result, Is.Not.Null);

        ChatResult chatResult = result!.ToChatResult(null, null);

        Assert.That(chatResult.Choices![0].StopDetails, Is.Not.Null);
        Assert.That(chatResult.Choices[0].StopDetails!.Category, Is.Null);
        Assert.That(chatResult.Choices[0].StopDetails.Explanation, Is.Null);
    }

    [Test]
    public void ChatResult_Deserialize_AnthropicProvider_ParsesStopDetails()
    {
        ChatResult? chatResult = ChatResult.Deserialize(LLmProviders.Anthropic, RefusalWithCyberCategoryJson, null, null);

        Assert.That(chatResult, Is.Not.Null);
        Assert.That(chatResult!.Choices![0].StopDetails, Is.Not.Null);
        Assert.That(chatResult.Choices[0].StopDetails!.Category, Is.EqualTo(ChatRefusalCategory.Cyber));
    }

    [Test]
    public void AnthropicStreamMsgDelta_DeserializesStopDetails()
    {
        const string json = """
            {
              "type": "message_delta",
              "delta": {
                "stop_reason": "refusal",
                "stop_details": {
                  "type": "refusal",
                  "category": "bio",
                  "explanation": "Biological content policy."
                }
              },
              "usage": {
                "output_tokens": 7
              }
            }
            """;

        dynamic? parsed = JsonConvert.DeserializeObject<dynamic>(json);
        Assert.That(parsed, Is.Not.Null);

        string deltaJson = parsed!.delta.ToString();
        VendorAnthropicStopDetails? stopDetails = JsonConvert.DeserializeObject<VendorAnthropicStopDetails>(
            parsed.delta.stop_details.ToString());

        Assert.That(stopDetails, Is.Not.Null);
        Assert.That(stopDetails!.Category, Is.EqualTo("bio"));
        Assert.That(stopDetails.ToChatStopDetails()!.Category, Is.EqualTo(ChatRefusalCategory.Bio));
    }

    [Test]
    public async Task InboundStream_MapsRefusalStopReasonFromMessageDelta()
    {
        const string sse = """
            event: message_start
            data: {"type":"message_start","message":{"id":"msg_stream_refusal","type":"message","role":"assistant","content":[],"model":"claude-opus-4-8","stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":42,"output_tokens":0}}}

            event: message_delta
            data: {"type":"message_delta","delta":{"stop_reason":"refusal","stop_sequence":null,"stop_details":{"type":"refusal","category":"cyber","explanation":"Unauthorized access techniques."}},"usage":{"output_tokens":0}}

            event: message_stop
            data: {"type":"message_stop"}

            """;

        ChatResult? finishChunk = await CollectFinishChunk(sse);

        Assert.That(finishChunk, Is.Not.Null);
        Assert.That(finishChunk!.Choices![0].FinishReason, Is.EqualTo(ChatMessageFinishReasons.Refusal));
        Assert.That(finishChunk.Choices[0].StopDetails, Is.Not.Null);
        Assert.That(finishChunk.Choices[0].StopDetails!.Category, Is.EqualTo(ChatRefusalCategory.Cyber));
        Assert.That(finishChunk.Usage?.CompletionTokens, Is.EqualTo(0));
        Assert.That(finishChunk.IsRefusalWithoutBillableOutput, Is.True);
    }

    [Test]
    public async Task InboundStream_RefusalAfterPartialOutput_IsBillable()
    {
        const string sse = """
            event: message_start
            data: {"type":"message_start","message":{"id":"msg_stream_refusal_partial","type":"message","role":"assistant","content":[],"model":"claude-opus-4-8","stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":42,"output_tokens":0}}}

            event: content_block_start
            data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

            event: content_block_delta
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hello.."}}

            event: content_block_stop
            data: {"type":"content_block_stop","index":0}

            event: message_delta
            data: {"type":"message_delta","delta":{"stop_reason":"refusal","stop_sequence":null},"usage":{"output_tokens":3}}

            event: message_stop
            data: {"type":"message_stop"}

            """;

        List<ChatResult?> chunks = await CollectStreamChunks(sse);
        ChatResult? finishChunk = chunks.LastOrDefault(x => x?.StreamInternalKind is ChatResultStreamInternalKinds.FinishData);

        Assert.That(finishChunk, Is.Not.Null);
        Assert.That(finishChunk!.Choices![0].FinishReason, Is.EqualTo(ChatMessageFinishReasons.Refusal));
        Assert.That(finishChunk.Usage?.CompletionTokens, Is.EqualTo(3));
        Assert.That(finishChunk.IsRefusalWithoutBillableOutput, Is.False);

        ChatResult? appendChunk = chunks.LastOrDefault(x => x?.StreamInternalKind is ChatResultStreamInternalKinds.AppendAssistantMessage);
        Assert.That(appendChunk?.Choices?[0].Delta?.Content, Is.EqualTo("Hello.."));
    }

    [Test]
    public void IsRefusalWithoutBillableOutput_NonStreamingRefusalWithOutput_IsFalse()
    {
        ChatResult chatResult = new ChatResult
        {
            Choices =
            [
                new ChatChoice
                {
                    FinishReason = ChatMessageFinishReasons.Refusal
                }
            ],
            Usage = new ChatUsage(LLmProviders.Anthropic) { CompletionTokens = 12 }
        };

        Assert.That(chatResult.IsRefusalWithoutBillableOutput, Is.False);
    }

    [Test]
    public void IsRefusalWithoutBillableOutput_EndTurn_IsFalse()
    {
        ChatResult chatResult = new ChatResult
        {
            Choices =
            [
                new ChatChoice
                {
                    FinishReason = ChatMessageFinishReasons.EndTurn
                }
            ],
            Usage = new ChatUsage(LLmProviders.Anthropic) { CompletionTokens = 0 }
        };

        Assert.That(chatResult.IsRefusalWithoutBillableOutput, Is.False);
    }

    private static async Task<ChatResult?> CollectFinishChunk(string sse)
    {
        return (await CollectStreamChunks(sse))
            .LastOrDefault(x => x?.StreamInternalKind is ChatResultStreamInternalKinds.FinishData);
    }

    private static async Task<List<ChatResult?>> CollectStreamChunks(string sse)
    {
        AnthropicEndpointProvider provider = new AnthropicEndpointProvider();
        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(sse));
        StreamReader reader = new StreamReader(stream);
        ChatRequest request = new ChatRequest
        {
            Stream = true,
            Messages = [new ChatMessage(ChatMessageRoles.User, "test")]
        };

        List<ChatResult?> chunks = [];
        await foreach (ChatResult? chunk in provider.InboundStream(reader, request, null))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }
}
