using A2A;

namespace LlmTornado.A2A;

public interface IA2ARuntimeConfiguration
{
    Task StartAgentTaskAsync(AgentTask task, CancellationToken cancellationToken);
    Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken);
}
