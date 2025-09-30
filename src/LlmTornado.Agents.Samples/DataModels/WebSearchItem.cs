namespace LlmTornado.Agents.Samples.DataModels;

public struct WebSearchItem
{
    public string reason { get; set; }
    public string query { get; set; }

    public WebSearchItem(string reason, string query)
    {
        this.reason = reason;
        this.query = query;
    }
}
