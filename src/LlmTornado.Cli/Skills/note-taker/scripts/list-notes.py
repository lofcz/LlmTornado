"""
List notes — shows all notes with title, tags, date, and a short preview.
Usage: list-notes.py [--tag tagname] [--sort date|title]

Arguments:
  --tag    Filter to notes with this tag (optional).
  --sort   Sort by 'date' (default, newest first) or 'title' (alphabetical).

Output:
  Formatted list of all notes.
"""

import os
import sys


def get_notes_dir() -> str:
    home = os.path.expanduser("~")
    return os.path.join(home, ".llm-tornado", "notes")


def parse_frontmatter(content: str) -> tuple[dict, str]:
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
                        value = value.strip("[]")
                        meta[key] = [t.strip() for t in value.split(",") if t.strip()]
                    else:
                        meta[key] = value

    return meta, body


def main():
    tag_filter: str | None = None
    sort_by = "date"

    args = sys.argv[1:]
    i = 0
    while i < len(args):
        if args[i] == "--tag" and i + 1 < len(args):
            tag_filter = args[i + 1].lower()
            i += 2
        elif args[i] == "--sort" and i + 1 < len(args):
            sort_by = args[i + 1]
            i += 2
        else:
            i += 1

    notes_dir = get_notes_dir()
    if not os.path.isdir(notes_dir):
        print("No notes found. Create a note first with add-note.")
        return

    notes = []
    all_tags: set[str] = set()

    for fname in os.listdir(notes_dir):
        if not fname.endswith(".md"):
            continue

        fpath = os.path.join(notes_dir, fname)
        try:
            content = open(fpath, "r", encoding="utf-8").read()
        except OSError:
            continue

        meta, body = parse_frontmatter(content)
        tags = meta.get("tags", [])
        all_tags.update(t.lower() for t in tags)

        if tag_filter and tag_filter not in [t.lower() for t in tags]:
            continue

        size = os.path.getsize(fpath)
        preview = body[:100].replace("\n", " ").strip()
        if len(body) > 100:
            preview += "..."

        notes.append({
            "filename": fname,
            "title": meta.get("title", fname),
            "tags": tags,
            "date": meta.get("date", ""),
            "preview": preview,
            "size": size,
        })

    if sort_by == "title":
        notes.sort(key=lambda n: n["title"].lower())
    else:
        notes.sort(key=lambda n: n["date"], reverse=True)

    if not notes:
        if tag_filter:
            print(f"No notes found with tag '{tag_filter}'.")
        else:
            print("No notes found.")
        return

    filter_msg = f" (tag: {tag_filter})" if tag_filter else ""
    print(f"Notes: {len(notes)}{filter_msg}\n")

    for n in notes:
        tags_str = f" [{', '.join(n['tags'])}]" if n["tags"] else ""
        date_str = n["date"][:10] if n["date"] else "no date"
        print(f"  {date_str}  {n['title']}{tags_str}")
        print(f"           {n['filename']} ({n['size']} bytes)")
        print(f"           {n['preview']}")
        print()

    if all_tags:
        print(f"All tags: {', '.join(sorted(all_tags))}")


if __name__ == "__main__":
    main()
