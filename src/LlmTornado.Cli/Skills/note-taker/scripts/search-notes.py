"""
Search notes — searches all notes for a keyword or tag.
Usage: search-notes.py <query> [--tag tagname]

Arguments:
  query   Text to search for within note content and titles.
  --tag   Filter to notes with this tag (optional).

Output:
  Matching notes with title, tags, date, and context snippet.
"""

import os
import re
import sys


def get_notes_dir() -> str:
    home = os.path.expanduser("~")
    return os.path.join(home, ".llm-tornado", "notes")


def parse_frontmatter(content: str) -> tuple[dict, str]:
    """Parse YAML frontmatter and return (metadata, body)."""
    meta: dict = {}
    body = content

    if content.startswith("---"):
        end = content.find("---", 3)
        if end > 0:
            yaml_block = content[3:end].strip()
            body = content[end + 3:].strip()

            for line in yaml_block.split("\n"):
                line = line.strip()
                colon = line.find(":")
                if colon > 0:
                    key = line[:colon].strip()
                    value = line[colon + 1:].strip().strip('"')
                    if key == "tags":
                        # Parse [tag1, tag2] format
                        value = value.strip("[]")
                        meta[key] = [t.strip() for t in value.split(",") if t.strip()]
                    else:
                        meta[key] = value

    return meta, body


def get_context(text: str, query: str, context_chars: int = 120) -> str:
    """Get a snippet around the first occurrence of query."""
    lower = text.lower()
    idx = lower.find(query.lower())
    if idx < 0:
        return text[:context_chars] + ("..." if len(text) > context_chars else "")

    start = max(0, idx - context_chars // 2)
    end = min(len(text), idx + len(query) + context_chars // 2)

    snippet = text[start:end].replace("\n", " ").strip()
    prefix = "..." if start > 0 else ""
    suffix = "..." if end < len(text) else ""
    return f"{prefix}{snippet}{suffix}"


def main():
    if len(sys.argv) < 2:
        print("Usage: search-notes.py <query> [--tag tagname]")
        sys.exit(1)

    args = sys.argv[1:]
    query_parts = []
    tag_filter: str | None = None

    i = 0
    while i < len(args):
        if args[i] == "--tag" and i + 1 < len(args):
            tag_filter = args[i + 1].lower()
            i += 2
        else:
            query_parts.append(args[i])
            i += 1

    query = " ".join(query_parts)
    notes_dir = get_notes_dir()

    if not os.path.isdir(notes_dir):
        print("No notes directory found. Create a note first with add-note.")
        sys.exit(0)

    results = []
    for fname in sorted(os.listdir(notes_dir)):
        if not fname.endswith(".md"):
            continue

        fpath = os.path.join(notes_dir, fname)
        try:
            content = open(fpath, "r", encoding="utf-8").read()
        except (OSError, PermissionError):
            continue

        meta, body = parse_frontmatter(content)
        tags = meta.get("tags", [])

        # Tag filter
        if tag_filter and tag_filter not in [t.lower() for t in tags]:
            continue

        # Text search (search title, tags, and body)
        if query:
            searchable = f"{meta.get('title', '')} {' '.join(tags)} {body}".lower()
            if query.lower() not in searchable:
                continue

        snippet = get_context(body, query) if query else body[:150] + ("..." if len(body) > 150 else "")
        results.append({
            "filename": fname,
            "title": meta.get("title", fname),
            "tags": tags,
            "date": meta.get("date", ""),
            "snippet": snippet,
        })

    if not results:
        print(f'No notes found matching "{query}"' + (f" with tag '{tag_filter}'" if tag_filter else ""))
        return

    print(f"Found {len(results)} note(s):\n")
    for r in results:
        tags_str = f" [{', '.join(r['tags'])}]" if r["tags"] else ""
        date_str = f"  {r['date']}" if r["date"] else ""
        print(f"📝 {r['title']}{tags_str}{date_str}")
        print(f"   File: {r['filename']}")
        print(f"   {r['snippet']}")
        print()


if __name__ == "__main__":
    main()
