using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;

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
    public async Task<SkillListResponse> ListSkillsAsync(int? limit = null, string? before = null, string? after = null)
    {
        var queryParams = new Dictionary<string, object>();
        
        if (limit.HasValue)
        {
            queryParams["limit"] = limit.Value;
        }
        
        if (!string.IsNullOrEmpty(before))
        {
            queryParams["before"] = before;
        }
        
        if (!string.IsNullOrEmpty(after))
        {
            queryParams["after"] = after;
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
        return (await HttpPost<Skill>(provider, Endpoint, postData: request).ConfigureAwait(false)).Data!;
    }
    
    /// <summary>
    /// Creates a new skill.
    /// </summary>
    /// <param name="name">The name of the skill</param>
    /// <param name="description">Optional description of what the skill does</param>
    /// <returns>The created skill</returns>
    public async Task<Skill> CreateSkillAsync(string name, string? description = null)
    {
        return await CreateSkillAsync(new CreateSkillRequest(name, description ?? string.Empty));
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
    /// Updates a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill to update</param>
    /// <param name="request">The update request</param>
    /// <returns>The updated skill</returns>
    public async Task<Skill> UpdateSkillAsync(string skillId, UpdateSkillRequest request)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return (await HttpAtomic<Skill>(provider, Endpoint, HttpVerbs.Patch, GetUrl(provider, $"/{skillId}"), postData: request).ConfigureAwait(false)).Data!;
    }
    
    /// <summary>
    /// Deletes a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill to delete</param>
    /// <returns>True if deletion was successful</returns>
    public async Task<bool> DeleteSkillAsync(string skillId)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        HttpCallResult<Skill> result = await HttpAtomic<Skill>(provider, Endpoint, HttpVerbs.Delete, GetUrl(provider, $"/{skillId}")).ConfigureAwait(false);
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
    public async Task<SkillVersionListResponse> ListSkillVersionsAsync(string skillId, int? limit = null, string? before = null, string? after = null)
    {
        var queryParams = new Dictionary<string, object>();
        
        if (limit.HasValue)
        {
            queryParams["limit"] = limit.Value;
        }
        
        if (!string.IsNullOrEmpty(before))
        {
            queryParams["before"] = before;
        }
        
        if (!string.IsNullOrEmpty(after))
        {
            queryParams["after"] = after;
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
        return (await HttpPost<SkillVersion>(provider, Endpoint, GetUrl(provider, $"/{skillId}/versions"), postData: request).ConfigureAwait(false)).Data!;
    }
    
    /// <summary>
    /// Creates a new version of a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill</param>
    /// <param name="systemPrompt">The system prompt for this version</param>
    /// <returns>The created skill version</returns>
    public async Task<SkillVersion> CreateSkillVersionAsync(string skillId, string systemPrompt)
    {
        return await CreateSkillVersionAsync(skillId, new CreateSkillVersionRequest(systemPrompt));
    }
    
    /// <summary>
    /// Gets a specific version of a skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill</param>
    /// <param name="versionId">The ID of the version to retrieve</param>
    /// <returns>The requested skill version</returns>
    public async Task<SkillVersion> GetSkillVersionAsync(string skillId, string versionId)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return (await HttpGet<SkillVersion>(provider, Endpoint, GetUrl(provider, $"/{skillId}/versions/{versionId}")).ConfigureAwait(false)).Data!;
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
        HttpCallResult<SkillVersion> result = await HttpAtomic<SkillVersion>(provider, Endpoint, HttpVerbs.Delete, GetUrl(provider, $"/{skillId}/versions/{versionId}")).ConfigureAwait(false);
        return result.Ok;
    }
}
