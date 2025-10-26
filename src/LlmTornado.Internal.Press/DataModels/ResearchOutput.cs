using System;

namespace LlmTornado.Internal.Press.DataModels;

public class ResearchOutput
{
    public ResearchFact[] Facts { get; set; } = Array.Empty<ResearchFact>();
    public ResearchSource[] Sources { get; set; } = Array.Empty<ResearchSource>();
    public string[] KeyInsights { get; set; } = Array.Empty<string>();
    public string Summary { get; set; } = string.Empty;
    public DateTime ResearchDate { get; set; } = DateTime.UtcNow;
}

public class ResearchFact
{
    public string Fact { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class ResearchSource
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
}

