using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;

namespace LlmTornado.Agents.API.Models;

/// <summary>
/// Request model for creating a new chat runtime instance
/// </summary>
public class CreateChatRuntimeRequest
{
    /// <summary>
    /// Configuration type for the runtime (e.g., "simple", "orchestrated")
    /// </summary>
    public string ConfigurationType { get; set; } = "simple";
    
    /// <summary>
    /// Agent name identifier
    /// </summary>
    public string AgentName { get; set; } = "DefaultAgent";
    
    /// <summary>
    /// Initial instructions for the agent
    /// </summary>
    public string Instructions { get; set; } = "You are a helpful assistant";
    
    /// <summary>
    /// Whether to enable streaming for this runtime
    /// </summary>
    public bool EnableStreaming { get; set; } = true;
}

/// <summary>
/// Response model for chat runtime creation
/// </summary>
public class CreateChatRuntimeResponse
{
    /// <summary>
    /// Unique identifier for the created runtime
    /// </summary>
    public string RuntimeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the runtime creation
    /// </summary>
    public string Status { get; set; } = "created";
}

/// <summary>
/// Request model for sending a message to a chat runtime
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// The message content to send
    /// </summary>
    public string Content { get; set; } = string.Empty;

    public string? Base64File { get; set; }
}

/// <summary>
/// Response model for message sending
/// </summary>
public class SendMessageResponse
{
    /// <summary>
    /// Response message content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Message role
    /// </summary>
    public string Role { get; set; } = "assistant";
    
    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the response was streamed
    /// </summary>
    public bool IsStreamed { get; set; }
}

/// <summary>
/// API model for runtime status information
/// </summary>
public class RuntimeStatusResponse
{
    /// <summary>
    /// Runtime identifier
    /// </summary>
    public string RuntimeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the runtime
    /// </summary>
    public string Status { get; set; } = "unknown";
    
    /// <summary>
    /// Whether streaming is enabled
    /// </summary>
    public bool StreamingEnabled { get; set; }
    
    /// <summary>
    /// Number of messages in the conversation
    /// </summary>
    public int MessageCount { get; set; }
}

/// <summary>
/// API model for streaming events
/// </summary>
public class StreamingEventResponse
{
    /// <summary>
    /// Event type identifier
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Event sequence number
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Event data payload
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// Timestamp of the event
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Runtime ID that generated the event
    /// </summary>
    public string RuntimeId { get; set; } = string.Empty;
}