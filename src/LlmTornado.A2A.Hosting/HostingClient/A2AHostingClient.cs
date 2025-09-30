using A2A;
using LlmTornado.A2A.Hosting.Models;
using System.Text;
using System.Text.Json;

namespace LlmTornado.A2A.Hosting.Client;

/// <summary>
/// Service for communicating with A2A Hosting API
/// </summary>
public partial class A2AHostingClient
{
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _jsonOptions;
    protected readonly string _apiBaseUrl;

    public A2AHostingClient(HttpClient httpClient, string apiBaseUrl)
    {
        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl ?? "https://localhost:5000";
        _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<string[]> GetServerConfigurationsAsync()
    {
        var response = await _httpClient.GetAsync("/api/A2ADispatch/configurations");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<string[]>(json, _jsonOptions) ?? Array.Empty<string>();
    }

    public async Task<ServerInfo[]> GetActiveServersAsync()
    {
        var response = await _httpClient.GetAsync("/api/A2ADispatch/active");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ServerInfo[]>(json, _jsonOptions) ?? Array.Empty<ServerInfo>();
    }

    public async Task<ServerCreationResult> CreateServerAsync(ServerCreationRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/api/A2ADispatch/create", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ServerCreationResult>(responseJson, _jsonOptions) 
            ?? new ServerCreationResult { Success = false, ErrorMessage = "Failed to deserialize response" };
    }

    public async Task DeleteServerAsync(string serverId)
    {
        var response = await _httpClient.DeleteAsync($"/api/A2ADispatch/delete/{serverId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ServerStatus> GetServerStatusAsync(string serverId)
    {
        var response = await _httpClient.GetAsync($"/api/A2ADispatch/status/{serverId}");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ServerStatus>(json, _jsonOptions) 
            ?? new ServerStatus { ServerId = serverId, Status = "unknown" };
    }

    public async Task<AgentCard> GetAgentCardAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync($"/api/A2AContainer/GetAgentCard?endpoint={Uri.EscapeDataString(endpoint)}");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AgentCard>(json, _jsonOptions) 
            ?? new AgentCard();
    }

    public async Task<AgentMessage> SendMessageAsync(string endpoint, List<Part> parts)
    {
        var message = new ContainerAgentMessage(parts.ToArray(), endpoint);
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/api/A2AContainer/SendMessage", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AgentMessage>(responseJson, _jsonOptions) 
            ?? new AgentMessage();
    }

    public async Task<AgentTask> GetTaskStatusAsync(string endpoint, string taskId)
    {
        var response = await _httpClient.GetAsync($"/api/A2AContainer/GetTaskStatus?endpoint={Uri.EscapeDataString(endpoint)}&taskId={Uri.EscapeDataString(taskId)}");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AgentTask>(json, _jsonOptions) 
            ?? new AgentTask();
    }

    public async Task CancelTaskAsync(string endpoint, string taskId)
    {
        var request = new ContainerCancelTaskRequest(endpoint, taskId);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/api/A2AContainer/CancelTask", content);
        response.EnsureSuccessStatusCode();
    }
}