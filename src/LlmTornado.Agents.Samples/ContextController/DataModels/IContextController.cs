namespace LlmTornado.Agents.Samples.ContextController;

public interface IContextController
{
    public Task<AgentContext> GetAgentContext();
}

