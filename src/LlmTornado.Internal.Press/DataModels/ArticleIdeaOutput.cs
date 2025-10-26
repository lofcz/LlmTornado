using System.Collections.Generic;

namespace LlmTornado.Internal.Press.DataModels;

public class ArticleIdeaOutput
{
    public ArticleIdea[] Ideas { get; set; } = Array.Empty<ArticleIdea>();
}

public class ArticleIdea
{
    public string Title { get; set; } = string.Empty;
    public string IdeaSummary { get; set; } = string.Empty;
    public double EstimatedRelevance { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Reasoning { get; set; } = string.Empty;
}

