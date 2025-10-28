using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.Samples.DataModels;
using LlmTornado.Chat;

namespace LlmTornado.Agents.Samples.ResearchAgent;

public class DeepExitRunnable : OrchestrationRunnable<ReportData, ChatMessage>
{
    public DeepExitRunnable(Orchestration orchestrator, string runnableName = "") : base(orchestrator, runnableName)
    {
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ReportData, ChatMessage> process)
    {
        this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
        return new ChatMessage(Code.ChatMessageRoles.Assistant, process.Input.ToString());
    }
}
