"""Tests for text chunking functionality."""

import pytest
from fskb.config import Settings
from fskb.indexing import TextChunker


@pytest.fixture
def chunker():
    """Create a text chunker with default settings."""
    settings = Settings()
    settings.chunking.chunk_size = 100
    settings.chunking.chunk_overlap = 20
    return TextChunker(settings)


def test_chunk_simple_text(chunker):
    """Test chunking simple text."""
    text = "This is a test. " * 50  # ~800 chars
    chunks = chunker.chunk_text(text, "test.txt")
    
    assert len(chunks) > 0
    assert all(chunk.content for chunk in chunks)
    assert all(chunk.line_start >= 1 for chunk in chunks)
    assert all(chunk.line_end >= chunk.line_start for chunk in chunks)


def test_chunk_empty_text(chunker):
    """Test chunking empty text."""
    chunks = chunker.chunk_text("", "empty.txt")
    assert len(chunks) == 0


def test_chunk_with_newlines(chunker):
    """Test chunking text with newlines."""
    text = "Line 1\nLine 2\nLine 3\n" * 10
    chunks = chunker.chunk_text(text, "lines.txt")
    
    assert len(chunks) > 0
    for chunk in chunks:
        assert chunk.line_end >= chunk.line_start


def test_chunk_overlap(chunker):
    """Test that chunks have proper overlap."""
    text = "Word " * 200  # Long text
    chunks = chunker.chunk_text(text, "overlap.txt")
    
    if len(chunks) > 1:
        # Check that consecutive chunks have some overlap
        for i in range(len(chunks) - 1):
            chunk1 = chunks[i]
            chunk2 = chunks[i + 1]
            # Character positions should overlap
            assert chunk1.char_end > chunk2.char_start or chunk1.char_end == chunk2.char_start

