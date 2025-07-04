using System;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Threads;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LlmTornado.Responses.Events;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
///     This endpoint classifies text against the OpenAI Content Policy
/// </summary>
public class ResponsesEndpoint : EndpointBase
{
    private static readonly FrozenDictionary<string, ResponseEventTypes> EventTypeToEnum = new Dictionary<string, ResponseEventTypes>
    {
        ["response.web_search_call.searching"] = ResponseEventTypes.ResponseWebSearchCallSearching,
        ["response.web_search_call.in_progress"] = ResponseEventTypes.ResponseWebSearchCallInProgress,
        ["response.web_search_call.completed"] = ResponseEventTypes.ResponseWebSearchCallCompleted,
        ["response.refusal.done"] = ResponseEventTypes.ResponseRefusalDone,
        ["response.refusal.delta"] = ResponseEventTypes.ResponseRefusalDelta,
        ["response.reasoning_summary_text.done"] = ResponseEventTypes.ResponseReasoningSummaryTextDone,
        ["response.reasoning_summary_text.delta"] = ResponseEventTypes.ResponseReasoningSummaryTextDelta,
        ["response.reasoning_summary_part.done"] = ResponseEventTypes.ResponseReasoningSummaryPartDone,
        ["response.reasoning_summary_part.added"] = ResponseEventTypes.ResponseReasoningSummaryPartAdded,
        ["response.reasoning_summary.done"] = ResponseEventTypes.ResponseReasoningSummaryDone,
        ["response.reasoning_summary.delta"] = ResponseEventTypes.ResponseReasoningSummaryDelta,
        ["response.reasoning.done"] = ResponseEventTypes.ResponseReasoningDone,
        ["response.reasoning.delta"] = ResponseEventTypes.ResponseReasoningDelta,
        ["response.queued"] = ResponseEventTypes.ResponseQueued,
        ["response.output_text.done"] = ResponseEventTypes.ResponseOutputTextDone,
        ["response.output_text.delta"] = ResponseEventTypes.ResponseOutputTextDelta,
        ["response.output_text_annotation.added"] = ResponseEventTypes.ResponseOutputTextAnnotationAdded,
        ["response.output_item.added"] = ResponseEventTypes.ResponseOutputItemAdded,
        ["response.output_item.done"] = ResponseEventTypes.ResponseOutputItemDone,
        ["response.content_part.added"] = ResponseEventTypes.ResponseContentPartAdded,
        ["response.content_part.done"] = ResponseEventTypes.ResponseContentPartDone,
        ["response.mcp_list_tools.in_progress"] = ResponseEventTypes.ResponseMcpListToolsInProgress,
        ["response.mcp_list_tools.failed"] = ResponseEventTypes.ResponseMcpListToolsFailed,
        ["response.mcp_list_tools.completed"] = ResponseEventTypes.ResponseMcpListToolsCompleted,
        ["response.mcp_call.in_progress"] = ResponseEventTypes.ResponseMcpCallInProgress,
        ["response.mcp_call.failed"] = ResponseEventTypes.ResponseMcpCallFailed,
        ["response.mcp_call.completed"] = ResponseEventTypes.ResponseMcpCallCompleted,
        ["response.mcp_call.arguments.done"] = ResponseEventTypes.ResponseMcpCallArgumentsDone,
        ["response.mcp_call.arguments.delta"] = ResponseEventTypes.ResponseMcpCallArgumentsDelta,
        ["response.in_progress"] = ResponseEventTypes.ResponseInProgress,
        ["response.image_generation_call.partial_image"] = ResponseEventTypes.ResponseImageGenerationCallPartialImage,
        ["response.image_generation_call.in_progress"] = ResponseEventTypes.ResponseImageGenerationCallInProgress,
        ["response.image_generation_call.generating"] = ResponseEventTypes.ResponseImageGenerationCallGenerating,
        ["response.image_generation_call.completed"] = ResponseEventTypes.ResponseImageGenerationCallCompleted,
        ["response.function_call_arguments.done"] = ResponseEventTypes.ResponseFunctionCallArgumentsDone,
        ["response.function_call_arguments.delta"] = ResponseEventTypes.ResponseFunctionCallArgumentsDelta,
        ["response.file_search_call.searching"] = ResponseEventTypes.ResponseFileSearchCallSearching,
        ["response.file_search_call.in_progress"] = ResponseEventTypes.ResponseFileSearchCallInProgress,
        ["response.file_search_call.completed"] = ResponseEventTypes.ResponseFileSearchCallCompleted,
        ["error"] = ResponseEventTypes.ResponseError,
        ["response.code_interpreter_call.in_progress"] = ResponseEventTypes.ResponseCodeInterpreterCallInProgress,
        ["response.code_interpreter_call_code.done"] = ResponseEventTypes.ResponseCodeInterpreterCallCodeDone,
        ["response.code_interpreter_call_code.delta"] = ResponseEventTypes.ResponseCodeInterpreterCallCodeDelta,
        ["response.failed"] = ResponseEventTypes.ResponseFailed,
        ["response.completed"] = ResponseEventTypes.ResponseCompleted,
        ["response.created"] = ResponseEventTypes.ResponseCreated,
        ["response.incomplete"] = ResponseEventTypes.ResponseIncomplete,
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<ResponseEventTypes, Type> EventTypeToType = new Dictionary<ResponseEventTypes, Type>
    {
        [ResponseEventTypes.ResponseWebSearchCallSearching] = typeof(ResponseWebSearchCallSearchingEvent),
        [ResponseEventTypes.ResponseWebSearchCallInProgress] = typeof(ResponseWebSearchCallInProgressEvent),
        [ResponseEventTypes.ResponseWebSearchCallCompleted] = typeof(ResponseWebSearchCallCompletedEvent),
        [ResponseEventTypes.ResponseRefusalDone] = typeof(ResponseRefusalDoneEvent),
        [ResponseEventTypes.ResponseRefusalDelta] = typeof(ResponseRefusalDeltaEvent),
        [ResponseEventTypes.ResponseReasoningSummaryTextDone] = typeof(ResponseReasoningSummaryTextDoneEvent),
        [ResponseEventTypes.ResponseReasoningSummaryTextDelta] = typeof(ResponseReasoningSummaryTextDeltaEvent),
        [ResponseEventTypes.ResponseReasoningSummaryPartDone] = typeof(ResponseReasoningSummaryPartDoneEvent),
        [ResponseEventTypes.ResponseReasoningSummaryPartAdded] = typeof(ResponseReasoningSummaryPartAddedEvent),
        [ResponseEventTypes.ResponseReasoningSummaryDone] = typeof(ResponseReasoningSummaryDoneEvent),
        [ResponseEventTypes.ResponseReasoningSummaryDelta] = typeof(ResponseReasoningSummaryDeltaEvent),
        [ResponseEventTypes.ResponseReasoningDone] = typeof(ResponseReasoningDoneEvent),
        [ResponseEventTypes.ResponseReasoningDelta] = typeof(ResponseReasoningDeltaEvent),
        [ResponseEventTypes.ResponseQueued] = typeof(ResponseQueuedEvent),
        [ResponseEventTypes.ResponseOutputTextDone] = typeof(ResponseOutputTextDoneEvent),
        [ResponseEventTypes.ResponseOutputTextDelta] = typeof(ResponseOutputTextDeltaEvent),
        [ResponseEventTypes.ResponseOutputTextAnnotationAdded] = typeof(ResponseOutputTextAnnotationAddedEvent),
        [ResponseEventTypes.ResponseOutputItemAdded] = typeof(ResponseOutputItemAddedEvent),
        [ResponseEventTypes.ResponseOutputItemDone] = typeof(ResponseOutputItemDoneEvent),
        [ResponseEventTypes.ResponseContentPartAdded] = typeof(ResponseContentPartAddedEvent),
        [ResponseEventTypes.ResponseContentPartDone] = typeof(ResponseContentPartDoneEvent),
        [ResponseEventTypes.ResponseMcpListToolsInProgress] = typeof(ResponseMcpListToolsInProgressEvent),
        [ResponseEventTypes.ResponseMcpListToolsFailed] = typeof(ResponseMcpListToolsFailedEvent),
        [ResponseEventTypes.ResponseMcpListToolsCompleted] = typeof(ResponseMcpListToolsCompletedEvent),
        [ResponseEventTypes.ResponseMcpCallInProgress] = typeof(ResponseMcpCallInProgressEvent),
        [ResponseEventTypes.ResponseMcpCallFailed] = typeof(ResponseMcpCallFailedEvent),
        [ResponseEventTypes.ResponseMcpCallCompleted] = typeof(ResponseMcpCallCompletedEvent),
        [ResponseEventTypes.ResponseMcpCallArgumentsDone] = typeof(ResponseMcpCallArgumentsDoneEvent),
        [ResponseEventTypes.ResponseMcpCallArgumentsDelta] = typeof(ResponseMcpCallArgumentsDeltaEvent),
        [ResponseEventTypes.ResponseInProgress] = typeof(ResponseInProgressEvent),
        [ResponseEventTypes.ResponseImageGenerationCallPartialImage] = typeof(ResponseImageGenerationCallPartialImageEvent),
        [ResponseEventTypes.ResponseImageGenerationCallInProgress] = typeof(ResponseImageGenerationCallInProgressEvent),
        [ResponseEventTypes.ResponseImageGenerationCallGenerating] = typeof(ResponseImageGenerationCallGeneratingEvent),
        [ResponseEventTypes.ResponseImageGenerationCallCompleted] = typeof(ResponseImageGenerationCallCompletedEvent),
        [ResponseEventTypes.ResponseFunctionCallArgumentsDone] = typeof(ResponseFunctionCallArgumentsDoneEvent),
        [ResponseEventTypes.ResponseFunctionCallArgumentsDelta] = typeof(ResponseFunctionCallArgumentsDeltaEvent),
        [ResponseEventTypes.ResponseFileSearchCallSearching] = typeof(ResponseFileSearchCallSearchingEvent),
        [ResponseEventTypes.ResponseFileSearchCallInProgress] = typeof(ResponseFileSearchCallInProgressEvent),
        [ResponseEventTypes.ResponseFileSearchCallCompleted] = typeof(ResponseFileSearchCallCompletedEvent),
        [ResponseEventTypes.ResponseError] = typeof(ResponseErrorEvent),
        [ResponseEventTypes.ResponseCodeInterpreterCallInProgress] = typeof(ResponseCodeInterpreterCallInProgressEvent),
        [ResponseEventTypes.ResponseCodeInterpreterCallCodeDone] = typeof(ResponseCodeInterpreterCallCodeDoneEvent),
        [ResponseEventTypes.ResponseCodeInterpreterCallCodeDelta] = typeof(ResponseCodeInterpreterCallCodeDeltaEvent),
        [ResponseEventTypes.ResponseFailed] = typeof(ResponseFailedEvent),
        [ResponseEventTypes.ResponseCompleted] = typeof(ResponseCompletedEvent),
        [ResponseEventTypes.ResponseCreated] = typeof(ResponseCreatedEvent),
        [ResponseEventTypes.ResponseIncomplete] = typeof(ResponseIncompleteEvent),
    }.ToFrozenDictionary();

    private static IResponsesEvent? DeserializeEvent(string data, ResponseEventTypes eventType)
    {
        if (EventTypeToType.TryGetValue(eventType, out Type? type))
        {
            return (IResponsesEvent?)JsonConvert.DeserializeObject(data, type);
        }
        
        return null;
    }

    private static T DeserializeEvent<T>(string data) where T : class
    {
        return data.JsonDecode<T>()!;
    }

    internal ResponsesEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  For example, "completions".
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Responses;

    /// <summary>
    /// Creates a responses API request.
    /// </summary>
    /// <param name="request">The request</param>
    public async Task<ResponseResult?> CreateResponse(ResponseRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        TornadoRequestContent requestBody = request.Serialize(provider);
        
        HttpCallResult<ResponseResult> result = await HttpPost<ResponseResult>(provider, Endpoint, url: requestBody.Url, postData: requestBody.Body, model: request.Model, ct: request.CancellationToken).ConfigureAwait(false);
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }

        return result.Data;
    }

