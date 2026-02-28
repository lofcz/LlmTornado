"""
URL fetcher — downloads a web page and returns its content.
Usage: fetch-url.py <url> [--max-chars 20000]

Arguments:
  url         The URL to fetch.
  --max-chars Maximum characters to return (default: 20000).

Output:
  The page content (HTML or text), truncated to max-chars.

Requires: requests (pip install requests)
"""

import sys

try:
    import requests
except ImportError:
    print("ERROR: 'requests' package is required. Install with: pip install requests")
    sys.exit(1)

HEADERS = {
    "User-Agent": "Mozilla/5.0 (compatible; LlmTornado-CLI/1.0)",
    "Accept": "text/html,application/xhtml+xml,text/plain",
}


def main():
    if len(sys.argv) < 2:
        print("Usage: fetch-url.py <url> [--max-chars 20000]")
        sys.exit(1)

    url = sys.argv[1]
    max_chars = 20000

    if "--max-chars" in sys.argv:
        idx = sys.argv.index("--max-chars")
        if idx + 1 < len(sys.argv):
            max_chars = int(sys.argv[idx + 1])

    if not url.startswith(("http://", "https://")):
        url = "https://" + url

    try:
        resp = requests.get(url, headers=HEADERS, timeout=20, allow_redirects=True)
        resp.raise_for_status()
    except requests.RequestException as e:
        print(f"ERROR: Failed to fetch '{url}': {e}")
        sys.exit(1)

    content_type = resp.headers.get("content-type", "")
    print(f"URL: {resp.url}")
    print(f"Status: {resp.status_code}")
    print(f"Content-Type: {content_type}")
    print(f"Size: {len(resp.text):,} chars")
    print("---")

    text = resp.text
    if len(text) > max_chars:
        text = text[:max_chars]
        print(text)
        print(f"\n[TRUNCATED at {max_chars:,} chars — full page is {len(resp.text):,} chars]")
    else:
        print(text)


if __name__ == "__main__":
    main()
