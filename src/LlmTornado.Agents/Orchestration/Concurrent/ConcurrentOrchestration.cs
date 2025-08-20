using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;

namespace LlmTornado.Agents.Orchestration;

public class RunnablePassthru : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    public override async ValueTask<ChatMessage> Invoke(ChatMessage input)
    {
        return input;
    }
}

public class ConcurrentRunnerResultCollector : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    
    public override async ValueTask<ChatMessage> Invoke(ChatMessage input)
    {
        ChatMessage combinedResult = new ChatMessage(Code.ChatMessageRoles.Assistant);
        combinedResult.Parts = new List<ChatMessagePart>();
        foreach (ChatMessage msg in Input)
        {
            combinedResult.Parts.AddRange(msg.Parts ?? new List<ChatMessagePart>());
        }
        return combinedResult;
    }
}

internal class ConcurrentOrchestration : AgentOrchestration
{
    public ConcurrentOrchestration(TornadoAgent[] agents)
    {
        SetupRunnableAgents(agents);
    }

    public void SetupRunnableAgents(TornadoAgent[] _agents)
    {
        List<RunnableAgent> runnableAgents = new List<RunnableAgent>();

        foreach (var agent in _agents)
        {
            var runnableAgent = new RunnableAgent(agent);
            runnableAgents.Add(runnableAgent);
        }

        RunnablePassthru passThru = new RunnablePassthru() { AllowsParallelAdvances = true };
        ConcurrentRunnerResultCollector collector = new ConcurrentRunnerResultCollector() { IsDeadEnd = true, CombineInput = true };

        SetEntryRunnable(passThru);
        SetRunnableWithResult(collector);

        foreach(var agent in runnableAgents)
        {
            passThru.AddAdvancer(agent);
            agent.AddAdvancer(collector);
        }

        foreach (var runnableAgent in runnableAgents)
        {
           AddRunnable(runnableAgent.Agent.Name, runnableAgent);
        }

        AddRunnable("passthru", passThru);
        AddRunnable("collector", collector);
    }

    public override List<ChatMessage> GetMessages()
    {
        return this.Results!;
    }
}
