"""
Delete note — removes a note from the notes directory.
Usage: delete-note.py <filename>

Arguments:
  filename  The note filename to delete (e.g., my-note.md).

Output:
  Confirmation of deletion or error if not found.
"""

import os
import sys


def get_notes_dir() -> str:
    home = os.path.expanduser("~")
    return os.path.join(home, ".llm-tornado", "notes")


def main():
    if len(sys.argv) < 2:
        print("Usage: delete-note.py <filename>")
        sys.exit(1)

    filename = sys.argv[1]
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
        print(f"Note '{filename}' not found.")
        # Suggest similar files
        if os.path.isdir(notes_dir):
            matches = [f for f in os.listdir(notes_dir)
                        if f.endswith(".md") and filename.replace(".md", "").lower() in f.lower()]
            if matches:
                print("Did you mean:")
                for m in matches[:5]:
                    print(f"  - {m}")
        sys.exit(1)

    try:
        os.remove(filepath)
        print(f"Deleted: {filename}")
    except (OSError, PermissionError) as e:
        print(f"Error deleting note: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
