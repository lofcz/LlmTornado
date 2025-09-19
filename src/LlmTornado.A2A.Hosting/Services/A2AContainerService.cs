using A2A;
using System.Net.ServerSentEvents;

namespace LlmTornado.A2A.Hosting.Services;

public interface IA2AContainerService
{
    Task<AgentCard> GetAgentCardAsync(string endPoint);
    Task<A2AResponse> SendMessageAsync(string endPoint, List<Part> parts);
    Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<SseItem<A2AEvent>, Task>? onEventReceived);
    Task<AgentTask> CancelTaskAsync(string endPoint, string taskId);
    Task<AgentTask> GetTaskAsync(string endPoint, string taskId);
}

public class A2AContainerService : IA2AContainerService
{
    public async Task<AgentCard> GetAgentCardAsync(string endPoint)
    {
        return await new A2ACardResolver(new Uri(endPoint)).GetAgentCardAsync();
    }

    public async Task<A2AResponse> SendMessageAsync(string endPoint, List<Part> parts)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));

        AgentMessage message = new()
        {
            Role = MessageRole.User,
            Parts = parts
        };

        A2AResponse response = await client.SendMessageAsync(new MessageSendParams
        {
            Message = message
        });

        return response;
    }

    public async Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<SseItem<A2AEvent>, Task>? onEventReceived)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));

        AgentMessage userMessage = new()
        {
            Role = MessageRole.User,
            MessageId = Guid.NewGuid().ToString(),
            Parts = parts
        };

        await foreach (SseItem<A2AEvent> sseItem in client.SendMessageStreamingAsync(new MessageSendParams { Message = userMessage }))
        {
            await onEventReceived?.Invoke(sseItem);
        }

        Console.WriteLine(" Streaming completed.");
    }

    public async Task<AgentTask> CancelTaskAsync(string endPoint, string taskId)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));
        return await client.CancelTaskAsync(taskId);
    }

    public async Task<AgentTask> GetTaskAsync(string endPoint, string taskId)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));
        return await client.GetTaskAsync(taskId);
    }
}
