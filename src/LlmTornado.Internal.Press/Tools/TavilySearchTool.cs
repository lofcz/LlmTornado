using LlmTornado.Internal.Press.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Tools;

public class TavilySearchTool
{
    private readonly string _apiKey;
    private readonly TavilyConfiguration _config;
    private readonly HttpClient _httpClient;
    private const string TavilyApiUrl = "https://api.tavily.com/search";

    public TavilySearchTool(string apiKey, TavilyConfiguration config)
    {
        _apiKey = apiKey;
        _config = config;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Search the web using Tavily API
    /// </summary>
    [Description("Searches the web for current information using Tavily search API")]
    public async Task<string> SearchAsync(
        [Description("The search query")] string query,
        [Description("Maximum number of results")] int? maxResults = null)
    {
        TavilySearchRequest request = new TavilySearchRequest
        {
            ApiKey = _apiKey,
            Query = query,
            MaxResults = maxResults ?? _config.MaxResults,
            SearchDepth = _config.SearchDepth,
            IncludeDomains = _config.IncludeDomains.Count > 0 ? _config.IncludeDomains : null,
            ExcludeDomains = _config.ExcludeDomains.Count > 0 ? _config.ExcludeDomains : null,
            IncludeAnswer = true,
            IncludeRawContent = false
        };

        string json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        });

        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(TavilyApiUrl, content);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            TavilySearchResponse? searchResponse = JsonConvert.DeserializeObject<TavilySearchResponse>(responseJson);

            if (searchResponse == null)
            {
                return "Error: Failed to parse Tavily response";
            }

            return FormatSearchResults(searchResponse);
        }
        catch (Exception ex)
        {
            return $"Error performing Tavily search: {ex.Message}";
        }
    }

    private string FormatSearchResults(TavilySearchResponse response)
    {
        StringBuilder sb = new StringBuilder();

        if (!string.IsNullOrEmpty(response.Answer))
        {
            sb.AppendLine("**Answer:**");
            sb.AppendLine(response.Answer);
            sb.AppendLine();
        }

        if (response.Results != null && response.Results.Count > 0)
        {
            sb.AppendLine("**Search Results:**");
            sb.AppendLine();

            for (int i = 0; i < response.Results.Count; i++)
            {
                TavilySearchResult result = response.Results[i];
                sb.AppendLine($"{i + 1}. **{result.Title}**");
                sb.AppendLine($"   URL: {result.Url}");
                sb.AppendLine($"   Score: {result.Score:F2}");
                if (!string.IsNullOrEmpty(result.Content))
                {
                    sb.AppendLine($"   Content: {result.Content}");
                }
                if (result.PublishedDate.HasValue)
                {
                    sb.AppendLine($"   Published: {result.PublishedDate.Value:yyyy-MM-dd}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}

// Tavily API request/response models
internal class TavilySearchRequest
{
    [JsonProperty("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonProperty("query")]
    public string Query { get; set; } = string.Empty;

    [JsonProperty("max_results")]
    public int MaxResults { get; set; } = 5;

    [JsonProperty("search_depth")]
    public string SearchDepth { get; set; } = "basic";

    [JsonProperty("include_domains")]
    public List<string>? IncludeDomains { get; set; }

    [JsonProperty("exclude_domains")]
    public List<string>? ExcludeDomains { get; set; }

    [JsonProperty("include_answer")]
    public bool IncludeAnswer { get; set; }

    [JsonProperty("include_raw_content")]
    public bool IncludeRawContent { get; set; }
}

internal class TavilySearchResponse
{
    [JsonProperty("answer")]
    public string? Answer { get; set; }

    [JsonProperty("query")]
    public string Query { get; set; } = string.Empty;

    [JsonProperty("results")]
    public List<TavilySearchResult> Results { get; set; } = [];

    [JsonProperty("response_time")]
    public double ResponseTime { get; set; }
}

internal class TavilySearchResult
{
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("score")]
    public double Score { get; set; }

    [JsonProperty("published_date")]
    public DateTime? PublishedDate { get; set; }
}

