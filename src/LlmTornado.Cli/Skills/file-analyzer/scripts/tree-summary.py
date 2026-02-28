"""
Tree summary — shows directory structure with file counts and sizes per folder.
Usage: tree-summary.py <path> [--depth 3]

Arguments:
  path    Directory to summarize.
  --depth Maximum depth to display (default: 3).

Output:
  Indented tree with file count and total size per directory.
"""

import os
import sys


def format_size(size: int) -> str:
    if size < 1024:
        return f"{size} B"
    if size < 1024 * 1024:
        return f"{size / 1024:.1f} KB"
    if size < 1024 * 1024 * 1024:
        return f"{size / (1024 * 1024):.1f} MB"
    return f"{size / (1024 * 1024 * 1024):.1f} GB"


SKIP_DIRS = {".git", "node_modules", "bin", "obj", "__pycache__", ".vs", ".idea", ".vscode"}


def summarize(dir_path: str, depth: int, max_depth: int, prefix: str = "") -> tuple[int, int]:
    """Returns (total_files, total_bytes)."""
    entries = []
    try:
        entries = sorted(os.scandir(dir_path), key=lambda e: (not e.is_dir(), e.name.lower()))
    except (OSError, PermissionError):
        return 0, 0

    dirs = [(e.name, e.path) for e in entries if e.is_dir() and not e.name.startswith(".") and e.name not in SKIP_DIRS]
    files = [e for e in entries if e.is_file()]

    local_files = len(files)
    local_bytes = 0
    ext_counts: dict[str, int] = {}

    for f in files:
        try:
            size = f.stat().st_size
            local_bytes += size
            ext = os.path.splitext(f.name)[1].lower() or "(none)"
            ext_counts[ext] = ext_counts.get(ext, 0) + 1
        except OSError:
            pass

    total_files = local_files
    total_bytes = local_bytes

    # Build extension summary
    top_exts = sorted(ext_counts.items(), key=lambda x: -x[1])[:5]
    ext_str = ", ".join(f"{ext}({n})" for ext, n in top_exts)

    dir_name = os.path.basename(dir_path) or dir_path
    print(f"{prefix}{dir_name}/  [{local_files} files, {format_size(local_bytes)}] {ext_str}")

    if depth < max_depth:
        for i, (name, path) in enumerate(dirs):
            is_last = i == len(dirs) - 1
            connector = "└── " if is_last else "├── "
            child_prefix = prefix + ("    " if is_last else "│   ")

            sub_files, sub_bytes = summarize(path, depth + 1, max_depth, prefix=prefix + connector.replace(connector, "│   " if not is_last else "    "))
            # Re-print with child prefix
            # Actually, let the recursive call handle its own printing
            cf, cb = summarize_inner(path, depth + 1, max_depth, prefix + ("    " if is_last else "│   "))
            total_files += cf
            total_bytes += cb
    elif dirs:
        print(f"{prefix}    ... {len(dirs)} subdirectories (increase --depth to see)")
        for _, path in dirs:
            for root, subdirs, subfiles in os.walk(path):
                parts = root.replace("\\", "/").split("/")
                if any(p in SKIP_DIRS or p.startswith(".") for p in parts[len(path.replace("\\", "/").split("/")):]):
                    continue
                for f in subfiles:
                    total_files += 1
                    try:
                        total_bytes += os.path.getsize(os.path.join(root, f))
                    except OSError:
                        pass

    return total_files, total_bytes


def summarize_inner(dir_path: str, depth: int, max_depth: int, prefix: str) -> tuple[int, int]:
    """Inner recursive summary that prints and returns counts."""
    entries = []
    try:
        entries = sorted(os.scandir(dir_path), key=lambda e: (not e.is_dir(), e.name.lower()))
    except (OSError, PermissionError):
        return 0, 0

    dirs = [(e.name, e.path) for e in entries if e.is_dir() and not e.name.startswith(".") and e.name not in SKIP_DIRS]
    files = [e for e in entries if e.is_file()]

    local_files = len(files)
    local_bytes = 0
    ext_counts: dict[str, int] = {}

    for f in files:
        try:
            size = f.stat().st_size
            local_bytes += size
            ext = os.path.splitext(f.name)[1].lower() or "(none)"
            ext_counts[ext] = ext_counts.get(ext, 0) + 1
        except OSError:
            pass

    total_files = local_files
    total_bytes = local_bytes

    top_exts = sorted(ext_counts.items(), key=lambda x: -x[1])[:5]
    ext_str = ", ".join(f"{ext}({n})" for ext, n in top_exts)

    dir_name = os.path.basename(dir_path)
    print(f"{prefix}{dir_name}/  [{local_files} files, {format_size(local_bytes)}] {ext_str}")

    if depth < max_depth:
        for i, (name, path) in enumerate(dirs):
            is_last = i == len(dirs) - 1
            child_prefix = prefix + ("    " if is_last else "│   ")
            cf, cb = summarize_inner(path, depth + 1, max_depth, child_prefix)
            total_files += cf
            total_bytes += cb
    elif dirs:
        print(f"{prefix}    ... {len(dirs)} subdirectories")
        for _, path in dirs:
            for root, _, subfiles in os.walk(path):
                for f in subfiles:
                    total_files += 1
                    try:
                        total_bytes += os.path.getsize(os.path.join(root, f))
                    except OSError:
                        pass

    return total_files, total_bytes


def main():
    if len(sys.argv) < 2:
        print("Usage: tree-summary.py <path> [--depth 3]")
        sys.exit(1)

    target = sys.argv[1]
    max_depth = 3

    if "--depth" in sys.argv:
        idx = sys.argv.index("--depth")
        if idx + 1 < len(sys.argv):
            max_depth = int(sys.argv[idx + 1])

    if not os.path.isdir(target):
        print(f"Error: '{target}' is not a directory.")
        sys.exit(1)

    total_files, total_bytes = summarize_inner(target, 0, max_depth, "")
    print(f"\n--- Total: {total_files:,} files, {format_size(total_bytes)} ---")


if __name__ == "__main__":
    main()
