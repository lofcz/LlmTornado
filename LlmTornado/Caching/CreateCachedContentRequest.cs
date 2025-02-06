using System;
using System.Collections.Generic;
using LlmTornado.Caching.Vendors.Google;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
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
    
    /// <summary>
    /// Optional. Input only. Immutable. A list of Tools the model may use to generate the next response
    /// </summary>
    /// <returns></returns>
    public List<Tool>? Tools { get; set; }

    /// <summary>
    /// Optional. Input only. Immutable. Tool config. This config is shared for all tools.
    /// </summary>
    public OutboundToolChoice? ToolChoice { get; set; }

    /// <summary>
    /// Creates a caching request. Either content or system must be set.
    /// </summary>
    /// <param name="timeToLive"></param>
    /// <param name="model"></param>
    /// <param name="contents"></param>
    /// <param name="system"></param>
    /// <param name="tools"></param>
    /// <param name="toolChoice"></param>
    public CreateCachedContentRequest(TimeSpan timeToLive, ChatModel model, List<CachedContent>? contents = null, CachedContent? system = null, List<Tool>? tools = null, OutboundToolChoice? toolChoice = null)
    {
        TimeToLive = timeToLive;
        Model = model;
        Contents = contents;
        System = system;
        Tools = tools;
        ToolChoice = toolChoice;
    }
    
    /// <summary>
    /// Creates a caching request. Either content or system must be set.
    /// </summary>
    /// <param name="secondsToLive"></param>
    /// <param name="model"></param>
    /// <param name="contents"></param>
    /// <param name="system"></param>
    /// <param name="tools"></param>
    /// <param name="toolChoice"></param>
    public CreateCachedContentRequest(int secondsToLive, ChatModel model, List<CachedContent>? contents = null, CachedContent? system = null, List<Tool>? tools = null, OutboundToolChoice? toolChoice = null)
    {
        TimeToLive = TimeSpan.FromSeconds(secondsToLive);
        Model = model;
        Contents = contents;
        System = system;
        Tools = tools;
        ToolChoice = toolChoice;
    }

    /// <summary>
    /// Creates an empty caching request. You must additionally set at least one of: <see cref="System"/>, <see cref="Tools"/>, <see cref="Contents"/>
    /// </summary>
    /// <param name="secondsToLive"></param>
    /// <param name="model"></param>
    public CreateCachedContentRequest(int secondsToLive, ChatModel model)
    {
        TimeToLive = TimeSpan.FromSeconds(secondsToLive);
        Model = model;
    }
    
    /// <summary>
    /// Creates an empty caching request. You must additionally set at least one of: <see cref="System"/>, <see cref="Tools"/>, <see cref="Contents"/>
    /// </summary>
    /// <param name="timeToLive"></param>
    /// <param name="model"></param>
    public CreateCachedContentRequest(TimeSpan timeToLive, ChatModel model)
    {
        TimeToLive = timeToLive;
        Model = model;
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