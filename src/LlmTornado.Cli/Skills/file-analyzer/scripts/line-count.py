"""
Line counter — counts lines in a single file or recursively in a directory.
Usage: line-count.py <path> [--ext .cs,.py,.md]

Arguments:
  path   File or directory to analyze.
  --ext  Comma-separated list of extensions to include (optional, default: all).

Output:
  Per-file line counts grouped by extension, plus a grand total.
"""

import os
import sys
from collections import defaultdict


def count_lines(file_path: str) -> int:
    try:
        with open(file_path, "r", encoding="utf-8", errors="replace") as f:
            return sum(1 for _ in f)
    except (OSError, PermissionError):
        return -1


def main():
    if len(sys.argv) < 2:
        print("Usage: line-count.py <path> [--ext .cs,.py,.md]")
        sys.exit(1)

    target = sys.argv[1]
    ext_filter: set[str] | None = None

    if "--ext" in sys.argv:
        idx = sys.argv.index("--ext")
        if idx + 1 < len(sys.argv):
            ext_filter = {e.strip().lower() if e.startswith(".") else f".{e.strip().lower()}" for e in sys.argv[idx + 1].split(",")}

    if os.path.isfile(target):
        lines = count_lines(target)
        print(f"{target}: {lines} lines")
        return

    if not os.path.isdir(target):
        print(f"Error: '{target}' is not a valid file or directory.")
        sys.exit(1)

    stats: dict[str, list[tuple[str, int]]] = defaultdict(list)
    total = 0
    file_count = 0
    errors = 0

    for root, _dirs, files in os.walk(target):
        # Skip hidden and common non-source directories
        parts = root.replace("\\", "/").split("/")
        if any(p.startswith(".") or p in ("node_modules", "bin", "obj", "__pycache__", ".git") for p in parts):
            continue

        for fname in files:
            ext = os.path.splitext(fname)[1].lower()
            if ext_filter and ext not in ext_filter:
                continue

            fpath = os.path.join(root, fname)
            lines = count_lines(fpath)
            if lines < 0:
                errors += 1
                continue

            rel = os.path.relpath(fpath, target)
            category = ext if ext else "(no extension)"
            stats[category].append((rel, lines))
            total += lines
            file_count += 1

    # Print grouped results
    for ext in sorted(stats.keys()):
        file_list = sorted(stats[ext], key=lambda x: -x[1])
        ext_total = sum(l for _, l in file_list)
        print(f"\n## {ext} ({len(file_list)} files, {ext_total:,} lines)")
        for rel_path, lines in file_list[:25]:
            print(f"  {rel_path}: {lines:,}")
        if len(file_list) > 25:
            print(f"  ... and {len(file_list) - 25} more files")

    print(f"\n--- Total: {file_count:,} files, {total:,} lines ---")
    if errors:
        print(f"({errors} files could not be read)")


if __name__ == "__main__":
    main()
