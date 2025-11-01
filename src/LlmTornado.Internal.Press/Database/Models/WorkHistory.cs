using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LlmTornado.Internal.Press.Database.Models;

public class WorkHistory
{
    [Key]
    public int Id { get; set; }

    public int? ArticleId { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string? Details { get; set; }
    
    public string? AgentName { get; set; }
    
    public string? Status { get; set; }

    public string? DataJson { get; set; } // Additional structured data

    // Navigation property
    [ForeignKey("ArticleId")]
    public virtual Article? Article { get; set; }
}

public static class WorkAction
{
    public const string QueueCreated = "QueueCreated";
    public const string TrendAnalysis = "TrendAnalysis";
    public const string IdeaGenerated = "IdeaGenerated";
    public const string ResearchStarted = "ResearchStarted";
    public const string ResearchCompleted = "ResearchCompleted";
    public const string WritingStarted = "WritingStarted";
    public const string WritingCompleted = "WritingCompleted";
    public const string ReviewStarted = "ReviewStarted";
    public const string ReviewCompleted = "ReviewCompleted";
    public const string ImageGenerated = "ImageGenerated";
    public const string ArticleExported = "ArticleExported";
    public const string ArticlePublished = "ArticlePublished";
    public const string ErrorOccurred = "ErrorOccurred";
}

