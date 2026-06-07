using System.Reflection;
using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Tests;

/// <summary>
/// Documents Anthropic model deprecations and replacement discoverability.
/// </summary>
[TestFixture]
public class AnthropicModelDeprecationTests
{
    [Test]
    public void Opus41_IsMarkedObsoleteWithAugust2026Retirement()
    {
        ObsoleteAttribute? attr = typeof(ChatModelAnthropicClaude41)
            .GetField(nameof(ChatModelAnthropicClaude41.ModelOpus250805), BindingFlags.Public | BindingFlags.Static)!
            .GetCustomAttribute<ObsoleteAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Message, Does.Contain("August 5, 2026"));
        Assert.That(attr.Message, Does.Contain("ChatModel.Anthropic.Claude48.Opus"));
    }

    [Test]
    public void Opus48_IsDiscoverableOnChatModelAnthropic()
    {
        Assert.That(ChatModel.Anthropic.Claude48.Opus.Name, Is.EqualTo("claude-opus-4-8"));
        Assert.That(ChatModel.Anthropic.Claude48.NextOpus.Name, Is.EqualTo("claude-opus-4-8"));
        Assert.That(ChatModel.Anthropic.AllModels, Has.Some.Matches<IModel>(m => m.Name == "claude-opus-4-8"));
    }
}
