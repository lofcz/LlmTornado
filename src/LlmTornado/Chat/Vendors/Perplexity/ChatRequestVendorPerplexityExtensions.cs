using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Perplexity;

/// <summary>
/// Chat features supported only by Perplexity.
/// </summary>
public class ChatRequestVendorPerplexityExtensions
{
    /// <summary>
    /// What results to prioritize.
    /// </summary>
    public ChatRequestVendorPerplexitySearchModes? SearchMode { get; set; }
    
    /// <summary>
    /// Include only results after given date.
    /// </summary>
    public DateTime? SearchAfterDateFilter { get; set; }
    
    /// <summary>
    /// Include only results before given date.
    /// </summary>
    public DateTime? SearchBeforeDateFilter { get; set; }
    
    /// <summary>
    /// Filters search results based on time (e.g., 'week', 'day').
    /// </summary>
    public string? SearchRecencyFilter { get; set; }
    
    /// <summary>
    /// Determines whether related questions should be returned.
    /// </summary>
    public bool? ReturnRelatedQuestions { get; set; }
    
    /// <summary>
    /// Determines whether search results should include images.
    /// </summary>
    public bool? ReturnImages { get; set; }
    
    /// <summary>
    /// Domains which will be included in the search.
    /// </summary>
    public List<string>? IncludeDomains { get; set; }
    
    /// <summary>
    /// Domains which will be excluded from the search.
    /// </summary>
    public List<string>? ExcludeDomains { get; set; }
}

/// <summary>
/// Search modes for Perplexity
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRequestVendorPerplexitySearchModes
{
    /// <summary>
    /// Prioritize results from peer-reviewed papers, journal articles, and research publications.
    /// </summary>
    [EnumMember(Value = "academic")] 
    Academic
}