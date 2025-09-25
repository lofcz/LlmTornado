using A2A;
using System.Net.ServerSentEvents;
using System.Text.Json;

namespace LlmTornado.A2A.Hosting.Services;

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

    public async Task SendMessageStreamingAsync(string endPoint, List<Part> parts, Func<SseItem<A2AEvent>, Task>? onStreamingEventReceived)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));

        AgentMessage userMessage = new()
        {
            Role = MessageRole.User,
            Parts = parts
        };
        
        await foreach (SseItem<A2AEvent> sseItem in client.SendMessageStreamingAsync(new MessageSendParams { Message = userMessage }))
        {
            await onStreamingEventReceived?.Invoke(sseItem);
        }

        Console.WriteLine(" Streaming completed.");
    }

    public async Task ListenForTaskEventAsync(string endPoint, string taskId, Func<SseItem<A2AEvent>, ValueTask>? onTaskEventReceived = null)
    {
        if(onTaskEventReceived == null)
        {
            return;
        }
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));
        await foreach (SseItem<A2AEvent> sseItem in client.SubscribeToTaskAsync(taskId))
        {
            await onTaskEventReceived.Invoke(sseItem);
            Console.WriteLine(" Task event received: " + JsonSerializer.Serialize(sseItem.Data));
        }
    }

    public async Task SetPushNotifications(string endPoint)
    {
        A2ACardResolver cardResolver = new(new Uri(endPoint));
        AgentCard agentCard = await cardResolver.GetAgentCardAsync();
        A2AClient client = new A2AClient(new Uri(agentCard.Url));
        await client.SetPushNotificationAsync(new TaskPushNotificationConfig()
        {
            PushNotificationConfig = new PushNotificationConfig()
            {
                Url = "https://your-callback-url.com/notifications",
                Authentication = new PushNotificationAuthenticationInfo()
                {
                    Credentials = "your-auth"
                }
            }
        });
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
