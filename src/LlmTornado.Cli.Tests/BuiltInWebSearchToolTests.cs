using LlmTornado.Cli.Core.Tools;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class BuiltInWebSearchToolTests
{
    [Test]
    public void ParseResults_Extracts_Title_Url_And_Snippet()
    {
        const string html = """
            <html>
              <a rel="nofollow" href="https://example.com/page">Example &amp; Result</a>
              <td class="result-snippet">A <b>useful</b> snippet &amp; detail.</td>
            </html>
            """;

        List<WebSearchResult> results = BuiltInWebSearchTool.ParseResults(html, 8);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Title, Is.EqualTo("Example & Result"));
        Assert.That(results[0].Url, Is.EqualTo("https://example.com/page"));
        Assert.That(results[0].Snippet, Is.EqualTo("A useful snippet & detail."));
    }

    [Test]
    public void ParseResults_Decodes_DuckDuckGo_Redirects()
    {
        const string html = """
            <html>
              <a rel="nofollow" href="/l/?kh=-1&amp;uddg=https%3A%2F%2Fexample.com%2Fdecoded">Decoded</a>
            </html>
            """;

        List<WebSearchResult> results = BuiltInWebSearchTool.ParseResults(html, 8);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Url, Is.EqualTo("https://example.com/decoded"));
    }
}
