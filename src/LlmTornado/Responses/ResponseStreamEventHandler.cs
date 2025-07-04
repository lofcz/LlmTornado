using System;
using System.Threading.Tasks;
using LlmTornado.Responses.Events;
using LlmTornado.Threads;

namespace LlmTornado.Responses;

/// <summary>
///     Enables subscribing to various Responses API streaming events.
/// </summary>
public class ResponseStreamEventHandler
{
    /// <summary>
    ///     Called when a response.created event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseCreatedEvent, ValueTask>? OnResponseCreated { get; set; }

    /// <summary>
    ///     Called when a response.in_progress event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseInProgressEvent, ValueTask>? OnResponseInProgress { get; set; }

    /// <summary>
    ///     Called when a response.completed event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseCompletedEvent, ValueTask>? OnResponseCompleted { get; set; }

    /// <summary>
    ///     Called when a response.failed event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseFailedEvent, ValueTask>? OnResponseFailed { get; set; }

    /// <summary>
    ///     Called when a response.incomplete event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseIncompleteEvent, ValueTask>? OnResponseIncomplete { get; set; }

    /// <summary>
    ///     Called when a response.queued event arrives.
    /// </summary>
    public Func<ResponseQueuedEvent, ValueTask>? OnResponseQueued { get; set; }

    /// <summary>
    ///     Called when a response.error event arrives.
    /// </summary>
    public Func<ResponseErrorEvent, ValueTask>? OnResponseError { get; set; }

    /// <summary>
    ///     Called when a response.output_item.added event arrives. This event contains the added output item.
    /// </summary>
    public Func<ResponseOutputItemAddedEvent, ValueTask>? OnResponseOutputItemAdded { get; set; }

    /// <summary>
    ///     Called when a response.output_item.done event arrives. This event contains the output item marked done.
    /// </summary>
    public Func<ResponseOutputItemDoneEvent, ValueTask>? OnResponseOutputItemDone { get; set; }

    /// <summary>
    ///     Called when a response.content_part.added event arrives.
    /// </summary>
    public Func<ResponseContentPartAddedEvent, ValueTask>? OnResponseContentPartAdded { get; set; }

    /// <summary>
    ///     Called when a response.content_part.done event arrives.
    /// </summary>
    public Func<ResponseContentPartDoneEvent, ValueTask>? OnResponseContentPartDone { get; set; }

    /// <summary>
    ///     Called when a response.text.delta event arrives.
    /// </summary>
    public Func<ResponseOutputTextDeltaEvent, ValueTask>? OnResponseOutputTextDelta { get; set; }

    /// <summary>
    ///     Called when a response.text.done event arrives.
    /// </summary>
    public Func<ResponseOutputTextDoneEvent, ValueTask>? OnResponseOutputTextDone { get; set; }

    /// <summary>
    ///     Called when a response.text.annotation.added event arrives.
    /// </summary>
    public Func<ResponseOutputTextAnnotationAddedEvent, ValueTask>? OnResponseOutputTextAnnotationAdded { get; set; }

    /// <summary>
    ///     Called when a response.refusal.delta event arrives.
    /// </summary>
    public Func<ResponseRefusalDeltaEvent, ValueTask>? OnResponseRefusalDelta { get; set; }

    /// <summary>
    ///     Called when a response.refusal.done event arrives.
    /// </summary>
    public Func<ResponseRefusalDoneEvent, ValueTask>? OnResponseRefusalDone { get; set; }

    /// <summary>
    ///     Called when a response.function_call.arguments.delta event arrives.
    /// </summary>
    public Func<ResponseFunctionCallArgumentsDeltaEvent, ValueTask>? OnResponseFunctionCallArgumentsDelta { get; set; }

    /// <summary>
    ///     Called when a response.function_call.arguments.done event arrives.
    /// </summary>
    public Func<ResponseFunctionCallArgumentsDoneEvent, ValueTask>? OnResponseFunctionCallArgumentsDone { get; set; }

    /// <summary>
    ///     Called when a response.file_search_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseFileSearchCallInProgressEvent, ValueTask>? OnResponseFileSearchCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.file_search_call.searching event arrives.
    /// </summary>
    public Func<ResponseFileSearchCallSearchingEvent, ValueTask>? OnResponseFileSearchCallSearching { get; set; }

    /// <summary>
    ///     Called when a response.file_search_call.completed event arrives.
    /// </summary>
    public Func<ResponseFileSearchCallCompletedEvent, ValueTask>? OnResponseFileSearchCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.web_search_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseWebSearchCallInProgressEvent, ValueTask>? OnResponseWebSearchCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.web_search_call.searching event arrives.
    /// </summary>
    public Func<ResponseWebSearchCallSearchingEvent, ValueTask>? OnResponseWebSearchCallSearching { get; set; }

