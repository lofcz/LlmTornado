using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using LlmTornado.Common;

namespace LlmTornado.Cli.Core.Tools;

public static partial class BuiltInWebSearchTool
{
    private const string DuckDuckGoLiteUrl = "https://lite.duckduckgo.com/lite/";
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
    };

    public static Tool Build()
    {
        return new Tool(
            new Func<WebSearchToolRequest, Task<string>>(SearchAsync),
            "web_search",
            "Search the web using DuckDuckGo Lite. Returns compact JSON results with title, URL, and snippet.");
    }

    private static async Task<string> SearchAsync(WebSearchToolRequest request)
    {
        string query = request.Query?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(query))
            return "ERROR: query is required.";

        int limit = Math.Clamp(request.Limit <= 0 ? 8 : request.Limit, 1, 10);

        try
        {
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, DuckDuckGoLiteUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["q"] = query,
                }),
            };
            httpRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; LlmTornado-CLI/1.0)");
            httpRequest.Headers.Accept.ParseAdd("text/html");

            using HttpResponseMessage response = await Client.SendAsync(httpRequest).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            List<WebSearchResult> results = ParseResults(html, limit);

            return JsonSerializer.Serialize(new
            {
                query,
                results,
                count = results.Count,
            }, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return $"ERROR: Search request failed: {ex.Message}";
        }
    }

    internal static List<WebSearchResult> ParseResults(string html, int limit)
    {
        List<WebSearchResult> results = [];
        MatchCollection links = LinkRegex().Matches(html);
        MatchCollection snippets = SnippetRegex().Matches(html);

        for (int i = 0; i < links.Count && results.Count < limit; i++)
        {
            Match link = links[i];
            string url = CleanUrl(WebUtility.HtmlDecode(link.Groups["url"].Value));
            if (string.IsNullOrWhiteSpace(url) || url.Contains("duckduckgo.com", StringComparison.OrdinalIgnoreCase))
                continue;

            string title = CleanText(link.Groups["title"].Value);
            string snippet = i < snippets.Count ? CleanText(snippets[i].Groups["snippet"].Value) : "";

            results.Add(new WebSearchResult(title, url, snippet));
        }

        return results;
    }

    private static string CleanUrl(string url)
    {
        if (url.StartsWith("//", StringComparison.Ordinal))
            url = "https:" + url;

        if (url.StartsWith("/l/", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate("https://duckduckgo.com" + url, UriKind.Absolute, out Uri? redirect))
        {
            string? uddg = redirect.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .FirstOrDefault(parts => parts[0].Equals("uddg", StringComparison.OrdinalIgnoreCase))?[1];
            if (!string.IsNullOrWhiteSpace(uddg))
                url = WebUtility.UrlDecode(uddg);
        }

        return url.Trim();
    }

    private static string CleanText(string html)
    {
        string text = TagRegex().Replace(html, "");
        text = WebUtility.HtmlDecode(text);
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    [GeneratedRegex("""<a[^>]+rel="nofollow"[^>]+href="(?<url>[^"]+)"[^>]*>\s*(?<title>.+?)\s*</a>""", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();

    [GeneratedRegex("""<td[^>]*class="result-snippet"[^>]*>(?<snippet>.*?)</td>""", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex SnippetRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

public sealed class WebSearchToolRequest
{
    public string? Query { get; set; }
    public int Limit { get; set; } = 8;
}

public sealed record WebSearchResult(string Title, string Url, string Snippet);
