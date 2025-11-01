namespace LlmTornado.Internal.Press.DataModels;

public class ArticleOutput
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty; // Markdown format
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public int WordCount { get; set; }
    public string Slug { get; set; } = string.Empty;
}

