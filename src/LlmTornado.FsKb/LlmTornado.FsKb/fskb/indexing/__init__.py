"""Indexing components for file watching, git tracking, and chunking."""

from .file_watcher import FileWatcher
from .git_tracker import GitTracker
from .chunker import TextChunker, TextChunk
from .embedder import EmbeddingProvider
from .indexing_engine import IndexingEngine
from .recovery import RecoveryManager

__all__ = ["FileWatcher", "GitTracker", "TextChunker", "TextChunk", "EmbeddingProvider", "IndexingEngine", "RecoveryManager"]

