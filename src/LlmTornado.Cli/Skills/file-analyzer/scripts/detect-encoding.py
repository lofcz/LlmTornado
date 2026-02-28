"""
Encoding detector — checks file encoding using BOM detection and heuristics.
Usage: detect-encoding.py <path>

Arguments:
  path   File or directory to check.

Output:
  Per-file encoding with confidence and flags for non-UTF-8 files.
"""

import os
import sys


def detect_bom(data: bytes) -> str | None:
    if data[:3] == b"\xef\xbb\xbf":
        return "UTF-8-BOM"
    if data[:2] in (b"\xff\xfe", b"\xfe\xff"):
        return "UTF-16"
    if data[:4] in (b"\xff\xfe\x00\x00", b"\x00\x00\xfe\xff"):
        return "UTF-32"
    return None


def check_utf8(data: bytes) -> tuple[bool, float]:
    """Returns (is_valid_utf8, confidence)."""
    try:
        data.decode("utf-8")
        # count multi-byte sequences as evidence of real UTF-8
        multi = sum(1 for b in data if b > 127)
        confidence = 0.95 if multi > 0 else 0.80
        return True, confidence
    except UnicodeDecodeError:
        return False, 0.0


def is_likely_binary(data: bytes) -> bool:
    null_count = data[:8192].count(0)
    return null_count > 10


def detect_file(path: str) -> dict:
    try:
        with open(path, "rb") as f:
            data = f.read(65536)
    except (OSError, PermissionError):
        return {"encoding": "ERROR", "confidence": 0, "note": "Could not read"}

    if not data:
        return {"encoding": "EMPTY", "confidence": 1.0, "note": ""}

    if is_likely_binary(data):
        return {"encoding": "BINARY", "confidence": 0.9, "note": "Likely binary file"}

    bom = detect_bom(data)
    if bom:
        return {"encoding": bom, "confidence": 1.0, "note": "BOM detected"}

    is_utf8, conf = check_utf8(data)
    if is_utf8:
        return {"encoding": "UTF-8", "confidence": conf, "note": ""}

    # Fallback: try Latin-1 (always succeeds)
    return {"encoding": "LATIN-1/UNKNOWN", "confidence": 0.5, "note": "Not valid UTF-8"}


BINARY_EXTENSIONS = {".exe", ".dll", ".bin", ".obj", ".png", ".jpg", ".gif", ".zip", ".gz",
                     ".tar", ".pdf", ".woff", ".ttf", ".ico", ".bmp", ".mp3", ".mp4"}


def main():
    if len(sys.argv) < 2:
        print("Usage: detect-encoding.py <path>")
        sys.exit(1)

    target = sys.argv[1]

    if os.path.isfile(target):
        result = detect_file(target)
        flag = " ⚠" if result["encoding"] not in ("UTF-8", "UTF-8-BOM", "EMPTY") else ""
        print(f"{target}: {result['encoding']} (confidence: {result['confidence']:.0%}){flag}")
        if result["note"]:
            print(f"  Note: {result['note']}")
        return

    if not os.path.isdir(target):
        print(f"Error: '{target}' not found.")
        sys.exit(1)

    non_utf8 = []
    total = 0

    for root, _dirs, files in os.walk(target):
        parts = root.replace("\\", "/").split("/")
        if any(p.startswith(".") or p in ("node_modules", "bin", "obj", "__pycache__") for p in parts):
            continue

        for fname in files:
            ext = os.path.splitext(fname)[1].lower()
            if ext in BINARY_EXTENSIONS:
                continue

            fpath = os.path.join(root, fname)
            result = detect_file(fpath)
            total += 1

            if result["encoding"] not in ("UTF-8", "UTF-8-BOM", "EMPTY", "BINARY"):
                rel = os.path.relpath(fpath, target)
                non_utf8.append((rel, result["encoding"], result.get("note", "")))

    if non_utf8:
        print(f"⚠ Found {len(non_utf8)} non-UTF-8 file(s) out of {total}:\n")
        for rel, enc, note in sorted(non_utf8):
            extra = f"  ({note})" if note else ""
            print(f"  {rel}: {enc}{extra}")
    else:
        print(f"All {total} text files are UTF-8. ✓")


if __name__ == "__main__":
    main()
