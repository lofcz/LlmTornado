"""
Git branch tracking and monitoring.
Keeps track of current branch and detects branch switches.
"""

import asyncio
from pathlib import Path
from typing import Optional, Set
import git
from git.exc import InvalidGitRepositoryError, GitCommandError
from loguru import logger


class GitTracker:
    """Tracks git repository state and branch changes."""
    
    def __init__(self, root_path: Path):
        self.root_path = Path(root_path)
        self.repo: Optional[git.Repo] = None
        self.current_branch: Optional[str] = None
        self.current_commit: Optional[str] = None
        self.is_git_repo = False
        
        self._initialize()
    
    def _initialize(self):
        """Initialize git repository connection."""
        try:
            self.repo = git.Repo(self.root_path, search_parent_directories=True)
            self.is_git_repo = True
            self._update_state()
            logger.info(f"Git repository found at {self.root_path}, branch: {self.current_branch}")
        
        except InvalidGitRepositoryError:
            self.is_git_repo = False
            logger.debug(f"No git repository at {self.root_path}")
        
        except Exception as e:
            self.is_git_repo = False
            logger.warning(f"Error initializing git repository at {self.root_path}: {e}")
    
    def _update_state(self):
        """Update current branch and commit information."""
        if not self.is_git_repo or not self.repo:
            return
        
        try:
            # Get current branch name
            if self.repo.head.is_detached:
                self.current_branch = f"detached-{self.repo.head.commit.hexsha[:8]}"
            else:
                self.current_branch = self.repo.active_branch.name
            
            # Get current commit hash
            self.current_commit = self.repo.head.commit.hexsha
        
        except Exception as e:
            logger.warning(f"Error updating git state: {e}")
            self.current_branch = "unknown"
            self.current_commit = None
    
    def get_current_branch(self) -> str:
        """
        Get current branch name.
        Returns 'no-git' if not a git repository.
        """
        if not self.is_git_repo:
            return "no-git"
        
        self._update_state()
        return self.current_branch or "unknown"
    
    def get_current_commit(self) -> Optional[str]:
        """Get current commit hash."""
        if not self.is_git_repo:
            return None
        
        self._update_state()
        return self.current_commit
    
    def has_branch_changed(self) -> bool:
        """
        Check if branch has changed since last check.
        Updates internal state if changed.
        """
        if not self.is_git_repo:
            return False
        
        old_branch = self.current_branch
        old_commit = self.current_commit
        
        self._update_state()
        
        branch_changed = old_branch != self.current_branch
        commit_changed = old_commit != self.current_commit
        
        if branch_changed:
            logger.info(f"Branch changed: {old_branch} -> {self.current_branch}")
            return True
        
        if commit_changed:
            logger.debug(f"Commit changed: {old_commit[:8] if old_commit else 'None'} -> {self.current_commit[:8] if self.current_commit else 'None'}")
        
        return False
    
    def get_file_hash_at_commit(self, file_path: Path, commit: Optional[str] = None) -> Optional[str]:
        """
        Get the git hash of a file at a specific commit.
        Useful for detecting if a file is the same across branches.
        
        Args:
            file_path: Path to the file (relative to repo root)
            commit: Commit hash, defaults to current HEAD
        
        Returns:
            Git object hash of the file, or None if not in git
        """
        if not self.is_git_repo or not self.repo:
            return None
        
        try:
            commit_obj = self.repo.commit(commit) if commit else self.repo.head.commit
            
            # Convert to relative path from repo root
            repo_root = Path(self.repo.working_dir)
            if file_path.is_absolute():
                rel_path = file_path.relative_to(repo_root)
            else:
                rel_path = file_path
            
            # Get the blob (file object) from the commit tree
            blob = commit_obj.tree / str(rel_path)
            return blob.hexsha
        
        except (KeyError, ValueError):
            # File doesn't exist at this commit
            return None
        except Exception as e:
            logger.debug(f"Error getting file hash for {file_path}: {e}")
            return None
    
    def get_tracked_files(self) -> Set[Path]:
        """
        Get all files tracked by git in the current branch.
        
        Returns:
            Set of absolute paths to tracked files
        """
        if not self.is_git_repo or not self.repo:
            return set()
        
        try:
            repo_root = Path(self.repo.working_dir)
            tracked = set()
            
            # Get all files in the index
            for item in self.repo.head.commit.tree.traverse():
                if item.type == "blob":  # It's a file
                    file_path = repo_root / item.path
                    tracked.add(file_path)
            
            return tracked
        
        except Exception as e:
            logger.warning(f"Error getting tracked files: {e}")
            return set()
    
    def is_file_tracked(self, file_path: Path) -> bool:
        """Check if a file is tracked by git."""
        if not self.is_git_repo or not self.repo:
            return False
        
        try:
            repo_root = Path(self.repo.working_dir)
            if file_path.is_absolute():
                rel_path = file_path.relative_to(repo_root)
            else:
                rel_path = file_path
            
            # Check if file is in the index
            try:
                self.repo.head.commit.tree / str(rel_path)
                return True
            except KeyError:
                return False
        
        except Exception as e:
            logger.debug(f"Error checking if file is tracked {file_path}: {e}")
            return False
    
    def get_branches(self) -> Set[str]:
        """Get all branch names in the repository."""
        if not self.is_git_repo or not self.repo:
            return set()
        
        try:
            return {branch.name for branch in self.repo.branches}
        except Exception as e:
            logger.warning(f"Error getting branches: {e}")
            return set()
    
    async def monitor_branch_changes(self, callback, interval: float = 5.0):
        """
        Monitor for branch changes and call callback when detected.
        
        Args:
            callback: Async function to call when branch changes
            interval: Check interval in seconds
        """
        if not self.is_git_repo:
            logger.debug("Not a git repository, skipping branch monitoring")
            return
        
        logger.info(f"Starting branch monitoring for {self.root_path}")
        
        while True:
            try:
                if self.has_branch_changed():
                    await callback(self.current_branch, self.current_commit)
                
                await asyncio.sleep(interval)
            
            except asyncio.CancelledError:
                logger.info("Branch monitoring cancelled")
                break
            except Exception as e:
                logger.error(f"Error in branch monitoring: {e}")
                await asyncio.sleep(interval)

