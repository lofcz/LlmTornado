"""
Handle .gitignore and .fskbignore pattern matching.
"""

from pathlib import Path
from typing import List, Set, Callable
from loguru import logger
from .gitignore_parser import parse_gitignore, parse_gitignore_str


class IgnorePatternMatcher:
    """Matches files against .gitignore and .fskbignore patterns."""
    
    def __init__(self, root_path: Path, use_gitignore: bool = True, use_fskbignore: bool = True):
        self.root_path = Path(root_path)
        self.use_gitignore = use_gitignore
        self.use_fskbignore = use_fskbignore
        self._matchers: List[Callable] = []  # Compiled matcher functions (fast!)
        self._ignore_files: Set[Path] = set()
        
        # Git-like caching for performance
        self._ignore_cache: dict = {}  # path_str -> bool
        self._dir_cache: dict = {}     # directory_str -> bool (directory-level cache)
        
        self._load_patterns()
    
    def reload_patterns(self):
        """Reload patterns from ignore files (public method for when files change)."""
        logger.info(f"Reloading ignore patterns for {self.root_path}")
        self._load_patterns()
    
    def _load_patterns(self):
        """Load patterns from .gitignore and .fskbignore files."""
        self._matchers.clear()
        self._ignore_files.clear()
        
        # Clear caches when reloading patterns
        self._ignore_cache.clear()
        self._dir_cache.clear()
        
        # Default patterns to always ignore
        default_patterns = """
.git/
*.pyc
__pycache__/
.DS_Store
Thumbs.db
.fskb/
node_modules/
.venv/
venv/
env/
bin/
obj/
.vs/
.vscode/
*.min.js
*.min.css
*.min.js.map
*.min.css.map
*.map
.vitepress/cache/
.vitepress/dist/
**/dist/**
**/build/**
**/.vitepress/dist/**
**/.next/**
**/.nuxt/**
**/out/**
""".strip()
        
        # Load default patterns (compiled to matcher function)
        default_matcher = parse_gitignore_str(default_patterns, self.root_path)
        self._matchers.append(default_matcher)
        
        # Load .gitignore if it exists
        if self.use_gitignore:
            gitignore_path = self.root_path / ".gitignore"
            if gitignore_path.exists():
                self._ignore_files.add(gitignore_path)
                gitignore_matcher = parse_gitignore(gitignore_path, self.root_path)
                self._matchers.append(gitignore_matcher)
                logger.debug(f"Loaded .gitignore from {gitignore_path}")
        
        # Load .fskbignore if it exists
        if self.use_fskbignore:
            fskbignore_path = self.root_path / ".fskbignore"
            if fskbignore_path.exists():
                self._ignore_files.add(fskbignore_path)
                fskbignore_matcher = parse_gitignore(fskbignore_path, self.root_path)
                self._matchers.append(fskbignore_matcher)
                logger.debug(f"Loaded .fskbignore from {fskbignore_path}")
        
        logger.debug(f"Loaded {len(self._matchers)} matcher functions for {self.root_path}")
    
    def _read_ignore_file(self, ignore_file: Path) -> List[str]:
        """Read patterns from an ignore file."""
        if not ignore_file.exists():
            return []
        
        try:
            self._ignore_files.add(ignore_file)
            with open(ignore_file, "r", encoding="utf-8") as f:
                lines = []
                for line in f:
                    line = line.strip()
                    # Skip empty lines and comments
                    if line and not line.startswith("#"):
                        lines.append(line)
                
                logger.debug(f"Read {len(lines)} patterns from {ignore_file}")
                return lines
        
        except Exception as e:
            logger.warning(f"Failed to read {ignore_file}: {e}")
            return []
    
    def should_ignore(self, file_path: Path) -> bool:
        """
        Check if a file should be ignored (Git-like optimized version).
        
        Uses caching and directory-level checks for performance:
        - If parent directory is ignored, file is ignored (no pattern check needed)
        - Cache results to avoid redundant pattern matching
        - Early termination on first match
        
        Args:
            file_path: Absolute or relative path to check
        
        Returns:
            True if the file should be ignored
        """
        try:
            # Convert to relative path from root
            if file_path.is_absolute():
                rel_path = file_path.relative_to(self.root_path)
            else:
                rel_path = file_path
            
            # Convert to forward slashes for pathspec (cache key)
            rel_path_str = rel_path.as_posix()
            
            # Check cache first (O(1) lookup)
            if rel_path_str in self._ignore_cache:
                return self._ignore_cache[rel_path_str]
            
            # Git optimization: Check if any parent directory is ignored
            # If parent is ignored, all children are ignored (no need to check patterns)
            parent = rel_path.parent
            while parent != Path('.'):
                parent_str = parent.as_posix()
                
                # Check directory cache
                if parent_str in self._dir_cache:
                    if self._dir_cache[parent_str]:
                        # Parent is ignored, so this file is too
                        self._ignore_cache[rel_path_str] = True
                        return True
                    # Parent not ignored, continue checking
                    break
                
                # Check if parent directory itself is ignored using compiled matchers
                parent_path = self.root_path / parent_str
                for matcher in self._matchers:
                    if matcher(parent_path) or matcher(str(parent_path) + '/'):
                        # Parent directory is ignored
                        self._dir_cache[parent_str] = True
                        self._ignore_cache[rel_path_str] = True
                        return True
                
                # Mark parent as not ignored
                self._dir_cache[parent_str] = False
                parent = parent.parent
            
            # Check the file itself against compiled matchers (precompiled regex = fast!)
            for matcher in self._matchers:
                if matcher(file_path):
                    self._ignore_cache[rel_path_str] = True
                    return True
            
            # Not ignored - cache and return
            self._ignore_cache[rel_path_str] = False
            return False
        
        except ValueError:
            # Path is not relative to root, don't ignore
            return False
        except Exception as e:
            logger.warning(f"Error checking ignore patterns for {file_path}: {e}")
            return False
    
    def reload(self):
        """Reload patterns from ignore files."""
        logger.debug(f"Reloading ignore patterns for {self.root_path}")
        self._load_patterns()
    
    def clear_cache(self):
        """Clear ignore caches (useful for testing or memory management)."""
        self._ignore_cache.clear()
        self._dir_cache.clear()
    
    def get_cache_stats(self) -> dict:
        """Get cache statistics for debugging/monitoring."""
        return {
            "file_cache_size": len(self._ignore_cache),
            "dir_cache_size": len(self._dir_cache),
            "total_cached_decisions": len(self._ignore_cache) + len(self._dir_cache)
        }
    
    def get_ignore_files(self) -> Set[Path]:
        """Get list of ignore files being monitored."""
        return self._ignore_files.copy()

