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
    public Task<HttpCallResult<Thread>> CreateThreadAsync(CreateThreadRequest? request = null, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<Thread>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, null, request, cancellationToken);
    }

    /// <summary>
    /// Retrieves a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="Thread"/> to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Thread"/>.</returns>
    public Task<HttpCallResult<Thread>> RetrieveThreadAsync(string threadId, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<Thread>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}"), ct: cancellationToken);
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
    public Task<HttpCallResult<Thread>> ModifyThreadAsync(string threadId, ModifyThreadRequest request, CancellationToken? cancellationToken = null)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpPostRaw<Thread>(provider, CapabilityEndpoints.Threads, provider.ApiUrl(Endpoint, $"/{threadId}"), request, cancellationToken);
    }

    /// <summary>
    /// Delete a thread.
    /// </summary>
    /// <param name="threadId">The id of the <see cref="Thread"/> to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns>True, if was successfully deleted.</returns>
    public async Task<HttpCallResult<bool>> DeleteThreadAsync(string threadId, CancellationToken? cancellationToken = null)
    {        
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads, HttpMethod.Delete, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}"), ct: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, null);
    }

    /// <summary>
    /// Create a message.
    /// </summary>
    /// <param name="threadId">The id of the thread to create a message for.</param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Message"/>.</returns>
    public Task<HttpCallResult<Message>> CreateMessageAsync(string threadId, CreateMessageRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<Message>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages"), request, cancellationToken);
    }

    /// <summary>
    /// Returns a list of messages for a given thread.
    /// </summary>
    /// <param name="threadId">The id of the thread the messages belong to.</param>
    /// <param name="query"><see cref="ListQuery"/>.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ListResponse{Message}"/>.</returns>
    public Task<HttpCallResult<ListResponse<Message>>> ListMessagesAsync(string threadId, ListQuery? query = null, CancellationToken cancellationToken = default)
    {
         return HttpGetRaw<ListResponse<Message>>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages"),query, cancellationToken);
    }

    /// <summary>
    /// Retrieve a message.
    /// </summary>
    /// <param name="threadId">The id of the thread to which this message belongs.</param>
    /// <param name="messageId">The id of the message to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Message"/>.</returns>
    public Task<HttpCallResult<Message>> RetrieveMessageAsync(string threadId, string messageId, CancellationToken cancellationToken = default)
    {
         return HttpGetRaw<Message>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Threads, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages/{messageId}"), ct: cancellationToken);
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
    public Task<HttpCallResult<Message>> ModifyMessageAsync(string threadId, string messageId, ModifyMessageRequest request, CancellationToken cancellationToken = default)
    {
        return HttpPostRaw<Message>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages/{messageId}"), request, cancellationToken);
    }


    /// <summary>
    /// Deletes a message within a thread.
    /// </summary>
    /// <param name="threadId">The identifier of the thread containing the message.</param>
    /// <param name="messageId">The identifier of the message to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="HttpCallResult{Boolean}" /> indicating the success of the deletion.</returns>
    public async Task<HttpCallResult<bool>> DeleteMessageAsync(string threadId, string messageId, CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, HttpMethod.Delete, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{threadId}/messages/{messageId}"), ct: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, null);
    }
        

    /*
    #endregion Messages

    #region Runs

    /// <summary>
    /// Returns a list of runs belonging to a thread.
    /// </summary>
    /// <param name="threadId">The id of the thread the run belongs to.</param>
    /// <param name="query"><see cref="ListQuery"/>.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ListResponse{RunResponse}"/></returns>
    public async Task<ListResponse<RunResponse>> ListRunsAsync(string threadId, ListQuery query = null, CancellationToken cancellationToken = default)
    {
        var response = await client.Client.GetAsync(GetUrl($"/{threadId}/runs", query), cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<ListResponse<RunResponse>>(responseAsString, client);
    }

    /// <summary>
    /// Create a run.
    /// </summary>
    /// <param name="threadId">The id of the thread to run.</param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="RunResponse"/>.</returns>
    public async Task<RunResponse> CreateRunAsync(string threadId, CreateRunRequest request = null, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AssistantId))
        {
            var assistant = await client.AssistantsEndpoint.CreateAssistantAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            request = new CreateRunRequest(assistant, request);
        }

        var jsonContent = JsonSerializer.Serialize(request, OpenAIClient.JsonSerializationOptions).ToJsonStringContent(EnableDebug);
        var response = await client.Client.PostAsync(GetUrl($"/{threadId}/runs"), jsonContent, cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<RunResponse>(responseAsString, client);
    }

    /// <summary>
    /// Create a thread and run it in one request.
    /// </summary>
    /// <param name="request"><see cref="CreateThreadAndRunRequest"/>.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="RunResponse"/>.</returns>
    public async Task<RunResponse> CreateThreadAndRunAsync(CreateThreadAndRunRequest request = null, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AssistantId))
        {
            var assistant = await client.AssistantsEndpoint.CreateAssistantAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            request = new CreateThreadAndRunRequest(assistant, request);
        }

        var jsonContent = JsonSerializer.Serialize(request, OpenAIClient.JsonSerializationOptions).ToJsonStringContent(EnableDebug);
        var response = await client.Client.PostAsync(GetUrl("/runs"), jsonContent, cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<RunResponse>(responseAsString, client);
    }

    /// <summary>
    /// Retrieves a run.
    /// </summary>
    /// <param name="threadId">The id of the thread that was run.</param>
    /// <param name="runId">The id of the run to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="RunResponse"/>.</returns>
    public async Task<RunResponse> RetrieveRunAsync(string threadId, string runId, CancellationToken cancellationToken = default)
    {
        var response = await client.Client.GetAsync(GetUrl($"/{threadId}/runs/{runId}"), cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<RunResponse>(responseAsString, client);
    }

    /// <summary>
    /// Modifies a run.
    /// </summary>
    /// <remarks>
    /// Only the <see cref="RunResponse.Metadata"/> can be modified.
    /// </remarks>
    /// <param name="threadId">The id of the thread that was run.</param>
    /// <param name="runId">The id of the <see cref="RunResponse"/> to modify.</param>
    /// <param name="metadata">Set of 16 key-value pairs that can be attached to an object.
    /// This can be useful for storing additional information about the object in a structured format.
    /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="RunResponse"/>.</returns>
    public async Task<RunResponse> ModifyRunAsync(string threadId, string runId, IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        var jsonContent = JsonSerializer.Serialize(new { metadata }, OpenAIClient.JsonSerializationOptions).ToJsonStringContent(EnableDebug);
        var response = await client.Client.PostAsync(GetUrl($"/{threadId}/runs/{runId}"), jsonContent, cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<RunResponse>(responseAsString, client);
    }

    /// <summary>
    /// When a run has the status: "requires_action" and required_action.type is submit_tool_outputs,
    /// this endpoint can be used to submit the outputs from the tool calls once they're all completed.
    /// All outputs must be submitted in a single request.
    /// </summary>
    /// <param name="threadId">The id of the thread to which this run belongs.</param>
    /// <param name="runId">The id of the run that requires the tool output submission.</param>
    /// <param name="request"><see cref="SubmitToolOutputsRequest"/>.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="RunResponse"/>.</returns>
    public async Task<RunResponse> SubmitToolOutputsAsync(string threadId, string runId, SubmitToolOutputsRequest request, CancellationToken cancellationToken = default)
    {
        var jsonContent = JsonSerializer.Serialize(request, OpenAIClient.JsonSerializationOptions).ToJsonStringContent(EnableDebug);
        var response = await client.Client.PostAsync(GetUrl($"/{threadId}/runs/{runId}/submit_tool_outputs"), jsonContent, cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<RunResponse>(responseAsString, client);
    }

    /// <summary>
    /// Returns a list of run steps belonging to a run.
    /// </summary>
    /// <param name="threadId">The id of the thread to which the run and run step belongs.</param>
    /// <param name="runId">The id of the run to which the run step belongs.</param>
    /// <param name="query">Optional, <see cref="ListQuery"/>.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ListResponse{RunStep}"/>.</returns>
    public async Task<ListResponse<RunStepResponse>> ListRunStepsAsync(string threadId, string runId, ListQuery query = null, CancellationToken cancellationToken = default)
    {
        var response = await client.Client.GetAsync(GetUrl($"/{threadId}/runs/{runId}/steps", query), cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<ListResponse<RunStepResponse>>(responseAsString, client);
    }

    /// <summary>
    /// Retrieves a run step.
    /// </summary>
    /// <param name="threadId">The id of the thread to which the run and run step belongs.</param>
    /// <param name="runId">The id of the run to which the run step belongs.</param>
    /// <param name="stepId">The id of the run step to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="RunStepResponse"/>.</returns>
    public async Task<RunStepResponse> RetrieveRunStepAsync(string threadId, string runId, string stepId, CancellationToken cancellationToken = default)
    {
        var response = await client.Client.GetAsync(GetUrl($"/{threadId}/runs/{runId}/steps/{stepId}"), cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<RunStepResponse>(responseAsString, client);
    }

    /// <summary>
    /// Cancels a run that is <see cref="RunStatus.InProgress"/>.
    /// </summary>
    /// <param name="threadId">The id of the thread to which this run belongs.</param>
    /// <param name="runId">The id of the run to cancel.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="RunResponse"/>.</returns>
    public async Task<RunResponse> CancelRunAsync(string threadId, string runId, CancellationToken cancellationToken = default)
    {
        var response = await client.Client.PostAsync(GetUrl($"/{threadId}/runs/{runId}/cancel"), content: null, cancellationToken).ConfigureAwait(false);
        var responseAsString = await response.ReadAsStringAsync(EnableDebug, cancellationToken).ConfigureAwait(false);
        return response.Deserialize<RunResponse>(responseAsString, client);
    }

    #endregion Runs*/
}