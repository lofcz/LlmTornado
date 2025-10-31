"""
Indexing engine that coordinates file watching, chunking, embedding, and storage.
"""

import asyncio
import hashlib
import os
from pathlib import Path
from typing import Any, Dict, Optional, Set
from dataclasses import dataclass, field
from loguru import logger

from ..config import Settings
from ..utils import ResourceManager, IgnorePatternMatcher
from .file_watcher import FileWatcher
from .git_tracker import GitTracker
from .chunker import TextChunker
from .embedder import EmbeddingProvider
from ..storage.chroma_store import ChromaStore


@dataclass
class IndexingStats:
    """Statistics for indexing operations."""
    files_scanned: int = 0
    files_indexed: int = 0
    chunks_created: int = 0
    chunks_embedded: int = 0
    errors: int = 0
    current_file: Optional[str] = None
    indexing_start_time: Optional[float] = None  # Timestamp when actual indexing started
    files_indexed_at_start: int = 0  # Files already indexed when timer started (for ETA calculation)


@dataclass
class RootState:
    """State for a single root directory."""
    root_path: Path
    git_tracker: GitTracker
    file_watcher: FileWatcher
    ignore_matcher: IgnorePatternMatcher
    current_branch: str
    indexed_files: Dict[str, Dict[str, Any]] = field(default_factory=dict)  # file_path -> {hash, mtime, size}
    stats: IndexingStats = field(default_factory=IndexingStats)
    paused: bool = False  # Whether indexing is paused for this root


