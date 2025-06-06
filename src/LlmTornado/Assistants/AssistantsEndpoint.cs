using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Assistants;

/// <summary>
///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
///     interpreter and file based retrieval.
/// </summary>
public sealed class AssistantsEndpoint : EndpointBase
{
    internal AssistantsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    /// Assistants endpoint.
    /// </summary>
    protected override CapabilityEndpoints Endpoint =>  CapabilityEndpoints.Assistants;
    
    /// <summary>
    ///     Get list of assistants.
    /// </summary>
    /// <param name="query">(optional) <see cref="ListQuery" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns>
    ///     <see cref="ListResponse{Assistant}" />
    /// </returns>
    public Task<HttpCallResult<ListResponse<Assistant>>> ListAssistantsAsync(ListQuery? query = null, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<ListResponse<Assistant>>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Assistants, GetUrl(Api.GetProvider(LLmProviders.OpenAi)), query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }

    /// <summary>
    ///     Create an assistant.
    /// </summary>
    /// <param name="request"><see cref="CreateAssistantRequest" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="Assistant" />.</returns>
    public Task<HttpCallResult<Assistant>> CreateAssistantAsync(CreateAssistantRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<Assistant>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Assistants, GetUrl(Api.GetProvider(LLmProviders.OpenAi)), request, ct: cancellationToken);
    }

    /// <summary>
    ///     Retrieves an assistant.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant to retrieve.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="Assistant" />.</returns>
    public Task<HttpCallResult<Assistant>> RetrieveAssistantAsync(string assistantId, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<Assistant>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Assistants, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{assistantId}"), null, cancellationToken);
    }

    /// <summary>
    ///     Modifies an assistant. All fields in the existing assistant are replaced with the fields from
    ///     <see cref="request" />.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant to modify.</param>
    /// <param name="request"><see cref="CreateAssistantRequest" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="Assistant" />.</returns>
    public Task<HttpCallResult<Assistant>> ModifyAssistantAsync(string assistantId, CreateAssistantRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<Assistant>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Assistants, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{assistantId}"), request, ct: cancellationToken);
    }

    /// <summary>
    ///     Delete an assistant.
    /// </summary>
    /// <param name="assistantId">The ID of the assistant to delete.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns>True, if the assistant was deleted.</returns>
    public async Task<HttpCallResult<bool>> DeleteAssistantAsync(string assistantId, CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi), CapabilityEndpoints.Assistants, HttpVerbs.Delete, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{assistantId}"), ct: cancellationToken).ConfigureAwait(false);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, status.Request);
    }
}