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
