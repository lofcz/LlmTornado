using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Google;

namespace LlmTornado.Tests;

/// <summary>
/// Regression tests for the Google/Gemini/Gemma vendor part mapper. Does not require API credentials.
/// </summary>
[TestFixture]
public class GoogleReasoningPartTests
{
    [Test]
    public void ThoughtPart_WithText_IsClassifiedAsReasoning_AndNotAppendedToPlaintext()
    {
        VendorGoogleChatRequestMessagePart part = new VendorGoogleChatRequestMessagePart
        {
            Text = "the model is thinking about the answer",
            Thought = true
        };

        StringBuilder sb = new StringBuilder();
        ChatMessagePart result = part.ToMessagePart(sb);

        Assert.That(result.Type, Is.EqualTo(ChatMessageTypes.Reasoning));
        Assert.That(result.Reasoning?.Content, Is.EqualTo("the model is thinking about the answer"));

        // The bug: sb (which becomes ChatMessage.Content / the streamed "content" field)
        // used to receive the thought text unconditionally, mixing reasoning into the
        // user-visible answer even though the part was correctly flagged as a thought.
        Assert.That(sb.ToString(), Is.Empty);
    }

    [Test]
    public void NonThoughtPart_WithText_IsClassifiedAsText_AndAppendedToPlaintext()
    {
        VendorGoogleChatRequestMessagePart part = new VendorGoogleChatRequestMessagePart
        {
            Text = "42",
            Thought = false
        };

        StringBuilder sb = new StringBuilder();
        ChatMessagePart result = part.ToMessagePart(sb);

        Assert.That(result.Type, Is.EqualTo(ChatMessageTypes.Text));
        Assert.That(result.Text, Is.EqualTo("42"));
        Assert.That(sb.ToString(), Is.EqualTo("42"));
    }

    [Test]
    public void ThoughtPart_WithoutText_StillReportsReasoningSignature()
    {
        // Redacted-thought blocks: content can be empty while a signature is still present.
        VendorGoogleChatRequestMessagePart part = new VendorGoogleChatRequestMessagePart
        {
            Text = null,
            Thought = true,
            ThoughtSignature = "opaque-signature"
        };

        StringBuilder sb = new StringBuilder();
        ChatMessagePart result = part.ToMessagePart(sb);

        Assert.That(result.Type, Is.EqualTo(ChatMessageTypes.Reasoning));
        Assert.That(result.Reasoning?.Signature, Is.EqualTo("opaque-signature"));
        Assert.That(sb.ToString(), Is.Empty);
    }
}
