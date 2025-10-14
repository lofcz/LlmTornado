using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace ChatBot.States;

public class ParallelRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    public ParallelRunnable(Orchestration orchestrator, string runnableName = "") : base(orchestrator, runnableName)
    {
    }

    public override ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        Orchestrator.RuntimeProperties.AddOrUpdate("CurrentTask", process.Input.Content, (key, oldValue) => process.Input.Content);
        return ValueTask.FromResult(process.Input);
    }
}
