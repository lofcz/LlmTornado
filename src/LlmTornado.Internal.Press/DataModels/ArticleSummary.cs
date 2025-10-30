namespace LlmTornado.Internal.Press.DataModels;

/// <summary>
/// Comprehensive summary of an article for use by other AI agents
/// </summary>
public class ArticleSummary
{
    /// <summary>
    /// 2-3 sentence executive summary of the article
    /// </summary>
    public string ExecutiveSummary { get; set; } = string.Empty;
    
    /// <summary>
    /// Key technical points covered in the article
    /// </summary>
    public string[] KeyPoints { get; set; } = [];
    
    /// <summary>
    /// Suggestion for what kind of visual/image would best represent this article
    /// </summary>
    public string VisualSuggestion { get; set; } = string.Empty;
    
    /// <summary>
    /// Target audience description
    /// </summary>
    public string TargetAudience { get; set; } = string.Empty;
    
    /// <summary>
    /// Emotional tone of the article
    /// </summary>
    public string EmotionalTone { get; set; } = string.Empty;
    
    /// <summary>
    /// One-liner hook for social media (max 100 chars)
    /// </summary>
    public string SocialMediaHook { get; set; } = string.Empty;
    
    /// <summary>
    /// Full summary text from the AI
    /// </summary>
    public string FullSummary { get; set; } = string.Empty;
}

