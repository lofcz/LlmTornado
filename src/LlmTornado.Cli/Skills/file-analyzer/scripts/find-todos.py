"""
TODO/FIXME finder — scans files for common task markers.
Usage: find-todos.py <path> [--ext .cs,.py,.js]

Arguments:
  path   File or directory to scan.
  --ext  Comma-separated extensions to include (optional, default: all text).

Output:
  Grouped list of TODO, FIXME, HACK, XXX comments with file locations.
"""

import os
import re
import sys
from collections import defaultdict

MARKERS = re.compile(r"\b(TODO|FIXME|HACK|XXX|BUG|OPTIMIZE|REFACTOR)\b", re.IGNORECASE)

BINARY_EXTENSIONS = {".exe", ".dll", ".bin", ".obj", ".png", ".jpg", ".gif", ".zip", ".gz", ".tar", ".pdf", ".woff", ".ttf"}


def scan_file(file_path: str, base: str) -> list[tuple[str, str, int, str]]:
    results = []
    ext = os.path.splitext(file_path)[1].lower()
    if ext in BINARY_EXTENSIONS:
        return results

    try:
        with open(file_path, "r", encoding="utf-8", errors="replace") as f:
            for lineno, line in enumerate(f, 1):
                match = MARKERS.search(line)
                if match:
                    tag = match.group(1).upper()
                    rel = os.path.relpath(file_path, base) if base else file_path
                    results.append((tag, rel, lineno, line.strip()))
    except (OSError, PermissionError):
        pass
    return results


def main():
    if len(sys.argv) < 2:
        print("Usage: find-todos.py <path> [--ext .cs,.py,.js]")
        sys.exit(1)

    target = sys.argv[1]
    ext_filter: set[str] | None = None

    if "--ext" in sys.argv:
        idx = sys.argv.index("--ext")
        if idx + 1 < len(sys.argv):
            ext_filter = {e.strip().lower() if e.startswith(".") else f".{e.strip().lower()}" for e in sys.argv[idx + 1].split(",")}

    findings: dict[str, list[tuple[str, int, str]]] = defaultdict(list)
    base = target if os.path.isdir(target) else os.path.dirname(target)

    if os.path.isfile(target):
        for tag, rel, lineno, text in scan_file(target, base):
            findings[tag].append((rel, lineno, text))
    elif os.path.isdir(target):
        for root, _dirs, files in os.walk(target):
            parts = root.replace("\\", "/").split("/")
            if any(p.startswith(".") or p in ("node_modules", "bin", "obj", "__pycache__") for p in parts):
                continue
            for fname in files:
                ext = os.path.splitext(fname)[1].lower()
                if ext_filter and ext not in ext_filter:
                    continue
                fpath = os.path.join(root, fname)
                for tag, rel, lineno, text in scan_file(fpath, base):
                    findings[tag].append((rel, lineno, text))
    else:
        print(f"Error: '{target}' not found.")
        sys.exit(1)

    if not findings:
        print("No TODO/FIXME markers found.")
        return

    total = sum(len(v) for v in findings.values())
    print(f"Found {total} marker(s):\n")

    for tag in ["FIXME", "BUG", "HACK", "TODO", "XXX", "OPTIMIZE", "REFACTOR"]:
        items = findings.get(tag)
        if not items:
            continue
        print(f"## {tag} ({len(items)})")
        for rel, lineno, text in sorted(items)[:50]:
            print(f"  {rel}:{lineno}  {text}")
        if len(items) > 50:
            print(f"  ... and {len(items) - 50} more")
        print()


if __name__ == "__main__":
    main()
