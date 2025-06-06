using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.XAi;

/// <summary>
/// Chat features supported only by XAi.
/// </summary>
public class ChatRequestVendorXAiExtensions
{
    /// <summary>
    /// Set the parameters to be used for searched data. If not set, no data will be acquired by the model.
    /// </summary>
    public ChatRequestVendorXAiExtensionsSearchParameters? SearchParameters { get; set; }
}

/// <summary>
/// Set the parameters to be used for searched data. If not set, no data will be acquired by the model.
/// </summary>
public class ChatRequestVendorXAiExtensionsSearchParameters
{
    /// <summary>
    /// Date from which to consider the results.
    /// </summary>
    public DateTime? FromDate { get; set; }
    
    /// <summary>
    /// Date up to which to consider the results.
    /// </summary>
    public DateTime? ToDate { get; set; }
    
    /// <summary>
    /// Maximum number of search results to use. (default 15, min 1, max 30)
    /// </summary>
    public int? MaxSearchResults { get; set; }
    
    /// <summary>
    /// Choose the mode to query realtime data:<br/>
    /// off (default): no search performed and no external will be considered.<br/>
    /// on: the model will search in every source for relevant data.<br/>
    /// auto: the model choose whether to search data or not and where to search the data.
    /// </summary>
    public ChatRequestVendorXAiExtensionsSearchParametersModes? Mode { get; set; }
    
    /// <summary>
    /// Whether to return citations in the response or not.
    /// </summary>
    public bool? ReturnCitations { get; set; }
    
    /// <summary>
    /// List of sources to search in. If no sources specified, the model will look over the web and X by default.
    /// </summary>
    [JsonIgnore]
    public List<IChatRequestVendorXAiExtensionsSearchParametersSource>? Sources { get; set; }
}

/// <summary>
/// Shared interface for XAI sources.
/// </summary>
public interface IChatRequestVendorXAiExtensionsSearchParametersSource
{
    
}

/// <summary>
/// X Source.
/// </summary>
public class ChatRequestVendorXAiExtensionsSearchParametersSourceX : IChatRequestVendorXAiExtensionsSearchParametersSource
{
    /// <summary>
    /// X Handles of the users from whom to consider the posts. Only available if mode is auto, on or x.
    /// </summary>
    public List<string>? XHandles { get; set; }
}

/// <summary>
/// Web Source.
/// </summary>
public class ChatRequestVendorXAiExtensionsSearchParametersSourceWeb : IChatRequestVendorXAiExtensionsSearchParametersSource
{
    /// <summary>
    /// ISO alpha-2 code of the country. If the country is set, only data coming from this country will be considered. See https://en.wikipedia.org/wiki/ISO_3166-2.
    /// </summary>
    public string? Country { get; set; }
    
    /// <summary>
    /// List of website to exclude from the search results without protocol specification or subdomains. A maximum of 5 websites can be excluded.
    /// </summary>
    public List<string>? ExcludedWebsites { get; set; }
    
    /// <summary>
    /// If set to true, mature content won't be considered during the search. Default to true.
    /// </summary>
    public bool? SafeSearch { get; set; }
}

/// <summary>
/// News Source.
/// </summary>
public class ChatRequestVendorXAiExtensionsSearchParametersSourceNews : IChatRequestVendorXAiExtensionsSearchParametersSource
{
    /// <summary>
    /// ISO alpha-2 code of the country. If the country is set, only data coming from this country will be considered. See https://en.wikipedia.org/wiki/ISO_3166-2.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// If set to true, mature content won't be considered during the search. Default to true.
    /// </summary>
    public bool? SafeSearch { get; set; }
}

/// <summary>
/// RSS Source.
/// </summary>
public class ChatRequestVendorXAiExtensionsSearchParametersSourceRss : IChatRequestVendorXAiExtensionsSearchParametersSource
{
    /// <summary>
    /// Links of the RSS feeds.
    /// </summary>
    public List<string> Links { get; set; } = [];
}

/// <summary>
/// Modes of search.
/// </summary>
public enum ChatRequestVendorXAiExtensionsSearchParametersModes
{
    /// <summary>
    ///  The model choose whether to search data or not and where to search the data.
    /// </summary>
    Auto,
    
    /// <summary>
    /// No search performed and no external will be considered.
    /// </summary>
    Off,
    
    /// <summary>
    /// The model will search in every source for relevant data. 
    /// </summary>
    On
}