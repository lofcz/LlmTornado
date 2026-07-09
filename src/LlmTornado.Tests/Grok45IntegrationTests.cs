using LlmTornado.Chat.Models;
using LlmTornado.Code.Models;

namespace LlmTornado.Tests;

/// <summary>
/// Registration tests for Grok 4.5 and Grok Build models.
/// </summary>
[TestFixture]
public class Grok45IntegrationTests
{
    [Test]
    public void Grok45_ModelRegistration_Works()
    {
        Assert.That(ChatModel.XAi.Grok45.V45.Name, Is.EqualTo("grok-4.5"));
        Assert.That(ChatModel.XAi.Grok45.V45.ContextTokens, Is.EqualTo(500_000));
        Assert.That(ChatModel.XAi.Grok45.V45.Aliases, Does.Contain("grok-4.5-latest"));
        Assert.That(ChatModel.XAi.Grok45.V45.Aliases, Does.Contain("grok-build-latest"));
        Assert.That(ChatModel.XAi.AllModels, Does.Contain(ChatModel.XAi.Grok45.V45));
        Assert.That(ChatModel.XAi.OwnsModel("grok-4.5"), Is.True);
    }

    [Test]
    public void GrokBuild_ModelRegistration_Works()
    {
        Assert.That(ChatModel.XAi.GrokBuild.V01.Name, Is.EqualTo("grok-build-0.1"));
        Assert.That(ChatModel.XAi.GrokBuild.V01.ContextTokens, Is.EqualTo(256_000));
        Assert.That(ChatModel.XAi.GrokBuild.V01.Aliases, Does.Contain("grok-build"));
        Assert.That(ChatModel.XAi.GrokBuild.V01.Aliases, Does.Contain("grok-code-fast-1"));
        Assert.That(ChatModel.XAi.GrokBuild.V01.Aliases, Does.Contain("grok-code-fast"));
        Assert.That(ChatModel.XAi.GrokBuild.V01.Aliases, Does.Contain("grok-code-fast-1-0825"));
        Assert.That(ChatModel.XAi.AllModels, Does.Contain(ChatModel.XAi.GrokBuild.V01));
        Assert.That(ChatModel.XAi.OwnsModel("grok-build-0.1"), Is.True);
    }

    [Test]
    public void GrokCode_RemainsRegistered()
    {
        Assert.That(ChatModel.XAi.GrokCode.Fast1.Name, Is.EqualTo("grok-code-fast-1"));
        Assert.That(ChatModel.XAi.AllModels, Does.Contain((IModel)ChatModel.XAi.GrokCode.Fast1));
    }
}

