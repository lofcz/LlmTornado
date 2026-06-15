using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LlmTornado.Agents;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Tests;

[TestFixture]
public class AgentRunnerTests
{
    private TornadoApi? _openAiProvider;
    private TornadoApi? _ollamaProvider;
    private string? _openApiKey;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _openApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (!string.IsNullOrEmpty(_openApiKey))
        {
            _openAiProvider = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, _openApiKey)]);
        }

        // Standard local AI setup as used in tools/demo
        _ollamaProvider = new TornadoApi([new ProviderAuthentication(LLmProviders.Custom, "http://localhost:11434")]);
    }

    private async Task TestErrorHandling(TornadoApi provider, ChatModel model)
    {
        // 1. Force a request error (e.g., using a bogus URL or bad model to trigger a response error)
        var brokenProvider = new TornadoApi([new ProviderAuthentication(provider.GetProvider(LLmProviders.OpenAi)?.Provider ?? LLmProviders.Custom, "http://localhost:99999")]);
        var agent = new TornadoAgent(brokenProvider, model, instructions: "say hi");
        
        var optionsThrow = new TornadoRunnerOptions { ThrowOnResponseError = true, ThrowOnRequestError = true };
        var optionsNoThrow = new TornadoRunnerOptions { ThrowOnResponseError = false, ThrowOnRequestError = false };

        // Should not throw, should set exit reason
        var conversation = await TornadoRunner.RunAsync(agent, "test", runnerOptions: optionsNoThrow);
        
        Assert.That(agent.LastRunExitReason, Is.Not.Null, "Exit reason shouldn't be null");
        Assert.That(agent.LastRunExitReason, Is.InstanceOf<ResponseErrorExitReason>().Or.InstanceOf<RequestErrorExitReason>(), $"Exit reason was {agent.LastRunExitReason.GetType().Name}");
        
        // Should throw, should set exit reason
        var ex = Assert.CatchAsync<Exception>(async () => await TornadoRunner.RunAsync(agent, "test", runnerOptions: optionsThrow));
        Assert.That(agent.LastRunExitReason, Is.Not.Null, "Exit reason shouldn't be null after throw");
    }

    private static string FailingTool() 
    {
        throw new Exception("Tool failed deliberately");
    }

    private async Task TestExitConditions(TornadoApi provider, ChatModel model)
    {
        var agent = new TornadoAgent(provider, model, instructions: "Reply with the exact word 'testing' and nothing else.");

        // 1. Completed
        await TornadoRunner.RunAsync(agent, "hello", singleTurn: true);
        Assert.That(agent.LastRunExitReason, Is.InstanceOf<CompletedExitReason>(), "Should complete normally");

        // 2. Cancelled
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();
        await TornadoRunner.RunAsync(agent, "hello", cancellationToken: cts.Token, runnerOptions: new TornadoRunnerOptions { ThrowOnCancelled = false });
        Assert.That(agent.LastRunExitReason, Is.InstanceOf<CancelledExitReason>(), "Should be cancelled");

        // 3. MaxTokensReached
        await TornadoRunner.RunAsync(agent, "hello", runnerOptions: new TornadoRunnerOptions { TokenLimit = 1, ThrowOnTokenLimitExceeded = false }, singleTurn: true);
        Assert.That(agent.LastRunExitReason, Is.InstanceOf<MaxTokensReachedExitReason>(), "Should hit max tokens");

        // 4. InputGuardRailTriggered
        GuardRailFunction guardRail = _ => ValueTask.FromResult(new GuardRailFunctionOutput("tripped", true));
        Assert.ThrowsAsync<GuardRailTriggerException>(async () => await TornadoRunner.RunAsync(agent, "hello", guardRail: guardRail));
        Assert.That(agent.LastRunExitReason, Is.InstanceOf<InputGuardRailTriggeredExitReason>(), "Should hit guardrail");

        // 5. ToolError
        var toolAgent = new TornadoAgent(provider, model, tools: [ (Func<string>)FailingTool ], instructions: "You must call the FailingTool function right now.");
        await TornadoRunner.RunAsync(toolAgent, "Call FailingTool", runnerOptions: new TornadoRunnerOptions { ThrowOnToolError = false }, singleTurn: false, maxTurns: 2);
        Assert.That(toolAgent.LastRunExitReason, Is.InstanceOf<ToolErrorExitReason>().Or.InstanceOf<CompletedExitReason>(), "Should have tool error or complete if it swallowed it and kept going");
    }

    [Test]
    public async Task OpenAIAgentRunner_ErrorHandlingFlags()
    {
        if (_openAiProvider == null) Assert.Ignore("OPENAI_API_KEY not set");
        await TestErrorHandling(_openAiProvider, ChatModel.OpenAi.O3.Mini);
        await TestExitConditions(_openAiProvider, ChatModel.OpenAi.O3.Mini);
    }

    [Test]
    public async Task LocalAIAgentRunner_ErrorHandlingFlags()
    {
        try
        {
            // Optional: quick ping to check if ollama is actually up, otherwise Ignore.
            var models = await _ollamaProvider.Models.GetModels();
            if(models == null || models.Count == 0) Assert.Ignore("Ollama not responding properly");
        }
        catch
        {
            Assert.Ignore("Ollama not running locally");
        }

        await TestErrorHandling(_ollamaProvider, new ChatModel("qwen3:14b"));
        await TestExitConditions(_ollamaProvider, new ChatModel("qwen3:14b"));
    }
}
