"""
Duplicate file finder — locates files with identical content via SHA-256 hashing.
Usage: find-duplicates.py <path> [--min-size 1]

Arguments:
  path       Directory to search.
  --min-size Minimum file size in bytes to consider (default: 1).

Output:
  Groups of duplicate files with their sizes.
"""

import hashlib
import os
import sys
from collections import defaultdict


def hash_file(path: str) -> str | None:
    try:
        h = hashlib.sha256()
        with open(path, "rb") as f:
            while chunk := f.read(65536):
                h.update(chunk)
        return h.hexdigest()
    except (OSError, PermissionError):
        return None


def format_size(size: int) -> str:
    if size < 1024:
        return f"{size} B"
    if size < 1024 * 1024:
        return f"{size / 1024:.1f} KB"
    if size < 1024 * 1024 * 1024:
        return f"{size / (1024 * 1024):.1f} MB"
    return f"{size / (1024 * 1024 * 1024):.1f} GB"


def main():
    if len(sys.argv) < 2:
        print("Usage: find-duplicates.py <path> [--min-size 1]")
        sys.exit(1)

    target = sys.argv[1]
    min_size = 1

    if "--min-size" in sys.argv:
        idx = sys.argv.index("--min-size")
        if idx + 1 < len(sys.argv):
            min_size = int(sys.argv[idx + 1])

    if not os.path.isdir(target):
        print(f"Error: '{target}' is not a directory.")
        sys.exit(1)

    # Phase 1: Group by size (quick filter)
    size_groups: dict[int, list[str]] = defaultdict(list)
    for root, _dirs, files in os.walk(target):
        parts = root.replace("\\", "/").split("/")
        if any(p.startswith(".") or p in ("node_modules", "bin", "obj", "__pycache__") for p in parts):
            continue

        for fname in files:
            fpath = os.path.join(root, fname)
            try:
                size = os.path.getsize(fpath)
                if size >= min_size:
                    size_groups[size].append(fpath)
            except OSError:
                pass

    # Phase 2: Hash only files with matching sizes
    hash_groups: dict[str, list[tuple[str, int]]] = defaultdict(list)
    candidates = 0

    for size, paths in size_groups.items():
        if len(paths) < 2:
            continue
        candidates += len(paths)
        for fpath in paths:
            h = hash_file(fpath)
            if h:
                rel = os.path.relpath(fpath, target)
                hash_groups[h].append((rel, size))

    # Filter to actual duplicates
    dupes = {h: files for h, files in hash_groups.items() if len(files) > 1}

    if not dupes:
        print(f"No duplicate files found (scanned {sum(len(v) for v in size_groups.values())} files).")
        return

    total_wasted = 0
    group_num = 0

    for h, files in sorted(dupes.items(), key=lambda x: -x[1][0][1]):
        group_num += 1
        size = files[0][1]
        wasted = size * (len(files) - 1)
        total_wasted += wasted

        print(f"\n## Group {group_num} — {format_size(size)} each, {len(files)} copies")
        for rel, _ in sorted(files):
            print(f"  {rel}")

    print(f"\n--- {group_num} duplicate group(s), {format_size(total_wasted)} wasted ---")


if __name__ == "__main__":
    main()