class IndexingEngine:
    """
    Main indexing engine that coordinates all indexing operations.
    Manages multiple roots with branch-aware indexing.
    """
    
    def __init__(
        self,
        settings: Settings,
        resource_manager: ResourceManager,
        embedding_provider: EmbeddingProvider,
        chroma_store: ChromaStore,
    ):
        self.settings = settings
        self.resource_manager = resource_manager
        self.embedding_provider = embedding_provider
        self.chroma_store = chroma_store
        self.chunker = TextChunker(settings)
        
        # Root states
        self.roots: Dict[Path, RootState] = {}
        
        # Priority indexing queue (priority, timestamp, task_data)
        # Lower priority number = higher priority
        # Priority levels: 0 = modified/changed, 1 = new file, 2 = bulk scan
        self._indexing_queue: asyncio.PriorityQueue = asyncio.PriorityQueue()
        self._worker_tasks: list[asyncio.Task] = []
        self._running = False
        self._queue_counter = 0  # For FIFO ordering within same priority
    
    async def start(self):
        """Start the indexing engine."""
        if self._running:
            return
        
        self._running = True
        
        # Start worker tasks
        worker_count = self.resource_manager.get_optimal_worker_count()
        logger.info(f"Starting {worker_count} indexing workers")
        
        for i in range(worker_count):
            task = asyncio.create_task(self._indexing_worker(i))
            self._worker_tasks.append(task)
        
        logger.info("Indexing engine started")
    
    async def stop(self):
        """Stop the indexing engine."""
        self._running = False
        
        # Save metadata cache for all roots before stopping
        logger.info("Saving metadata cache for all roots...")
        save_tasks = []
        for root_path, root_state in self.roots.items():
            save_tasks.append(self._save_metadata_cache(root_path, root_state))
        
        if save_tasks:
            try:
                await asyncio.wait_for(asyncio.gather(*save_tasks, return_exceptions=True), timeout=5.0)
                logger.info("✓ Metadata cache saved for all roots")
            except asyncio.TimeoutError:
                logger.warning("Metadata cache save timed out")
        
        # Stop all file watchers (non-blocking)
        for root_state in self.roots.values():
            try:
                await asyncio.wait_for(root_state.file_watcher.stop(), timeout=2.0)
            except asyncio.TimeoutError:
                logger.warning("File watcher stop timed out")
        
        # Cancel workers immediately (don't wait for queue)
        for task in self._worker_tasks:
            task.cancel()
        
        # Wait for workers to finish with timeout
        try:
            await asyncio.wait_for(
                asyncio.gather(*self._worker_tasks, return_exceptions=True),
                timeout=5.0
            )
        except asyncio.TimeoutError:
            logger.warning("Worker shutdown timed out, forcing exit")
        
        self._worker_tasks.clear()
        
        # Clear queue without waiting
        while not self._indexing_queue.empty():
            try:
                self._indexing_queue.get_nowait()
                self._indexing_queue.task_done()
            except (asyncio.QueueEmpty, ValueError):
                # QueueEmpty can happen in race conditions
                # ValueError if task_done() called more than get()
                break
        
        logger.info("Indexing engine stopped")
    
    async def add_root(self, root_path: Path) -> bool:
        """
        Add a root directory to index.
        
        Args:
            root_path: Path to root directory
        
        Returns:
            True if successfully added
        """
        root_path = Path(root_path).resolve()
        
        if not root_path.exists():
            logger.error(f"Root path does not exist: {root_path}")
            return False
        
        if not root_path.is_dir():
            logger.error(f"Root path is not a directory: {root_path}")
            return False
        
        if root_path in self.roots:
            logger.warning(f"Root already indexed: {root_path}")
            return False
        
        logger.info(f"Adding root: {root_path}")
        
        try:
            # Initialize components
            git_tracker = GitTracker(root_path)
            ignore_matcher = IgnorePatternMatcher(
                root_path,
                use_gitignore=self.settings.indexing.respect_gitignore,
                use_fskbignore=self.settings.indexing.use_fskbignore,
            )
            
            file_watcher = FileWatcher(
                root_path=root_path,
                callback=self._on_file_change,
                text_extensions=set(self.settings.indexing.text_extensions),
                max_file_size_mb=self.settings.indexing.max_file_size_mb,
                debounce_delay_ms=self.settings.resource.debounce_delay_ms,
                ignore_matcher=ignore_matcher,
            )
            
            current_branch = git_tracker.get_current_branch()
            
            # Create root state
            root_state = RootState(
                root_path=root_path,
                git_tracker=git_tracker,
                file_watcher=file_watcher,
                ignore_matcher=ignore_matcher,
                current_branch=current_branch,
            )
            
            # Ensure stats object has all fields (for compatibility with old state)
            if not hasattr(root_state.stats, 'indexing_start_time'):
                root_state.stats.indexing_start_time = None
            
            self.roots[root_path] = root_state
            
            # Start branch monitoring
            asyncio.create_task(
                git_tracker.monitor_branch_changes(
                    lambda branch, commit: self._on_branch_change(root_path, branch, commit),
                    interval=5.0,
                )
            )
            
            # Set up ignore file change callback
            root_state.file_watcher.set_ignore_change_callback(
                lambda ignore_file: self._on_ignore_file_changed(root_path, ignore_file)
            )
            
            # Scan existing files (will start file watcher after scan completes)
            asyncio.create_task(self._initial_scan(root_path))
            
            logger.info(f"Root added successfully: {root_path}")
            return True
        
        except Exception as e:
            logger.error(f"Error adding root {root_path}: {e}")
            return False
    
    async def remove_root(self, root_path: Path) -> bool:
        """
        Remove a root directory from indexing.
        
        Args:
            root_path: Path to root directory
        
        Returns:
            True if successfully removed
        """
        root_path = Path(root_path).resolve()
        
        if root_path not in self.roots:
            logger.warning(f"Root not found: {root_path}")
            return False
        
        logger.info(f"Removing root: {root_path}")
        
        try:
            root_state = self.roots[root_path]
            await root_state.file_watcher.stop()
            del self.roots[root_path]
            
            logger.info(f"Root removed: {root_path}")
            return True
        
        except Exception as e:
            logger.error(f"Error removing root {root_path}: {e}")
            return False
    
    async def _initial_scan(self, root_path: Path):
        """Perform initial scan of existing files in a root."""
        root_state = self.roots.get(root_path)
        if not root_state:
            return
        
        logger.info(f"Starting initial scan: {root_path}")
        root_state.stats.current_file = "Initializing scan..."
        
        # Initialize start time for ETA calculation
        import time
        root_state.stats.indexing_start_time = time.time()
        
        try:
            # Load ALL metadata from fast msgpack cache (indexed files + ignore files)
            root_state.stats.current_file = "Loading metadata cache..."
            logger.info(f"Loading metadata from msgpack cache for branch {root_state.current_branch}")
            cache_data = root_state.file_watcher.load_metadata_cache(root_state.current_branch)
            indexed_files = cache_data["indexed_files"]
            
            # Get chunk count from cache (no ChromaDB query needed!)
            chunk_count = cache_data.get("chunk_count", 0)
            
            if indexed_files:
                logger.info(f"✓ Loaded {len(indexed_files)} files + {chunk_count} chunks from msgpack cache")
                # Normalize paths to forward slashes once when loading (performance optimization)
                normalized_indexed_files = {
                    k.replace('\\', '/'): v for k, v in indexed_files.items()
                }
                root_state.indexed_files.update(normalized_indexed_files)
                
                # Verify and reconcile with ChromaDB (cache might be stale)
                logger.info("Reconciling cache with ChromaDB...")
                reconcile_start = time.time()
                
                def update_reconcile_progress(phase: str = "Cache refresh"):
                    """Update UI with reconciliation progress and timer."""
                    elapsed = time.time() - reconcile_start
                    mins, secs = divmod(int(elapsed), 60)
                    time_str = f"{mins}:{secs:02d}"
                    root_state.stats.current_file = f"{phase} ({time_str})"
                
                # Initial update
                update_reconcile_progress()
                
                # Get actual indexed files from ChromaDB
                chroma_files_raw = await self.chroma_store.get_indexed_files(root_path, root_state.current_branch)
                await asyncio.sleep(0)  # Yield for UI update
                update_reconcile_progress()
                
                actual_chunk_count = await self.chroma_store.get_branch_chunk_count(root_path, root_state.current_branch)
                await asyncio.sleep(0)  # Yield for UI update
                update_reconcile_progress()
                
                # Normalize ChromaDB file paths to forward slashes for comparison
                chroma_files = {k.replace('\\', '/'): v for k, v in chroma_files_raw.items()}
                
                # Reconcile: Remove files from cache that aren't in ChromaDB
                cached_file_set = set(root_state.indexed_files.keys())
                chroma_file_set = set(chroma_files.keys())
                
                missing_in_chroma = cached_file_set - chroma_file_set
                if missing_in_chroma:
                    logger.warning(
                        f"Found {len(missing_in_chroma)} files in cache but not in ChromaDB - removing from cache"
                    )
                    if len(missing_in_chroma) > 100:
                        # Add periodic updates for large deletions
                        for i, file_key in enumerate(missing_in_chroma):
                            del root_state.indexed_files[file_key]
                            if i % 100 == 0:
                                update_reconcile_progress()
                                await asyncio.sleep(0)
                    else:
                        # Small number, delete all at once
                        for file_key in missing_in_chroma:
                            del root_state.indexed_files[file_key]
                
                # Update cached metadata from ChromaDB (source of truth)
                if len(chroma_files) > 100:
                    # Add periodic updates for large updates
                    for i, (file_key, chroma_metadata) in enumerate(chroma_files.items()):
                        if file_key in root_state.indexed_files:
                            root_state.indexed_files[file_key].update(chroma_metadata)
                        if i % 100 == 0:
                            update_reconcile_progress()
                            await asyncio.sleep(0)
                else:
                    # Small number, update all at once
                    for file_key, chroma_metadata in chroma_files.items():
                        if file_key in root_state.indexed_files:
                            root_state.indexed_files[file_key].update(chroma_metadata)
                
                # Update chunk count from actual ChromaDB value
                if actual_chunk_count != chunk_count:
                    logger.warning(
                        f"Cache mismatch: cache says {chunk_count} chunks, "
                        f"ChromaDB has {actual_chunk_count} chunks. Using ChromaDB value."
                    )
                    chunk_count = actual_chunk_count
                
                logger.info(f"✓ Reconciled: {len(root_state.indexed_files)} files, {chunk_count} chunks")
                root_state.stats.chunks_created = chunk_count
                root_state.stats.chunks_embedded = chunk_count
            else:
                logger.info(f"No indexed files found in msgpack cache (first run or cache missing)")
            
            # Check if ignore files changed since last run (loads from msgpack cache internally)
            root_state.stats.current_file = "Checking ignore files..."
            logger.info("Checking ignore files...")
            ignore_start = time.time()
            
            def update_ignore_progress(message: str):
                """Update UI with ignore checking progress."""
                root_state.stats.current_file = message
            
            ignore_files_changed, updated_ignore_stats = await root_state.file_watcher.check_ignore_files_on_startup(
                root_state.current_branch,
                update_ignore_progress
            )
            ignore_time = (time.time() - ignore_start) * 1000
            
            if ignore_files_changed:
                logger.info(f"✓ Ignore files changed, took {ignore_time:.0f}ms - will do full rescan")
                # Reload patterns in ignore matcher
                root_state.ignore_matcher.reload_patterns()
            else:
                logger.info(f"✓ Ignore files unchanged, took {ignore_time:.0f}ms")
            
            # Use Git-like hybrid approach if we have cached data
            use_cache_optimization = bool(indexed_files) and not ignore_files_changed
            
            if use_cache_optimization:
                logger.info("✓ Using Git-like optimization (cached + new file detection)")
                
                # Fast path: Use cached files + quick scan for new files
                def quick_scan_for_new_files():
                    """
                    Git-like recursive directory walk with early termination.
                    Never descends into ignored directories (huge performance win).
                    """
                    # Use root_state.indexed_files which has normalized paths (forward slashes)
                    known_files = set(root_state.indexed_files.keys())
                    cached_files = []
                    new_files = []
                    
                    def walk_directory(directory: Path, rel_prefix: str = ""):
                        """Recursively walk directory, skipping ignored dirs."""
                        try:
                            # Use scandir for efficiency (returns DirEntry objects with cached stat)
                            with os.scandir(directory) as entries:
                                for entry in entries:
                                    # Construct relative path
                                    entry_rel_path = f"{rel_prefix}{entry.name}" if rel_prefix else entry.name
                                    entry_path = Path(entry.path)
                                    
                                    if entry.is_dir(follow_symlinks=False):
                                        # Git optimization: Check if directory is ignored BEFORE descending
                                        # Add trailing slash for directory pattern matching
                                        if root_state.ignore_matcher.should_ignore(entry_path):
                                            # Skip this entire directory tree (never call readdir on it)
                                            continue
                                        
                                        # Directory not ignored - recurse into it
                                        walk_directory(entry_path, entry_rel_path + "/")
                                    
                                    elif entry.is_file(follow_symlinks=False):
                                        # Normalize to forward slashes for comparison with cached paths
                                        entry_rel_path_normalized = entry_rel_path.replace('\\', '/')
                                        
                                        # Check if this is a known file or new file
                                        if entry_rel_path_normalized in known_files:
                                            # Known file - just add it (we already know it's a text file)
                                            cached_files.append(entry_path)
                                        else:
                                            # Potential new file - check if ignored first (cheap)
                                            if not root_state.ignore_matcher.should_ignore(entry_path):
                                                # Not ignored - check if it's a text file (expensive)
                                                if root_state.file_watcher.is_text_file(entry_path):
                                                    new_files.append(entry_path)
                        
                        except PermissionError:
                            # Skip directories we can't read
                            pass
                        except Exception as e:
                            logger.debug(f"Error scanning {directory}: {e}")
                    
                    # Start recursive walk from root
                    walk_directory(root_path)
                    
                    return cached_files, new_files
                
                # Run quick scan in thread pool (use run_in_executor for better compatibility)
                scan_start = time.time()
                loop = asyncio.get_event_loop()
                cached_files, new_files = await loop.run_in_executor(None, quick_scan_for_new_files)
                scan_time = (time.time() - scan_start) * 1000
                
                text_files = cached_files + new_files
                logger.info(f"✓ Found {len(cached_files)} cached + {len(new_files)} new files in {scan_time:.0f}ms")
                
                # Log ignore cache performance (Git-like optimization stats)
                cache_stats = root_state.ignore_matcher.get_cache_stats()
                logger.debug(f"Ignore cache: {cache_stats['file_cache_size']} files, {cache_stats['dir_cache_size']} dirs cached")
            else:
                logger.info("Performing full filesystem scan (first run or ignore files changed)")
                
                # Set initial scanning message
                root_state.stats.current_file = "Scanning files..."
                scan_start_time = time.time()
                
                # Progress callback to update stats during scanning with timer
                def update_progress(scanned, found):
                    root_state.stats.files_scanned = scanned
                    elapsed = time.time() - scan_start_time
                    mins, secs = divmod(int(elapsed), 60)
                    time_str = f"{mins}:{secs:02d}"
                    root_state.stats.current_file = f"Scanning files ({time_str}): {scanned} scanned, {found} found"
                
                # Scan for text files (this runs in thread pool now, so won't block)
                text_files = await root_state.file_watcher.scan_existing_files(update_progress)
            
            root_state.stats.files_scanned = len(text_files)
            
            # Filter out already indexed files with matching hash
            files_to_index = []
            files_skipped = 0
            
            root_state.stats.current_file = "Checking which files need indexing..."
            
            for file_path in text_files:
                # Use relative path as key to match ChromaDB storage format
                rel_path = file_path.relative_to(root_path) if file_path.is_absolute() else file_path
                file_key = str(rel_path).replace('\\', '/')  # Normalize to forward slashes ONCE when adding
                
                # Check if already indexed
                if file_key in root_state.indexed_files:
                    # File was previously indexed - use mtime AND size for fast check
                    try:
                        # Get current file metadata (both very fast)
                        file_stat = file_path.stat()
                        current_mtime = file_stat.st_mtime
                        current_size = file_stat.st_size
                        
                        indexed_info = root_state.indexed_files[file_key]
                        indexed_mtime = indexed_info.get("mtime", 0.0)
                        indexed_size = indexed_info.get("size", 0)
                        
                        # Fast check: if BOTH mtime and size unchanged, skip (NO HASHING!)
                        if current_mtime == indexed_mtime and current_size == indexed_size:
                            files_skipped += 1
                            continue  # Skip - file hasn't changed
                        
                        # Mtime or size changed - verify with hash to avoid false positives
                        # (e.g., file touched but content unchanged, or size coincidentally same)
                        current_hash = await self._hash_file(file_path)
                        if not current_hash:
                            # Hash failed, queue for re-indexing
                            files_to_index.append(file_path)
                            continue
                        
                        indexed_hash = indexed_info.get("hash", "")
                        if indexed_hash == current_hash:
                            # Content unchanged, update metadata and skip
                            root_state.indexed_files[file_key]["mtime"] = current_mtime
                            root_state.indexed_files[file_key]["size"] = current_size
                            files_skipped += 1
                            continue
                        
                        # Hash changed, queue for re-indexing
                        files_to_index.append(file_path)
                    except Exception as e:
                        logger.debug(f"Error checking {file_path}: {e}")
                        # On error, queue for re-indexing
                        files_to_index.append(file_path)
                else:
                    # Not indexed yet - queue it (no hashing needed!)
                    files_to_index.append(file_path)
            
            # Set files_indexed to count only the files that are already done (and still exist)
            root_state.stats.files_indexed = files_skipped
            
            logger.info(
                f"Scan complete: {len(text_files)} files found, "
                f"{files_skipped} already indexed, "
                f"{len(files_to_index)} need indexing"
            )
            
            if not files_to_index:
                root_state.stats.current_file = None
                logger.info(f"All files already indexed for {root_path}")
                
                # Save ignore file stats for future startups
                await self._save_metadata_cache(root_path, root_state)
                
                # Start file watcher now that scan is complete
                root_state.file_watcher.start()
                logger.info(f"File watcher started for {root_path}")
                return
            
            root_state.stats.current_file = f"Queueing {len(files_to_index)} files for indexing..."
            
            # Reset timer and baseline for ETA calculation (now we're actually indexing NEW files)
            import time
            root_state.stats.indexing_start_time = time.time()
            root_state.stats.files_indexed_at_start = files_skipped  # Start from number of skipped files
            
            # Queue files for indexing in batches
            batch_size = 100
            
            for i in range(0, len(files_to_index), batch_size):
                batch = files_to_index[i:i + batch_size]
                for file_path in batch:
                    # Priority 2 for bulk scan
                    priority_item = (2, self._queue_counter, (root_path, file_path, "created"))
                    self._queue_counter += 1
                    await self._indexing_queue.put(priority_item)
                
                # Update progress
                root_state.stats.current_file = f"Queued {min(i + batch_size, len(files_to_index))}/{len(files_to_index)} files"
                
                # Yield control after each batch
                await asyncio.sleep(0)
            
            root_state.stats.current_file = None
            logger.info(f"Initial scan complete: {root_path}, {len(files_to_index)} files queued")
            
            # Save metadata cache (ignore files + indexed files) for future startups
            await self._save_metadata_cache(root_path, root_state)
            
            # Start file watcher now that initial scan and queueing is complete
            root_state.file_watcher.start()
            logger.info(f"File watcher started for {root_path}")
        
        except Exception as e:
            logger.error(f"Error in initial scan for {root_path}: {e}")
            root_state.stats.current_file = None
            # Start file watcher even on error to enable future file changes
            if root_state and root_state.file_watcher:
                root_state.file_watcher.start()
                logger.info(f"File watcher started for {root_path} (after error)")
    
    async def _on_file_change(self, file_path: Path, event_type: str):
        """Handle file change events from file watcher."""
        # Find the root for this file
        root_path = None
        for root in self.roots.keys():
            try:
                file_path.relative_to(root)
                root_path = root
                break
            except ValueError:
                continue
        
        if not root_path:
            logger.debug(f"File not in any root: {file_path}")
            return
        
        # Queue for indexing with priority
        # Priority 0 for modified files, priority 1 for new files
        priority = 0 if event_type == "modified" else 1
        priority_item = (priority, self._queue_counter, (root_path, file_path, event_type))
        self._queue_counter += 1
        await self._indexing_queue.put(priority_item)
    
    async def _on_branch_change(self, root_path: Path, new_branch: str, new_commit: str):
        """Handle branch change events."""
        root_state = self.roots.get(root_path)
        if not root_state:
            return
        
        logger.info(f"Branch changed in {root_path}: {root_state.current_branch} -> {new_branch}")
        
        root_state.current_branch = new_branch
        
        # Stop file watcher during re-scan (will be restarted after scan completes)
        await root_state.file_watcher.stop()
        logger.info(f"File watcher stopped for branch change re-scan")
        
        # Re-scan files for new branch
        await self._initial_scan(root_path)
    
    async def _on_ignore_file_changed(self, root_path: Path, ignore_file: Path):
        """Handle changes to .gitignore or .fskbignore files."""
        root_state = self.roots.get(root_path)
        if not root_state:
            return
        
        logger.info(f"Processing ignore file change: {ignore_file}")
        
        # Reload ignore patterns
        root_state.ignore_matcher.reload_patterns()
        
        # Get all currently indexed files
        indexed_files = list(root_state.indexed_files.keys())
        
        # Check which indexed files are now ignored
        files_to_remove = []
        for file_key in indexed_files:
            file_path = root_path / file_key
            if root_state.ignore_matcher.should_ignore(file_path):
                files_to_remove.append(file_key)
        
        if files_to_remove:
            logger.info(f"Removing {len(files_to_remove)} files now ignored")
            
            # Remove from index and ChromaDB
            for file_key in files_to_remove:
                file_path = root_path / file_key
                
                # Delete chunks from ChromaDB
                chunks_deleted = await self.chroma_store.delete_file_chunks(
                    root_path=root_path,
                    branch_name=root_state.current_branch,
                    file_path=file_path,
                )
                
                # Update stats
                if chunks_deleted > 0:
                    root_state.stats.chunks_created = max(0, root_state.stats.chunks_created - chunks_deleted)
                    root_state.stats.chunks_embedded = max(0, root_state.stats.chunks_embedded - chunks_deleted)
                
                # Remove from indexed files
                if file_key in root_state.indexed_files:
                    del root_state.indexed_files[file_key]
                root_state.stats.files_indexed = max(0, root_state.stats.files_indexed - 1)
                root_state.stats.files_scanned = max(0, root_state.stats.files_scanned - 1)
            
            logger.info(f"Cleanup complete: {len(files_to_remove)} files removed from index")
        
        # Scan for new files that were previously ignored but now included
        await self._scan_for_newly_included_files(root_path)
        
        # Save updated metadata cache (ignore files + indexed files)
        await self._save_metadata_cache(root_path, root_state)
    
    async def _scan_for_newly_included_files(self, root_path: Path):
        """Scan for files that were previously ignored but are now included."""
        root_state = self.roots.get(root_path)
        if not root_state:
            return
        
        try:
            logger.info(f"Scanning for newly included files in {root_path}")
            
            # Scan for all text files
            text_files = set()
            for item in root_path.rglob("*"):
                if item.is_file() and root_state.file_watcher.is_text_file(item):
                    # Check if not ignored
                    if not root_state.ignore_matcher.should_ignore(item):
                        text_files.add(item)
            
            # Find files that exist on disk but aren't indexed
            files_to_add = []
            for file_path in text_files:
                rel_path = file_path.relative_to(root_path) if file_path.is_absolute() else file_path
                file_key = str(rel_path).replace('\\', '/')  # Normalize to forward slashes
                
                if file_key not in root_state.indexed_files:
                    files_to_add.append(file_path)
            
            if files_to_add:
                logger.info(f"Found {len(files_to_add)} newly included files, queueing for indexing")
                
                # Queue for indexing (priority 1 for newly included files)
                for file_path in files_to_add:
                    priority_item = (1, self._queue_counter, (root_path, file_path, "created"))
                    self._queue_counter += 1
                    await self._indexing_queue.put(priority_item)
                
                # Update scanned count
                root_state.stats.files_scanned += len(files_to_add)
            else:
                logger.info("No newly included files found")
        
        except Exception as e:
            logger.error(f"Error scanning for newly included files: {e}")
    
    async def _save_metadata_cache(self, root_path: Path, root_state):
        """Save ALL metadata (ignore files + indexed files) to fast msgpack cache."""
        try:
            # Get ignore file stats
            ignore_stats_raw = root_state.file_watcher.get_ignore_file_stats()
            ignore_stats = {}
            for file_path_str, (mtime, size, hash_val) in ignore_stats_raw.items():
                # Convert to relative path
                try:
                    rel_path = Path(file_path_str).relative_to(root_path)
                    ignore_stats[str(rel_path)] = {"mtime": mtime, "size": size, "hash": hash_val}
                except ValueError:
                    # Already relative
                    ignore_stats[file_path_str] = {"mtime": mtime, "size": size, "hash": hash_val}
            
            # Get indexed file stats (already in correct format)
            indexed_files = root_state.indexed_files
            
            # Get current chunk count
            chunk_count = root_state.stats.chunks_created
            
            # Save to msgpack cache (ultra-fast!)
            root_state.file_watcher.save_metadata_cache(
                root_state.current_branch,
                ignore_stats,
                indexed_files,
                chunk_count
            )
            logger.debug(f"Saved metadata cache: {len(indexed_files)} indexed files, {chunk_count} chunks, {len(ignore_stats)} ignore files")
        except Exception as e:
            logger.error(f"Error saving metadata cache: {e}")
    
    async def _indexing_worker(self, worker_id: int):
        """Worker task that processes indexing queue."""
        logger.info(f"Indexing worker {worker_id} started")
        
        while self._running:
            try:
                # Get item from priority queue with timeout
                try:
                    priority_item = await asyncio.wait_for(
                        self._indexing_queue.get(),
                        timeout=1.0
                    )
                    # Unpack: (priority, counter, (root_path, file_path, event_type))
                    _, _, task_data = priority_item
                    root_path, file_path, event_type = task_data
                except asyncio.TimeoutError:
                    # Log occasional timeout to show workers are alive
                    if worker_id == 0:  # Only worker 0 logs to avoid spam
                        queue_size = self._indexing_queue.qsize()
                        if queue_size > 0:
                            logger.warning(f"Worker timeout but queue has {queue_size} items - checking resource manager...")
                    continue
                
                # Check if root is paused
                root_state = self.roots.get(root_path)
                if root_state and root_state.paused:
                    # Put the file back in queue with same priority
                    priority_item = (priority_item[0], self._queue_counter, task_data)
                    self._queue_counter += 1
                    await self._indexing_queue.put(priority_item)
                    # Mark as done so we don't deadlock
                    self._indexing_queue.task_done()
                    # Sleep longer when paused to avoid busy-waiting
                    await asyncio.sleep(0.5)
                    continue
                
                # Wait if throttled
                await self.resource_manager.wait_if_throttled()
                
                # Process the file
                try:
                    await self._index_file(root_path, file_path, event_type)
                    
                    # Log progress periodically
                    root_state = self.roots.get(root_path)
                    if root_state:
                        if root_state.stats.files_indexed == 1:
                            # Log first file to confirm workers are running
                            logger.info(f"Indexing started - first file processed")
                        elif root_state.stats.files_indexed % 50 == 0:
                            remaining = self._indexing_queue.qsize()
                            logger.info(
                                f"Progress: {root_state.stats.files_indexed}/{root_state.stats.files_scanned} files indexed "
                                f"({root_state.stats.chunks_embedded} chunks), {remaining} in queue"
                            )
                            
                            # Save metadata cache every 50 files for recovery (in sync with progress logging)
                            asyncio.create_task(self._save_metadata_cache(root_path, root_state))
                
                except Exception as e:
                    logger.error(f"Error indexing {file_path}: {e}")
                    root_state = self.roots.get(root_path)
                    if root_state:
                        root_state.stats.errors += 1
                
                # Mark task as done
                self._indexing_queue.task_done()
                
                # Clear current file if queue is empty and all files are indexed
                root_state = self.roots.get(root_path)
                if root_state:
                    queue_size = self._indexing_queue.qsize()
                    if queue_size == 0 and root_state.stats.files_indexed >= root_state.stats.files_scanned:
                        root_state.stats.current_file = None
                        
                        # Save metadata when going idle to persist last batch (<50 files)
                        if root_state.stats.files_indexed > 0:
                            asyncio.create_task(self._save_metadata_cache(root_path, root_state))
                            logger.info(f"Saved metadata on idle for {root_path}")
                
                # Yield control
                await asyncio.sleep(0)
            
            except asyncio.CancelledError:
                break
            except Exception as e:
                logger.error(f"Error in indexing worker {worker_id}: {e}")
        
        logger.debug(f"Indexing worker {worker_id} stopped")
    
    async def _index_file(self, root_path: Path, file_path: Path, event_type: str):
        """Index a single file."""
        root_state = self.roots.get(root_path)
        if not root_state:
            return
        
        # Ensure file_path is absolute (resolve relative paths against root)
        if not file_path.is_absolute():
            file_path = root_path / file_path
        
        root_state.stats.current_file = str(file_path)
        
        # Use relative path as key to match ChromaDB storage format
        rel_path = file_path.relative_to(root_path)
        file_key = str(rel_path).replace('\\', '/')  # Normalize to forward slashes
        
        if event_type == "deleted":
            # Remove from index and get count of deleted chunks
            chunks_deleted = await self.chroma_store.delete_file_chunks(
                root_path=root_path,
                branch_name=root_state.current_branch,
                file_path=file_path,
            )
            
            if file_key in root_state.indexed_files:
                del root_state.indexed_files[file_key]
            
            # Update stats to reflect deleted chunks and file
            if chunks_deleted > 0:
                root_state.stats.chunks_created = max(0, root_state.stats.chunks_created - chunks_deleted)
                root_state.stats.chunks_embedded = max(0, root_state.stats.chunks_embedded - chunks_deleted)
            
            root_state.stats.files_indexed = max(0, root_state.stats.files_indexed - 1)
            root_state.stats.files_scanned = max(0, root_state.stats.files_scanned - 1)
            
            logger.info(f"Removed from index: {file_path} ({chunks_deleted} chunks reclaimed)")
            return
        
        # Get file metadata (mtime and size) and hash
        try:
            file_stat = file_path.stat()
            file_mtime = file_stat.st_mtime
            file_size = file_stat.st_size
        except Exception as e:
            logger.error(f"Error getting file stats for {file_path}: {e}")
            return
        
        file_hash = await self._hash_file(file_path)
        if not file_hash:
            return
        
        # Check if already indexed with same hash
        if file_key in root_state.indexed_files:
            indexed_info = root_state.indexed_files[file_key]
            if indexed_info.get("hash") == file_hash:
                logger.debug(f"File unchanged, skipping: {file_path}")
                return
        
        # Chunk the file
        chunks = await asyncio.to_thread(self.chunker.chunk_file, str(file_path))
        
        if not chunks:
            logger.debug(f"No chunks generated for {file_path}")
            root_state.stats.files_indexed += 1  # Count as indexed even if no chunks
            return
        
        # Check cache for existing embeddings by content hash
        content_hashes = [chunk.content_hash for chunk in chunks]
        cached_embeddings = await self.chroma_store.get_cached_embeddings(content_hashes)
        
        # Separate chunks into cached vs needs-embedding
        chunks_to_embed = []
        chunks_to_embed_indices = []
        final_embeddings = [None] * len(chunks)  # Placeholder list
        
        for i, (chunk, content_hash) in enumerate(zip(chunks, content_hashes)):
            cached = cached_embeddings.get(content_hash)
            if cached is not None:
                final_embeddings[i] = cached
            else:
                chunks_to_embed.append(chunk.content)
                chunks_to_embed_indices.append(i)
        
        # Generate embeddings only for non-cached chunks
        cache_hits = len(chunks) - len(chunks_to_embed)
        if cache_hits > 0:
            logger.debug(f"Cache hit: {cache_hits}/{len(chunks)} chunks for {file_path.name}")
        
        if chunks_to_embed:
            new_embeddings = await self.embedding_provider.embed_texts(chunks_to_embed)
            
            # Place new embeddings in final list
            for idx, embedding in zip(chunks_to_embed_indices, new_embeddings):
                final_embeddings[idx] = embedding
            
            # Cache the newly generated embeddings (deduplicate first)
            new_hashes = [content_hashes[i] for i in chunks_to_embed_indices]
            
            # Deduplicate hashes before caching (keep first occurrence)
            unique_cache = {}
            for hash_val, emb in zip(new_hashes, new_embeddings):
                if hash_val not in unique_cache:
                    unique_cache[hash_val] = emb
            
            if unique_cache:
                await self.chroma_store.cache_embeddings(
                    list(unique_cache.keys()),
                    list(unique_cache.values())
                )
        
        embeddings = final_embeddings
        
        # If file was previously indexed (modified), delete old chunks first
        old_chunks_deleted = 0
        if file_key in root_state.indexed_files:
            logger.debug(f"Deleting old chunks for modified file: {file_path.name}")
            old_chunks_deleted = await self.chroma_store.delete_file_chunks(
                root_path=root_path,
                branch_name=root_state.current_branch,
                file_path=file_path,
            )
            
            # Subtract old chunks from stats
            if old_chunks_deleted > 0:
                root_state.stats.chunks_created = max(0, root_state.stats.chunks_created - old_chunks_deleted)
                root_state.stats.chunks_embedded = max(0, root_state.stats.chunks_embedded - old_chunks_deleted)
        
        # Add new chunks to stats
        root_state.stats.chunks_created += len(embeddings)
        root_state.stats.chunks_embedded += len(embeddings)
        
        # Store in ChromaDB
        chunk_dicts = [chunk.to_dict() for chunk in chunks]
        await self.chroma_store.add_chunks(
            root_path=root_path,
            branch_name=root_state.current_branch,
            file_path=file_path,
            file_hash=file_hash,
            file_mtime=file_mtime,
            file_size=file_size,
            chunks=chunk_dicts,
            embeddings=embeddings,
        )
        
        # Update state - mark file as indexed with hash, mtime, and size
        root_state.indexed_files[file_key] = {
            "hash": file_hash,
            "mtime": file_mtime,
            "size": file_size
        }
        root_state.stats.files_indexed += 1
        
        # Clear current file marker after successful indexing
        root_state.stats.current_file = None
        
        # Only log individual files in debug mode
        num_chunks = len(chunks)
        logger.debug(f"Indexed: {file_path} ({num_chunks} chunks)")
        
        # Clear embedding references to free memory immediately (AFTER logging)
        del embeddings
        del final_embeddings
        if chunks_to_embed:
            del new_embeddings
        del chunks
        del chunk_dicts
        
        # Explicit GPU cleanup to prevent fragmentation
        try:
            import torch
            if torch.cuda.is_available():
                torch.cuda.empty_cache()
        except:
            pass
    
    @staticmethod
    async def _hash_file(file_path: Path) -> Optional[str]:
        """Calculate SHA256 hash of a file."""
        try:
            def _hash():
                hasher = hashlib.sha256()
                with open(file_path, "rb") as f:
                    while chunk := f.read(8192):
                        hasher.update(chunk)
                return hasher.hexdigest()
            
            return await asyncio.to_thread(_hash)
        
        except Exception as e:
            logger.error(f"Error hashing file {file_path}: {e}")
            return None
    
    def get_stats(self, root_path: Optional[Path] = None) -> Dict:
        """Get indexing statistics."""
        if root_path:
            root_state = self.roots.get(root_path)
            if root_state:
                return {
                    "root_path": str(root_path),
                    "branch": root_state.current_branch,
                    "files_scanned": root_state.stats.files_scanned,
                    "files_indexed": root_state.stats.files_indexed,
                    "chunks_created": root_state.stats.chunks_created,
                    "chunks_embedded": root_state.stats.chunks_embedded,
                    "errors": root_state.stats.errors,
                    "current_file": root_state.stats.current_file,
                }
        
        # Aggregate stats for all roots
        total_stats = {
            "roots": len(self.roots),
            "files_scanned": 0,
            "files_indexed": 0,
            "chunks_created": 0,
            "chunks_embedded": 0,
            "errors": 0,
            "queue_size": self._indexing_queue.qsize(),
        }
        
        for root_state in self.roots.values():
            total_stats["files_scanned"] += root_state.stats.files_scanned
            total_stats["files_indexed"] += root_state.stats.files_indexed
            total_stats["chunks_created"] += root_state.stats.chunks_created
            total_stats["chunks_embedded"] += root_state.stats.chunks_embedded
            total_stats["errors"] += root_state.stats.errors
        
        return total_stats
    
    def pause_root(self, root_path: Path) -> bool:
        """
        Pause indexing for a specific root.
        
        Args:
            root_path: Path to root directory
        
        Returns:
            True if successfully paused
        """
        root_state = self.roots.get(root_path)
        if not root_state:
            return False
        
        root_state.paused = True
        logger.info(f"Paused indexing for {root_path}")
        return True
    
    def resume_root(self, root_path: Path) -> bool:
        """
        Resume indexing for a specific root.
        
        Args:
            root_path: Path to root directory
        
        Returns:
            True if successfully resumed
        """
        root_state = self.roots.get(root_path)
        if not root_state:
            return False
        
        root_state.paused = False
        logger.info(f"Resumed indexing for {root_path}")
        return True
    
    def is_root_paused(self, root_path: Path) -> bool:
        """Check if a root is paused."""
        root_state = self.roots.get(root_path)
        return root_state.paused if root_state else False

