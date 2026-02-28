"""
View note — reads and displays the full content of a note.
Usage: view-note.py <filename>

Arguments:
  filename  The note filename (e.g., my-note.md). Looked up in the notes directory.

Output:
  Full note content including frontmatter.
"""

import os
import sys


def get_notes_dir() -> str:
    home = os.path.expanduser("~")
    return os.path.join(home, ".llm-tornado", "notes")


def main():
    if len(sys.argv) < 2:
        print("Usage: view-note.py <filename>")
        sys.exit(1)

    filename = sys.argv[1]
    # Add .md extension if not present
    if not filename.endswith(".md"):
        filename += ".md"

    notes_dir = get_notes_dir()
    filepath = os.path.join(notes_dir, filename)

    # Security: ensure we stay inside the notes directory
    filepath = os.path.realpath(filepath)
    notes_real = os.path.realpath(notes_dir)
    if not filepath.startswith(notes_real):
        print("Error: Access denied — path is outside the notes directory.")
        sys.exit(1)

    if not os.path.exists(filepath):
        # Try fuzzy match
        matches = [f for f in os.listdir(notes_dir)
                    if f.endswith(".md") and filename.replace(".md", "").lower() in f.lower()]
        if matches:
            print(f"Note '{filename}' not found. Did you mean:")
            for m in matches[:5]:
                print(f"  - {m}")
        else:
            print(f"Note '{filename}' not found.")
        sys.exit(1)

    try:
        content = open(filepath, "r", encoding="utf-8").read()
    except (OSError, PermissionError) as e:
        print(f"Error reading note: {e}")
        sys.exit(1)

    print(content)


if __name__ == "__main__":
    main()
