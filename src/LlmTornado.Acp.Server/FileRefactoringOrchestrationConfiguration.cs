using LlmTornado.Acp.Server.Skills;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Acp.Server;

internal sealed class FileRefactoringOrchestrationConfiguration : OrchestrationRuntimeConfiguration
{
    public FileRefactoringOrchestrationConfiguration(TornadoApi api, ChatModel model, string cwd, List<Tool> tools, AgentSkill? skill = null)
    {
        MessageHistoryFileLocation = Path.Combine(Path.GetTempPath(), $"acp_refactor_{Guid.NewGuid():N}.json");

        AnalyzeRunnable analyze = new(api, model, tools, this, skill);
        PlanRunnable plan = new(api, model, this, skill);
        EditRunnable edit = new(api, model, tools, this, skill);
        VerifyRunnable verify = new(api, model, this, skill);
        FinalizeRunnable finalize = new(this);

        analyze.AddAdvancer(plan);
        plan.AddAdvancer(edit);
        edit.AddAdvancer(verify);

        verify.AddAdvancer(
            result => result.IsSuccess,
            result => result.FinalMessage,
            finalize);

        verify.AddAdvancer(
            result => !result.IsSuccess && result.NextPlan is not null,
            result => result.NextPlan!,
            edit);

        SetEntryRunnable(analyze);
        SetRunnableWithResult(finalize);
    }
}
