using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Files;

namespace LlmTornado.Skills;

/// <summary>
/// Endpoint for managing Anthropic Skills.
/// Skills allow you to create specialized prompts and configurations that Claude can automatically select and use.
/// </summary>
public class SkillsEndpoint : EndpointBase
{
    /// <summary>
    /// Constructor for the skills endpoint. Typically not called directly.
    /// </summary>
    /// <param name="api">The API instance to use</param>
    internal SkillsEndpoint(TornadoApi api) : base(api)
    {
    }
    
    /// <summary>
    /// Gets the capability endpoint type.
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Skills;
    
    /// <summary>
    /// Lists all skills for the authenticated account.
    /// </summary>
    /// <param name="limit">Maximum number of skills to return (default 20, max 100)</param>
    /// <param name="before">Return skills with IDs before this cursor</param>
    /// <param name="after">Return skills with IDs after this cursor</param>
    /// <returns>A list of skills</returns>
    public async Task<SkillListResponse> ListSkillsAsync(int? limit = null, string? page = null, string? source = null)
    {
        var queryParams = new Dictionary<string, object>();
        
        if (limit.HasValue)
        {
            queryParams["limit"] = limit.Value;
        }

        if (!string.IsNullOrEmpty(page))
        {
            queryParams["page"] = page;
        }


        if (!string.IsNullOrEmpty(source))
        {
            queryParams["source"] = source;
        }
        
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return (await HttpGet<SkillListResponse>(provider, Endpoint, queryParams: queryParams).ConfigureAwait(false)).Data!;
    }
    
    /// <summary>
    /// Creates a new skill.
    /// </summary>
    /// <param name="request">The skill creation request</param>
    /// <returns>The created skill</returns>
    public async Task<Skill> CreateSkillAsync(CreateSkillRequest request)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        MultipartFormDataContent content = request.ToMultipartContent();
        
        try
        {
            var result = (await HttpPost<Skill>(provider, Endpoint, postData: content).ConfigureAwait(false));
            return result.Data!;
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            content.Dispose();
        }
    }
    
    /// <summary>
    /// Creates a new skill.
    /// </summary>
    /// <param name="name">The display title of the skill</param>
    /// <param name="files">Optional files to upload for the skill</param>
    /// <returns>The created skill</returns>
    public async Task<Skill> CreateSkillAsync(string name, FileUploadRequest[]? files = null)
    {
        return await CreateSkillAsync(new CreateSkillRequest(name, files));
    }
    
    /// <summary>
    /// Gets a specific skill by ID.
    /// </summary>
    /// <param name="skillId">The ID of the skill to retrieve</param>
    /// <returns>The requested skill</returns>
    public async Task<Skill> GetSkillAsync(string skillId)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return (await HttpGet<Skill>(provider, Endpoint, GetUrl(provider, $"/{skillId}")).ConfigureAwait(false)).Data!;
    }
    
    
    /// <summary>
    /// Deletes a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill to delete</param>
    /// <returns>True if deletion was successful</returns>
    public async Task<bool> DeleteSkillAsync(string skillId)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        HttpCallResult<SkillDeleteResponse> result = await HttpAtomic<SkillDeleteResponse>(provider, Endpoint, HttpVerbs.Delete, GetUrl(provider, $"/{skillId}")).ConfigureAwait(false);
        return result.Ok;
    }
    
    /// <summary>
    /// Lists all versions of a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill</param>
    /// <param name="limit">Maximum number of versions to return (default 20, max 100)</param>
    /// <param name="before">Return versions with IDs before this cursor</param>
    /// <param name="after">Return versions with IDs after this cursor</param>
    /// <returns>A list of skill versions</returns>
    public async Task<SkillVersionListResponse> ListSkillVersionsAsync(string skillId, int? limit = null, string? page = null)
    {
        var queryParams = new Dictionary<string, object>();

        if (limit.HasValue)
        {
            queryParams["limit"] = limit.Value;
        }

        if (!string.IsNullOrEmpty(page))
        {
            queryParams["page"] = page;
        }

        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return (await HttpGet<SkillVersionListResponse>(provider, Endpoint, GetUrl(provider, $"/{skillId}/versions"), queryParams: queryParams).ConfigureAwait(false)).Data!;
    }
    
    /// <summary>
    /// Creates a new version of a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill</param>
    /// <param name="request">The version creation request</param>
    /// <returns>The created skill version</returns>
    public async Task<SkillVersion> CreateSkillVersionAsync(string skillId, CreateSkillVersionRequest request)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        MultipartFormDataContent content = request.ToMultipartContent();
        
        try
        {
            return (await HttpPost<SkillVersion>(provider, Endpoint, GetUrl(provider, $"/{skillId}/versions"), postData: content).ConfigureAwait(false)).Data!;
        }
        finally
        {
            content.Dispose();
        }
    }
    
    
    /// <summary>
    /// Gets a specific version of a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill</param>
    /// <param name="versionId">The ID of the version to retrieve</param>
    /// <returns>The requested skill version</returns>
    public async Task<SkillVersion> GetSkillVersionAsync(string skillId, string version)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return (await HttpGet<SkillVersion>(provider, Endpoint, GetUrl(provider, $"/{skillId}/versions/{version}")).ConfigureAwait(false)).Data!;
    }
    
    /// <summary>
    /// Deletes a specific version of a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill</param>
    /// <param name="versionId">The ID of the version to delete</param>
    /// <returns>True if deletion was successful</returns>
    public async Task<bool> DeleteSkillVersionAsync(string skillId, string versionId)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        HttpCallResult<SkillVersionDeleteResponse> result = await HttpAtomic<SkillVersionDeleteResponse>(provider, Endpoint, HttpVerbs.Delete, GetUrl(provider, $"/{skillId}/versions/{versionId}")).ConfigureAwait(false);
        return result.Ok;
    }
}
