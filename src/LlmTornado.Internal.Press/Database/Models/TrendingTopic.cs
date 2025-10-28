using System;
using System.ComponentModel.DataAnnotations;

namespace LlmTornado.Internal.Press.Database.Models;

public class TrendingTopic
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Topic { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public double Relevance { get; set; }

    [Required]
    public DateTime DiscoveredDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastSeenDate { get; set; } = DateTime.UtcNow;

    [Required]
    public string Source { get; set; } = string.Empty;
    
    public string Category { get; set; } = string.Empty;

    public string? MetadataJson { get; set; } // Additional data as JSON

    public bool IsActive { get; set; } = true;

    public int ArticleCount { get; set; } = 0; // How many articles used this trend
}

