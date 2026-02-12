using System.Text.Json.Serialization;

namespace LlmTornado.Acp;

/// <summary>
/// Request parameters for creating a new session.
/// </summary>
public class AcpNewSessionRequest
{
    [JsonPropertyName("cwd")]
    public string Cwd { get; set; } = string.Empty;

    [JsonPropertyName("mcpServers")]
    public List<AcpMcpServerConfig> McpServers { get; set; } = [];
}

/// <summary>
/// Response from creating a new session.
/// </summary>
public class AcpNewSessionResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("modes")]
    public AcpSessionModeState? Modes { get; set; }

    [JsonPropertyName("configOptions")]
    public List<AcpSessionConfigOption>? ConfigOptions { get; set; }
}

/// <summary>
/// Request parameters for loading an existing session.
/// </summary>
public class AcpLoadSessionRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("cwd")]
    public string Cwd { get; set; } = string.Empty;

    [JsonPropertyName("mcpServers")]
    public List<AcpMcpServerConfig> McpServers { get; set; } = [];
}

/// <summary>
/// Response from loading an existing session.
/// </summary>
public class AcpLoadSessionResponse
{
    [JsonPropertyName("modes")]
    public AcpSessionModeState? Modes { get; set; }

    [JsonPropertyName("configOptions")]
    public List<AcpSessionConfigOption>? ConfigOptions { get; set; }
}

/// <summary>
/// A mode the agent can operate in.
/// </summary>
public class AcpSessionMode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// The set of modes and the one currently active.
/// </summary>
public class AcpSessionModeState
{
    [JsonPropertyName("currentModeId")]
    public string CurrentModeId { get; set; } = string.Empty;

    [JsonPropertyName("availableModes")]
    public List<AcpSessionMode> AvailableModes { get; set; } = [];
}

/// <summary>
/// A session configuration option.
/// </summary>
public class AcpSessionConfigOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "select";

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("currentValue")]
    public string? CurrentValue { get; set; }

    /// <summary>
    /// Per ACP spec this is a plain array of <see cref="AcpSessionConfigSelectOption"/> (ungrouped)
    /// or <see cref="AcpSessionConfigSelectGroup"/> (grouped).
    /// Use grouped format for Rider compatibility.
    /// </summary>
    [JsonPropertyName("options")]
    public List<AcpSessionConfigSelectGroup>? Options { get; set; }
}

/// <summary>
/// A group of possible values for a session configuration option.
/// </summary>
public class AcpSessionConfigSelectGroup
{
    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public List<AcpSessionConfigSelectOption> Options { get; set; } = [];
}

/// <summary>
/// A possible value for a session configuration option.
/// </summary>
public class AcpSessionConfigSelectOption
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Request parameters for setting a session mode.
/// </summary>
public class AcpSetSessionModeRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("modeId")]
    public string ModeId { get; set; } = string.Empty;
}

/// <summary>
/// Response to session/set_mode method.
/// </summary>
public class AcpSetSessionModeResponse
{
}

/// <summary>
/// Request parameters for setting a session configuration option.
/// </summary>
public class AcpSetSessionConfigOptionRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("configId")]
    public string ConfigId { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Response to session/set_config_option method.
/// </summary>
public class AcpSetSessionConfigOptionResponse
{
    [JsonPropertyName("configOptions")]
    public List<AcpSessionConfigOption> ConfigOptions { get; set; } = [];
}

/// <summary>
/// MCP server configuration.
/// </summary>
public class AcpMcpServerConfig
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    [JsonPropertyName("env")]
    public List<AcpEnvVariable>? Env { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("headers")]
    public List<AcpHttpHeader>? Headers { get; set; }
}

/// <summary>
/// An environment variable.
/// </summary>
public class AcpEnvVariable
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// An HTTP header.
/// </summary>
public class AcpHttpHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
