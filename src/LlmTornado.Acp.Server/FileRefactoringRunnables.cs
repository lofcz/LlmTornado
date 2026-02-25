using LlmTornado.Acp.Server.Skills;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using System.Linq;

namespace LlmTornado.Acp.Server;

internal static class RefactorAgentRunner
{
    public static async ValueTask<string> RunAsync(TornadoAgent agent, string prompt, CancellationToken cancellationToken)
    {
        Conversation result = await agent.Run(
            input: prompt,
            streaming: false,
            cancellationToken: cancellationToken);

        ChatMessage? assistant = result.Messages.LastOrDefault(m => m.Role == ChatMessageRoles.Assistant);
        return assistant?.Content ?? result.Messages.LastOrDefault()?.Content ?? string.Empty;
    }

    /// <summary>
    /// Gets skill-derived instructions for a pipeline stage, falling back to a default if no skill stage is defined.
    /// </summary>
    public static string GetStageInstructions(AgentSkill? skill, string stage, string fallback)
    {
        if (skill?.StageInstructions.TryGetValue(stage, out string? instructions) == true && !string.IsNullOrWhiteSpace(instructions))
        {
            return instructions;
        }

        return fallback;
    }
}

internal sealed class AnalyzeRunnable : OrchestrationRunnable<ChatMessage, RefactorAnalysis>
{
    private readonly TornadoAgent _agent;

    public AnalyzeRunnable(TornadoApi api, ChatModel model, List<Tool> tools, Orchestration orchestrator, AgentSkill? skill = null)
        : base(orchestrator, "analyze")
    {
        string instructions = RefactorAgentRunner.GetStageInstructions(skill, "analyze",
            "Analyze the user's refactoring request. Identify impacted files, symbols, and constraints. Be concise.");

        _agent = new TornadoAgent(
            api,
            model,
            name: "RefactorAnalyze",
            instructions: instructions,
            tools: tools.ConvertAll<Delegate>(t => t.Delegate!),
            streaming: false);
    }

    public override async ValueTask<RefactorAnalysis> Invoke(RunnableProcess<ChatMessage, RefactorAnalysis> input)
    {
        string prompt = input.Input.Content ?? string.Empty;
        string analysis = await RefactorAgentRunner.RunAsync(_agent, prompt, cts.Token);

        return new RefactorAnalysis
        {
            OriginalPrompt = prompt,
            AnalysisSummary = analysis
        };
    }
}

internal sealed class PlanRunnable : OrchestrationRunnable<RefactorAnalysis, RefactorPlan>
{
    private readonly TornadoAgent _agent;

    public PlanRunnable(TornadoApi api, ChatModel model, Orchestration orchestrator, AgentSkill? skill = null)
        : base(orchestrator, "plan")
    {
        string instructions = RefactorAgentRunner.GetStageInstructions(skill, "plan",
            "Create a concrete step-by-step refactoring plan with ordered edits and verification checks.");

        _agent = new TornadoAgent(
            api,
            model,
            name: "RefactorPlan",
            instructions: instructions,
            streaming: false);
    }

    public override async ValueTask<RefactorPlan> Invoke(RunnableProcess<RefactorAnalysis, RefactorPlan> input)
    {
        RefactorAnalysis analysis = input.Input;
        string prompt = $"""
            User request:
            {analysis.OriginalPrompt}

            Analysis:
            {analysis.AnalysisSummary}

            Produce an actionable refactoring plan.
            """;

        string planText = await RefactorAgentRunner.RunAsync(_agent, prompt, cts.Token);

        return new RefactorPlan
        {
            OriginalPrompt = analysis.OriginalPrompt,
            AnalysisSummary = analysis.AnalysisSummary,
            PlanText = planText,
            Attempt = 1,
            MaxAttempts = 2
        };
    }
}

internal sealed class EditRunnable : OrchestrationRunnable<RefactorPlan, RefactorEditResult>
{
    private readonly TornadoAgent _agent;

