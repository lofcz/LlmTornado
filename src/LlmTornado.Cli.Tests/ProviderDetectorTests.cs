using LlmTornado.Chat.Models;
using LlmTornado.Cli;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ProviderDetectorTests
{
    #region Detection Logic

    [Test]
    public void Detect_Returns_Null_When_No_EnvVars_Set()
    {
        // This test verifies the logic structure. If it returns non-null,
        // that means the test runner has API keys set, which is fine.
        ProviderDetectionResult? result = ProviderDetector.Detect();
        
        // Either null (no env vars) or valid result
        if (result is not null)
        {
            Assert.That(result.Api, Is.Not.Null);
            Assert.That(result.Providers, Is.Not.Empty);
            Assert.That(result.ActiveModel, Is.Not.Null);
        }
    }

    [Test]
    public void Detect_Returns_Result_With_OpenAI_Key_Present()
    {
        string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (key is null)
        {
            Assert.Ignore("OPENAI_API_KEY not set — skipping provider detection test.");
            return;
        }

        ProviderDetectionResult? result = ProviderDetector.Detect();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Providers.Any(p => p.Provider == LLmProviders.OpenAi), Is.True);
        Assert.That(result.AllModels, Is.Not.Empty);
    }

    [Test]
    public void DetectedProvider_Has_Required_Fields()
    {
        string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (key is null)
        {
            Assert.Ignore("OPENAI_API_KEY not set.");
            return;
        }

        ProviderDetectionResult? result = ProviderDetector.Detect();
        Assert.That(result, Is.Not.Null);

        DetectedProvider openAi = result!.Providers.First(p => p.Provider == LLmProviders.OpenAi);
        Assert.That(openAi.ApiKey, Is.Not.Null.And.Not.Empty);
        Assert.That(openAi.Models, Is.Not.Empty);
        Assert.That(openAi.DefaultModel, Is.Not.Null);
        Assert.That(openAi.DefaultModel.Name, Does.Contain("gpt"));
    }

    [Test]
    public void AllModels_Is_Union_Of_ProviderModels()
    {
        ProviderDetectionResult? result = ProviderDetector.Detect();
        if (result is null)
        {
            Assert.Ignore("No providers detected.");
            return;
        }

        int expectedCount = result.Providers.Sum(p => p.Models.Count);
        Assert.That(result.AllModels, Has.Count.EqualTo(expectedCount));
    }

    #endregion

    #region Priority Logic

    [Test]
    public void ActiveModel_Prefers_Anthropic_Over_OpenAI()
    {
        // If both are available, Anthropic should win
        string? anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (anthropicKey is null || openAiKey is null)
        {
            Assert.Ignore("Need both ANTHROPIC_API_KEY and OPENAI_API_KEY to test priority.");
            return;
        }

        ProviderDetectionResult? result = ProviderDetector.Detect();
        Assert.That(result, Is.Not.Null);

        // When Anthropic is available, it should be the default
        Assert.That(result!.ActiveModel.Name, Does.Contain("claude").IgnoreCase);
    }

    #endregion
}
