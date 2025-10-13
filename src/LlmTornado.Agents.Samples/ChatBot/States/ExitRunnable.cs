using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace ChatBot.States;

public class ExitRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    public ExitRunnable(Orchestration orchestrator, string runnableName = "") : base(orchestrator, runnableName)
    {
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
        return process.Input;
    }
}
