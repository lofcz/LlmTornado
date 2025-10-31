"""
Query processing and search engine for semantic search.
"""

import asyncio
from pathlib import Path
from typing import List, Dict, Any, Optional
from loguru import logger
from ..config import Settings
from ..indexing.embedder import EmbeddingProvider
from ..storage.chroma_store import ChromaStore


class SearchResult:
    """Represents a search result with context."""
    
    def __init__(
        self,
        content: str,
        file_path: str,
        line_start: int,
        line_end: int,
        score: float,
        branch: str,
        context_before: Optional[str] = None,
        context_after: Optional[str] = None,
    ):
        self.content = content
        self.file_path = file_path
        self.line_start = line_start
        self.line_end = line_end
        self.score = score
        self.branch = branch
        self.context_before = context_before
        self.context_after = context_after
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary."""
        return {
            "content": self.content,
            "file_path": self.file_path,
            "line_start": self.line_start,
            "line_end": self.line_end,
            "score": self.score,
            "branch": self.branch,
            "context_before": self.context_before,
            "context_after": self.context_after,
        }
    
    def __repr__(self) -> str:
        return (
            f"SearchResult(file={self.file_path}, "
            f"lines={self.line_start}-{self.line_end}, "
            f"score={self.score:.3f})"
        )


class QueryEngine:
    """Handles query processing and semantic search."""
    
    def __init__(
        self,
        settings: Settings,
        embedding_provider: EmbeddingProvider,
        chroma_store: ChromaStore,
    ):
        self.settings = settings
        self.embedding_provider = embedding_provider
        self.chroma_store = chroma_store
    
    async def search(
        self,
        query: str,
        root_path: Path,
        branch_name: str,
        top_k: Optional[int] = None,
        include_context: bool = True,
    ) -> List[SearchResult]:
        """
        Search for chunks matching the query.
        
        Args:
            query: Search query string
            root_path: Root directory to search in
            branch_name: Branch name to search
            top_k: Number of results (default from config)
            include_context: Whether to include context lines
        
        Returns:
            List of search results
        """
        if not query.strip():
            logger.warning("Empty query")
            return []
        
        if top_k is None:
            top_k = self.settings.search.top_k
        
        try:
            # Generate query embedding
            logger.debug(f"Generating embedding for query: {query[:50]}...")
            query_embedding = await self.embedding_provider.embed_single(query)
            
            # Search in ChromaDB
            logger.debug(f"Searching in {root_path}, branch {branch_name}")
            raw_results = await self.chroma_store.search(
                root_path=root_path,
                branch_name=branch_name,
                query_embedding=query_embedding,
                top_k=top_k,
            )
            
            # Format results
            results = []
            for raw_result in raw_results:
                metadata = raw_result["metadata"]
                
                # Calculate similarity score from cosine distance
                # ChromaDB returns cosine distance (0 = identical, 2 = opposite)
                # Convert to cosine similarity: similarity = 1 - (distance / 2)
                distance = raw_result.get("distance", 2.0)
                similarity = 1.0 - (distance / 2.0)
                similarity = max(0.0, min(1.0, similarity))  # Clamp to [0, 1]
                
                # Apply minimum similarity threshold
                if similarity < self.settings.search.min_similarity:
                    continue
                
                # Get context if requested
                context_before = None
                context_after = None
                
                if include_context:
                    context_before, context_after = await self._get_context(
                        root_path=root_path,
                        file_path=Path(metadata["file_path"]),
                        line_start=metadata["line_start"],
                        line_end=metadata["line_end"],
                    )
                
                result = SearchResult(
                    content=raw_result["content"],
                    file_path=metadata["file_path"],
                    line_start=metadata["line_start"],
                    line_end=metadata["line_end"],
                    score=similarity,
                    branch=metadata["branch"],
                    context_before=context_before,
                    context_after=context_after,
                )
                
                results.append(result)
            
            logger.info(f"Found {len(results)} results for query: {query[:50]}...")
            return results
        
        except Exception as e:
            logger.error(f"Error during search: {e}")
            return []
    
    async def _get_context(
        self,
        root_path: Path,
        file_path: Path,
        line_start: int,
        line_end: int,
    ) -> tuple[Optional[str], Optional[str]]:
        """
        Get context lines before and after a chunk.
        
        Args:
            root_path: Root directory
            file_path: File path relative to root
            line_start: Start line of chunk
            line_end: End line of chunk
        
        Returns:
            Tuple of (context_before, context_after)
        """
        try:
            full_path = root_path / file_path
            
            if not full_path.exists():
                return None, None
            
            # Read file lines
            with open(full_path, "r", encoding="utf-8", errors="ignore") as f:
                lines = f.readlines()
            
            # Get context lines
            before_count = self.settings.search.context_lines_before
            after_count = self.settings.search.context_lines_after
            
            # Lines are 1-indexed in our system
            start_idx = max(0, line_start - 1 - before_count)
            before_idx = line_start - 1
            after_idx = line_end
            end_idx = min(len(lines), line_end + after_count)
            
            context_before = "".join(lines[start_idx:before_idx]) if before_count > 0 else None
            context_after = "".join(lines[after_idx:end_idx]) if after_count > 0 else None
            
            return context_before, context_after
        
        except Exception as e:
            logger.debug(f"Error getting context for {file_path}: {e}")
            return None, None
    
    async def search_all_branches(
        self,
        query: str,
        root_path: Path,
        top_k: Optional[int] = None,
    ) -> Dict[str, List[SearchResult]]:
        """
        Search across all branches in a root.
        
        Args:
            query: Search query
            root_path: Root directory
            top_k: Number of results per branch
        
        Returns:
            Dictionary mapping branch names to search results
        """
        try:
            # Get all branches
            branches = await self.chroma_store.list_branches(root_path)
            
            if not branches:
                logger.warning(f"No branches found in {root_path}")
                return {}
            
            # Search each branch
            results = {}
            for branch in branches:
                branch_results = await self.search(
                    query=query,
                    root_path=root_path,
                    branch_name=branch,
                    top_k=top_k,
                )
                if branch_results:
                    results[branch] = branch_results
            
            return results
        
        except Exception as e:
            logger.error(f"Error searching all branches: {e}")
            return {}

