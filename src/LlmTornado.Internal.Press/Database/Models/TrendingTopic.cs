using System;
using System.ComponentModel.DataAnnotations;

namespace LlmTornado.Internal.Press.Database.Models;

public class TrendingTopic
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public double Relevance { get; set; }

    [Required]
    public DateTime DiscoveredDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastSeenDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(200)]
    public string Source { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Category { get; set; } = string.Empty;

    public string? MetadataJson { get; set; } // Additional data as JSON

    public bool IsActive { get; set; } = true;

    public int ArticleCount { get; set; } = 0; // How many articles used this trend
}