    /// <summary>
    ///     Called when a response.web_search_call.completed event arrives.
    /// </summary>
    public Func<ResponseWebSearchCallCompletedEvent, ValueTask>? OnResponseWebSearchCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.reasoning.delta event arrives.
    /// </summary>
    public Func<ResponseReasoningDeltaEvent, ValueTask>? OnResponseReasoningDelta { get; set; }

    /// <summary>
    ///     Called when a response.reasoning.done event arrives.
    /// </summary>
    public Func<ResponseReasoningDoneEvent, ValueTask>? OnResponseReasoningDone { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.part.added event arrives.
    /// </summary>
    public Func<ResponseReasoningSummaryPartAddedEvent, ValueTask>? OnResponseReasoningSummaryPartAdded { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.part.done event arrives.
    /// </summary>
    public Func<ResponseReasoningSummaryPartDoneEvent, ValueTask>? OnResponseReasoningSummaryPartDone { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.text.delta event arrives.
    /// </summary>
    public Func<ResponseReasoningSummaryTextDeltaEvent, ValueTask>? OnResponseReasoningSummaryTextDelta { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.text.done event arrives.
    /// </summary>
    public Func<ResponseReasoningSummaryTextDoneEvent, ValueTask>? OnResponseReasoningSummaryTextDone { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.delta event arrives.
    /// </summary>
    public Func<ResponseReasoningSummaryDeltaEvent, ValueTask>? OnResponseReasoningSummaryDelta { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.done event arrives.
    /// </summary>
    public Func<ResponseReasoningSummaryDoneEvent, ValueTask>? OnResponseReasoningSummaryDone { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseImageGenerationCallInProgressEvent, ValueTask>? OnResponseImageGenerationCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.generating event arrives.
    /// </summary>
    public Func<ResponseImageGenerationCallGeneratingEvent, ValueTask>? OnResponseImageGenerationCallGenerating { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.partial_image event arrives.
    /// </summary>
    public Func<ResponseImageGenerationCallPartialImageEvent, ValueTask>? OnResponseImageGenerationCallPartialImage { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.completed event arrives.
    /// </summary>
    public Func<ResponseImageGenerationCallCompletedEvent, ValueTask>? OnResponseImageGenerationCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.arguments.delta event arrives.
    /// </summary>
    public Func<ResponseMcpCallArgumentsDeltaEvent, ValueTask>? OnResponseMcpCallArgumentsDelta { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.arguments.done event arrives.
    /// </summary>
    public Func<ResponseMcpCallArgumentsDoneEvent, ValueTask>? OnResponseMcpCallArgumentsDone { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseMcpCallInProgressEvent, ValueTask>? OnResponseMcpCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.completed event arrives.
    /// </summary>
    public Func<ResponseMcpCallCompletedEvent, ValueTask>? OnResponseMcpCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.failed event arrives.
    /// </summary>
    public Func<ResponseMcpCallFailedEvent, ValueTask>? OnResponseMcpCallFailed { get; set; }

    /// <summary>
    ///     Called when a response.mcp_list_tools.in_progress event arrives.
    /// </summary>
    public Func<ResponseMcpListToolsInProgressEvent, ValueTask>? OnResponseMcpListToolsInProgress { get; set; }

    /// <summary>
    ///     Called when a response.mcp_list_tools.completed event arrives.
    /// </summary>
    public Func<ResponseMcpListToolsCompletedEvent, ValueTask>? OnResponseMcpListToolsCompleted { get; set; }

    /// <summary>
    ///     Called when a response.mcp_list_tools.failed event arrives.
    /// </summary>
    public Func<ResponseMcpListToolsFailedEvent, ValueTask>? OnResponseMcpListToolsFailed { get; set; }

    /// <summary>
    ///     Called when a response.code_interpreter_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseCodeInterpreterCallInProgressEvent, ValueTask>? OnResponseCodeInterpreterCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.code_interpreter_call.code.delta event arrives.
    /// </summary>
    public Func<ResponseCodeInterpreterCallCodeDeltaEvent, ValueTask>? OnResponseCodeInterpreterCallCodeDelta { get; set; }

    /// <summary>
    ///     Called when a response.code_interpreter_call.code.done event arrives.
    /// </summary>
    public Func<ResponseCodeInterpreterCallCodeDoneEvent, ValueTask>? OnResponseCodeInterpreterCallCodeDone { get; set; }

    /// <summary>
    ///     Called when any response event arrives. This handler receives the event as IResponsesEvent interface.
    /// </summary>
    public Func<IResponsesEvent, ValueTask>? OnEvent { get; set; }

    /// <summary>
    ///     Called when raw server-sent event data arrives. This handler receives the raw SSE data before any parsing.
    /// </summary>
    public Func<ServerSentEvent, ValueTask>? OnSse { get; set; }
} 