    /// <summary>
    ///     Stream Realtime API events as they arrive, using the provided event handler to process each event type.
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <param name="eventsHandler">Optional event handler to process streaming events.</param>
    /// <param name="token">Optional cancellation token.</param>
    public async Task StreamResponseRich(ResponseRequest request, ResponseStreamEventHandler? eventsHandler = null, CancellationToken token = default)
    {
        bool? streamOption = request.Stream;
        request.Stream = true;
        IEndpointProvider provider = Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        TornadoRequestContent requestBody = request.Serialize(provider);
        request.Stream = streamOption;
        
        await using TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(provider, Endpoint, requestBody.Url, queryParams: null, HttpVerbs.Post, requestBody.Body, request.Model, token);

        if (tornadoStreamRequest.Exception is not null)
        {
            throw tornadoStreamRequest.Exception;
        }

        if (tornadoStreamRequest.StreamReader is not null)
        {
            await foreach (ServerSentEvent runStreamEvent in provider.InboundStream(tornadoStreamRequest.StreamReader).WithCancellation(token))
            {
                if (eventsHandler is null)
                {
                    continue;
                }

                // Call OnSse first for debugging purposes
                if (eventsHandler.OnSse != null)
                {
                    await eventsHandler.OnSse(runStreamEvent);
                }

                string type = runStreamEvent.EventType;
                if (EventTypeToEnum.TryGetValue(type, out ResponseEventTypes eventType))
                {
                    // Call generic handler first
                    if (eventsHandler.OnEvent != null)
                    {
                        // We need to deserialize to the correct type based on the enum
                        var evt = DeserializeEvent(runStreamEvent.Data, eventType);
                        if (evt != null)
                            await eventsHandler.OnEvent(evt);
                    }
                    
                    // Call specific handler based on event type enum
                    switch (eventType)
                    {
                        case ResponseEventTypes.ResponseCreated:
                            if (eventsHandler.OnResponseCreated != null)
                                await eventsHandler.OnResponseCreated(DeserializeEvent<ResponseCreatedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseInProgress:
                            if (eventsHandler.OnResponseInProgress != null)
                                await eventsHandler.OnResponseInProgress(DeserializeEvent<ResponseInProgressEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCompleted:
                            if (eventsHandler.OnResponseCompleted != null)
                                await eventsHandler.OnResponseCompleted(DeserializeEvent<ResponseCompletedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFailed:
                            if (eventsHandler.OnResponseFailed != null)
                                await eventsHandler.OnResponseFailed(DeserializeEvent<ResponseFailedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseIncomplete:
                            if (eventsHandler.OnResponseIncomplete != null)
                                await eventsHandler.OnResponseIncomplete(DeserializeEvent<ResponseIncompleteEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseQueued:
                            if (eventsHandler.OnResponseQueued != null)
                                await eventsHandler.OnResponseQueued(DeserializeEvent<ResponseQueuedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseError:
                            if (eventsHandler.OnResponseError != null)
                                await eventsHandler.OnResponseError(DeserializeEvent<ResponseErrorEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputItemAdded:
                            if (eventsHandler.OnResponseOutputItemAdded != null)
                                await eventsHandler.OnResponseOutputItemAdded(DeserializeEvent<ResponseOutputItemAddedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputItemDone:
                            if (eventsHandler.OnResponseOutputItemDone != null)
                                await eventsHandler.OnResponseOutputItemDone(DeserializeEvent<ResponseOutputItemDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseContentPartAdded:
                            if (eventsHandler.OnResponseContentPartAdded != null)
                                await eventsHandler.OnResponseContentPartAdded(DeserializeEvent<ResponseContentPartAddedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseContentPartDone:
                            if (eventsHandler.OnResponseContentPartDone != null)
                                await eventsHandler.OnResponseContentPartDone(DeserializeEvent<ResponseContentPartDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputTextDelta:
                            if (eventsHandler.OnResponseOutputTextDelta != null)
                                await eventsHandler.OnResponseOutputTextDelta(DeserializeEvent<ResponseOutputTextDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputTextDone:
                            if (eventsHandler.OnResponseOutputTextDone != null)
                                await eventsHandler.OnResponseOutputTextDone(DeserializeEvent<ResponseOutputTextDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputTextAnnotationAdded:
                            if (eventsHandler.OnResponseOutputTextAnnotationAdded != null)
                                await eventsHandler.OnResponseOutputTextAnnotationAdded(DeserializeEvent<ResponseOutputTextAnnotationAddedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseRefusalDelta:
                            if (eventsHandler.OnResponseRefusalDelta != null)
                                await eventsHandler.OnResponseRefusalDelta(DeserializeEvent<ResponseRefusalDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseRefusalDone:
                            if (eventsHandler.OnResponseRefusalDone != null)
                                await eventsHandler.OnResponseRefusalDone(DeserializeEvent<ResponseRefusalDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFunctionCallArgumentsDelta:
                            if (eventsHandler.OnResponseFunctionCallArgumentsDelta != null)
                                await eventsHandler.OnResponseFunctionCallArgumentsDelta(DeserializeEvent<ResponseFunctionCallArgumentsDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFunctionCallArgumentsDone:
                            if (eventsHandler.OnResponseFunctionCallArgumentsDone != null)
                                await eventsHandler.OnResponseFunctionCallArgumentsDone(DeserializeEvent<ResponseFunctionCallArgumentsDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFileSearchCallInProgress:
                            if (eventsHandler.OnResponseFileSearchCallInProgress != null)
                                await eventsHandler.OnResponseFileSearchCallInProgress(DeserializeEvent<ResponseFileSearchCallInProgressEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFileSearchCallSearching:
                            if (eventsHandler.OnResponseFileSearchCallSearching != null)
                                await eventsHandler.OnResponseFileSearchCallSearching(DeserializeEvent<ResponseFileSearchCallSearchingEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFileSearchCallCompleted:
                            if (eventsHandler.OnResponseFileSearchCallCompleted != null)
                                await eventsHandler.OnResponseFileSearchCallCompleted(DeserializeEvent<ResponseFileSearchCallCompletedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseWebSearchCallInProgress:
                            if (eventsHandler.OnResponseWebSearchCallInProgress != null)
                                await eventsHandler.OnResponseWebSearchCallInProgress(DeserializeEvent<ResponseWebSearchCallInProgressEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseWebSearchCallSearching:
                            if (eventsHandler.OnResponseWebSearchCallSearching != null)
                                await eventsHandler.OnResponseWebSearchCallSearching(DeserializeEvent<ResponseWebSearchCallSearchingEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseWebSearchCallCompleted:
                            if (eventsHandler.OnResponseWebSearchCallCompleted != null)
                                await eventsHandler.OnResponseWebSearchCallCompleted(DeserializeEvent<ResponseWebSearchCallCompletedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningDelta:
                            if (eventsHandler.OnResponseReasoningDelta != null)
                                await eventsHandler.OnResponseReasoningDelta(DeserializeEvent<ResponseReasoningDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningDone:
                            if (eventsHandler.OnResponseReasoningDone != null)
                                await eventsHandler.OnResponseReasoningDone(DeserializeEvent<ResponseReasoningDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryPartAdded:
                            if (eventsHandler.OnResponseReasoningSummaryPartAdded != null)
                                await eventsHandler.OnResponseReasoningSummaryPartAdded(DeserializeEvent<ResponseReasoningSummaryPartAddedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryPartDone:
                            if (eventsHandler.OnResponseReasoningSummaryPartDone != null)
                                await eventsHandler.OnResponseReasoningSummaryPartDone(DeserializeEvent<ResponseReasoningSummaryPartDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryTextDelta:
                            if (eventsHandler.OnResponseReasoningSummaryTextDelta != null)
                                await eventsHandler.OnResponseReasoningSummaryTextDelta(DeserializeEvent<ResponseReasoningSummaryTextDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryTextDone:
                            if (eventsHandler.OnResponseReasoningSummaryTextDone != null)
                                await eventsHandler.OnResponseReasoningSummaryTextDone(DeserializeEvent<ResponseReasoningSummaryTextDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryDelta:
                            if (eventsHandler.OnResponseReasoningSummaryDelta != null)
                                await eventsHandler.OnResponseReasoningSummaryDelta(DeserializeEvent<ResponseReasoningSummaryDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryDone:
                            if (eventsHandler.OnResponseReasoningSummaryDone != null)
                                await eventsHandler.OnResponseReasoningSummaryDone(DeserializeEvent<ResponseReasoningSummaryDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallInProgress:
                            if (eventsHandler.OnResponseImageGenerationCallInProgress != null)
                                await eventsHandler.OnResponseImageGenerationCallInProgress(DeserializeEvent<ResponseImageGenerationCallInProgressEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallGenerating:
                            if (eventsHandler.OnResponseImageGenerationCallGenerating != null)
                                await eventsHandler.OnResponseImageGenerationCallGenerating(DeserializeEvent<ResponseImageGenerationCallGeneratingEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallPartialImage:
                            if (eventsHandler.OnResponseImageGenerationCallPartialImage != null)
                                await eventsHandler.OnResponseImageGenerationCallPartialImage(DeserializeEvent<ResponseImageGenerationCallPartialImageEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallCompleted:
                            if (eventsHandler.OnResponseImageGenerationCallCompleted != null)
                                await eventsHandler.OnResponseImageGenerationCallCompleted(DeserializeEvent<ResponseImageGenerationCallCompletedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallArgumentsDelta:
                            if (eventsHandler.OnResponseMcpCallArgumentsDelta != null)
                                await eventsHandler.OnResponseMcpCallArgumentsDelta(DeserializeEvent<ResponseMcpCallArgumentsDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallArgumentsDone:
                            if (eventsHandler.OnResponseMcpCallArgumentsDone != null)
                                await eventsHandler.OnResponseMcpCallArgumentsDone(DeserializeEvent<ResponseMcpCallArgumentsDoneEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallInProgress:
                            if (eventsHandler.OnResponseMcpCallInProgress != null)
                                await eventsHandler.OnResponseMcpCallInProgress(DeserializeEvent<ResponseMcpCallInProgressEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallCompleted:
                            if (eventsHandler.OnResponseMcpCallCompleted != null)
                                await eventsHandler.OnResponseMcpCallCompleted(DeserializeEvent<ResponseMcpCallCompletedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallFailed:
                            if (eventsHandler.OnResponseMcpCallFailed != null)
                                await eventsHandler.OnResponseMcpCallFailed(DeserializeEvent<ResponseMcpCallFailedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpListToolsInProgress:
                            if (eventsHandler.OnResponseMcpListToolsInProgress != null)
                                await eventsHandler.OnResponseMcpListToolsInProgress(DeserializeEvent<ResponseMcpListToolsInProgressEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpListToolsCompleted:
                            if (eventsHandler.OnResponseMcpListToolsCompleted != null)
                                await eventsHandler.OnResponseMcpListToolsCompleted(DeserializeEvent<ResponseMcpListToolsCompletedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpListToolsFailed:
                            if (eventsHandler.OnResponseMcpListToolsFailed != null)
                                await eventsHandler.OnResponseMcpListToolsFailed(DeserializeEvent<ResponseMcpListToolsFailedEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCodeInterpreterCallInProgress:
                            if (eventsHandler.OnResponseCodeInterpreterCallInProgress != null)
                                await eventsHandler.OnResponseCodeInterpreterCallInProgress(DeserializeEvent<ResponseCodeInterpreterCallInProgressEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCodeInterpreterCallCodeDelta:
                            if (eventsHandler.OnResponseCodeInterpreterCallCodeDelta != null)
                                await eventsHandler.OnResponseCodeInterpreterCallCodeDelta(DeserializeEvent<ResponseCodeInterpreterCallCodeDeltaEvent>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCodeInterpreterCallCodeDone:
                            if (eventsHandler.OnResponseCodeInterpreterCallCodeDone != null)
                                await eventsHandler.OnResponseCodeInterpreterCallCodeDone(DeserializeEvent<ResponseCodeInterpreterCallCodeDoneEvent>(runStreamEvent.Data));
                            break;
                    }
                }
            }
        }
    }
}
