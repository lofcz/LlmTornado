namespace LlmTornado.Agents.Samples.DataModels;

public struct WebSearchResult
{
    public string Query { get; set; }
    public string Results { get; set; }
    public string Summary { get; set; }
    public WebSearchResult(string query, string results, string summary)
    {
        Query = query;
        Results = results;
        Summary = summary;
    }
}

