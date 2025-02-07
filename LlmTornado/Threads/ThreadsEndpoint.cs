using System;
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
    /// <returns><see cref="Message"/>.</returns>
    public Task<HttpCallResult<Message>> CreateMessageAsync(string threadId, CreateMessageRequest request,
        CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<Message>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages"), request, ct: cancellationToken);
    }

    /// <summary>
    /// Returns a list of messages for a given thread.
    /// </summary>
    /// <param name="threadId">The id of the thread the messages belong to.</param>
    /// <param name="query"><see cref="ListQuery"/>.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ListResponse{Message}"/>.</returns>
    public Task<HttpCallResult<ListResponse<Message>>> ListMessagesAsync(string threadId, ListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        return HttpGetRaw<ListResponse<Message>>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages"),
            query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }

    /// <summary>
    /// Retrieve a message.
    /// </summary>
    /// <param name="threadId">The id of the thread to which this message belongs.</param>
    /// <param name="messageId">The id of the message to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Message"/>.</returns>
    public Task<HttpCallResult<Message>> RetrieveMessageAsync(string threadId, string messageId,
        CancellationToken cancellationToken = default)
    {
        return HttpGetRaw<Message>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages/{messageId}"), ct: cancellationToken);
    }

    /// <summary>
    /// Modifies a message.
    /// </summary>
    /// <remarks>
    /// Only the <see cref="Message.Metadata"/> can be modified.
    /// </remarks>
    /// <param name="threadId"></param>
    /// <param name="messageId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Message"/>.</returns>
    public Task<HttpCallResult<Message>> ModifyMessageAsync(string threadId, string messageId,
        ModifyMessageRequest request, CancellationToken cancellationToken = default)
    {
        return HttpPostRaw<Message>(Api.GetProvider(LLmProviders.OpenAi), Endpoint,
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

    public async Task StreamRun(string threadId, CreateRunRequest request, RunStreamEventHandler eventHandler, CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs"), postData: request, verb: HttpMethod.Post, token: cancellationToken);

        if (tornadoStreamRequest.Exception is not null)
        {
            if (eventHandler?.HttpExceptionHandler is null)
            {
                throw tornadoStreamRequest.Exception;
            }

            await eventHandler.HttpExceptionHandler(new HttpFailedRequest
            {
                Exception = tornadoStreamRequest.Exception,
                Result = tornadoStreamRequest.CallResponse,
                Request = tornadoStreamRequest.CallRequest,
                RawMessage = tornadoStreamRequest.Response ?? new HttpResponseMessage(),
                Body = null //TODO: unifify this
            });

            await tornadoStreamRequest.DisposeAsync();
        }

        if (eventHandler?.OutboundHttpRequestHandler is not null && tornadoStreamRequest.CallRequest is not null)
        {
            await eventHandler.OutboundHttpRequestHandler(tornadoStreamRequest.CallRequest);
        }


        OpenAiEndpointProvider provider = (Api.GetProvider(LLmProviders.OpenAi) as OpenAiEndpointProvider)!;

        await foreach (RunStreamEvent runStreamEvent in provider.InboundStream(tornadoStreamRequest.StreamReader!).WithCancellation(cancellationToken))
        {
            // Split OpenAi event type. e.g. thread.run.completed => [thread, run, completed]
            string[] split = runStreamEvent.EventType.Split('.');
            RunStreamEventTypeObject objectType;
            RunStreamEventTypeStatus status = RunStreamEventTypeStatus.Unknown;

            // if split.Length == 1, it is either Done, or Error event, Otherwise convert to valid object type and status
            if (split.Length == 1)
            {
                objectType = JsonConvert.DeserializeObject<RunStreamEventTypeObject>($"\"{split[0]}\"");
            }
            else
            {
                objectType = JsonConvert.DeserializeObject<RunStreamEventTypeObject>($"\"{string.Join('.', split[..^1])}\"");
                status = JsonConvert.DeserializeObject<RunStreamEventTypeStatus>($"\"{split[^1]}\"");
            }

            switch (objectType)
            {
                case RunStreamEventTypeObject.Thread:
                    TornadoThread thread = JsonConvert.DeserializeObject<TornadoThread>(runStreamEvent.Data)!;
                    eventHandler?.OnThreadStatusChanged?.Invoke(thread, status);
                    break;
                case RunStreamEventTypeObject.Run:
                    TornadoRun run = JsonConvert.DeserializeObject<TornadoRun>(runStreamEvent.Data)!;
                    eventHandler?.OnRunStatusChanged?.Invoke(run, status);
                    break;
                case RunStreamEventTypeObject.RunStep:
                    if (status == RunStreamEventTypeStatus.Delta)
                    {
                        RunStepDelta delta = JsonConvert.DeserializeObject<RunStepDelta>(runStreamEvent.Data)!;
                        eventHandler?.OnRunStepDelta?.Invoke(delta);
                    }
                    else
                    {
                        TornadoRunStep runStep = JsonConvert.DeserializeObject<TornadoRunStep>(runStreamEvent.Data)!;
                        eventHandler?.OnRunStepStatusChanged?.Invoke(runStep, status);
                    }

                    break;
                case RunStreamEventTypeObject.Message:
                    if (status == RunStreamEventTypeStatus.Delta)
                    {
                        MessageDelta delta = JsonConvert.DeserializeObject<MessageDelta>(runStreamEvent.Data)!;
                        eventHandler?.OnMessageDelta?.Invoke(delta);
                    }
                    else
                    {
                        Message message = JsonConvert.DeserializeObject<Message>(runStreamEvent.Data)!;
                        eventHandler?.OnMessageStatusChanged?.Invoke(message, status);
                    }

                    break;
                case RunStreamEventTypeObject.Error:
                    eventHandler?.OnErrorReceived?.Invoke(runStreamEvent.Data);
                    break;
                case RunStreamEventTypeObject.Done:
                    eventHandler?.OnDone?.Invoke();
                    break;
                case RunStreamEventTypeObject.Unknown:
                default:
                    eventHandler?.OnUnknownEventReceived?.Invoke(runStreamEvent.EventType, runStreamEvent.Data);
                    break;
            }
        }

        await tornadoStreamRequest.DisposeAsync();
    }
    
    
    public async Task StreamSubmitToolOutput(string threadId, string runId, SubmitToolOutputsRequest request, RunStreamEventHandler eventHandler,
        CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}/submit_tool_outputs"), postData: request, verb: HttpMethod.Post, token: cancellationToken);

        if (tornadoStreamRequest.Exception is not null)
        {
            if (eventHandler?.HttpExceptionHandler is null)
            {
                throw tornadoStreamRequest.Exception;
            }

            await eventHandler.HttpExceptionHandler(new HttpFailedRequest
            {
                Exception = tornadoStreamRequest.Exception,
                Result = tornadoStreamRequest.CallResponse,
                Request = tornadoStreamRequest.CallRequest,
                RawMessage = tornadoStreamRequest.Response ?? new HttpResponseMessage(),
                Body = null //TODO: unifify this
            });

            await tornadoStreamRequest.DisposeAsync();
        }

        if (eventHandler?.OutboundHttpRequestHandler is not null && tornadoStreamRequest.CallRequest is not null)
        {
            await eventHandler.OutboundHttpRequestHandler(tornadoStreamRequest.CallRequest);
        }


        OpenAiEndpointProvider provider = (Api.GetProvider(LLmProviders.OpenAi) as OpenAiEndpointProvider)!;

        await foreach (RunStreamEvent runStreamEvent in provider.InboundStream(tornadoStreamRequest.StreamReader!).WithCancellation(cancellationToken))
        {
            // Split OpenAi event type. e.g. thread.run.completed => [thread, run, completed]
            string[] split = runStreamEvent.EventType.Split('.');
            RunStreamEventTypeObject objectType;
            RunStreamEventTypeStatus status = RunStreamEventTypeStatus.Unknown;

            // if split.Length == 1, it is either Done, or Error event, Otherwise convert to valid object type and status
            if (split.Length == 1)
            {
                objectType = JsonConvert.DeserializeObject<RunStreamEventTypeObject>($"\"{split[0]}\"");
            }
            else
            {
                objectType = JsonConvert.DeserializeObject<RunStreamEventTypeObject>($"\"{string.Join('.', split[..^1])}\"");
                status = JsonConvert.DeserializeObject<RunStreamEventTypeStatus>($"\"{split[^1]}\"");
            }
            
            switch (objectType)
            {
                case RunStreamEventTypeObject.Thread:
                    TornadoThread thread = JsonConvert.DeserializeObject<TornadoThread>(runStreamEvent.Data)!;
                    eventHandler?.OnThreadStatusChanged?.Invoke(thread, status);
                    break;
                case RunStreamEventTypeObject.Run:
                    TornadoRun run = JsonConvert.DeserializeObject<TornadoRun>(runStreamEvent.Data)!;
                    eventHandler?.OnRunStatusChanged?.Invoke(run, status);
                    break;
                case RunStreamEventTypeObject.RunStep:
                    if (status == RunStreamEventTypeStatus.Delta)
                    {
                        RunStepDelta delta = JsonConvert.DeserializeObject<RunStepDelta>(runStreamEvent.Data)!;
                        eventHandler?.OnRunStepDelta?.Invoke(delta);
                    }
                    else
                    {
                        TornadoRunStep runStep = JsonConvert.DeserializeObject<TornadoRunStep>(runStreamEvent.Data)!;
                        eventHandler?.OnRunStepStatusChanged?.Invoke(runStep, status);
                    }

                    break;
                case RunStreamEventTypeObject.Message:
                    if (status == RunStreamEventTypeStatus.Delta)
                    {
                        MessageDelta delta = JsonConvert.DeserializeObject<MessageDelta>(runStreamEvent.Data)!;
                        eventHandler?.OnMessageDelta?.Invoke(delta);
                    }
                    else
                    {
                        Message message = JsonConvert.DeserializeObject<Message>(runStreamEvent.Data)!;
                        eventHandler?.OnMessageStatusChanged?.Invoke(message, status);
                    }

                    break;
                case RunStreamEventTypeObject.Error:
                    eventHandler?.OnErrorReceived?.Invoke(runStreamEvent.Data);
                    break;
                case RunStreamEventTypeObject.Done:
                    eventHandler?.OnDone?.Invoke();
                    break;
                case RunStreamEventTypeObject.Unknown:
                default:
                    eventHandler?.OnUnknownEventReceived?.Invoke(runStreamEvent.EventType, runStreamEvent.Data);
                    break;
            }
        }

        await tornadoStreamRequest.DisposeAsync();
    }
}