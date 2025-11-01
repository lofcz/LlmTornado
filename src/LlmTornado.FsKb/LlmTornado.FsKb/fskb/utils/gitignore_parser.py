"""
High-performance gitignore parser with per-directory support.
Based on https://github.com/mherrmann/gitignore_parser
"""

import collections
import os
import re
from os.path import abspath, dirname, join
from pathlib import Path
import sys
from typing import Callable, List, Reversible, Union, Tuple, Optional
from loguru import logger


def handle_negation(file_path: str, rules: Reversible["IgnoreRule"]) -> bool:
    """Handle negation rules - later rules override earlier ones."""
    for rule in reversed(rules):
        if rule.match(file_path):
            return not rule.negation
    return False


def parse_gitignore(full_path: Union[str, Path], base_dir: Optional[Union[str, Path]] = None) -> Callable[[Union[str, Path]], bool]:
    """
    Parse a .gitignore file and return a matcher function.
    
    Args:
        full_path: Path to the .gitignore file
        base_dir: Base directory for relative patterns (defaults to parent of full_path)
    
    Returns:
        A function that takes a file path and returns True if it should be ignored
    """
    full_path = Path(full_path)
    if base_dir is None:
        base_dir = full_path.parent
    
    if not full_path.exists():
        return lambda file_path: False
    
    try:
        with open(full_path, encoding='utf-8') as ignore_file:
            return _parse_gitignore_lines(ignore_file, str(full_path), base_dir)
    except Exception as e:
        logger.warning(f"Failed to parse {full_path}: {e}")
        return lambda file_path: False


def parse_gitignore_str(gitignore_str: str, base_dir: Union[str, Path]) -> Callable[[Union[str, Path]], bool]:
    """Parse gitignore patterns from a string."""
    full_path = join(str(base_dir), '.gitignore')
    lines = gitignore_str.splitlines()
    return _parse_gitignore_lines(lines, full_path, base_dir)


def _parse_gitignore_lines(lines, full_path: str, base_dir: Union[str, Path]) -> Callable[[Union[str, Path]], bool]:
    """Parse gitignore lines and return a matcher function."""
    base_dir = _normalize_path(base_dir)
    rules = []
    
    for line_no, line in enumerate(lines, start=1):
        rule = rule_from_pattern(
            line.rstrip('\n'), 
            base_path=base_dir, 
            source=(full_path, line_no)
        )
        if rule:
            rules.append(rule)
    
    if not rules:
        return lambda file_path: False
    
    # Fast path: no negation rules
    if not any(r.negation for r in rules):
        return lambda file_path: any(r.match(file_path) for r in rules)
    else:
        # Slow path: handle negation (later rules override earlier ones)
        return lambda file_path: handle_negation(file_path, rules)


def rule_from_pattern(
    pattern: str, 
    base_path: Optional[Path] = None, 
    source: Optional[Tuple[str, int]] = None
) -> Optional["IgnoreRule"]:
    """
    Convert a .gitignore pattern to an IgnoreRule.
    
    Returns None for comments, blank lines, or invalid patterns.
    """
    # Store the exact pattern for repr/str
    orig_pattern = pattern
    
    # Early returns
    if pattern.strip() == '' or pattern[0] == '#':
        return None
    
    # Handle negation
    if pattern[0] == '!':
        negation = True
        pattern = pattern[1:]
    else:
        negation = False
    
    # Multi-asterisks not surrounded by slashes should be treated like single asterisks
    pattern = re.sub(r'([^/])\*{2,}', r'\1*', pattern)
    pattern = re.sub(r'\*{2,}([^/])', r'*\1', pattern)
    
    # Special case: '/' doesn't match anything
    if pattern.rstrip() == '/':
        return None
    
    directory_only = pattern[-1] == '/'
    
    # A slash means we're anchored to the base_path
    anchored = '/' in pattern[:-1]
    
    if pattern[0] == '/':
        pattern = pattern[1:]
    
    if pattern[0:2] == '**':
        pattern = pattern[2:]
        anchored = False
    
    if pattern[0] == '/':
        pattern = pattern[1:]
    
    if pattern[-1] == '/':
        pattern = pattern[:-1]
    
    # Unescape leading # or !
    if len(pattern) > 1 and pattern[0] == '\\' and pattern[1] in ('#', '!'):
        pattern = pattern[1:]
    
    # Handle trailing spaces (ignored unless escaped)
    i = len(pattern) - 1
    striptrailingspaces = True
    while i > 1 and pattern[i] == ' ':
        if pattern[i-1] == '\\':
            pattern = pattern[:i-1] + pattern[i:]
            i = i - 1
            striptrailingspaces = False
        else:
            if striptrailingspaces:
                pattern = pattern[:i]
        i = i - 1
    
    # Compile pattern to regex
    regex = fnmatch_pathname_to_regex(
        pattern, directory_only, negation, anchored=bool(anchored)
    )
    
    return IgnoreRule(
        pattern=orig_pattern,
        regex=regex,
        negation=negation,
        directory_only=directory_only,
        anchored=anchored,
        base_path=base_path if base_path else None,
        source=source
    )


