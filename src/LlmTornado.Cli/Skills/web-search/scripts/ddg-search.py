"""
DuckDuckGo search — searches the web using DuckDuckGo Lite (no API key needed).
Usage: ddg-search.py <query> [--max 8]

Arguments:
  query  The search query string.
  --max  Maximum number of results to return (default: 8).

Output:
  Numbered list of results with title, URL, and snippet.

Requires: requests (pip install requests)
"""

import re
import sys
import urllib.parse

try:
    import requests
except ImportError:
    print("ERROR: 'requests' package is required. Install with: pip install requests")
    sys.exit(1)

DDG_URL = "https://lite.duckduckgo.com/lite/"
HEADERS = {
    "User-Agent": "Mozilla/5.0 (compatible; LlmTornado-CLI/1.0)",
    "Accept": "text/html",
}


def search_ddg(query: str, max_results: int = 8) -> list[dict]:
    """Search DuckDuckGo Lite and parse results from HTML."""
    try:
        resp = requests.post(DDG_URL, data={"q": query}, headers=HEADERS, timeout=15)
        resp.raise_for_status()
    except requests.RequestException as e:
        print(f"ERROR: Search request failed: {e}")
        return []

    html = resp.text
    results = []

    # DuckDuckGo Lite returns results in a table structure.
    # Extract links and their associated snippets.
    link_pattern = re.compile(
        r'<a[^>]+rel="nofollow"[^>]+href="([^"]+)"[^>]*>\s*(.+?)\s*</a>',
        re.DOTALL
    )
    snippet_pattern = re.compile(r'<td[^>]*class="result-snippet"[^>]*>(.*?)</td>', re.DOTALL)

    links = link_pattern.findall(html)
    snippets = snippet_pattern.findall(html)

    for i, (url, title) in enumerate(links):
        if i >= max_results:
            break

        # Clean HTML entities from title
        title = re.sub(r"<[^>]+>", "", title).strip()
        title = title.replace("&amp;", "&").replace("&lt;", "<").replace("&gt;", ">").replace("&#x27;", "'").replace("&quot;", '"')

        # Skip DuckDuckGo internal links
        if "duckduckgo.com" in url:
            continue

        snippet = ""
        if i < len(snippets):
            snippet = re.sub(r"<[^>]+>", "", snippets[i]).strip()
            snippet = snippet.replace("&amp;", "&").replace("&lt;", "<").replace("&gt;", ">")

        results.append({"title": title, "url": url, "snippet": snippet})

    return results


def main():
    if len(sys.argv) < 2:
        print("Usage: ddg-search.py <query> [--max 8]")
        sys.exit(1)

    # Reconstruct query from args (stop at --max flag)
    args = sys.argv[1:]
    max_results = 8
    query_parts = []

    i = 0
    while i < len(args):
        if args[i] == "--max" and i + 1 < len(args):
            max_results = int(args[i + 1])
            i += 2
        else:
            query_parts.append(args[i])
            i += 1

    query = " ".join(query_parts)
    if not query:
        print("Error: empty query.")
        sys.exit(1)

    results = search_ddg(query, max_results)

    if not results:
        print(f'No results found for: "{query}"')
        print("Try rephrasing your query or using different keywords.")
        return

    print(f'Search results for: "{query}"\n')
    for i, r in enumerate(results, 1):
        print(f"{i}. {r['title']}")
        print(f"   URL: {r['url']}")
        if r["snippet"]:
            print(f"   {r['snippet']}")
        print()


if __name__ == "__main__":
    main()
