using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LlmTornado.Internal.Press.Database.Models;

public class Article
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? ImageUrl { get; set; }

    public string? ImageVariationsJson { get; set; } // Image variation URLs as JSON dictionary

    [Required]
    public string Tags { get; set; } = "[]"; // Stored as JSON array

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedDate { get; set; }

    [Required]
    public string Objective { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = ArticleStatus.Draft;

    public int WordCount { get; set; }

    public double QualityScore { get; set; }

    public int IterationCount { get; set; }
    
    public string? Slug { get; set; }

    public string? SourcesJson { get; set; } // Research sources as JSON

    public string? MetadataJson { get; set; } // Additional metadata as JSON

    public string? SummaryJson { get; set; } // Article summary for use by other agents (ArticleSummary as JSON)

    // Navigation property
    public virtual ICollection<WorkHistory> WorkHistory { get; set; } = new List<WorkHistory>();
}

public static class ArticleStatus
{
    public const string Draft = "Draft";
    public const string InReview = "InReview";
    public const string Approved = "Approved";
    public const string Published = "Published";
    public const string Failed = "Failed";
}

