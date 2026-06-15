using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;

namespace LlmTornado.Skills;

/// <summary>
/// OpenAI Skills API (<c>/v1/skills</c>) for uploading and managing skills referenced by the Responses shell tool.
/// </summary>
public class OpenAiSkillsEndpoint : EndpointBase
{
    internal OpenAiSkillsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Skills;

    /// <summary>
    /// Creates a new skill from uploaded files or a zip bundle.
    /// </summary>
    public async Task<OpenAiSkill> CreateSkillAsync(OpenAiSkillUploadRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        using MultipartFormDataContent content = request.ToMultipartContent();
        HttpCallResult<OpenAiSkill> result = await HttpPost<OpenAiSkill>(
            provider,
            Endpoint,
            postData: content,
            ct: cancellationToken).ConfigureAwait(false);

        if (!result.Ok)
        {
            throw result.Exception;
        }

        return result.Data;
    }

    /// <summary>
    /// Lists skills for the current OpenAI project.
    /// </summary>
    public Task<OpenAiSkillList> ListSkillsAsync(int? limit = null, string? after = null, string? order = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object>? query = BuildListQuery(limit, after, order);
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpGetData<OpenAiSkillList>(provider, null, query, cancellationToken);
    }

    /// <summary>
    /// Retrieves a skill by ID.
    /// </summary>
    public Task<OpenAiSkill> GetSkillAsync(string skillId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpGetData<OpenAiSkill>(provider, $"/{skillId}", null, cancellationToken);
    }

    /// <summary>
    /// Updates the default version pointer for a skill.
    /// </summary>
    public Task<OpenAiSkill> SetDefaultVersionAsync(string skillId, string defaultVersion, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpPostData<OpenAiSkill>(provider, $"/{skillId}", new { default_version = defaultVersion }, cancellationToken);
    }

    /// <summary>
    /// Deletes a skill by ID.
    /// </summary>
    public async Task<bool> DeleteSkillAsync(string skillId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        HttpCallResult<OpenAiSkillDeleted> result = await HttpDelete<OpenAiSkillDeleted>(
            provider,
            Endpoint,
            url: GetUrl(provider, $"/{skillId}"),
            ct: cancellationToken).ConfigureAwait(false);

        return result.Ok && result.Data.Deleted;
    }

    /// <summary>
    /// Creates a new immutable skill version.
    /// </summary>
    public async Task<OpenAiSkillVersion> CreateSkillVersionAsync(string skillId, OpenAiSkillUploadRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        using MultipartFormDataContent content = request.ToMultipartContent();
        HttpCallResult<OpenAiSkillVersion> result = await HttpPost<OpenAiSkillVersion>(
            provider,
            Endpoint,
            url: GetUrl(provider, $"/{skillId}/versions"),
            postData: content,
            ct: cancellationToken).ConfigureAwait(false);

        if (!result.Ok)
        {
            throw result.Exception;
        }

        return result.Data;
    }

    /// <summary>
    /// Lists versions for a skill.
    /// </summary>
    public Task<OpenAiSkillVersionList> ListSkillVersionsAsync(string skillId, int? limit = null, string? after = null, string? order = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object>? query = BuildListQuery(limit, after, order);
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpGetData<OpenAiSkillVersionList>(provider, $"/{skillId}/versions", query, cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific skill version.
    /// </summary>
    public Task<OpenAiSkillVersion> GetSkillVersionAsync(string skillId, string version, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        return HttpGetData<OpenAiSkillVersion>(provider, $"/{skillId}/versions/{version}", null, cancellationToken);
    }

    /// <summary>
    /// Deletes a skill version.
    /// </summary>
    public async Task<bool> DeleteSkillVersionAsync(string skillId, string version, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
        HttpCallResult<OpenAiSkillDeleted> result = await HttpAtomic<OpenAiSkillDeleted>(
            provider,
            Endpoint,
            HttpVerbs.Delete,
            GetUrl(provider, $"/{skillId}/versions/{version}"),
            ct: cancellationToken).ConfigureAwait(false);

        return result.Ok;
    }

    private static Dictionary<string, object>? BuildListQuery(int? limit, string? after, string? order)
    {
        Dictionary<string, object>? query = null;

        if (limit is not null)
        {
            query ??= new Dictionary<string, object>();
            query["limit"] = limit.Value;
        }

        if (!string.IsNullOrEmpty(after))
        {
            query ??= new Dictionary<string, object>();
            query["after"] = after;
        }

        if (!string.IsNullOrEmpty(order))
        {
            query ??= new Dictionary<string, object>();
            query["order"] = order;
        }

        return query;
    }

    private async Task<T> HttpGetData<T>(IEndpointProvider provider, string? pathSuffix, Dictionary<string, object>? query, CancellationToken cancellationToken)
    {
        HttpCallResult<T> result = await HttpGet<T>(
            provider,
            Endpoint,
            url: pathSuffix is null ? null : GetUrl(provider, pathSuffix),
            queryParams: query,
            ct: cancellationToken).ConfigureAwait(false);

        if (!result.Ok)
        {
            throw result.Exception;
        }

        return result.Data;
    }

    private async Task<T> HttpPostData<T>(IEndpointProvider provider, string pathSuffix, object body, CancellationToken cancellationToken)
    {
        HttpCallResult<T> result = await HttpPost<T>(
            provider,
            Endpoint,
            url: GetUrl(provider, pathSuffix),
            postData: body,
            ct: cancellationToken).ConfigureAwait(false);

        if (!result.Ok)
        {
            throw result.Exception;
        }

        return result.Data;
    }
}
