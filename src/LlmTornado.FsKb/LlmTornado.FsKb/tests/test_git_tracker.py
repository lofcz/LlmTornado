"""Tests for git tracking functionality."""

import pytest
from pathlib import Path
import tempfile
import subprocess
from fskb.indexing import GitTracker


@pytest.fixture
def git_repo():
    """Create a temporary git repository for testing."""
    with tempfile.TemporaryDirectory() as tmpdir:
        repo_path = Path(tmpdir)
        
        # Initialize git repo
        subprocess.run(["git", "init"], cwd=repo_path, check=True, capture_output=True)
        subprocess.run(
            ["git", "config", "user.email", "test@test.com"],
            cwd=repo_path,
            check=True,
            capture_output=True
        )
        subprocess.run(
            ["git", "config", "user.name", "Test User"],
            cwd=repo_path,
            check=True,
            capture_output=True
        )
        
        # Create initial commit
        test_file = repo_path / "test.txt"
        test_file.write_text("test content")
        subprocess.run(["git", "add", "."], cwd=repo_path, check=True, capture_output=True)
        subprocess.run(
            ["git", "commit", "-m", "Initial commit"],
            cwd=repo_path,
            check=True,
            capture_output=True
        )
        
        yield repo_path


def test_git_tracker_initialization(git_repo):
    """Test git tracker initialization."""
    tracker = GitTracker(git_repo)
    
    assert tracker.is_git_repo
    assert tracker.current_branch is not None
    assert tracker.current_commit is not None


def test_get_current_branch(git_repo):
    """Test getting current branch."""
    tracker = GitTracker(git_repo)
    branch = tracker.get_current_branch()
    
    # Should be main or master
    assert branch in ["main", "master"]


def test_non_git_directory():
    """Test tracker with non-git directory."""
    with tempfile.TemporaryDirectory() as tmpdir:
        tracker = GitTracker(Path(tmpdir))
        
        assert not tracker.is_git_repo
        assert tracker.get_current_branch() == "no-git"


def test_get_branches(git_repo):
    """Test getting list of branches."""
    tracker = GitTracker(git_repo)
    branches = tracker.get_branches()
    
    assert len(branches) > 0
    assert any(b in ["main", "master"] for b in branches)

