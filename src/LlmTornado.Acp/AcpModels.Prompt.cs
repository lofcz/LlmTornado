using System.Text.Json.Serialization;

namespace LlmTornado.Acp;

/// <summary>
/// Request parameters for sending a user prompt to the agent.
/// </summary>
public class AcpPromptRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public List<AcpContentBlock> Prompt { get; set; } = [];
}

/// <summary>
/// Response from processing a user prompt.
/// </summary>
public class AcpPromptResponse
{
    [JsonPropertyName("stopReason")]
    public string StopReason { get; set; } = AcpStopReasons.EndTurn;
}

/// <summary>
/// Reasons why an agent stops processing a prompt turn.
/// </summary>
public static class AcpStopReasons
{
    public const string EndTurn = "end_turn";
    public const string MaxTokens = "max_tokens";
    public const string MaxTurnRequests = "max_turn_requests";
    public const string Refusal = "refusal";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// Notification to cancel ongoing operations for a session.
/// </summary>
public class AcpCancelNotification
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// Content block representing displayable information in ACP.
/// </summary>
public class AcpContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = AcpContentBlockTypes.Text;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("resource")]
    public AcpResourceContents? Resource { get; set; }

    [JsonPropertyName("annotations")]
    public AcpAnnotations? Annotations { get; set; }
}

/// <summary>
/// Content block type identifiers.
/// </summary>
public static class AcpContentBlockTypes
{
    public const string Text = "text";
    public const string Image = "image";
    public const string Audio = "audio";
    public const string ResourceLink = "resource_link";
    public const string Resource = "resource";
}

/// <summary>
/// Resource content that can be embedded in a message.
/// </summary>
public class AcpResourceContents
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("blob")]
    public string? Blob { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

/// <summary>
/// Optional annotations for the client.
/// </summary>
public class AcpAnnotations
{
    [JsonPropertyName("audience")]
    public List<string>? Audience { get; set; }

    [JsonPropertyName("priority")]
    public double? Priority { get; set; }

    [JsonPropertyName("lastModified")]
    public string? LastModified { get; set; }
}
