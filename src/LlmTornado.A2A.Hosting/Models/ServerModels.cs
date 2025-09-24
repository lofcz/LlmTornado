using System.Text.Json.Serialization;

namespace LlmTornado.A2A.Hosting.Models;

public class ServerInfo
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerConfiguration { get; set; } = string.Empty;
    public int HostPort { get; set; }
    public int RemotePort { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string? MountPath { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Container creation result model
/// </summary>
public class ServerCreationResult
{
    /// <summary>
    /// Whether the container was created successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Server ID
    /// </summary>
    public string? ServerId { get; set; }

    /// <summary>
    /// Server endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Container creation result model
/// </summary>
public class ServerCreationRequest
{
    /// <summary>
    /// Whether the container was created successfully
    /// </summary>
    [JsonPropertyName("agentImageKey")]
    public string AgentImageKey { get; set; } = string.Empty;

    [JsonPropertyName("environmentVariables")]
    public string[] EnvironmentVariables { get; set; } = Array.Empty<string>();

}

public class ServerStatus
{
    /// <summary>
    /// Server ID
    /// </summary>
    public string? ServerId { get; set; }

    /// <summary>
    /// Server state (running, stopped, etc.)
    /// </summary>
    public string Status { get; set; } = "unknown";

    /// <summary>
    /// Whether the server is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Server endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Error message if any
    /// </summary>
    public string? ErrorMessage { get; set; }
}

