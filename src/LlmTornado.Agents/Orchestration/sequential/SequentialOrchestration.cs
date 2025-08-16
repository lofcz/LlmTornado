using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration
{
    internal class SequentialOrchestration : ChatOrchestration
    {
        public SequentialOrchestration(string agentName, AgentRunner[]? agents = null) : base(agentName)
        {
            
        }
    }
}
