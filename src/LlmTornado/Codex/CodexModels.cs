using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Codex;

/// <summary>
/// Settings used to launch and identify the official Codex app-server.
/// </summary>
public sealed class CodexAppServerOptions
{
    /// <summary>
    /// Path or command name of the Codex executable. Defaults to resolving <c>codex</c> from <c>PATH</c>.
    /// </summary>
    public string ExecutablePath { get; set; } = "codex";

    /// <summary>
    /// Optional working directory for the app-server process.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Optional environment variables added to the app-server process.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// Optional Codex configuration overrides passed as repeated <c>--config key=value</c> arguments.
    /// </summary>
    public List<string> ConfigOverrides { get; set; } = [];

    /// <summary>
    /// Client identifier reported to Codex and OpenAI compliance logs.
    /// </summary>
    public string ClientName { get; set; } = "llmtornado";

    /// <summary>
    /// Human-readable client title reported during initialization.
    /// </summary>
    public string ClientTitle { get; set; } = "LLM Tornado";

    /// <summary>
    /// Client version reported during initialization. The assembly version is used when omitted.
    /// </summary>
    public string? ClientVersion { get; set; }

    /// <summary>
    /// Handles server-initiated requests such as Codex approval prompts. Unhandled requests are rejected.
    /// </summary>
    public CodexServerRequestHandler? ServerRequestHandler { get; set; }
}

/// <summary>
/// Handles a request initiated by the Codex app-server.
/// </summary>
public delegate Task<JToken?> CodexServerRequestHandler(CodexServerRequest request, CancellationToken cancellationToken);

/// <summary>
/// A request initiated by the Codex app-server.
/// </summary>
public sealed class CodexServerRequest
{
    internal CodexServerRequest(JToken id, string method, JObject parameters)
    {
        Id = id;
        Method = method;
        Parameters = parameters;
    }

    /// <summary>
    /// Request identifier supplied by the app-server.
    /// </summary>
    public JToken Id { get; }

    /// <summary>
    /// App-server method name.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Method parameters.
    /// </summary>
    public JObject Parameters { get; }
}

/// <summary>
/// An app-server notification.
/// </summary>
public sealed class CodexNotification
{
    internal CodexNotification(string method, JObject parameters)
    {
        Method = method;
        Parameters = parameters;
    }

    /// <summary>
    /// Notification method.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Notification parameters.
    /// </summary>
    public JObject Parameters { get; }
}

