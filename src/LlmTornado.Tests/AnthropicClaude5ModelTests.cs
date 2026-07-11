using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Tests;

/// <summary>
/// Registration and capability tests for Claude 5 generation models (Fable 5, Sonnet 5).
/// </summary>
[TestFixture]
public class AnthropicClaude5ModelTests
{
    [Test]
    public void Fable5_IsDiscoverableOnChatModelAnthropic()
    {
        Assert.That(ChatModel.Anthropic.Claude5.Fable.Name, Is.EqualTo("claude-fable-5"));
        Assert.That(ChatModel.Anthropic.AllModels, Has.Some.Matches<IModel>(m => m.Name == "claude-fable-5"));
    }

    [Test]
    public void Sonnet5_IsDiscoverableOnChatModelAnthropic()
    {
        Assert.That(ChatModel.Anthropic.Claude5.Sonnet.Name, Is.EqualTo("claude-sonnet-5"));
        Assert.That(ChatModel.Anthropic.AllModels, Has.Some.Matches<IModel>(m => m.Name == "claude-sonnet-5"));
    }

    [Test]
    public void Claude5_OwnsModel_ReturnsTrue()
    {
        Assert.That(ChatModel.Anthropic.OwnsModel("claude-fable-5"), Is.True);
        Assert.That(ChatModel.Anthropic.OwnsModel("claude-sonnet-5"), Is.True);
    }

    [TestCase("claude-fable-5")]
    [TestCase("claude-sonnet-5")]
    public void Claude5_IsClaude5Model_Recognized(string modelName)
    {
        Assert.That(ChatModelAnthropicHelper.IsClaude5Model(modelName), Is.True);
    }

    [TestCase("claude-fable-5")]
    [TestCase("claude-sonnet-5")]
    public void Claude5_SupportsAdaptiveThinking(string modelName)
    {
        Assert.That(ChatModelAnthropicHelper.SupportsAdaptiveThinking(modelName), Is.True);
    }

    [TestCase("claude-fable-5")]
    [TestCase("claude-sonnet-5")]
    public void Claude5_SupportsEffort(string modelName)
    {
        Assert.That(ChatModelAnthropicHelper.IsEffortCompatibleModel(modelName), Is.True);
    }

    [TestCase("claude-fable-5")]
    [TestCase("claude-sonnet-5")]
    public void Claude5_RejectsNonDefaultSamplingParams(string modelName)
    {
        Assert.That(ChatModelAnthropicHelper.RejectsNonDefaultSamplingParams(modelName), Is.True);
    }

    [TestCase("claude-fable-5")]
    [TestCase("claude-sonnet-5")]
    public void Claude5_RequiresAdaptiveThinkingWhenEnabled(string modelName)
    {
        Assert.That(ChatModelAnthropicHelper.RequiresAdaptiveThinkingWhenEnabled(modelName), Is.True);
    }

    [TestCase("claude-fable-5")]
    [TestCase("claude-sonnet-5")]
    public void Claude5_SupportsHighResVision(string modelName)
    {
        Assert.That(ChatModelAnthropicHelper.SupportsHighResVision(modelName), Is.True);
    }

    [Test]
    public void Claude5_ContextWindow_IsOneMillionTokens()
    {
        Assert.That(ChatModel.Anthropic.Claude5.Fable.ContextTokens, Is.EqualTo(1_000_000));
        Assert.That(ChatModel.Anthropic.Claude5.Sonnet.ContextTokens, Is.EqualTo(1_000_000));
    }
}
