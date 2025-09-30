using LlmTornado.A2A.Hosting.Models;

namespace LlmTornado.A2A.Hosting.Services
{
    public interface IA2ADispatchService
    {
        Task<ServerCreationResult> DispatchServerAsync(ServerCreationRequest creationRequest);
        Task<bool> RemoveServerAsync(string serverId);
        Task<ServerStatus> GetServerStatusAsync(string serverId);

        string[] GetServerConfigurations();

        IEnumerable<ServerInfo> ListServers();
    }
}