/// <summary>
/// Metadata returned by the app-server initialization handshake.
/// </summary>
public sealed class CodexInitialization
{
    /// <summary>
    /// User agent used by the app-server for upstream requests.
    /// </summary>
    [JsonProperty("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Active Codex home directory.
    /// </summary>
    [JsonProperty("codexHome")]
    public string? CodexHome { get; set; }

    /// <summary>
    /// Runtime platform family.
    /// </summary>
    [JsonProperty("platformFamily")]
    public string? PlatformFamily { get; set; }

    /// <summary>
    /// Runtime operating system.
    /// </summary>
    [JsonProperty("platformOs")]
    public string? PlatformOs { get; set; }
}

/// <summary>
/// Current Codex account details.
/// </summary>
public sealed class CodexAccount
{
    /// <summary>
    /// Authentication account type.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// ChatGPT account email when available.
    /// </summary>
    [JsonProperty("email")]
    public string? Email { get; set; }

    /// <summary>
    /// ChatGPT subscription plan when available.
    /// </summary>
    [JsonProperty("planType")]
    public string? PlanType { get; set; }
}

/// <summary>
/// Result of reading the current Codex account.
/// </summary>
public sealed class CodexAccountResult
{
    /// <summary>
    /// Current account, or null when signed out.
    /// </summary>
    [JsonProperty("account")]
    public CodexAccount? Account { get; set; }

    /// <summary>
    /// Whether the selected model provider requires OpenAI authentication.
    /// </summary>
    [JsonProperty("requiresOpenaiAuth")]
    public bool RequiresOpenAiAuthentication { get; set; }
}

/// <summary>
/// Result of a ChatGPT login attempt.
/// </summary>
public sealed class CodexLoginResult
{
    /// <summary>
    /// Login attempt identifier.
    /// </summary>
    [JsonProperty("loginId")]
    public string? LoginId { get; set; }

    /// <summary>
    /// Whether authentication completed successfully.
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Authentication error reported by the app-server.
    /// </summary>
    [JsonProperty("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Browser-based ChatGPT login managed by the Codex app-server.
/// </summary>
public sealed class CodexBrowserLogin
{
    private readonly CodexSession session;

    internal CodexBrowserLogin(CodexSession session, string loginId, Uri authorizationUrl)
    {
        this.session = session;
        LoginId = loginId;
        AuthorizationUrl = authorizationUrl;
    }

    /// <summary>
    /// Identifier of this login attempt.
    /// </summary>
    public string LoginId { get; }

    /// <summary>
    /// URL the host application should open in a browser.
    /// </summary>
    public Uri AuthorizationUrl { get; }

    /// <summary>
    /// Waits for the app-server to report completion of the browser login.
    /// </summary>
    public Task<CodexLoginResult> WaitAsync(CancellationToken cancellationToken = default)
        => session.WaitForLoginAsync(LoginId, cancellationToken);

    /// <summary>
    /// Cancels this login attempt.
    /// </summary>
    public Task CancelAsync(CancellationToken cancellationToken = default)
        => session.CancelLoginAsync(LoginId, cancellationToken);
}

/// <summary>
/// A reasoning effort advertised for a Codex model.
/// </summary>
public sealed class CodexReasoningEffort
{
    /// <summary>
    /// Reasoning effort identifier sent to the app-server.
    /// </summary>
    [JsonProperty("reasoningEffort")]
    public string ReasoningEffort { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable effort description.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// A service tier advertised for a Codex model.
/// </summary>
public sealed class CodexServiceTier
{
    /// <summary>
    /// Service-tier identifier sent with a turn request.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable service-tier name.
    /// </summary>
    [JsonProperty("name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable service-tier description.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// A model advertised by the authenticated Codex app-server.
/// </summary>
public sealed class CodexModel
{
    /// <summary>
    /// Catalog entry identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Model identifier used for thread and turn requests.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable model name.
    /// </summary>
    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Model description supplied by Codex.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Model-specific base instructions required by the direct Codex backend.
    /// </summary>
    [JsonProperty("baseInstructions")]
    public string BaseInstructions { get; set; } = string.Empty;

    /// <summary>
    /// Whether this entry is hidden from the default model picker.
    /// </summary>
    [JsonProperty("hidden")]
    public bool Hidden { get; set; }

    /// <summary>
    /// Whether this is the catalog's default model.
    /// </summary>
    [JsonProperty("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Default reasoning effort for this model.
    /// </summary>
    [JsonProperty("defaultReasoningEffort")]
    public string DefaultReasoningEffort { get; set; } = string.Empty;

    /// <summary>
    /// Supported reasoning efforts in the server-defined display order.
    /// </summary>
    [JsonProperty("supportedReasoningEfforts")]
    public List<CodexReasoningEffort> SupportedReasoningEfforts { get; set; } = [];

    /// <summary>
    /// Service tiers supported by this model.
    /// </summary>
    [JsonProperty("serviceTiers")]
    public List<CodexServiceTier> ServiceTiers { get; set; } = [];

    /// <summary>
    /// Catalog default service-tier identifier for this model.
    /// </summary>
    [JsonProperty("defaultServiceTier")]
    public string DefaultServiceTier { get; set; } = string.Empty;

    /// <summary>
    /// Input modalities advertised by the model.
    /// </summary>
    [JsonProperty("inputModalities")]
    public List<string> InputModalities { get; set; } = [];

    /// <summary>
    /// Whether the model supports Codex personality settings.
    /// </summary>
    [JsonProperty("supportsPersonality")]
    public bool SupportsPersonality { get; set; }
}

internal sealed class CodexModelPage
{
    [JsonProperty("data")]
    public List<CodexModel> Data { get; set; } = [];

    [JsonProperty("nextCursor")]
    public string? NextCursor { get; set; }
}

/// <summary>
/// Options for a new Codex thread.
/// </summary>
public sealed class CodexThreadOptions
{
    /// <summary>
    /// Model identifier from <see cref="CodexSession.ListModelsAsync"/>.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Working directory available to the Codex thread.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Codex approval policy, such as <c>never</c> or <c>on-request</c>.
    /// </summary>
    public string? ApprovalPolicy { get; set; }

    /// <summary>
    /// Codex sandbox mode, such as <c>readOnly</c> or <c>workspaceWrite</c>.
    /// </summary>
    public string? Sandbox { get; set; }

    /// <summary>
    /// If true, keeps the thread in memory without persisting it.
    /// </summary>
    public bool? Ephemeral { get; set; }
}

/// <summary>
/// Per-turn text generation options.
/// </summary>
public sealed class CodexTurnOptions
{
    /// <summary>
    /// Optional model override from <see cref="CodexSession.ListModelsAsync"/>.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Optional reasoning effort advertised by the selected model.
    /// </summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>
    /// Optional service tier advertised by the selected model.
    /// </summary>
    public string? ServiceTier { get; set; }

    /// <summary>
    /// Receives streamed assistant text deltas.
    /// </summary>
    public Func<CodexTextDelta, Task>? OnTextDelta { get; set; }
}

/// <summary>
/// A streamed assistant text delta.
/// </summary>
public sealed class CodexTextDelta
{
    internal CodexTextDelta(string threadId, string turnId, string itemId, string delta)
    {
        ThreadId = threadId;
        TurnId = turnId;
        ItemId = itemId;
        Delta = delta;
    }

    /// <summary>
    /// Thread that produced the delta.
    /// </summary>
    public string ThreadId { get; }

    /// <summary>
    /// Turn that produced the delta.
    /// </summary>
    public string TurnId { get; }

    /// <summary>
    /// Agent-message item that produced the delta.
    /// </summary>
    public string ItemId { get; }

    /// <summary>
    /// Text appended by this event.
    /// </summary>
    public string Delta { get; }
}

/// <summary>
/// Final result of a Codex text turn.
/// </summary>
public sealed class CodexTurnResult
{
    internal CodexTurnResult(string threadId, string turnId, string finalResponse, string? status, JObject turn)
    {
        ThreadId = threadId;
        TurnId = turnId;
        FinalResponse = finalResponse;
        Status = status;
        Turn = turn;
    }

    /// <summary>
    /// Thread that produced the result.
    /// </summary>
    public string ThreadId { get; }

    /// <summary>
    /// Completed turn identifier.
    /// </summary>
    public string TurnId { get; }

    /// <summary>
    /// Concatenated assistant text.
    /// </summary>
    public string FinalResponse { get; }

    /// <summary>
    /// Final app-server turn status.
    /// </summary>
    public string? Status { get; }

    /// <summary>
    /// Raw completed turn object for protocol fields not projected by this wrapper.
    /// </summary>
    public JObject Turn { get; }
}

/// <summary>
/// Error returned by the Codex app-server.
/// </summary>
public sealed class CodexRpcException : Exception
{
    internal CodexRpcException(int code, string message, JToken? data = null) : base(message)
    {
        Code = code;
        DataToken = data;
    }

    /// <summary>
    /// JSON-RPC error code.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// Optional app-server error data.
    /// </summary>
    public JToken? DataToken { get; }

    /// <summary>
    /// Whether the app-server reported transient overload.
    /// </summary>
    public bool IsRetryable => Code == -32001;
}
