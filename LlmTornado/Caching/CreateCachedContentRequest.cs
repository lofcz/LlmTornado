using System;
using System.Collections.Generic;
using LlmTornado.Caching.Vendors.Google;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Caching;

/// <summary>
/// Request to cache content.
/// </summary>
public class CreateCachedContentRequest
{
    /// <summary>
    /// How long should be the resource cached.
    /// </summary>
    public TimeSpan TimeToLive { get; set; }

    /// <summary>
    /// The resource name referring to the cached content. Format: cachedContents/{id}
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The user-generated meaningful display name of the cached content. Maximum 128 Unicode characters.
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// The name of the Model to use for cached content.
    /// Context caching is only available for stable models with fixed versions (for example, gemini-1.5-pro-001). You must include the version postfix (for example, the -001 in gemini-1.5-pro-001).
    /// </summary>
    public ChatModel Model { get; set; }
    
    /// <summary>
    /// System message to cache. Currently only text-based messages are supported.
    /// </summary>
    public CachedContent? System { get; set; }
    
    /// <summary>
    /// The content to cache.
    /// </summary>
    public List<CachedContent>? Contents { get; set; }
    
    // todo: map tools, toolConfig: https://ai.google.dev/api/caching#Content

    /// <summary>
    /// Creates a caching request. Either content or system must be set.
    /// </summary>
    /// <param name="timeToLive"></param>
    /// <param name="model"></param>
    /// <param name="contents"></param>
    /// <param name="system"></param>
    public CreateCachedContentRequest(TimeSpan timeToLive, ChatModel model, List<CachedContent>? contents = null, CachedContent? system = null)
    {
        TimeToLive = timeToLive;
        Model = model;
        Contents = contents;
        System = system;
    }
    
    /// <summary>
    /// Creates a caching request. Either content or system must be set.
    /// </summary>
    /// <param name="secondsToLive"></param>
    /// <param name="model"></param>
    /// <param name="contents"></param>
    /// <param name="system"></param>
    public CreateCachedContentRequest(int secondsToLive, ChatModel model, List<CachedContent>? contents = null, CachedContent? system = null)
    {
        TimeToLive = TimeSpan.FromSeconds(secondsToLive);
        Model = model;
        Contents = contents;
        System = system;
    }

    internal object Serialize(LLmProviders provider)
    {
        return provider switch
        {
            LLmProviders.Google => new VendorGoogleCachingCreateCachedContentRequest(this),
            _ => new { }
        };
    } 
}