    public EditRunnable(TornadoApi api, ChatModel model, List<Tool> tools, Orchestration orchestrator, AgentSkill? skill = null)
        : base(orchestrator, "edit")
    {
        string instructions = RefactorAgentRunner.GetStageInstructions(skill, "edit",
            "Execute the refactoring plan using tools. Keep edits minimal and safe.");

        _agent = new TornadoAgent(
            api,
            model,
            name: "RefactorEdit",
            instructions: instructions,
            tools: tools.ConvertAll<Delegate>(t => t.Delegate!),
            streaming: false);
    }

    public override async ValueTask<RefactorEditResult> Invoke(RunnableProcess<RefactorPlan, RefactorEditResult> input)
    {
        RefactorPlan plan = input.Input;

        string prompt = $"""
            Execute this refactoring plan (attempt {plan.Attempt}/{plan.MaxAttempts}):

            {plan.PlanText}

            Return a short summary of applied edits and any unresolved concerns.
            """;

        string summary = await RefactorAgentRunner.RunAsync(_agent, prompt, cts.Token);

        return new RefactorEditResult
        {
            Plan = plan,
            EditSummary = summary
        };
    }
}

internal sealed class VerifyRunnable : OrchestrationRunnable<RefactorEditResult, RefactorVerificationResult>
{
    private readonly TornadoAgent _agent;

    public VerifyRunnable(TornadoApi api, ChatModel model, Orchestration orchestrator, AgentSkill? skill = null)
        : base(orchestrator, "verify")
    {
        string instructions = RefactorAgentRunner.GetStageInstructions(skill, "verify",
            "Verify whether the requested refactoring is complete and correct. Start your answer with PASS or FAIL.");

        _agent = new TornadoAgent(
            api,
            model,
            name: "RefactorVerify",
            instructions: instructions,
            streaming: false);
    }

    public override async ValueTask<RefactorVerificationResult> Invoke(RunnableProcess<RefactorEditResult, RefactorVerificationResult> input)
    {
        RefactorEditResult editResult = input.Input;

        string prompt = $"""
            Original user request:
            {editResult.Plan.OriginalPrompt}

            Analysis:
            {editResult.Plan.AnalysisSummary}

            Plan:
            {editResult.Plan.PlanText}

            Edit summary:
            {editResult.EditSummary}

            Verify if this fulfills the request. Reply with PASS or FAIL first, then brief reasoning.
            """;

        string verification = await RefactorAgentRunner.RunAsync(_agent, prompt, cts.Token);
        bool isPass = verification.Contains("PASS", StringComparison.OrdinalIgnoreCase) &&
                      !verification.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase);

        if (isPass || editResult.Plan.Attempt >= editResult.Plan.MaxAttempts)
        {
            string finalText = isPass
                ? $"Refactoring completed.\n\n{editResult.EditSummary}\n\nVerification:\n{verification}"
                : $"Refactoring stopped after {editResult.Plan.Attempt} attempts.\n\nLatest edit summary:\n{editResult.EditSummary}\n\nVerification:\n{verification}";

            return new RefactorVerificationResult
            {
                IsSuccess = true,
                FinalMessage = new ChatMessage(ChatMessageRoles.Assistant, finalText),
                NextPlan = null
            };
        }

        RefactorPlan retryPlan = new()
        {
            OriginalPrompt = editResult.Plan.OriginalPrompt,
            AnalysisSummary = editResult.Plan.AnalysisSummary,
            PlanText = $"{editResult.Plan.PlanText}\n\nVerification feedback to fix:\n{verification}",
            Attempt = editResult.Plan.Attempt + 1,
            MaxAttempts = editResult.Plan.MaxAttempts
        };

        return new RefactorVerificationResult
        {
            IsSuccess = false,
            FinalMessage = new ChatMessage(ChatMessageRoles.Assistant, string.Empty),
            NextPlan = retryPlan
        };
    }
}

internal sealed class FinalizeRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    public FinalizeRunnable(Orchestration orchestrator)
        : base(orchestrator, "finalize")
    {
        AllowDeadEnd = true;
    }

    public override ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> input)
    {
        return ValueTask.FromResult(input.Input);
    }
}
