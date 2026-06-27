using LlmTornado.Chat;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class MessageTimestampPrefixerTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 6, 27, 14, 5, 22, TimeSpan.FromHours(-4));

    [Test]
    public void Prefix_TextMessage_Adds_Local_And_Utc_Timestamps()
    {
        ChatMessage message = new(ChatMessageRoles.User, "hello");

        bool changed = MessageTimestampPrefixer.Prefix(message, "user", FixedTime);

        Assert.That(changed, Is.True);
        Assert.That(message.Content, Does.StartWith("[timestamp role=user local=2026-06-27T14:05:22-04:00 utc=2026-06-27T18:05:22Z]"));
        Assert.That(message.Content, Does.EndWith("hello"));
    }

    [Test]
    public void Prefix_TextMessage_CanInclude_ContextUsedPercent()
    {
        ChatMessage message = new(ChatMessageRoles.User, "hello");

        bool changed = MessageTimestampPrefixer.Prefix(
            message,
            "user",
            FixedTime,
            contextUsedPercent: 12);

        Assert.That(changed, Is.True);
        Assert.That(message.Content, Does.StartWith("[timestamp role=user local=2026-06-27T14:05:22-04:00 utc=2026-06-27T18:05:22Z context_used=12%]"));
    }

    [Test]
    public void Prefix_MultipartMessage_Adds_Timestamp_To_First_TextPart()
    {
        ChatMessage message = new(ChatMessageRoles.User,
        [
            new ChatMessagePart(ChatMessageTypes.Image),
            new ChatMessagePart("describe this")
        ]);

        bool changed = MessageTimestampPrefixer.Prefix(message, "user", FixedTime);

        Assert.That(changed, Is.True);
        Assert.That(message.Parts![1].Text, Does.StartWith("[timestamp role=user local=2026-06-27T14:05:22-04:00 utc=2026-06-27T18:05:22Z]"));
    }

    [Test]
    public void Prefix_AlreadyPrefixedMessage_Does_Not_DoublePrefix()
    {
        ChatMessage message = new(ChatMessageRoles.User,
            "[timestamp role=user local=2026-06-27T14:05:22-04:00 utc=2026-06-27T18:05:22Z]\nhello");

        bool changed = MessageTimestampPrefixer.Prefix(message, "user", FixedTime);

        Assert.That(changed, Is.False);
        Assert.That(message.Content!.Split("[timestamp").Length - 1, Is.EqualTo(1));
    }

    [Test]
    public void Prefix_ToolMessage_Does_NotAlter()
    {
        ChatMessage message = new(ChatMessageRoles.Tool, "tool output");

        bool changed = MessageTimestampPrefixer.Prefix(message, "tool", FixedTime);

        Assert.That(changed, Is.False);
        Assert.That(message.Content, Is.EqualTo("tool output"));
    }

    [Test]
    public void PrefixAssistantMessages_OnlyPrefixesAssistantText()
    {
        List<ChatMessage> messages =
        [
            new(ChatMessageRoles.User, "hello"),
            new(ChatMessageRoles.Assistant, "hi"),
            new(ChatMessageRoles.Tool, "result")
        ];

        int changed = MessageTimestampPrefixer.PrefixAssistantMessages(messages, FixedTime);

        Assert.That(changed, Is.EqualTo(1));
        Assert.That(messages[1].Content, Does.StartWith("[timestamp role=assistant"));
        Assert.That(messages[0].Content, Is.EqualTo("hello"));
        Assert.That(messages[2].Content, Is.EqualTo("result"));
    }
}
