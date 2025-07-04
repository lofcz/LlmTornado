namespace LlmTornado.Responses.Events;

/// <summary>
/// Enumeration of all possible response event types.
/// </summary>
public enum ResponseEventTypes
{
    /// <summary>
    /// Response created event.
    /// </summary>
    ResponseCreated,
    
    /// <summary>
    /// Response in progress event.
    /// </summary>
    ResponseInProgress,
    
    /// <summary>
    /// Response completed event.
    /// </summary>
    ResponseCompleted,
    
    /// <summary>
    /// Response failed event.
    /// </summary>
    ResponseFailed,
    
    /// <summary>
    /// Response incomplete event.
    /// </summary>
    ResponseIncomplete,
    
    /// <summary>
    /// Response queued event.
    /// </summary>
    ResponseQueued,
    
    /// <summary>
    /// Response error event.
    /// </summary>
    ResponseError,
    
    /// <summary>
    /// Response output item added event.
    /// </summary>
    ResponseOutputItemAdded,
    
    /// <summary>
    /// Response output item done event.
    /// </summary>
    ResponseOutputItemDone,
    
    /// <summary>
    /// Response content part added event.
    /// </summary>
    ResponseContentPartAdded,
    
    /// <summary>
    /// Response content part done event.
    /// </summary>
    ResponseContentPartDone,
    
    /// <summary>
    /// Response output text delta event.
    /// </summary>
    ResponseOutputTextDelta,
    
    /// <summary>
    /// Response output text done event.
    /// </summary>
    ResponseOutputTextDone,
    
    /// <summary>
    /// Response output text annotation added event.
    /// </summary>
    ResponseOutputTextAnnotationAdded,
    
    /// <summary>
    /// Response refusal delta event.
    /// </summary>
    ResponseRefusalDelta,
    
    /// <summary>
    /// Response refusal done event.
    /// </summary>
    ResponseRefusalDone,
    
    /// <summary>
    /// Response function call arguments delta event.
    /// </summary>
    ResponseFunctionCallArgumentsDelta,
    
    /// <summary>
    /// Response function call arguments done event.
    /// </summary>
    ResponseFunctionCallArgumentsDone,
    
    /// <summary>
    /// Response file search call in progress event.
    /// </summary>
    ResponseFileSearchCallInProgress,
    
    /// <summary>
    /// Response file search call searching event.
    /// </summary>
    ResponseFileSearchCallSearching,
    
    /// <summary>
    /// Response file search call completed event.
    /// </summary>
    ResponseFileSearchCallCompleted,
    
    /// <summary>
    /// Response web search call in progress event.
    /// </summary>
    ResponseWebSearchCallInProgress,
    
    /// <summary>
    /// Response web search call searching event.
    /// </summary>
    ResponseWebSearchCallSearching,
    
    /// <summary>
    /// Response web search call completed event.
    /// </summary>
    ResponseWebSearchCallCompleted,
    
    /// <summary>
    /// Response reasoning delta event.
    /// </summary>
    ResponseReasoningDelta,
    
    /// <summary>
    /// Response reasoning done event.
    /// </summary>
    ResponseReasoningDone,
    
    /// <summary>
    /// Response reasoning summary part added event.
    /// </summary>
    ResponseReasoningSummaryPartAdded,
    
    /// <summary>
    /// Response reasoning summary part done event.
    /// </summary>
    ResponseReasoningSummaryPartDone,
    
    /// <summary>
    /// Response reasoning summary text delta event.
    /// </summary>
    ResponseReasoningSummaryTextDelta,
    
    /// <summary>
    /// Response reasoning summary text done event.
    /// </summary>
    ResponseReasoningSummaryTextDone,
    
    /// <summary>
    /// Response reasoning summary delta event.
    /// </summary>
    ResponseReasoningSummaryDelta,
    
    /// <summary>
    /// Response reasoning summary done event.
    /// </summary>
    ResponseReasoningSummaryDone,
    
    /// <summary>
    /// Response image generation call in progress event.
    /// </summary>
    ResponseImageGenerationCallInProgress,
    
    /// <summary>
    /// Response image generation call generating event.
    /// </summary>
    ResponseImageGenerationCallGenerating,
    
    /// <summary>
    /// Response image generation call partial image event.
    /// </summary>
    ResponseImageGenerationCallPartialImage,
    
    /// <summary>
    /// Response image generation call completed event.
    /// </summary>
    ResponseImageGenerationCallCompleted,
    
    /// <summary>
    /// Response MCP call arguments delta event.
    /// </summary>
    ResponseMcpCallArgumentsDelta,
    
    /// <summary>
    /// Response MCP call arguments done event.
    /// </summary>
    ResponseMcpCallArgumentsDone,
    
    /// <summary>
    /// Response MCP call in progress event.
    /// </summary>
    ResponseMcpCallInProgress,
    
    /// <summary>
    /// Response MCP call completed event.
    /// </summary>
    ResponseMcpCallCompleted,
    
    /// <summary>
    /// Response MCP call failed event.
    /// </summary>
    ResponseMcpCallFailed,
    
    /// <summary>
    /// Response MCP list tools in progress event.
    /// </summary>
    ResponseMcpListToolsInProgress,
    
    /// <summary>
    /// Response MCP list tools completed event.
    /// </summary>
    ResponseMcpListToolsCompleted,
    
    /// <summary>
    /// Response MCP list tools failed event.
    /// </summary>
    ResponseMcpListToolsFailed,
    
    /// <summary>
    /// Response code interpreter call in progress event.
    /// </summary>
    ResponseCodeInterpreterCallInProgress,
    
    /// <summary>
    /// Response code interpreter call code delta event.
    /// </summary>
    ResponseCodeInterpreterCallCodeDelta,
    
    /// <summary>
    /// Response code interpreter call code done event.
    /// </summary>
    ResponseCodeInterpreterCallCodeDone
} 