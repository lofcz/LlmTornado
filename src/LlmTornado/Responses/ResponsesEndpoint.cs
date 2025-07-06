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
/// OpenAI's most advanced interface for generating model responses. Supports text and image inputs, and text outputs. Create stateful interactions with the model, using the output of previous responses as input. Extend the model's capabilities with built-in tools for file search, web search, computer use, and more. Allow the model access to external systems and data using function calling.
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
        [ResponseEventTypes.ResponseWebSearchCallSearching] = typeof(ResponseEventWebSearchCallSearching),
        [ResponseEventTypes.ResponseWebSearchCallInProgress] = typeof(ResponseEventWebSearchCallInProgress),
        [ResponseEventTypes.ResponseWebSearchCallCompleted] = typeof(ResponseEventWebSearchCallCompleted),
        [ResponseEventTypes.ResponseRefusalDone] = typeof(ResponseEventRefusalDone),
        [ResponseEventTypes.ResponseRefusalDelta] = typeof(ResponseEventRefusalDelta),
        [ResponseEventTypes.ResponseReasoningSummaryTextDone] = typeof(ResponseEventReasoningSummaryTextDone),
        [ResponseEventTypes.ResponseReasoningSummaryTextDelta] = typeof(ResponseEventReasoningSummaryTextDelta),
        [ResponseEventTypes.ResponseReasoningSummaryPartDone] = typeof(ResponseEventReasoningSummaryPartDone),
        [ResponseEventTypes.ResponseReasoningSummaryPartAdded] = typeof(ResponseEventReasoningSummaryPartAdded),
        [ResponseEventTypes.ResponseReasoningSummaryDone] = typeof(ResponseEventReasoningSummaryDone),
        [ResponseEventTypes.ResponseReasoningSummaryDelta] = typeof(ResponseEventReasoningSummaryDelta),
        [ResponseEventTypes.ResponseReasoningDone] = typeof(ResponseEventReasoningDone),
        [ResponseEventTypes.ResponseReasoningDelta] = typeof(ResponseEventReasoningDelta),
        [ResponseEventTypes.ResponseQueued] = typeof(ResponseEventQueued),
        [ResponseEventTypes.ResponseOutputTextDone] = typeof(ResponseEventOutputTextDone),
        [ResponseEventTypes.ResponseOutputTextDelta] = typeof(ResponseEventOutputTextDelta),
        [ResponseEventTypes.ResponseOutputTextAnnotationAdded] = typeof(ResponseEventOutputTextAnnotationAdded),
        [ResponseEventTypes.ResponseOutputItemAdded] = typeof(ResponseEventOutputItemAdded),
        [ResponseEventTypes.ResponseOutputItemDone] = typeof(ResponseEventOutputItemDone),
        [ResponseEventTypes.ResponseContentPartAdded] = typeof(ResponseEventContentPartAdded),
        [ResponseEventTypes.ResponseContentPartDone] = typeof(ResponseEventContentPartDone),
        [ResponseEventTypes.ResponseMcpListToolsInProgress] = typeof(ResponseEventMcpListToolsInProgress),
        [ResponseEventTypes.ResponseMcpListToolsFailed] = typeof(ResponseEventMcpListToolsFailed),
        [ResponseEventTypes.ResponseMcpListToolsCompleted] = typeof(ResponseEventMcpListToolsCompleted),
        [ResponseEventTypes.ResponseMcpCallInProgress] = typeof(ResponseEventMcpCallInProgress),
        [ResponseEventTypes.ResponseMcpCallFailed] = typeof(ResponseEventMcpCallFailed),
        [ResponseEventTypes.ResponseMcpCallCompleted] = typeof(ResponseEventMcpCallCompleted),
        [ResponseEventTypes.ResponseMcpCallArgumentsDone] = typeof(ResponseEventMcpCallArgumentsDone),
        [ResponseEventTypes.ResponseMcpCallArgumentsDelta] = typeof(ResponseEventMcpCallArgumentsDelta),
        [ResponseEventTypes.ResponseInProgress] = typeof(ResponseEventInProgress),
        [ResponseEventTypes.ResponseImageGenerationCallPartialImage] = typeof(ResponseEventImageGenerationCallPartialImage),
        [ResponseEventTypes.ResponseImageGenerationCallInProgress] = typeof(ResponseEventImageGenerationCallInProgress),
        [ResponseEventTypes.ResponseImageGenerationCallGenerating] = typeof(ResponseEventImageGenerationCallGenerating),
        [ResponseEventTypes.ResponseImageGenerationCallCompleted] = typeof(ResponseEventImageGenerationCallCompleted),
        [ResponseEventTypes.ResponseFunctionCallArgumentsDone] = typeof(ResponseEventFunctionCallArgumentsDone),
        [ResponseEventTypes.ResponseFunctionCallArgumentsDelta] = typeof(ResponseEventFunctionCallArgumentsDelta),
        [ResponseEventTypes.ResponseFileSearchCallSearching] = typeof(ResponseEventFileSearchCallSearching),
        [ResponseEventTypes.ResponseFileSearchCallInProgress] = typeof(ResponseEventFileSearchCallInProgress),
        [ResponseEventTypes.ResponseFileSearchCallCompleted] = typeof(ResponseEventFileSearchCallCompleted),
        [ResponseEventTypes.ResponseError] = typeof(ResponseEventError),
        [ResponseEventTypes.ResponseCodeInterpreterCallInProgress] = typeof(ResponseEventCodeInterpreterCallInProgress),
        [ResponseEventTypes.ResponseCodeInterpreterCallCodeDone] = typeof(ResponseEventCodeInterpreterCallCodeDone),
        [ResponseEventTypes.ResponseCodeInterpreterCallCodeDelta] = typeof(ResponseEventCodeInterpreterCallCodeDelta),
        [ResponseEventTypes.ResponseFailed] = typeof(ResponseEventFailed),
        [ResponseEventTypes.ResponseCompleted] = typeof(ResponseEventCompleted),
        [ResponseEventTypes.ResponseCreated] = typeof(ResponseEventCreated),
        [ResponseEventTypes.ResponseIncomplete] = typeof(ResponseEventIncomplete),
    }.ToFrozenDictionary();

    private static IResponseEvent? DeserializeEvent(string data, ResponseEventTypes eventType)
    {
        if (EventTypeToType.TryGetValue(eventType, out Type? type))
        {
            return (IResponseEvent?)JsonConvert.DeserializeObject(data, type);
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
    /// Retrieves a model response with the given ID.
    /// </summary>
    public async Task<ResponseResult?> GetResponse(string responseId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return (await HttpGet<ResponseResult>(provider, Endpoint, url: GetUrl(provider, $"/{responseId}"), ct: cancellationToken).ConfigureAwait(false)).Data;
    }
    
    /// <summary>
    /// Deletes a model response with the given ID.
    /// </summary>
    public async Task<ResponseDeleted?> DeleteResponse(string responseId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        HttpCallResult<ResponseDeleted> data = await HttpDelete<ResponseDeleted>(provider, Endpoint, url: GetUrl(provider, $"/{responseId}"), ct: cancellationToken).ConfigureAwait(false);
        return data.Data;
    }
    
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
    /// Creates a new session.
    /// </summary>
    public ResponsesSession CreateSession(ResponseRequest request, ResponseStreamEventHandler eventsHandler)
    {
        return new ResponsesSession
        {
            Request = request,
            Endpoint = this,
            EventsHandler = eventsHandler
        };
    }
    
    /// <summary>
    /// Creates a new session.
    /// </summary>
    public ResponsesSession CreateSession()
    {
        return new ResponsesSession
        {
            Request = null,
            Endpoint = this,
            EventsHandler = null
        };
    }
    
    /// <summary>
    /// Creates a new session.
    /// </summary>
    public ResponsesSession CreateSession(ResponseStreamEventHandler eventsHandler)
    {
        return new ResponsesSession
        {
            Request = null,
            Endpoint = this,
            EventsHandler = eventsHandler
        };
    }

    /// <summary>
    ///     Stream Realtime API events as they arrive, using the provided event handler to process each event type.
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <param name="eventsHandler">Optional event handler to process streaming events.</param>
    /// <param name="token">Optional cancellation token.</param>
    public async Task StreamResponseRich(ResponseRequest request, ResponseStreamEventHandler? eventsHandler = null, CancellationToken token = default)
    {
        await StreamResponseRichInternal(request, null, eventsHandler, token).ConfigureAwait(false);
    }
    
    internal async Task StreamResponseRichInternal(ResponseRequest request, ResponsesSession? session = null, ResponseStreamEventHandler? eventsHandler = null, CancellationToken token = default)
    {
        bool? streamOption = request.Stream;
        request.Stream = true;
        IEndpointProvider provider = Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        TornadoRequestContent requestBody = request.Serialize(provider);
        request.Stream = streamOption;

        await using TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(provider, Endpoint, requestBody.Url, queryParams: null, HttpVerbs.Post, requestBody.Body, request.Model, token).ConfigureAwait(false);

        if (tornadoStreamRequest.Exception is not null)
        {
            throw tornadoStreamRequest.Exception;
        }

        if (tornadoStreamRequest.StreamReader is not null)
        {
            await foreach (ServerSentEvent runStreamEvent in provider.InboundStream(tornadoStreamRequest.StreamReader).WithCancellation(token).ConfigureAwait(false))
            {
                if (eventsHandler is null)
                {
                    continue;
                }
                
                if (eventsHandler.OnSse != null)
                {
                    await eventsHandler.OnSse(runStreamEvent);
                }

                string type = runStreamEvent.EventType;
                
                if (EventTypeToEnum.TryGetValue(type, out ResponseEventTypes eventType))
                {
                    if (eventsHandler.OnEvent is not null || eventType is ResponseEventTypes.ResponseCompleted or ResponseEventTypes.ResponseCreated)
                    {
                        IResponseEvent? evt = DeserializeEvent(runStreamEvent.Data, eventType);
                        
                        if (eventType is ResponseEventTypes.ResponseCreated)
                        {
                            if (evt is ResponseEventCreated created)
                            {
                                if (session is not null)
                                {
                                    session.CurrentResponse = created.Response;
                                }
                            }
                        }
                        
                        if (evt is not null && eventsHandler.OnEvent is not null)
                        {
                            await eventsHandler.OnEvent(evt);
                        }
                    }
                    
                    // Call specific handler based on event type enum
                    switch (eventType)
                    {
                        case ResponseEventTypes.ResponseCreated:
                            if (eventsHandler.OnResponseCreated != null)
                                await eventsHandler.OnResponseCreated(DeserializeEvent<ResponseEventCreated>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseInProgress:
                            if (eventsHandler.OnResponseInProgress != null)
                                await eventsHandler.OnResponseInProgress(DeserializeEvent<ResponseEventInProgress>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCompleted:
                            if (eventsHandler.OnResponseCompleted != null)
                                await eventsHandler.OnResponseCompleted(DeserializeEvent<ResponseEventCompleted>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFailed:
                            if (eventsHandler.OnResponseFailed != null)
                                await eventsHandler.OnResponseFailed(DeserializeEvent<ResponseEventFailed>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseIncomplete:
                            if (eventsHandler.OnResponseIncomplete != null)
                                await eventsHandler.OnResponseIncomplete(DeserializeEvent<ResponseEventIncomplete>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseQueued:
                            if (eventsHandler.OnResponseQueued != null)
                                await eventsHandler.OnResponseQueued(DeserializeEvent<ResponseEventQueued>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseError:
                            if (eventsHandler.OnResponseError != null)
                                await eventsHandler.OnResponseError(DeserializeEvent<ResponseEventError>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputItemAdded:
                            if (eventsHandler.OnResponseOutputItemAdded != null)
                                await eventsHandler.OnResponseOutputItemAdded(DeserializeEvent<ResponseEventOutputItemAdded>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputItemDone:
                            if (eventsHandler.OnResponseOutputItemDone != null)
                                await eventsHandler.OnResponseOutputItemDone(DeserializeEvent<ResponseEventOutputItemDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseContentPartAdded:
                            if (eventsHandler.OnResponseContentPartAdded != null)
                                await eventsHandler.OnResponseContentPartAdded(DeserializeEvent<ResponseEventContentPartAdded>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseContentPartDone:
                            if (eventsHandler.OnResponseContentPartDone != null)
                                await eventsHandler.OnResponseContentPartDone(DeserializeEvent<ResponseEventContentPartDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputTextDelta:
                            if (eventsHandler.OnResponseOutputTextDelta != null)
                                await eventsHandler.OnResponseOutputTextDelta(DeserializeEvent<ResponseEventOutputTextDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputTextDone:
                            if (eventsHandler.OnResponseOutputTextDone != null)
                                await eventsHandler.OnResponseOutputTextDone(DeserializeEvent<ResponseEventOutputTextDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseOutputTextAnnotationAdded:
                            if (eventsHandler.OnResponseOutputTextAnnotationAdded != null)
                                await eventsHandler.OnResponseOutputTextAnnotationAdded(DeserializeEvent<ResponseEventOutputTextAnnotationAdded>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseRefusalDelta:
                            if (eventsHandler.OnResponseRefusalDelta != null)
                                await eventsHandler.OnResponseRefusalDelta(DeserializeEvent<ResponseEventRefusalDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseRefusalDone:
                            if (eventsHandler.OnResponseRefusalDone != null)
                                await eventsHandler.OnResponseRefusalDone(DeserializeEvent<ResponseEventRefusalDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFunctionCallArgumentsDelta:
                            if (eventsHandler.OnResponseFunctionCallArgumentsDelta != null)
                                await eventsHandler.OnResponseFunctionCallArgumentsDelta(DeserializeEvent<ResponseEventFunctionCallArgumentsDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFunctionCallArgumentsDone:
                            if (eventsHandler.OnResponseFunctionCallArgumentsDone != null)
                                await eventsHandler.OnResponseFunctionCallArgumentsDone(DeserializeEvent<ResponseEventFunctionCallArgumentsDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFileSearchCallInProgress:
                            if (eventsHandler.OnResponseFileSearchCallInProgress != null)
                                await eventsHandler.OnResponseFileSearchCallInProgress(DeserializeEvent<ResponseEventFileSearchCallInProgress>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFileSearchCallSearching:
                            if (eventsHandler.OnResponseFileSearchCallSearching != null)
                                await eventsHandler.OnResponseFileSearchCallSearching(DeserializeEvent<ResponseEventFileSearchCallSearching>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseFileSearchCallCompleted:
                            if (eventsHandler.OnResponseFileSearchCallCompleted != null)
                                await eventsHandler.OnResponseFileSearchCallCompleted(DeserializeEvent<ResponseEventFileSearchCallCompleted>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseWebSearchCallInProgress:
                            if (eventsHandler.OnResponseWebSearchCallInProgress != null)
                                await eventsHandler.OnResponseWebSearchCallInProgress(DeserializeEvent<ResponseEventWebSearchCallInProgress>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseWebSearchCallSearching:
                            if (eventsHandler.OnResponseWebSearchCallSearching != null)
                                await eventsHandler.OnResponseWebSearchCallSearching(DeserializeEvent<ResponseEventWebSearchCallSearching>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseWebSearchCallCompleted:
                            if (eventsHandler.OnResponseWebSearchCallCompleted != null)
                                await eventsHandler.OnResponseWebSearchCallCompleted(DeserializeEvent<ResponseEventWebSearchCallCompleted>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningDelta:
                            if (eventsHandler.OnResponseReasoningDelta != null)
                                await eventsHandler.OnResponseReasoningDelta(DeserializeEvent<ResponseEventReasoningDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningDone:
                            if (eventsHandler.OnResponseReasoningDone != null)
                                await eventsHandler.OnResponseReasoningDone(DeserializeEvent<ResponseEventReasoningDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryPartAdded:
                            if (eventsHandler.OnResponseReasoningSummaryPartAdded != null)
                                await eventsHandler.OnResponseReasoningSummaryPartAdded(DeserializeEvent<ResponseEventReasoningSummaryPartAdded>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryPartDone:
                            if (eventsHandler.OnResponseReasoningSummaryPartDone != null)
                                await eventsHandler.OnResponseReasoningSummaryPartDone(DeserializeEvent<ResponseEventReasoningSummaryPartDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryTextDelta:
                            if (eventsHandler.OnResponseReasoningSummaryTextDelta != null)
                                await eventsHandler.OnResponseReasoningSummaryTextDelta(DeserializeEvent<ResponseEventReasoningSummaryTextDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryTextDone:
                            if (eventsHandler.OnResponseReasoningSummaryTextDone != null)
                                await eventsHandler.OnResponseReasoningSummaryTextDone(DeserializeEvent<ResponseEventReasoningSummaryTextDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryDelta:
                            if (eventsHandler.OnResponseReasoningSummaryDelta != null)
                                await eventsHandler.OnResponseReasoningSummaryDelta(DeserializeEvent<ResponseEventReasoningSummaryDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseReasoningSummaryDone:
                            if (eventsHandler.OnResponseReasoningSummaryDone != null)
                                await eventsHandler.OnResponseReasoningSummaryDone(DeserializeEvent<ResponseEventReasoningSummaryDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallInProgress:
                            if (eventsHandler.OnResponseImageGenerationCallInProgress != null)
                                await eventsHandler.OnResponseImageGenerationCallInProgress(DeserializeEvent<ResponseEventImageGenerationCallInProgress>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallGenerating:
                            if (eventsHandler.OnResponseImageGenerationCallGenerating != null)
                                await eventsHandler.OnResponseImageGenerationCallGenerating(DeserializeEvent<ResponseEventImageGenerationCallGenerating>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallPartialImage:
                            if (eventsHandler.OnResponseImageGenerationCallPartialImage != null)
                                await eventsHandler.OnResponseImageGenerationCallPartialImage(DeserializeEvent<ResponseEventImageGenerationCallPartialImage>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseImageGenerationCallCompleted:
                            if (eventsHandler.OnResponseImageGenerationCallCompleted != null)
                                await eventsHandler.OnResponseImageGenerationCallCompleted(DeserializeEvent<ResponseEventImageGenerationCallCompleted>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallArgumentsDelta:
                            if (eventsHandler.OnResponseMcpCallArgumentsDelta != null)
                                await eventsHandler.OnResponseMcpCallArgumentsDelta(DeserializeEvent<ResponseEventMcpCallArgumentsDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallArgumentsDone:
                            if (eventsHandler.OnResponseMcpCallArgumentsDone != null)
                                await eventsHandler.OnResponseMcpCallArgumentsDone(DeserializeEvent<ResponseEventMcpCallArgumentsDone>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallInProgress:
                            if (eventsHandler.OnResponseMcpCallInProgress != null)
                                await eventsHandler.OnResponseMcpCallInProgress(DeserializeEvent<ResponseEventMcpCallInProgress>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallCompleted:
                            if (eventsHandler.OnResponseMcpCallCompleted != null)
                                await eventsHandler.OnResponseMcpCallCompleted(DeserializeEvent<ResponseEventMcpCallCompleted>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpCallFailed:
                            if (eventsHandler.OnResponseMcpCallFailed != null)
                                await eventsHandler.OnResponseMcpCallFailed(DeserializeEvent<ResponseEventMcpCallFailed>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpListToolsInProgress:
                            if (eventsHandler.OnResponseMcpListToolsInProgress != null)
                                await eventsHandler.OnResponseMcpListToolsInProgress(DeserializeEvent<ResponseEventMcpListToolsInProgress>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpListToolsCompleted:
                            if (eventsHandler.OnResponseMcpListToolsCompleted != null)
                                await eventsHandler.OnResponseMcpListToolsCompleted(DeserializeEvent<ResponseEventMcpListToolsCompleted>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseMcpListToolsFailed:
                            if (eventsHandler.OnResponseMcpListToolsFailed != null)
                                await eventsHandler.OnResponseMcpListToolsFailed(DeserializeEvent<ResponseEventMcpListToolsFailed>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCodeInterpreterCallInProgress:
                            if (eventsHandler.OnResponseCodeInterpreterCallInProgress != null)
                                await eventsHandler.OnResponseCodeInterpreterCallInProgress(DeserializeEvent<ResponseEventCodeInterpreterCallInProgress>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCodeInterpreterCallCodeDelta:
                            if (eventsHandler.OnResponseCodeInterpreterCallCodeDelta != null)
                                await eventsHandler.OnResponseCodeInterpreterCallCodeDelta(DeserializeEvent<ResponseEventCodeInterpreterCallCodeDelta>(runStreamEvent.Data));
                            break;
                        case ResponseEventTypes.ResponseCodeInterpreterCallCodeDone:
                            if (eventsHandler.OnResponseCodeInterpreterCallCodeDone != null)
                                await eventsHandler.OnResponseCodeInterpreterCallCodeDone(DeserializeEvent<ResponseEventCodeInterpreterCallCodeDone>(runStreamEvent.Data));
                            break;
                    }
                }
            }
        }
    }
}