IGNORE_RULE_FIELDS = [
    'pattern', 'regex',
    'negation', 'directory_only', 'anchored',
    'base_path',
    'source'
]


class IgnoreRule(collections.namedtuple('IgnoreRule_', IGNORE_RULE_FIELDS)):
    """A single gitignore rule with precompiled regex."""
    
    def __str__(self):
        return self.pattern
    
    def __repr__(self):
        return f'IgnoreRule(\'{self.pattern}\')'
    
    def match(self, abs_path: Union[str, Path]) -> bool:
        """Check if this rule matches the given path."""
        if self.base_path:
            rel_path = _normalize_path(abs_path).relative_to(self.base_path).as_posix()
        else:
            rel_path = _normalize_path(abs_path).as_posix()
        
        # Preserve trailing symbols on Windows
        if sys.platform.startswith('win'):
            rel_path += ' ' * _count_trailing_symbol(' ', str(abs_path))
            rel_path += '.' * _count_trailing_symbol('.', str(abs_path))
        
        # Preserve trailing slash for directory-only negation
        if self.negation and isinstance(abs_path, str) and abs_path[-1] == '/':
            rel_path += '/'
        
        if rel_path.startswith('./'):
            rel_path = rel_path[2:]
        
        return bool(re.search(self.regex, rel_path))


def fnmatch_pathname_to_regex(
    pattern: str, 
    directory_only: bool, 
    negation: bool, 
    anchored: bool = False
) -> str:
    """
    Convert fnmatch pattern to regex with FNM_PATHNAME behavior.
    Path separators don't match '*' and '?' wildcards.
    """
    i, n = 0, len(pattern)
    
    seps = [re.escape(os.sep)]
    if os.altsep is not None:
        seps.append(re.escape(os.altsep))
    seps_group = '[' + '|'.join(seps) + ']'
    nonsep = r'[^{}]'.format('|'.join(seps))
    
    res = []
    
    while i < n:
        c = pattern[i]
        i += 1
        
        if c == '*':
            try:
                if pattern[i] == '*':
                    i += 1
                    if i < n and pattern[i] == '/':
                        i += 1
                        res.append(''.join(['(.*', seps_group, ')?']))
                    else:
                        res.append('.*')
                else:
                    res.append(''.join([nonsep, '*']))
            except IndexError:
                res.append(''.join([nonsep, '*']))
        
        elif c == '?':
            res.append(nonsep)
        
        elif c == '/':
            res.append(seps_group)
        
        elif c == '[':
            j = i
            if j < n and pattern[j] == '!':
                j += 1
            if j < n and pattern[j] == ']':
                j += 1
            while j < n and pattern[j] != ']':
                j += 1
            if j >= n:
                res.append('\\[')
            else:
                stuff = pattern[i:j].replace('\\', '\\\\').replace('/', '')
                i = j + 1
                if stuff[0] == '!':
                    stuff = ''.join(['^', stuff[1:]])
                elif stuff[0] == '^':
                    stuff = ''.join('\\' + stuff)
                res.append(f'[{stuff}]')
        
        else:
            res.append(re.escape(c))
    
    # Anchoring
    if anchored:
        res.insert(0, '^')
    else:
        res.insert(0, f"(^|{seps_group})")
    
    # End anchor
    if not directory_only:
        res.append('$')
    elif directory_only and negation:
        res.append('/$')
    else:
        res.append('($|\\/)')
    
    return ''.join(res)


def _normalize_path(path: Union[str, Path]) -> Path:
    """Normalize a path without resolving symlinks."""
    return Path(abspath(path))


def _count_trailing_symbol(symbol: str, text: str) -> int:
    """Count trailing occurrences of a character."""
    count = 0
    for char in reversed(str(text)):
        if char == symbol:
            count += 1
        else:
            break
    return count

