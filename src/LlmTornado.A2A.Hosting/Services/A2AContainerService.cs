using A2A;

namespace LlmTornado.A2A.Hosting.Services;

public class A2AContainerService
{
    public async Task<List<AgentCard>> GetAgentCardsAsync(DockerDispatchService dockerService)
    {
        List<AgentCard> cards = new List<AgentCard>();
        foreach (var container in dockerService.GetActiveContainers())
        {
            // 2. Create agent card resolver
            A2ACardResolver agentCardResolver = new(new Uri(container.Endpoint));
            AgentCard agentCard = await agentCardResolver.GetAgentCardAsync();
            cards.Add(agentCard);
        }
        return cards;
    }
}
