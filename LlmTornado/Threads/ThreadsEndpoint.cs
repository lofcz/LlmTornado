using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     Create threads that assistants can interact with.<br />
///     <see href="https://platform.openai.com/docs/api-reference/threads" />
/// </summary>
public sealed class ThreadsEndpoint : EndpointBase
{
    internal ThreadsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     Endpoint specification
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Threads;

    /// <summary>
    ///     Create a thread.
    /// </summary>
    /// <param name="request"><see cref="CreateThreadRequest" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="TornadoThread" />.</returns>
    public Task<HttpCallResult<TornadoThread>> CreateThreadAsync(CreateThreadRequest? request = null,
        CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<TornadoThread>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, null, request,
            ct: cancellationToken);
    }

    /// <summary>
    /// Retrieves a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="TornadoThread"/> to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="TornadoThread"/>.</returns>
    public Task<HttpCallResult<TornadoThread>> RetrieveThreadAsync(string threadId,
        CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<TornadoThread>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}"), ct: cancellationToken);
    }

    /// <summary>
    /// Modifies a thread.
    /// </summary>
    /// <remarks>
    /// Only the <see cref="TornadoThread.Metadata"/> can be modified.
    /// </remarks>
    /// <param name="threadId">The id of the <see cref="TornadoThread"/> to modify.</param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="TornadoThread"/>.</returns>
    public Task<HttpCallResult<TornadoThread>> ModifyThreadAsync(string threadId, ModifyThreadRequest request,
        CancellationToken? cancellationToken = null)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpPostRaw<TornadoThread>(provider, CapabilityEndpoints.Threads, provider.ApiUrl(Endpoint, $"/{threadId}"),
            request, ct: cancellationToken);
    }

    /// <summary>
    /// Delete a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="TornadoThread"/> to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns>True, if was successfully deleted.</returns>
    public async Task<HttpCallResult<bool>> DeleteThreadAsync(string threadId,
        CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi),
                CapabilityEndpoints.Threads, HttpMethod.Delete,
                GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}"), ct: cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, status.Request);
    }

    /// <summary>
    /// Create a message.
    /// </summary>
    /// <param name="threadId">The id of the thread to create a message for.</param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="AssistantMessage"/>.</returns>
    public Task<HttpCallResult<AssistantMessage>> CreateMessageAsync(string threadId, CreateMessageRequest request,
        CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<AssistantMessage>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages"), request, ct: cancellationToken);
    }

    /// <summary>
    /// Returns a list of messages for a given thread.
    /// </summary>
    /// <param name="threadId">The id of the thread the messages belong to.</param>
    /// <param name="query"><see cref="ListQuery"/>.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ListResponse{Message}"/>.</returns>
    public Task<HttpCallResult<ListResponse<AssistantMessage>>> ListMessagesAsync(string threadId, ListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        return HttpGetRaw<ListResponse<AssistantMessage>>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages"),
            query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }

    /// <summary>
    /// Retrieve a message.
    /// </summary>
    /// <param name="threadId">The id of the thread to which this message belongs.</param>
    /// <param name="messageId">The id of the message to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="AssistantMessage"/>.</returns>
    public Task<HttpCallResult<AssistantMessage>> RetrieveMessageAsync(string threadId, string messageId,
        CancellationToken cancellationToken = default)
    {
        return HttpGetRaw<AssistantMessage>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages/{messageId}"), ct: cancellationToken);
    }

    /// <summary>
    /// Modifies a message.
    /// </summary>
    /// <remarks>
    /// Only the <see cref="AssistantMessage.Metadata"/> can be modified.
    /// </remarks>
    /// <param name="threadId"></param>
    /// <param name="messageId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="AssistantMessage"/>.</returns>
    public Task<HttpCallResult<AssistantMessage>> ModifyMessageAsync(string threadId, string messageId,
        ModifyMessageRequest request, CancellationToken cancellationToken = default)
    {
        return HttpPostRaw<AssistantMessage>(Api.GetProvider(LLmProviders.OpenAi), Endpoint,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages/{messageId}"), request,
            ct: cancellationToken);
    }


    /// <summary>
    /// Deletes a message within a thread.
    /// </summary>
    /// <param name="threadId">The identifier of the thread containing the message.</param>
    /// <param name="messageId">The identifier of the message to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="HttpCallResult{Boolean}" /> indicating the success of the deletion.</returns>
    public async Task<HttpCallResult<bool>> DeleteMessageAsync(string threadId, string messageId,
        CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi),
                Endpoint, HttpMethod.Delete,
                GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages/{messageId}"),
                ct: cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, status.Request);
    }

    /// <summary>
    /// Creates a run for a specific thread.
    /// </summary>
    /// <param name="threadId">The unique identifier of the thread.</param>
    /// <param name="request"><see cref="CreateRunRequest" /> containing the details of the run to be created.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> to cancel the operation.</param>
    /// <returns><see cref="HttpCallResult{TornadoRun}" /> representing the result of the run creation operation.</returns>
    public Task<HttpCallResult<TornadoRun>> CreateRunAsync(string threadId, CreateRunRequest request,
        CancellationToken? cancellationToken = null)
    {
        request.Stream = false;
        return HttpPostRaw<TornadoRun>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs"), request, ct: cancellationToken);
    }

    /// <summary>
    /// Retrieve a specific run within a thread.
    /// </summary>
    /// <param name="threadId">The unique identifier of the thread containing the run.</param>
    /// <param name="runId">The unique identifier of the run to be retrieved.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns><see cref="HttpCallResult{TornadoRun}" /> containing the result of the operation.</returns>
    public Task<HttpCallResult<TornadoRun>> RetrieveRunAsync(string threadId, string runId,
        CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<TornadoRun>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}"), ct: cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of runs associated with a specified thread.
    /// </summary>
    /// <param name="threadId">The unique identifier of the thread for which runs are to be retrieved.</param>
    /// <param name="query">Optional, <see cref="ListQuery" /> containing query parameters for filtering the runs.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns><see cref="HttpCallResult{TornadoRun}" /> containing a list of runs associated with the specified thread.</returns>
    public Task<HttpCallResult<List<TornadoRun>>> ListRunsAsync(string threadId, ListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        return HttpGetRaw<List<TornadoRun>>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs"),
            query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }

    /// <summary>
    /// Modify an existing run within a thread.
    /// </summary>
    /// <param name="threadId">The unique identifier of the thread containing the run to be modified.</param>
    /// <param name="runId">The unique identifier of the run to be modified.</param>
    /// <param name="request"><see cref="ModifyRunRequest"/> containing the modifications to apply to the run.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="HttpCallResult{TornadoRun}"/> containing the modified run details.</returns>
    public Task<HttpCallResult<TornadoRun>> ModifyRunAsync(string threadId, string runId, ModifyRunRequest request,
        CancellationToken cancellationToken = default)
    {
        return HttpPostRaw<TornadoRun>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}"), request, ct: cancellationToken);
    }

    /// <summary>
    /// Lists the run steps associated with a specific thread and run.
    /// </summary>
    /// <param name="threadId">The unique identifier of the thread.</param>
    /// <param name="runId">The unique identifier of the run.</param>
    /// <param name="query">Optional. An instance of <see cref="ListQuery" /> for additional query parameters.</param>
    /// <param name="cancellationToken">Optional. A <see cref="CancellationToken" /> used to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}" /> that represents the asynchronous operation, with a result of <see cref="HttpCallResult{TornadoRunStep}" /> containing a list of run steps.</returns>
    public Task<HttpCallResult<ListResponse<TornadoRunStep>>> ListRunStepsAsync(string threadId, string runId,
        ListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        return HttpGetRaw<ListResponse<TornadoRunStep>>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}/steps"),
            query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }

    /// <summary>
    /// Retrieve a specific run step associated with a thread and run.
    /// </summary>
    /// <param name="threadId">The identifier of the thread.</param>
    /// <param name="runId">The identifier of the run.</param>
    /// <param name="runStepId">The identifier of the run step.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> used to cancel the operation.</param>
    /// <returns><see cref="HttpCallResult{TornadoRunStep}" />.</returns>
    public Task<HttpCallResult<TornadoRunStep>> RetrieveRunStepAsync(string threadId, string runId, string runStepId,
        CancellationToken cancellationToken = default)
    {
        return HttpGetRaw<TornadoRunStep>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}/steps/{runStepId}"),
            ct: cancellationToken);
    }

    /// <summary>
    /// Submit tool outputs for a specific thread and run.
    /// </summary>
    /// <param name="threadId">The unique identifier of the thread.</param>
    /// <param name="runId">The unique identifier of the run.</param>
    /// <param name="request"><see cref="SubmitToolOutputsRequest" /> containing the tool output details.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> for request cancellation.</param>
    /// <returns><see cref="HttpCallResult{TornadoRun}" />.</returns>
    public Task<HttpCallResult<TornadoRun>> SubmitToolOutput(string threadId, string runId, SubmitToolOutputsRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Stream = false;
        return HttpPostRaw<TornadoRun>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}/submit_tool_outputs"), request, ct: cancellationToken);
    }

    /// <summary>
    /// Streams the run operation for a specific thread with provided request data and event handler.
    /// </summary>
    /// <param name="threadId">The identifier of the thread.</param>
    /// <param name="request">The request data for creating a run, <see cref="CreateRunRequest" />.</param>
    /// <param name="eventHandler">Handler for streaming run events, <see cref="RunStreamEventHandler" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StreamRun(string threadId, CreateRunRequest request, RunStreamEventHandler eventHandler, CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        string url = GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs");
        TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            url, postData: request, verb: HttpMethod.Post, token: cancellationToken);

        try
        {
            if (tornadoStreamRequest.Exception is not null)
            {
                if (eventHandler.HttpExceptionHandler is not null)
                {
                    await eventHandler.HttpExceptionHandler.Invoke(new HttpFailedRequest
                    {
                        Exception = tornadoStreamRequest.Exception,
                        Result = tornadoStreamRequest.CallResponse,
                        Request = tornadoStreamRequest.CallRequest,
                        RawMessage = tornadoStreamRequest.Response ?? new HttpResponseMessage(),
                        Body = new TornadoRequestContent(request, url)
                    });
                }

                return;
            }

            if (eventHandler.OutboundHttpRequestHandler is not null && tornadoStreamRequest.CallRequest is not null)
            {
                await eventHandler.OutboundHttpRequestHandler.Invoke(tornadoStreamRequest.CallRequest);
            }

            IEndpointProvider provider = Api.ResolveProvider(LLmProviders.OpenAi);

            if (provider is OpenAiEndpointProvider oaiProvider)
            {
                await foreach (RunStreamEvent runStreamEvent in oaiProvider.InboundStream(tornadoStreamRequest.StreamReader!).WithCancellation(cancellationToken))
                {
                    await HandleOpenAiStreamEvent(eventHandler, runStreamEvent);
                }
            }
        }
        finally
        {
            await tornadoStreamRequest.DisposeAsync();
        }
    }

    /// <summary>
    /// Streams the submission of tool outputs for a specific thread and run.
    /// </summary>
    /// <param name="threadId">The identifier of the thread.</param>
    /// <param name="runId">The identifier of the run.</param>
    /// <param name="request"><see cref="SubmitToolOutputsRequest" /> containing tool output data to be submitted.</param>
    /// <param name="eventHandler"><see cref="RunStreamEventHandler" /> for handling streaming events during the submission.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StreamSubmitToolOutput(string threadId, string runId, SubmitToolOutputsRequest request, RunStreamEventHandler eventHandler, CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        string url = GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}/submit_tool_outputs");
        TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            url, postData: request, verb: HttpMethod.Post, token: cancellationToken);

        try
        {
            if (tornadoStreamRequest.Exception is not null)
            {
                if (eventHandler.HttpExceptionHandler is not null)
                {
                    await eventHandler.HttpExceptionHandler.Invoke(new HttpFailedRequest
                    {
                        Exception = tornadoStreamRequest.Exception,
                        Result = tornadoStreamRequest.CallResponse,
                        Request = tornadoStreamRequest.CallRequest,
                        RawMessage = tornadoStreamRequest.Response ?? new HttpResponseMessage(),
                        Body = new TornadoRequestContent(request, url)
                    });   
                }
            }
            
            if (eventHandler.OutboundHttpRequestHandler is not null && tornadoStreamRequest.CallRequest is not null)
            {
                await eventHandler.OutboundHttpRequestHandler.Invoke(tornadoStreamRequest.CallRequest);
            }

            IEndpointProvider provider = Api.ResolveProvider(LLmProviders.OpenAi);

            if (provider is OpenAiEndpointProvider oaiProvider && tornadoStreamRequest.StreamReader is not null)
            {
                await foreach (RunStreamEvent runStreamEvent in oaiProvider.InboundStream(tornadoStreamRequest.StreamReader).WithCancellation(cancellationToken))
                {
                    await HandleOpenAiStreamEvent(eventHandler, runStreamEvent);
                }
            }
        }
        finally
        {
            await tornadoStreamRequest.DisposeAsync();   
        }
    }
    
    private static async ValueTask HandleOpenAiStreamEvent(RunStreamEventHandler eventHandler, RunStreamEvent runStreamEvent)
    {
        if (RunStreamEventTypeObjectCls.EventsMap.TryGetValue(runStreamEvent.EventType, out OpenAiAssistantStreamEvent? sse))
        {
            RunStreamEventTypeObject objectType = sse.ObjectType;
            RunStreamEventTypeStatus status = sse.Status;
            
            switch (objectType)
            {
                case RunStreamEventTypeObject.Thread:
                {
                    TornadoThread? thread = runStreamEvent.Data.JsonDecode<TornadoThread>();

                    if (thread is not null && eventHandler.OnThreadStatusChanged is not null)
                    {
                        await eventHandler.OnThreadStatusChanged.Invoke(thread, status);
                    }

                    break;
                }
                case RunStreamEventTypeObject.Run:
                {
                    TornadoRun? run = runStreamEvent.Data.JsonDecode<TornadoRun>();

                    if (run is not null && eventHandler.OnRunStatusChanged is not null)
                    {
                        await eventHandler.OnRunStatusChanged.Invoke(run, status);
                    }

                    break;
                }
                case RunStreamEventTypeObject.RunStep:
                {
                    if (status is RunStreamEventTypeStatus.Delta)
                    {
                        RunStepDelta? delta = runStreamEvent.Data.JsonDecode<RunStepDelta>();

                        if (delta is not null && eventHandler.OnRunStepDelta is not null)
                        {
                            await eventHandler.OnRunStepDelta.Invoke(delta);
                        }
                    }
                    else
                    {
                        TornadoRunStep? runStep = runStreamEvent.Data.JsonDecode<TornadoRunStep>();

                        if (runStep is not null && eventHandler.OnRunStepStatusChanged is not null)
                        {
                            await eventHandler.OnRunStepStatusChanged.Invoke(runStep, status);
                        }
                    }

                    break;
                }
                case RunStreamEventTypeObject.Message:
                {
                    if (status is RunStreamEventTypeStatus.Delta)
                    {
                        MessageDelta? delta = runStreamEvent.Data.JsonDecode<MessageDelta>();

                        if (delta is not null && eventHandler.OnMessageDelta is not null)
                        {
                            await eventHandler.OnMessageDelta.Invoke(delta);
                        }
                    }
                    else
                    {
                        AssistantMessage? message = JsonConvert.DeserializeObject<AssistantMessage>(runStreamEvent.Data);

                        if (message is not null && eventHandler.OnMessageStatusChanged is not null)
                        {
                            await eventHandler.OnMessageStatusChanged.Invoke(message, status);
                        }
                    }

                    break;
                }
                case RunStreamEventTypeObject.Error:
                {
                    if (eventHandler.OnErrorReceived is not null)
                    {
                        await eventHandler.OnErrorReceived.Invoke(runStreamEvent.Data);
                    }
                    
                    break;
                }
                case RunStreamEventTypeObject.Done:
                {
                    if (eventHandler.OnFinished is not null)
                    {
                        await eventHandler.OnFinished.Invoke();   
                    }
                    
                    break;
                }
                case RunStreamEventTypeObject.Unknown:
                default:
                {
                    if (eventHandler.OnUnknownEventReceived is not null)
                    {
                        await eventHandler.OnUnknownEventReceived.Invoke(runStreamEvent.EventType, runStreamEvent.Data);
                    }
                    
                    break;
                }
            }
        }
        #if DEBUG
        else
        {
            // unknown event; this is here just to allow breaking on it
            int unk = 0;
        }
        #endif
    }
}