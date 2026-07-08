using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Core.Telemetry;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class SessionTelemetryTests
{
    private static AgentUsageTelemetry Usage(int prompt, int completion, int? reasoning = null) => new()
    {
        PromptTokens = prompt,
        CompletionTokens = completion,
        TotalTokens = prompt + completion,
        CompletionReasoningTokens = reasoning,
    };

    [Test]
    public void NoUsage_HasNoRealData()
    {
        SessionTelemetry telemetry = new();
        Assert.That(telemetry.HasRealUsage, Is.False);
        Assert.That(telemetry.EstimatedNextPromptTokens, Is.Null);
    }

    [Test]
    public void MultipleRequestsPerTurn_LastPromptWins_CompletionAccumulates()
    {
        SessionTelemetry telemetry = new();
        telemetry.BeginTurn();

        // Tool loop: three requests, context grows each time.
        telemetry.OnUsageReceived(Usage(1000, 50));
        telemetry.OnUsageReceived(Usage(1200, 80, reasoning: 30));
        telemetry.OnUsageReceived(Usage(1500, 200, reasoning: 60));

        Assert.That(telemetry.EstimatedNextPromptTokens, Is.EqualTo(1500 + 200));
        Assert.That(telemetry.TurnCompletionTokens, Is.EqualTo(50 + 80 + 200));
        Assert.That(telemetry.TurnReasoningTokens, Is.EqualTo(30 + 60));
        Assert.That(telemetry.TurnRequestCount, Is.EqualTo(3));
    }

    [Test]
    public void BeginTurn_ResetsTurnAccumulators_KeepsLastUsage()
    {
        SessionTelemetry telemetry = new();
        telemetry.OnUsageReceived(Usage(900, 100));

        telemetry.BeginTurn();

        Assert.That(telemetry.TurnCompletionTokens, Is.Zero);
        Assert.That(telemetry.TurnRequestCount, Is.Zero);
        // Last real usage survives across turns — it's the basis for the next context estimate.
        Assert.That(telemetry.EstimatedNextPromptTokens, Is.EqualTo(1000));
    }

    [Test]
    public void InvalidateUsage_DropsRealData_CountsCompression()
    {
        SessionTelemetry telemetry = new();
        telemetry.OnUsageReceived(Usage(5000, 300));

        telemetry.InvalidateUsage();

        Assert.That(telemetry.HasRealUsage, Is.False);
        Assert.That(telemetry.EstimatedNextPromptTokens, Is.Null);
        Assert.That(telemetry.CompressionEvents, Is.EqualTo(1));
    }

    [Test]
    public void RequestPrepared_IsRecorded()
    {
        SessionTelemetry telemetry = new();
        AgentRequestTokenTelemetry prep = new()
        {
            RequestTokensBeforeSend = 4321,
            ContextWindowTokens = 32768,
            ContextWindowUtilization = 0.13,
            Source = AgentTokenMeasurementSource.EstimatorFallback,
            ModelName = "test",
        };

        telemetry.OnRequestPrepared(prep);

        Assert.That(telemetry.LastRequest, Is.SameAs(prep));
    }
}
