using System;

namespace LlmTornado.Internal.Press.DataModels;

public class TrendAnalysisOutput
{
    public TrendItem[] Trends { get; set; } = [];
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Summary { get; set; } = string.Empty;
}

public class TrendItem
{
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Relevance { get; set; }
    public string Category { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = [];
    public string Reasoning { get; set; } = string.Empty;
}

