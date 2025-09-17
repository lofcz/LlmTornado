using A2A;
using System.Net.ServerSentEvents;

namespace LlmTornado.A2A.Hosting.Services;

public interface IA2AContainerService
{
    Task<AgentCard> GetAgentCardAsync(string endPoint);
    Task<AgentMessage> SendMessageAsync(string endPoint, List<Part> parts);
    Task SendStreamingMessageAsync(string endPoint, List<Part> parts, Func<AgentMessage, Task>? onMessageReceived);
    Task<AgentTask> CancelTask(string endPoint, string taskId);
    Task<AgentTask> GetTask(string endPoint, string taskId);
}

public class A2AContainerService : IA2AContainerService
{
    public async Task<AgentCard> GetAgentCardAsync(string endPoint)
    {
        return await new A2ACardResolver(new Uri(endPoint)).GetAgentCardAsync();
    }

    public async Task<AgentMessage> SendMessageAsync(string endPoint, List<Part> parts)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));

        AgentMessage message = new()
        {
            Role = MessageRole.User,
            Parts = parts
        };

        var response = await client.SendMessageAsync(new MessageSendParams
        {
            Message = message
        });

        return (AgentMessage)response;
    }

    public async Task SendStreamingMessageAsync(string endPoint, List<Part> parts, Func<AgentMessage, Task>? onMessageReceived)
    {
        var client = new A2AClient(new Uri(endPoint));

        AgentMessage userMessage = new()
        {
            Role = MessageRole.User,
            MessageId = Guid.NewGuid().ToString(),
            Parts = parts
        };

        await foreach (SseItem<A2AEvent> sseItem in client.SendMessageStreamingAsync(new MessageSendParams { Message = userMessage }))
        {
            AgentMessage agentResponse = (AgentMessage)sseItem.Data;
            // Display each part of the response as it arrives
            onMessageReceived?.Invoke(agentResponse);
            Console.WriteLine($" Received streaming response chunk: {((TextPart)agentResponse.Parts[0]).Text}");
        }
    }

    public async Task<AgentTask> CancelTask(string endPoint, string taskId)
    {
        var client = new A2AClient(new Uri(endPoint));
        return await client.CancelTaskAsync(taskId);
    }

    public async Task<AgentTask> GetTask(string endPoint, string taskId)
    {
        var client = new A2AClient(new Uri(endPoint));
        return await client.GetTaskAsync(taskId);
    }

    private static void DisplayTaskDetails(AgentTask agentResponse)
    {
        Console.WriteLine(" Received task details:");
        Console.WriteLine($"  ID: {agentResponse.Id}");
        Console.WriteLine($"  Status: {agentResponse.Status.State}");
        Console.WriteLine($"  Artifact: {(agentResponse.Artifacts?[0].Parts?[0] as TextPart)?.Text}");
    }
}
