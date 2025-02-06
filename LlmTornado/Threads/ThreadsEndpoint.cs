using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

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
    /// <returns><see cref="Thread" />.</returns>
    public Task<HttpCallResult<Thread>> CreateThreadAsync(CreateThreadRequest? request = null,
        CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<Thread>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, null, request,
            ct: cancellationToken);
    }

    /// <summary>
    /// Retrieves a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="Thread"/> to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Thread"/>.</returns>
    public Task<HttpCallResult<Thread>> RetrieveThreadAsync(string threadId,
        CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<Thread>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}"), ct: cancellationToken);
    }

    /// <summary>
    /// Modifies a thread.
    /// </summary>
    /// <remarks>
    /// Only the <see cref="Thread.Metadata"/> can be modified.
    /// </remarks>
    /// <param name="threadId">The id of the <see cref="Thread"/> to modify.</param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Thread"/>.</returns>
    public Task<HttpCallResult<Thread>> ModifyThreadAsync(string threadId, ModifyThreadRequest request,
        CancellationToken? cancellationToken = null)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpPostRaw<Thread>(provider, CapabilityEndpoints.Threads, provider.ApiUrl(Endpoint, $"/{threadId}"),
            request, ct: cancellationToken);
    }

    /// <summary>
    /// Delete a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="Thread"/> to delete.</param>
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
    /// Deletes a run within a thread.
    /// </summary>
    /// <param name="threadId">The unique identifier of the thread containing the run.</param>
    /// <param name="runId">The unique identifier of the run to be deleted.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns><see cref="HttpCallResult{Boolean}" /> indicating the success of the deletion.</returns>
    public async Task<HttpCallResult<bool>> DeleteRunAsync(string threadId, string runId,
        CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> result = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi),
            CapabilityEndpoints.Threads, HttpMethod.Delete,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/runs/{runId}"), ct: cancellationToken);
        return new HttpCallResult<bool>(result.Code, result.Response, result.Data?.Deleted ?? false, result.Ok, result.Request);
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
}