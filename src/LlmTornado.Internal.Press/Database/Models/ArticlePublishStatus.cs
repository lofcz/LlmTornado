namespace LlmTornado.Internal.Press.Database.Models;

public class ArticlePublishStatus
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string Platform { get; set; } = string.Empty; // "devto", "medium", etc.
    public string Status { get; set; } = "Pending"; // Pending, Published, Failed
    public string? PublishedUrl { get; set; }
    public string? PlatformArticleId { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime? LastAttemptDate { get; set; }
    public int AttemptCount { get; set; } = 0;
    public string? LastError { get; set; }
    
    // Navigation
    public Article Article { get; set; } = null!;
}

