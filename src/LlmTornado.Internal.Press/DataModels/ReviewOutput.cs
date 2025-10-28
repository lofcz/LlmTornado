namespace LlmTornado.Internal.Press.DataModels;

public class ReviewOutput
{
    public bool Approved { get; set; }
    public double QualityScore { get; set; }
    public ReviewIssue[] Issues { get; set; } = [];
    public string[] Suggestions { get; set; } = [];
    public QualityMetrics Metrics { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class ReviewIssue
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string Suggestion { get; set; } = string.Empty;
}

public class QualityMetrics
{
    public int WordCount { get; set; }
    public int ReadabilityScore { get; set; }
    public int SeoScore { get; set; }
    public bool HasSources { get; set; }
    public bool HasClickbaitTitle { get; set; }
    public bool HasTemporalRelevance { get; set; }
    public bool ObjectiveAlignment { get; set; }
}

