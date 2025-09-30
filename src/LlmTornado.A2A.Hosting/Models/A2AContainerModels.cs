using A2A;

namespace LlmTornado.A2A.Hosting.Models;

public class ContainerAgentMessage
{
    public Part[] Parts { get; set; } 
    public string Endpoint { get; set; } 

    public ContainerAgentMessage(Part[] parts, string endpoint)
    {
        Parts = parts;
        Endpoint = endpoint;
    }
}

public class ContainerCancelTaskRequest
{
    public string Endpoint { get; set; } 
    public string TaskId { get; set; } 
    public ContainerCancelTaskRequest(string endpoint, string taskId)
    {
        Endpoint = endpoint;
        TaskId = taskId;
    }
}