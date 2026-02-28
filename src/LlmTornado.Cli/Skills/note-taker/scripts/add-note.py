"""
Add note — creates a new markdown note with YAML frontmatter.
Usage: add-note.py <title> [--tags tag1,tag2] [--content "note body"]

Arguments:
  title     The note title (also used to generate the filename).
  --tags    Comma-separated tags (optional).
  --content The note body text. If omitted, reads from stdin.

Output:
  Confirmation with the filename and path of the created note.
"""

import os
import re
import sys
from datetime import datetime, timezone


def get_notes_dir() -> str:
    home = os.path.expanduser("~")
    notes_dir = os.path.join(home, ".llm-tornado", "notes")
    os.makedirs(notes_dir, exist_ok=True)
    return notes_dir


def slugify(text: str) -> str:
    """Convert title to a filename-safe slug."""
    text = text.lower().strip()
    text = re.sub(r"[^\w\s-]", "", text)
    text = re.sub(r"[\s_]+", "-", text)
    text = re.sub(r"-+", "-", text)
    return text.strip("-")[:80]


def main():
    if len(sys.argv) < 2:
        print("Usage: add-note.py <title> [--tags tag1,tag2] [--content \"note body\"]")
        sys.exit(1)

    args = sys.argv[1:]
    title_parts = []
    tags: list[str] = []
    content = ""

    i = 0
    while i < len(args):
        if args[i] == "--tags" and i + 1 < len(args):
            tags = [t.strip() for t in args[i + 1].split(",") if t.strip()]
            i += 2
        elif args[i] == "--content" and i + 1 < len(args):
            content = args[i + 1]
            i += 2
        else:
            title_parts.append(args[i])
            i += 1

    title = " ".join(title_parts)
    if not title:
        print("Error: title is required.")
        sys.exit(1)

    # Read content from stdin if not provided via args
    if not content and not sys.stdin.isatty():
        content = sys.stdin.read()

    if not content:
        content = "(empty note)"

    notes_dir = get_notes_dir()
    slug = slugify(title)
    now = datetime.now(timezone.utc)

    # Ensure unique filename
    filename = f"{slug}.md"
    filepath = os.path.join(notes_dir, filename)
    counter = 1
    while os.path.exists(filepath):
        filename = f"{slug}-{counter}.md"
        filepath = os.path.join(notes_dir, filename)
        counter += 1

    # Build frontmatter
    tags_str = ", ".join(tags) if tags else ""
    frontmatter = f"""---
title: "{title}"
tags: [{tags_str}]
date: {now.strftime('%Y-%m-%dT%H:%M:%S')}
---
"""

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(frontmatter)
        f.write("\n")
        f.write(content)
        f.write("\n")

    print(f"Note created: {filename}")
    print(f"Path: {filepath}")
    print(f"Title: {title}")
    if tags:
        print(f"Tags: {', '.join(tags)}")
    print(f"Date: {now.strftime('%Y-%m-%d %H:%M UTC')}")


if __name__ == "__main__":
    main()
