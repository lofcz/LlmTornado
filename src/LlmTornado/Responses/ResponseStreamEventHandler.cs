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
    public Func<ResponseEventCreated, ValueTask>? OnResponseCreated { get; set; }

    /// <summary>
    ///     Called when a response.in_progress event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseEventInProgress, ValueTask>? OnResponseInProgress { get; set; }

    /// <summary>
    ///     Called when a response.completed event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseEventCompleted, ValueTask>? OnResponseCompleted { get; set; }

    /// <summary>
    ///     Called when a response.failed event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseEventFailed, ValueTask>? OnResponseFailed { get; set; }

    /// <summary>
    ///     Called when a response.incomplete event arrives. This event inherits from ResponseRequest and adds additional properties.
    /// </summary>
    public Func<ResponseEventIncomplete, ValueTask>? OnResponseIncomplete { get; set; }

    /// <summary>
    ///     Called when a response.queued event arrives.
    /// </summary>
    public Func<ResponseEventQueued, ValueTask>? OnResponseQueued { get; set; }

    /// <summary>
    ///     Called when a response.error event arrives.
    /// </summary>
    public Func<ResponseEventError, ValueTask>? OnResponseError { get; set; }

    /// <summary>
    ///     Called when a response.output_item.added event arrives. This event contains the added output item.
    /// </summary>
    public Func<ResponseEventOutputItemAdded, ValueTask>? OnResponseOutputItemAdded { get; set; }

    /// <summary>
    ///     Called when a response.output_item.done event arrives. This event contains the output item marked done.
    /// </summary>
    public Func<ResponseEventOutputItemDone, ValueTask>? OnResponseOutputItemDone { get; set; }

    /// <summary>
    ///     Called when a response.content_part.added event arrives.
    /// </summary>
    public Func<ResponseEventContentPartAdded, ValueTask>? OnResponseContentPartAdded { get; set; }

    /// <summary>
    ///     Called when a response.content_part.done event arrives.
    /// </summary>
    public Func<ResponseEventContentPartDone, ValueTask>? OnResponseContentPartDone { get; set; }

    /// <summary>
    ///     Called when a response.text.delta event arrives.
    /// </summary>
    public Func<ResponseEventOutputTextDelta, ValueTask>? OnResponseOutputTextDelta { get; set; }

    /// <summary>
    ///     Called when a response.text.done event arrives.
    /// </summary>
    public Func<ResponseEventOutputTextDone, ValueTask>? OnResponseOutputTextDone { get; set; }

    /// <summary>
    ///     Called when a response.text.annotation.added event arrives.
    /// </summary>
    public Func<ResponseEventOutputTextAnnotationAdded, ValueTask>? OnResponseOutputTextAnnotationAdded { get; set; }

    /// <summary>
    ///     Called when a response.refusal.delta event arrives.
    /// </summary>
    public Func<ResponseEventRefusalDelta, ValueTask>? OnResponseRefusalDelta { get; set; }

    /// <summary>
    ///     Called when a response.refusal.done event arrives.
    /// </summary>
    public Func<ResponseEventRefusalDone, ValueTask>? OnResponseRefusalDone { get; set; }

    /// <summary>
    ///     Called when a response.function_call.arguments.delta event arrives.
    /// </summary>
    public Func<ResponseEventFunctionCallArgumentsDelta, ValueTask>? OnResponseFunctionCallArgumentsDelta { get; set; }

    /// <summary>
    ///     Called when a response.function_call.arguments.done event arrives.
    /// </summary>
    public Func<ResponseEventFunctionCallArgumentsDone, ValueTask>? OnResponseFunctionCallArgumentsDone { get; set; }

    /// <summary>
    ///     Called when a response.file_search_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseEventFileSearchCallInProgress, ValueTask>? OnResponseFileSearchCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.file_search_call.searching event arrives.
    /// </summary>
    public Func<ResponseEventFileSearchCallSearching, ValueTask>? OnResponseFileSearchCallSearching { get; set; }

    /// <summary>
    ///     Called when a response.file_search_call.completed event arrives.
    /// </summary>
    public Func<ResponseEventFileSearchCallCompleted, ValueTask>? OnResponseFileSearchCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.web_search_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseEventWebSearchCallInProgress, ValueTask>? OnResponseWebSearchCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.web_search_call.searching event arrives.
    /// </summary>
    public Func<ResponseEventWebSearchCallSearching, ValueTask>? OnResponseWebSearchCallSearching { get; set; }

    /// <summary>
    ///     Called when a response.web_search_call.completed event arrives.
    /// </summary>
    public Func<ResponseEventWebSearchCallCompleted, ValueTask>? OnResponseWebSearchCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.reasoning.delta event arrives.
    /// </summary>
    public Func<ResponseEventReasoningDelta, ValueTask>? OnResponseReasoningDelta { get; set; }

    /// <summary>
    ///     Called when a response.reasoning.done event arrives.
    /// </summary>
    public Func<ResponseEventReasoningDone, ValueTask>? OnResponseReasoningDone { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.part.added event arrives.
    /// </summary>
    public Func<ResponseEventReasoningSummaryPartAdded, ValueTask>? OnResponseReasoningSummaryPartAdded { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.part.done event arrives.
    /// </summary>
    public Func<ResponseEventReasoningSummaryPartDone, ValueTask>? OnResponseReasoningSummaryPartDone { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.text.delta event arrives.
    /// </summary>
    public Func<ResponseEventReasoningSummaryTextDelta, ValueTask>? OnResponseReasoningSummaryTextDelta { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.text.done event arrives.
    /// </summary>
    public Func<ResponseEventReasoningSummaryTextDone, ValueTask>? OnResponseReasoningSummaryTextDone { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.delta event arrives.
    /// </summary>
    public Func<ResponseEventReasoningSummaryDelta, ValueTask>? OnResponseReasoningSummaryDelta { get; set; }

    /// <summary>
    ///     Called when a response.reasoning_summary.done event arrives.
    /// </summary>
    public Func<ResponseEventReasoningSummaryDone, ValueTask>? OnResponseReasoningSummaryDone { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseEventImageGenerationCallInProgress, ValueTask>? OnResponseImageGenerationCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.generating event arrives.
    /// </summary>
    public Func<ResponseEventImageGenerationCallGenerating, ValueTask>? OnResponseImageGenerationCallGenerating { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.partial_image event arrives.
    /// </summary>
    public Func<ResponseEventImageGenerationCallPartialImage, ValueTask>? OnResponseImageGenerationCallPartialImage { get; set; }

    /// <summary>
    ///     Called when a response.image_generation_call.completed event arrives.
    /// </summary>
    public Func<ResponseEventImageGenerationCallCompleted, ValueTask>? OnResponseImageGenerationCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.arguments.delta event arrives.
    /// </summary>
    public Func<ResponseEventMcpCallArgumentsDelta, ValueTask>? OnResponseMcpCallArgumentsDelta { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.arguments.done event arrives.
    /// </summary>
    public Func<ResponseEventMcpCallArgumentsDone, ValueTask>? OnResponseMcpCallArgumentsDone { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseEventMcpCallInProgress, ValueTask>? OnResponseMcpCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.completed event arrives.
    /// </summary>
    public Func<ResponseEventMcpCallCompleted, ValueTask>? OnResponseMcpCallCompleted { get; set; }

    /// <summary>
    ///     Called when a response.mcp_call.failed event arrives.
    /// </summary>
    public Func<ResponseEventMcpCallFailed, ValueTask>? OnResponseMcpCallFailed { get; set; }

    /// <summary>
    ///     Called when a response.mcp_list_tools.in_progress event arrives.
    /// </summary>
    public Func<ResponseEventMcpListToolsInProgress, ValueTask>? OnResponseMcpListToolsInProgress { get; set; }

    /// <summary>
    ///     Called when a response.mcp_list_tools.completed event arrives.
    /// </summary>
    public Func<ResponseEventMcpListToolsCompleted, ValueTask>? OnResponseMcpListToolsCompleted { get; set; }

    /// <summary>
    ///     Called when a response.mcp_list_tools.failed event arrives.
    /// </summary>
    public Func<ResponseEventMcpListToolsFailed, ValueTask>? OnResponseMcpListToolsFailed { get; set; }

    /// <summary>
    ///     Called when a response.code_interpreter_call.in_progress event arrives.
    /// </summary>
    public Func<ResponseEventCodeInterpreterCallInProgress, ValueTask>? OnResponseCodeInterpreterCallInProgress { get; set; }

    /// <summary>
    ///     Called when a response.code_interpreter_call.code.delta event arrives.
    /// </summary>
    public Func<ResponseEventCodeInterpreterCallCodeDelta, ValueTask>? OnResponseCodeInterpreterCallCodeDelta { get; set; }

    /// <summary>
    ///     Called when a response.code_interpreter_call.code.done event arrives.
    /// </summary>
    public Func<ResponseEventCodeInterpreterCallCodeDone, ValueTask>? OnResponseCodeInterpreterCallCodeDone { get; set; }

    /// <summary>
    ///     Called when any response event arrives. This handler receives the event as IResponsesEvent interface.
    /// </summary>
    public Func<IResponseEvent, ValueTask>? OnEvent { get; set; }

    /// <summary>
    ///     Called when raw server-sent event data arrives. This handler receives the raw SSE data before any parsing.
    /// </summary>
    public Func<ServerSentEvent, ValueTask>? OnSse { get; set; }
} 