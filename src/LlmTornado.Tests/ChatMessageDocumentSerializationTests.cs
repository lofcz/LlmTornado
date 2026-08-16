using LlmTornado.Chat;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Tests;

[TestFixture]
public class ChatMessageDocumentSerializationTests
{
    [Test]
    public void OpenAiChatRequest_SerializesBase64DocumentAsFileData()
    {
        var request = new ChatRequest
        {
            Messages =
            [
                new ChatMessage(ChatMessageRoles.User,
                [
                    new ChatMessagePart("dGVzdC1kYXRh", DocumentLinkTypes.Base64),
                    new ChatMessagePart("Summarize this document")
                ])
            ]
        };

        string json = JsonConvert.SerializeObject(request);

        Assert.That(json, Does.Contain("\"type\":\"file\""));
        Assert.That(json, Does.Contain("\"file_data\":\"dGVzdC1kYXRh\""));
    }
}
