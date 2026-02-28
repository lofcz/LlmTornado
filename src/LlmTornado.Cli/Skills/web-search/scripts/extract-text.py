"""
Text extractor — fetches a URL and extracts clean readable text, stripping HTML boilerplate.
Usage: extract-text.py <url> [--max-chars 15000]

Arguments:
  url         The URL to extract text from.
  --max-chars Maximum characters of extracted text (default: 15000).

Output:
  Clean, readable text content from the page.

Requires: requests (pip install requests)
"""

import re
import sys

try:
    import requests
except ImportError:
    print("ERROR: 'requests' package is required. Install with: pip install requests")
    sys.exit(1)

HEADERS = {
    "User-Agent": "Mozilla/5.0 (compatible; LlmTornado-CLI/1.0)",
    "Accept": "text/html,application/xhtml+xml",
}


def strip_html(html: str) -> str:
    """Simple HTML-to-text converter without external dependencies."""
    # Remove script and style blocks
    text = re.sub(r"<script[^>]*>.*?</script>", "", html, flags=re.DOTALL | re.IGNORECASE)
    text = re.sub(r"<style[^>]*>.*?</style>", "", text, flags=re.DOTALL | re.IGNORECASE)

    # Remove nav, header, footer (common boilerplate containers)
    for tag in ["nav", "header", "footer", "aside"]:
        text = re.sub(rf"<{tag}[^>]*>.*?</{tag}>", "", text, flags=re.DOTALL | re.IGNORECASE)

    # Convert block elements to newlines
    text = re.sub(r"<br\s*/?>", "\n", text, flags=re.IGNORECASE)
    text = re.sub(r"</(p|div|h[1-6]|li|tr|blockquote|pre)>", "\n", text, flags=re.IGNORECASE)
    text = re.sub(r"<(h[1-6])[^>]*>", "\n## ", text, flags=re.IGNORECASE)
    text = re.sub(r"<li[^>]*>", "• ", text, flags=re.IGNORECASE)

    # Strip remaining HTML tags
    text = re.sub(r"<[^>]+>", "", text)

    # Decode common entities
    text = text.replace("&amp;", "&")
    text = text.replace("&lt;", "<")
    text = text.replace("&gt;", ">")
    text = text.replace("&quot;", '"')
    text = text.replace("&#x27;", "'")
    text = text.replace("&nbsp;", " ")
    text = re.sub(r"&#(\d+);", lambda m: chr(int(m.group(1))), text)
    text = re.sub(r"&#x([0-9a-fA-F]+);", lambda m: chr(int(m.group(1), 16)), text)

    # Collapse whitespace
    lines = []
    for line in text.split("\n"):
        stripped = " ".join(line.split())
        if stripped:
            lines.append(stripped)

    # Collapse multiple blank lines
    result = []
    prev_blank = False
    for line in lines:
        if not line:
            if not prev_blank:
                result.append("")
            prev_blank = True
        else:
            result.append(line)
            prev_blank = False

    return "\n".join(result)


def main():
    if len(sys.argv) < 2:
        print("Usage: extract-text.py <url> [--max-chars 15000]")
        sys.exit(1)

    url = sys.argv[1]
    max_chars = 15000

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

    print(f"Source: {resp.url}")
    print("---\n")

    text = strip_html(resp.text)

    if len(text) > max_chars:
        print(text[:max_chars])
        print(f"\n[TRUNCATED at {max_chars:,} chars]")
    else:
        print(text)


if __name__ == "__main__":
    main()
