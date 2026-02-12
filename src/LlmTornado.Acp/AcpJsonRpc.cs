using System.Text.Json.Serialization;

namespace LlmTornado.Acp;

/// <summary>
/// JSON-RPC 2.0 request message.
/// </summary>
public class AcpJsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 success response.
/// </summary>
public class AcpJsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 error response.
/// </summary>
public class AcpJsonRpcErrorResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("error")]
    public AcpError Error { get; set; } = new();
}

/// <summary>
/// JSON-RPC 2.0 notification (no id).
/// </summary>
public class AcpJsonRpcNotification
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// JSON-RPC error object following the JSON-RPC 2.0 specification.
/// </summary>
public class AcpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Predefined error codes for common JSON-RPC and ACP-specific errors.
/// </summary>
public static class AcpErrorCodes
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;
    public const int AuthenticationRequired = -32000;
    public const int ResourceNotFound = -32002;
}

/// <summary>
/// ACP JSON-RPC method names.
/// </summary>
public static class AcpMethods
{
    public const string Initialize = "initialize";
    public const string Authenticate = "authenticate";
    public const string NewSession = "session/new";
    public const string LoadSession = "session/load";
    public const string Prompt = "session/prompt";
    public const string Cancel = "session/cancel";
    public const string Initialized = "initialized";
    public const string Update = "session/update";
    public const string SetMode = "session/set_mode";
    public const string SetConfigOption = "session/set_config_option";
    public const string RequestPermission = "session/request_permission";
    public const string ReadTextFile = "fs/read_text_file";
    public const string WriteTextFile = "fs/write_text_file";
    public const string CreateTerminal = "terminal/create";
    public const string TerminalOutput = "terminal/output";
    public const string ReleaseTerminal = "terminal/release";
    public const string WaitForTerminalExit = "terminal/wait_for_exit";
    public const string KillTerminal = "terminal/kill";
}
