using A2A;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.A2A;

public interface IA2ARuntimeConfiguration
{
    Task ExecuteAgentTaskAsync(AgentTask task, CancellationToken cancellationToken);
    Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken);
}
