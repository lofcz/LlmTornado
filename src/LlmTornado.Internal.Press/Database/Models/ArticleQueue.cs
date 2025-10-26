using System;
using System.ComponentModel.DataAnnotations;

namespace LlmTornado.Internal.Press.Database.Models;

public class ArticleQueue
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string IdeaSummary { get; set; } = string.Empty;

    public int Priority { get; set; } = 0;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = QueueStatus.Pending;

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedDate { get; set; }

    public DateTime? ScheduledDate { get; set; }

    public int? ArticleId { get; set; }

    [MaxLength(500)]
    public string Tags { get; set; } = "[]"; // Stored as JSON array

    public double EstimatedRelevance { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int AttemptCount { get; set; } = 0;
}

public static class QueueStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

