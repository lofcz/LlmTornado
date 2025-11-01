"""
ChromaDB-based vector storage with branch-aware indexing.
Uses ChromaDB which combines vector storage and metadata in one package.
"""

import asyncio
from pathlib import Path
from typing import List, Dict, Any, Optional, Set
import numpy as np
from loguru import logger

try:
    import chromadb
    from chromadb.config import Settings as ChromaSettings
    CHROMADB_AVAILABLE = True
except ImportError:
    CHROMADB_AVAILABLE = False
    logger.error("chromadb not installed, storage will not work")


class ChromaStore:
    """
    Vector storage using ChromaDB with branch-aware indexing.
    Each root gets its own collection with branch metadata.
    """
    
    def __init__(self, data_dir: Path, embedding_dimension: int):
        """
        Initialize ChromaDB storage.
        
        Args:
            data_dir: Directory for persistent storage
            embedding_dimension: Dimension of embeddings
        """
        if not CHROMADB_AVAILABLE:
            raise RuntimeError("chromadb not installed")
        
        self.data_dir = Path(data_dir)
        self.data_dir.mkdir(parents=True, exist_ok=True)
        self.embedding_dimension = embedding_dimension
        
        # Initialize ChromaDB client
        self.client = chromadb.PersistentClient(
            path=str(self.data_dir),
            settings=ChromaSettings(
                anonymized_telemetry=False,
                allow_reset=False,
            )
        )
        
        # Collection cache: root_path -> collection
        self._collections: Dict[str, Any] = {}
        
        # Global embedding cache collection (content_hash -> embedding)
        self._embedding_cache = None
        
        logger.info(f"ChromaDB initialized at {self.data_dir}")
    
    def _get_collection_name(self, root_path: Path) -> str:
        """Get collection name for a root path."""
        # Sanitize path for collection name
        name = str(root_path).replace("\\", "_").replace("/", "_").replace(":", "")
        # ChromaDB collection names must be 3-63 chars
        if len(name) > 63:
            name = name[:63]
        if len(name) < 3:
            name = name + "_collection"
        return name
    
    async def get_or_create_collection(self, root_path: Path) -> Any:
        """
        Get or create a collection for a root path.
        
        Args:
            root_path: Root directory path
        
        Returns:
            ChromaDB collection
        """
        root_str = str(root_path)
        
        if root_str in self._collections:
            return self._collections[root_str]
        
        collection_name = self._get_collection_name(root_path)
        
        # Create or get collection with cosine similarity
        collection = await asyncio.to_thread(
            self.client.get_or_create_collection,
            name=collection_name,
            metadata={"root_path": root_str, "hnsw:space": "cosine"}
        )
        
        self._collections[root_str] = collection
        logger.info(f"Collection ready: {collection_name}")
        
        return collection
    
    async def _get_embedding_cache(self) -> Any:
        """Get or create the global embedding cache collection."""
        if self._embedding_cache is not None:
            return self._embedding_cache
        
        # Create or get the embedding cache collection with cosine similarity
        self._embedding_cache = await asyncio.to_thread(
            self.client.get_or_create_collection,
            name="embedding_cache",
            metadata={"type": "cache", "hnsw:space": "cosine"}
        )
        
        logger.debug("Embedding cache collection ready")
        return self._embedding_cache
    
    async def get_cached_embeddings(
        self,
        content_hashes: List[str]
    ) -> Dict[str, Optional[np.ndarray]]:
        """
        Get cached embeddings for content hashes.
        
        Args:
            content_hashes: List of content hashes to look up (may contain duplicates)
        
        Returns:
            Dictionary mapping content_hash -> embedding (or None if not cached)
        """
        if not content_hashes:
            return {}
        
        try:
            cache = await self._get_embedding_cache()
            
            # Deduplicate hashes for query (ChromaDB doesn't allow duplicate IDs in get())
            unique_hashes = list(set(content_hashes))
            
            # Query by IDs (content hashes)
            results = await asyncio.to_thread(
                cache.get,
                ids=unique_hashes,
                include=["embeddings"]
            )
            
            # Build result map
            cached = {}
            if results and results.get("ids"):
                for i, hash_id in enumerate(results["ids"]):
                    # Check if embeddings list exists and has this index
                    embeddings_list = results.get("embeddings")
                    if embeddings_list is not None and i < len(embeddings_list):
                        embedding = embeddings_list[i]
                        if embedding is not None:
                            cached[hash_id] = np.array(embedding, dtype=np.float32)
            
            # Fill in None for missing (including all original hashes, even duplicates)
            for hash_val in content_hashes:
                if hash_val not in cached:
                    cached[hash_val] = None
            
            cache_hits = sum(1 for v in cached.values() if v is not None)
            if cache_hits > 0:
                logger.debug(f"Embedding cache: {cache_hits}/{len(unique_hashes)} unique hits (from {len(content_hashes)} total chunks)")
            
            return cached
        
        except Exception as e:
            logger.error(f"Error reading embedding cache: {e}")
            return {h: None for h in content_hashes}
    
    async def cache_embeddings(
        self,
        content_hashes: List[str],
        embeddings: List[np.ndarray]
    ):
        """
        Cache embeddings by their content hashes.
        
        Args:
            content_hashes: List of content hashes
            embeddings: Corresponding embedding vectors
        """
        if not content_hashes or not embeddings:
            return
        
        if len(content_hashes) != len(embeddings):
            logger.error(f"Hash/embedding count mismatch: {len(content_hashes)} vs {len(embeddings)}")
            return
        
        try:
            cache = await self._get_embedding_cache()
            
            # Add to cache (using content_hash as ID)
            await asyncio.to_thread(
                cache.upsert,
                ids=content_hashes,
                embeddings=[emb.tolist() for emb in embeddings],
                documents=[""] * len(content_hashes),  # Empty documents (not needed for cache)
                metadatas=[{"cached": "true"}] * len(content_hashes)
            )
            
            logger.debug(f"Cached {len(content_hashes)} embeddings")
        
        except Exception as e:
            logger.error(f"Error caching embeddings: {e}")
    
    async def add_chunks(
        self,
        root_path: Path,
        branch_name: str,
        file_path: Path,
        file_hash: str,
        file_mtime: float,
        file_size: int,
        chunks: List[Dict[str, Any]],
        embeddings: List[np.ndarray],
    ):
        """
        Add chunks with embeddings to the collection.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
            file_path: File path relative to root
            file_hash: Hash of the file content
            file_mtime: File modification time (timestamp)
            file_size: File size in bytes
            chunks: List of chunk dictionaries with metadata
            embeddings: List of embedding vectors
        """
        if not chunks or not embeddings:
            return
        
        if len(chunks) != len(embeddings):
            logger.error(f"Mismatch: {len(chunks)} chunks, {len(embeddings)} embeddings")
            return
        
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Prepare data for ChromaDB
            ids = []
            documents = []
            metadatas = []
            embeddings_list = []
            
            for i, (chunk, embedding) in enumerate(zip(chunks, embeddings)):
                # Create unique ID: branch_file_chunk
                rel_path = file_path.relative_to(root_path) if file_path.is_absolute() else file_path
                chunk_id = f"{branch_name}:{str(rel_path)}:{chunk['line_start']}-{chunk['line_end']}:{i}"
                
                ids.append(chunk_id)
                documents.append(chunk["content"])
                
                # Store metadata (including content_hash, file_type/language, mtime, and size)
                metadata = {
                    "branch": branch_name,
                    "file_path": str(rel_path),
                    "file_hash": file_hash,
                    "file_mtime": str(file_mtime),  # Store as string for ChromaDB compatibility
                    "file_size": str(file_size),    # Store as string for ChromaDB compatibility
                    "line_start": chunk["line_start"],
                    "line_end": chunk["line_end"],
                    "char_start": chunk["char_start"],
                    "char_end": chunk["char_end"],
                    "content_hash": chunk.get("content_hash", ""),
                    "file_type": chunk.get("file_type", ""),
                    "language": chunk.get("language", ""),
                }
                metadatas.append(metadata)
                embeddings_list.append(embedding.tolist())
            
            # Add to collection
            await asyncio.to_thread(
                collection.add,
                ids=ids,
                documents=documents,
                metadatas=metadatas,
                embeddings=embeddings_list,
            )
            
            logger.debug(f"Added {len(chunks)} chunks from {file_path} to {branch_name}")
        
        except Exception as e:
            logger.error(f"Error adding chunks to ChromaDB: {e}")
    
    async def search(
        self,
        root_path: Path,
        branch_name: str,
        query_embedding: np.ndarray,
        top_k: int = 10,
    ) -> List[Dict[str, Any]]:
        """
        Search for similar chunks in a specific branch.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name to search in
            query_embedding: Query embedding vector
            top_k: Number of results to return
        
        Returns:
            List of result dictionaries with content and metadata
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Query with branch filter
            results = await asyncio.to_thread(
                collection.query,
                query_embeddings=[query_embedding.tolist()],
                n_results=top_k,
                where={"branch": branch_name},
            )
            
            # Format results
            formatted_results = []
            
            if results and results["ids"] and results["ids"][0]:
                for i in range(len(results["ids"][0])):
                    result = {
                        "id": results["ids"][0][i],
                        "content": results["documents"][0][i],
                        "metadata": results["metadatas"][0][i],
                        "distance": results["distances"][0][i] if "distances" in results else 0.0,
                    }
                    formatted_results.append(result)
            
            return formatted_results
        
        except Exception as e:
            logger.error(f"Error searching ChromaDB: {e}")
            return []
    
    async def delete_file_chunks(
        self,
        root_path: Path,
        branch_name: str,
        file_path: Path,
    ) -> int:
        """
        Delete all chunks for a specific file in a branch.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
            file_path: File path to delete chunks for
        
        Returns:
            Number of chunks deleted
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            rel_path = file_path.relative_to(root_path) if file_path.is_absolute() else file_path
            
            # First, get count of chunks to delete
            results = await asyncio.to_thread(
                collection.get,
                where={
                    "$and": [
                        {"branch": branch_name},
                        {"file_path": str(rel_path)},
                    ]
                },
                include=[]  # Only get IDs for counting
            )
            
            chunk_count = len(results.get("ids", [])) if results else 0
            
            # Delete by metadata filter
            if chunk_count > 0:
                await asyncio.to_thread(
                    collection.delete,
                    where={
                        "$and": [
                            {"branch": branch_name},
                            {"file_path": str(rel_path)},
                        ]
                    }
                )
                
                logger.debug(f"Deleted {chunk_count} chunks for {file_path} in {branch_name}")
            
            return chunk_count
        
        except Exception as e:
            logger.error(f"Error deleting file chunks: {e}")
            return 0
    
    async def cleanup_orphaned_files(
        self,
        root_path: Path,
        branch_name: str,
        valid_file_paths: Set[str],
        indexed_files: Optional[Dict[str, Dict[str, Any]]] = None,
        progress_callback: Optional[callable] = None,
    ) -> int:
        """
        Remove chunks for files that no longer exist.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
            valid_file_paths: Set of relative file paths that currently exist
            indexed_files: Optional pre-loaded indexed files dict (to avoid duplicate query)
        
        Returns:
            Number of orphaned files cleaned up
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Get all indexed files for this branch (or use provided data)
            if indexed_files is None:
                indexed_files = await self.get_indexed_files(root_path, branch_name)
            
            # Find orphaned files (in index but not on disk)
            orphaned_files = set(indexed_files.keys()) - valid_file_paths
            
            if not orphaned_files:
                logger.debug(f"No orphaned files to clean up in {branch_name}")
                return 0
            
            logger.info(f"Cleaning up {len(orphaned_files)} orphaned files from {branch_name}")
            
            # Delete in batches for speed (ChromaDB supports OR queries)
            orphaned_list = list(orphaned_files)
            batch_size = 100
            total_deleted = 0
            
            for i in range(0, len(orphaned_list), batch_size):
                batch = orphaned_list[i:i+batch_size]
                
                # Update UI progress
                if progress_callback:
                    progress_callback(f"Cleaning up orphaned files: {total_deleted}/{len(orphaned_files)}")
                
                # Build OR query for batch delete
                await asyncio.to_thread(
                    collection.delete,
                    where={
                        "$and": [
                            {"branch": branch_name},
                            {"file_path": {"$in": batch}}
                        ]
                    }
                )
                
                total_deleted += len(batch)
                if i % 500 == 0 and i > 0:
                    logger.info(f"Cleaned up {total_deleted}/{len(orphaned_files)} orphaned files...")
            
            logger.info(f"✓ Cleaned up {len(orphaned_files)} orphaned files from ChromaDB")
            return len(orphaned_files)
        
        except Exception as e:
            logger.error(f"Error cleaning up orphaned files: {e}")
            return 0
    
    async def get_branch_stats(self, root_path: Path, branch_name: str) -> Dict[str, Any]:
        """
        Get statistics for a branch.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
        
        Returns:
            Dictionary with stats (file_count, chunk_count, etc.)
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Query all items for this branch
            results = await asyncio.to_thread(
                collection.get,
                where={"branch": branch_name},
                include=["metadatas"]
            )
            
            chunk_count = len(results["ids"]) if results and results["ids"] else 0
            
            # Count unique files
            unique_files = set()
            if results and results["metadatas"]:
                for metadata in results["metadatas"]:
                    unique_files.add(metadata["file_path"])
            
            return {
                "branch": branch_name,
                "chunk_count": chunk_count,
                "file_count": len(unique_files),
            }
        
        except Exception as e:
            logger.error(f"Error getting branch stats: {e}")
            return {"branch": branch_name, "chunk_count": 0, "file_count": 0}
    
    async def get_branch_chunk_count(self, root_path: Path, branch_name: str) -> int:
        """
        Get total chunk count for a branch (fast, no data retrieval).
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
        
        Returns:
            Total number of chunks indexed for this branch
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Filter by branch - get IDs only (no data/embeddings for speed)
            branch_results = await asyncio.to_thread(
                collection.get,
                where={"branch": branch_name},
                include=[]  # Don't include any data, just count
            )
            
            chunk_count = len(branch_results.get("ids", [])) if branch_results else 0
            logger.debug(f"Branch {branch_name} has {chunk_count} chunks")
            return chunk_count
        
        except Exception as e:
            logger.error(f"Error getting chunk count: {e}")
            return 0
    
    async def get_file_chunk_counts(self, root_path: Path, branch_name: str) -> Dict[str, int]:
        """
        Get chunk counts per file for a branch (single efficient query).
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
        
        Returns:
            Dictionary mapping file_path -> chunk_count
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Get all chunks for this branch - only need metadata, not embeddings
            branch_results = await asyncio.to_thread(
                collection.get,
                where={"branch": branch_name},
                include=["metadatas"]  # Only metadata, no embeddings
            )
            
            if not branch_results or not branch_results.get("metadatas"):
                return {}
            
            # Count chunks per file (normalize paths to forward slashes for consistency)
            file_counts: Dict[str, int] = {}
            for metadata in branch_results["metadatas"]:
                if metadata and "file_path" in metadata:
                    file_path = metadata["file_path"].replace('\\', '/')  # Normalize
                    file_counts[file_path] = file_counts.get(file_path, 0) + 1
            
            logger.debug(f"Got chunk counts for {len(file_counts)} files")
            return file_counts
        
        except Exception as e:
            logger.error(f"Error getting file chunk counts: {e}")
            return {}
    
    async def get_indexed_files(self, root_path: Path, branch_name: str) -> Dict[str, Dict[str, Any]]:
        """
        Get all indexed files with their hashes, mtimes, and sizes for a branch.
        Used for quick recovery after restart.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
        
        Returns:
            Dict of {file_path: {"hash": file_hash, "mtime": file_mtime, "size": file_size}}
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Query all items for this branch
            results = await asyncio.to_thread(
                collection.get,
                where={"branch": branch_name},
                include=["metadatas"]
            )
            
            # Build file -> {hash, mtime, size} mapping
            file_info = {}
            if results and results["metadatas"]:
                for metadata in results["metadatas"]:
                    file_path = metadata.get("file_path")
                    
                    # Early exit: skip if already seen this file
                    if not file_path or file_path in file_info:
                        continue
                    
                    file_hash = metadata.get("file_hash")
                    file_mtime = metadata.get("file_mtime", "0")  # Default to 0 if missing (old data)
                    file_size = metadata.get("file_size", "0")    # Default to 0 if missing (old data)
                    
                    file_info[file_path] = {
                        "hash": file_hash,
                        "mtime": float(file_mtime) if file_mtime else 0.0,
                        "size": int(file_size) if file_size else 0
                    }
            
            return file_info
        
        except Exception as e:
            logger.error(f"Error getting indexed files: {e}")
            return {}
    
    async def list_branches(self, root_path: Path) -> Set[str]:
        """
        List all branches in a collection.
        
        Args:
            root_path: Root directory path
        
        Returns:
            Set of branch names
        """
        try:
            collection = await self.get_or_create_collection(root_path)
            
            # Get all items
            results = await asyncio.to_thread(
                collection.get,
                include=["metadatas"]
            )
            
            branches = set()
            if results and results["metadatas"]:
                for metadata in results["metadatas"]:
                    branches.add(metadata["branch"])
            
            return branches
        
        except Exception as e:
            logger.error(f"Error listing branches: {e}")
            return set()
    
    async def save_ignore_file_metadata(
        self,
        root_path: Path,
        branch_name: str,
        ignore_files_stats: Dict[str, tuple[float, int, str]]
    ):
        """
        Store ignore file metadata for change detection.
        Uses a special metadata collection per root.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
            ignore_files_stats: Dict of {file_path_str: (mtime, size, hash)}
        """
        try:
            # Get or create metadata collection for this root
            collection_name = f"{self._get_collection_name(root_path)}_metadata"
            metadata_collection = await asyncio.to_thread(
                self.client.get_or_create_collection,
                name=collection_name,
                metadata={"type": "metadata"}
            )
            
            # Store each ignore file's metadata
            ids = []
            documents = []
            metadatas = []
            embeddings = []  # Empty embeddings (not used for metadata)
            
            for file_path_str, (mtime, size, hash_val) in ignore_files_stats.items():
                # Use branch + file path as ID
                id_str = f"{branch_name}:{file_path_str}"
                ids.append(id_str)
                documents.append(f"Ignore file: {file_path_str}")
                metadatas.append({
                    "branch": branch_name,
                    "file_path": file_path_str,
                    "mtime": str(mtime),
                    "size": str(size),
                    "hash": hash_val
                })
                # Use a dummy embedding (ChromaDB requires it)
                embeddings.append([0.0] * self.embedding_dimension)
            
            if ids:
                await asyncio.to_thread(
                    metadata_collection.upsert,
                    ids=ids,
                    documents=documents,
                    metadatas=metadatas,
                    embeddings=embeddings
                )
                logger.debug(f"Saved {len(ids)} ignore file metadata entries")
        
        except Exception as e:
            logger.error(f"Error saving ignore file metadata: {e}")
    
    async def load_ignore_file_metadata(
        self,
        root_path: Path,
        branch_name: str
    ) -> Dict[str, tuple[float, int, str]]:
        """
        Load ignore file metadata for change detection.
        
        Args:
            root_path: Root directory path
            branch_name: Git branch name
        
        Returns:
            Dict of {file_path_str: (mtime, size, hash)}
        """
        try:
            # Try to get metadata collection
            collection_name = f"{self._get_collection_name(root_path)}_metadata"
            try:
                metadata_collection = await asyncio.to_thread(
                    self.client.get_collection,
                    name=collection_name
                )
            except:
                # Collection doesn't exist yet
                return {}
            
            # Load metadata for this branch
            results = await asyncio.to_thread(
                metadata_collection.get,
                where={"branch": branch_name},
                include=["metadatas"]
            )
            
            stats = {}
            if results and results["metadatas"]:
                for metadata in results["metadatas"]:
                    file_path = metadata.get("file_path")
                    mtime_str = metadata.get("mtime", "0")
                    size_str = metadata.get("size", "0")
                    hash_val = metadata.get("hash", "")
                    
                    if file_path:
                        stats[file_path] = (
                            float(mtime_str) if mtime_str else 0.0,
                            int(size_str) if size_str else 0,
                            hash_val
                        )
            
            logger.debug(f"Loaded {len(stats)} ignore file metadata entries")
            return stats
        
        except Exception as e:
            logger.error(f"Error loading ignore file metadata: {e}")
            return {}
    
    async def close(self):
        """Close the ChromaDB client."""
        self._collections.clear()
        logger.info("ChromaDB client closed")

