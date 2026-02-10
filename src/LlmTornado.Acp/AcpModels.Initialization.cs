using System.Text.Json.Serialization;

namespace LlmTornado.Acp;

/// <summary>
/// Metadata about the implementation of a client or agent.
/// </summary>
public class AcpImplementation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

/// <summary>
/// Capabilities supported by the client.
/// </summary>
public class AcpClientCapabilities
{
    [JsonPropertyName("fs")]
    public AcpFileSystemCapability Fs { get; set; } = new();

    [JsonPropertyName("terminal")]
    public bool Terminal { get; set; }
}

/// <summary>
/// File system capabilities supported by the client.
/// </summary>
public class AcpFileSystemCapability
{
    [JsonPropertyName("readTextFile")]
    public bool ReadTextFile { get; set; }

    [JsonPropertyName("writeTextFile")]
    public bool WriteTextFile { get; set; }
}

/// <summary>
/// Capabilities supported by the agent.
/// </summary>
public class AcpAgentCapabilities
{
    [JsonPropertyName("loadSession")]
    public bool LoadSession { get; set; }

    [JsonPropertyName("mcpCapabilities")]
    public AcpMcpCapabilities McpCapabilities { get; set; } = new();

    [JsonPropertyName("promptCapabilities")]
    public AcpPromptCapabilities PromptCapabilities { get; set; } = new();

    [JsonPropertyName("sessionCapabilities")]
    public AcpSessionCapabilities SessionCapabilities { get; set; } = new();
}

/// <summary>
/// MCP capabilities supported by the agent.
/// </summary>
public class AcpMcpCapabilities
{
    [JsonPropertyName("http")]
    public bool Http { get; set; }

    [JsonPropertyName("sse")]
    public bool Sse { get; set; }
}

/// <summary>
/// Prompt capabilities supported by the agent.
/// </summary>
public class AcpPromptCapabilities
{
    [JsonPropertyName("audio")]
    public bool Audio { get; set; }

    [JsonPropertyName("embeddedContext")]
    public bool EmbeddedContext { get; set; }

    [JsonPropertyName("image")]
    public bool Image { get; set; }
}

/// <summary>
/// Session capabilities supported by the agent.
/// </summary>
public class AcpSessionCapabilities
{
}

/// <summary>
/// Describes an available authentication method.
/// </summary>
public class AcpAuthMethod
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Request parameters for the initialize method.
/// </summary>
public class AcpInitializeRequest
{
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; set; } = 1;

    [JsonPropertyName("clientCapabilities")]
    public AcpClientCapabilities ClientCapabilities { get; set; } = new();

    [JsonPropertyName("clientInfo")]
    public AcpImplementation? ClientInfo { get; set; }
}

/// <summary>
/// Response to the initialize method.
/// </summary>
public class AcpInitializeResponse
{
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; set; } = 1;

    [JsonPropertyName("agentCapabilities")]
    public AcpAgentCapabilities AgentCapabilities { get; set; } = new();

    [JsonPropertyName("agentInfo")]
    public AcpImplementation? AgentInfo { get; set; }

    [JsonPropertyName("authMethods")]
    public List<AcpAuthMethod> AuthMethods { get; set; } = [];
}

/// <summary>
/// Request parameters for the authenticate method.
/// </summary>
public class AcpAuthenticateRequest
{
    [JsonPropertyName("methodId")]
    public string MethodId { get; set; } = string.Empty;
}

/// <summary>
/// Response to the authenticate method.
/// </summary>
public class AcpAuthenticateResponse
{
}
