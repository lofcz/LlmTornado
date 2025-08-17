using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;

namespace LlmTornado.Agents.Orchestration;

internal class SequentialOrchestration : AgentOrchestration
{
    public SequentialOrchestration(TornadoAgent[] agents)
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

        SetEntryRunnable(runnableAgents[0]);
        SetRunnableWithResult(runnableAgents[runnableAgents.Count -1]);
        runnableAgents[runnableAgents.Count - 1].IsDeadEnd = true;

        for (int i = 0; i < runnableAgents.Count - 1; i++)
        {
            runnableAgents[i].AddAdvancer(runnableAgents[i + 1]);
        }

        foreach (var runnableAgent in runnableAgents)
        {
           AddRunnable(runnableAgent.Agent.Name, runnableAgent);
        }
    }

    
}
