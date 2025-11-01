"""
Text chunking with overlap using langchain's text splitter.
"""

import hashlib
from pathlib import Path
from typing import List, Dict, Any, Optional
from langchain_text_splitters import RecursiveCharacterTextSplitter
from loguru import logger
from ..config import Settings


class TextChunk:
    """Represents a chunk of text with metadata."""
    
    def __init__(
        self,
        content: str,
        line_start: int,
        line_end: int,
        char_start: int,
        char_end: int,
        content_hash: Optional[str] = None,
        file_type: Optional[str] = None,
        language: Optional[str] = None,
    ):
        self.content = content
        self.line_start = line_start
        self.line_end = line_end
        self.char_start = char_start
        self.char_end = char_end
        
        # Compute content hash if not provided (for embedding cache)
        if content_hash is None:
            self.content_hash = hashlib.sha256(content.encode('utf-8')).hexdigest()
        else:
            self.content_hash = content_hash
        
        self.file_type = file_type
        self.language = language
    
    def __repr__(self) -> str:
        return f"TextChunk(lines={self.line_start}-{self.line_end}, chars={len(self.content)}, hash={self.content_hash[:8]})"
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for storage."""
        return {
            "content": self.content,
            "line_start": self.line_start,
            "line_end": self.line_end,
            "char_start": self.char_start,
            "char_end": self.char_end,
            "content_hash": self.content_hash,
            "file_type": self.file_type or "",
            "language": self.language or "",
        }


class TextChunker:
    """Chunks text files with overlap for better context."""
    
    def __init__(self, settings: Settings):
        self.settings = settings
        self.chunk_size = settings.chunking.chunk_size
        self.chunk_overlap = settings.chunking.chunk_overlap
        self.separators = settings.chunking.separators
        
        # Use langchain's recursive text splitter
        self.splitter = RecursiveCharacterTextSplitter(
            chunk_size=self.chunk_size,
            chunk_overlap=self.chunk_overlap,
            separators=self.separators,
            length_function=len,
            is_separator_regex=False,
        )
        
        logger.debug(
            f"TextChunker initialized: size={self.chunk_size}, "
            f"overlap={self.chunk_overlap}"
        )
    
    def chunk_text(self, text: str, file_path: str = "") -> List[TextChunk]:
        """
        Split text into overlapping chunks with line number tracking.
        Optimized for speed - single pass through text.
        """
        if not text.strip():
            logger.debug(f"Empty text for {file_path}, skipping")
            return []
        
        # Detect file type and language from path
        file_type = ""
        language = ""
        if file_path:
            path_obj = Path(file_path)
            file_type = path_obj.suffix.lstrip('.').lower()
            language = self._detect_language(file_type)
        
        try:
            # Split into chunks using langchain
            text_chunks = self.splitter.split_text(text)
            
            if not text_chunks:
                return []
            
            chunks_with_metadata = []
            
            # Find actual positions of chunks in original text (handles overlap correctly)
            search_start = 0
            for chunk_text in text_chunks:
                # Find where this chunk actually appears in the original text
                chunk_start = text.find(chunk_text, search_start)
                if chunk_start == -1:
                    # Fallback: if exact match not found (rare), use approximate position
                    chunk_start = search_start
                
                chunk_end = chunk_start + len(chunk_text)
                
                # Calculate line numbers by counting newlines up to this point
                line_start = text[:chunk_start].count('\n') + 1
                line_end = text[:chunk_end].count('\n') + 1
                
                chunk = TextChunk(
                    content=chunk_text,
                    line_start=line_start,
                    line_end=line_end,
                    char_start=chunk_start,
                    char_end=chunk_end,
                    file_type=file_type,
                    language=language,
                )
                
                chunks_with_metadata.append(chunk)
                
                # Move search position forward (accounting for overlap)
                # Search from slightly before the end to handle overlap
                search_start = chunk_start + max(1, len(chunk_text) - self.chunk_overlap)
            
            logger.debug(
                f"Chunked {file_path}: {len(text)} chars -> "
                f"{len(chunks_with_metadata)} chunks"
            )
            
            return chunks_with_metadata
        
        except Exception as e:
            logger.error(f"Error chunking text for {file_path}: {e}")
            # Return entire text as single chunk on error
            return [TextChunk(
                content=text,
                line_start=1,
                line_end=text.count('\n') + 1,
                char_start=0,
                char_end=len(text),
                file_type=file_type,
                language=language,
            )]
    
    def _detect_language(self, file_type: str) -> str:
        """Detect programming language from file extension."""
        # Common language mappings
        lang_map = {
            # Compiled languages
            'cs': 'csharp', 'java': 'java', 'cpp': 'cpp', 'cc': 'cpp',
            'c': 'c', 'h': 'c', 'hpp': 'cpp', 'go': 'go', 'rs': 'rust',
            'swift': 'swift', 'kt': 'kotlin', 'scala': 'scala',
            
            # Scripting languages
            'py': 'python', 'js': 'javascript', 'ts': 'typescript',
            'jsx': 'javascript', 'tsx': 'typescript', 'rb': 'ruby',
            'php': 'php', 'pl': 'perl', 'lua': 'lua', 'r': 'r',
            
            # Shell
            'sh': 'shell', 'bash': 'shell', 'zsh': 'shell', 'fish': 'shell',
            'ps1': 'powershell', 'psm1': 'powershell',
            
            # Web
            'html': 'html', 'htm': 'html', 'xml': 'xml', 'css': 'css',
            'scss': 'scss', 'sass': 'sass', 'less': 'less',
            
            # Data/Config
            'json': 'json', 'yaml': 'yaml', 'yml': 'yaml', 'toml': 'toml',
            'ini': 'ini', 'cfg': 'config', 'conf': 'config',
            
            # Documentation
            'md': 'markdown', 'rst': 'rst', 'txt': 'text',
            
            # SQL
            'sql': 'sql',
        }
        return lang_map.get(file_type, file_type)
    
    def chunk_file(self, file_path: str) -> List[TextChunk]:
        """Read and chunk a file."""
        try:
            with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                text = f.read()
            return self.chunk_text(text, file_path)
        except Exception as e:
            logger.error(f"Error reading file {file_path}: {e}")
            return []

