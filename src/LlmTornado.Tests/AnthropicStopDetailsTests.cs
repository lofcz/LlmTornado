using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
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
}
