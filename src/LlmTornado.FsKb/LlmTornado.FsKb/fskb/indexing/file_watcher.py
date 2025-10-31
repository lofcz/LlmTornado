"""
Async file system monitoring with debouncing and text file detection.
Includes Merkle tree for efficient directory-level change detection.
"""

import asyncio
import hashlib
from pathlib import Path
from typing import Callable, Optional, Set, Dict, Any
from datetime import datetime, timedelta
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler, FileSystemEvent
from loguru import logger
import msgpack

from .merkle_tree import MerkleTree


class FileWatcher:
    """
    Watches file system for changes with debouncing and text file detection.
    """
    
    def __init__(
        self,
        root_path: Path,
        callback: Callable[[Path, str], None],
        text_extensions: Set[str],
        max_file_size_mb: int = 10,
        debounce_delay_ms: int = 500,
        ignore_matcher=None,
    ):
        """
        Initialize file watcher.
        
        Args:
            root_path: Root directory to watch
            callback: Async callback(file_path, event_type) for file changes
            text_extensions: Set of allowed file extensions
            max_file_size_mb: Maximum file size to process
            debounce_delay_ms: Debounce delay in milliseconds
            ignore_matcher: IgnorePatternMatcher instance
        """
        self.root_path = Path(root_path)
        self.callback = callback
        self.text_extensions = text_extensions
        self.max_file_size_bytes = max_file_size_mb * 1024 * 1024
        self.debounce_delay = debounce_delay_ms / 1000.0
        self.ignore_matcher = ignore_matcher
        
        # Debouncing state
        self._pending_events: Dict[Path, tuple[str, datetime]] = {}
        self._debounce_task: Optional[asyncio.Task] = None
        self._observer: Optional[Observer] = None
        self._running = False
        
        # Merkle tree for efficient change detection
        self._merkle_tree: Optional[MerkleTree] = None
        self._file_hashes: Dict[Path, str] = {}  # Cache of file hashes
        
        # Ignore file tracking (.gitignore, .fskbignore)
        self._ignore_file_stats: Dict[Path, tuple[float, int]] = {}  # path -> (mtime, size)
        self._ignore_file_hashes: Dict[Path, str] = {}  # path -> hash
        self._on_ignore_change_callback: Optional[Callable] = None
    
    def start(self):
        """Start watching the file system."""
        if self._running:
            return
        
        self._running = True
        
        # Create watchdog observer
        event_handler = _FileEventHandler(self)
        self._observer = Observer()
        self._observer.schedule(event_handler, str(self.root_path), recursive=True)
        self._observer.start()
        
        # Start debounce task
        self._debounce_task = asyncio.create_task(self._process_pending_events())
        
        logger.info(f"File watcher started for {self.root_path}")
    
    async def stop(self):
        """Stop watching the file system."""
        self._running = False
        
        if self._observer:
            self._observer.stop()
            self._observer.join(timeout=5)
        
        if self._debounce_task:
            self._debounce_task.cancel()
            try:
                await self._debounce_task
            except asyncio.CancelledError:
                pass
        
        logger.info(f"File watcher stopped for {self.root_path}")
    
    def set_ignore_change_callback(self, callback: Callable):
        """Set callback for when ignore files change."""
        self._on_ignore_change_callback = callback
    
    async def check_ignore_files_on_startup(self, branch: str, progress_callback=None) -> tuple[bool, Dict[str, Dict[str, Any]]]:
        """
        Check if .gitignore or .fskbignore changed since last run (loads from fast msgpack cache).
        
        Args:
            branch: Git branch name
            progress_callback: Optional callback to report progress
        
        Returns:
            Tuple of (changed: bool, updated_stats: Dict)
        """
        # Load from msgpack cache (ultra-fast!)
        import time
        check_start = time.time()
        cache_data = self.load_metadata_cache(branch)
        cached_ignore_files = cache_data["ignore_files"]
        load_time = (time.time() - check_start) * 1000
        logger.debug(f"Loaded ignore file cache in {load_time:.1f}ms ({len(cached_ignore_files)} files)")
        
        if progress_callback:
            progress_callback("Checking ignore files...")
        
        ignore_files = [
            self.root_path / ".gitignore",
            self.root_path / ".fskbignore",
        ]
        
        changed = False
        updated_stats = {}
        last_update = check_start
        
        def maybe_update_progress():
            """Update progress with current elapsed time."""
            nonlocal last_update
            if progress_callback:
                now = time.time()
                elapsed = now - check_start
                mins, secs = divmod(int(elapsed), 60)
                time_str = f"{mins}:{secs:02d}"
                progress_callback(f"Checking ignore files ({time_str})")
                last_update = now
        
        # Force initial update to show timer started
        if progress_callback:
            progress_callback(f"Checking ignore files (0:00)")
            last_update = check_start  # Reset timer after forced update
        
        for ignore_file in ignore_files:
            maybe_update_progress()  # Update at start of each file
            file_key = str(ignore_file.relative_to(self.root_path))
            
            if not ignore_file.exists():
                # If file didn't exist before and still doesn't, no change
                if file_key not in cached_ignore_files:
                    continue
                # File was deleted
                logger.info(f"Ignore file deleted: {ignore_file}")
                changed = True
                continue
            
            # Fast check: mtime + size
            stat = ignore_file.stat()
            current_mtime = stat.st_mtime
            current_size = stat.st_size
            
            cached_entry = cached_ignore_files.get(file_key, {})
            if cached_entry:
                cached_mtime = cached_entry.get("mtime", 0)
                cached_size = cached_entry.get("size", 0)
                cached_hash = cached_entry.get("hash", "")
                
                # If mtime and size match, no change
                if current_mtime == cached_mtime and current_size == cached_size:
                    # Update cache for future checks
                    self._ignore_file_stats[ignore_file] = (current_mtime, current_size)
                    self._ignore_file_hashes[ignore_file] = cached_hash
                    updated_stats[file_key] = {"mtime": current_mtime, "size": current_size, "hash": cached_hash}
                    continue
                
                # Mtime or size changed - verify with hash
                current_hash = await self._hash_file(ignore_file)
                maybe_update_progress()
                if current_hash != cached_hash:
                    logger.info(f"Ignore file changed: {ignore_file}")
                    changed = True
                    # Update cache
                    self._ignore_file_stats[ignore_file] = (current_mtime, current_size)
                    self._ignore_file_hashes[ignore_file] = current_hash
                    updated_stats[file_key] = {"mtime": current_mtime, "size": current_size, "hash": current_hash}
                else:
                    # Content unchanged (false positive from mtime/size)
                    self._ignore_file_stats[ignore_file] = (current_mtime, current_size)
                    self._ignore_file_hashes[ignore_file] = current_hash
                    updated_stats[file_key] = {"mtime": current_mtime, "size": current_size, "hash": current_hash}
            else:
                # New ignore file
                current_hash = await self._hash_file(ignore_file)
                maybe_update_progress()
                logger.info(f"New ignore file detected: {ignore_file}")
                changed = True
                self._ignore_file_stats[ignore_file] = (current_mtime, current_size)
                self._ignore_file_hashes[ignore_file] = current_hash
                updated_stats[file_key] = {"mtime": current_mtime, "size": current_size, "hash": current_hash}
            
            # Yield to allow UI updates
            await asyncio.sleep(0)
        
        return changed, updated_stats
    
    async def _check_ignore_file_change(self, ignore_file: Path):
        """Check if an ignore file changed (called when file watcher detects modification)."""
        if not ignore_file.exists():
            # File was deleted
            if ignore_file in self._ignore_file_stats:
                logger.info(f"Ignore file deleted: {ignore_file}")
                del self._ignore_file_stats[ignore_file]
                del self._ignore_file_hashes[ignore_file]
                
                if self._on_ignore_change_callback:
                    await self._on_ignore_change_callback(ignore_file)
            return
        
        # Fast check: mtime + size
        stat = ignore_file.stat()
        current_key = (stat.st_mtime, stat.st_size)
        
        cached_key = self._ignore_file_stats.get(ignore_file)
        
        if cached_key == current_key:
            # No change
            return
        
        # Mtime or size changed - verify with hash
        current_hash = await self._hash_file(ignore_file)
        cached_hash = self._ignore_file_hashes.get(ignore_file)
        
        if current_hash != cached_hash:
            # Confirmed change
            logger.info(f"Ignore file changed: {ignore_file}")
            self._ignore_file_stats[ignore_file] = current_key
            self._ignore_file_hashes[ignore_file] = current_hash
            
            if self._on_ignore_change_callback:
                await self._on_ignore_change_callback(ignore_file)
        else:
            # False positive - update cache
            self._ignore_file_stats[ignore_file] = current_key
    
    async def _hash_file(self, file_path: Path) -> str:
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
            logger.error(f"Error hashing {file_path}: {e}")
            return ""
    
    def get_ignore_file_stats(self) -> Dict[str, tuple[float, int, str]]:
        """Get current ignore file stats for persistence."""
        stats = {}
        for ignore_file, (mtime, size) in self._ignore_file_stats.items():
            hash_val = self._ignore_file_hashes.get(ignore_file, "")
            stats[str(ignore_file)] = (mtime, size, hash_val)
        return stats
    
    def _get_metadata_cache_path(self) -> Path:
        """Get path to fast binary metadata cache file."""
        cache_dir = self.root_path / ".fskb"
        return cache_dir / "metadata_cache.msgpack"
    
    def load_metadata_cache(self, branch: str) -> Dict[str, Any]:
        """
        Load ALL metadata from local msgpack cache (ultra-fast).
        
        Returns dict with:
        {
            "ignore_files": {...},
            "indexed_files": {...}
        }
        """
        cache_path = self._get_metadata_cache_path()
        if not cache_path.exists():
            return {"ignore_files": {}, "indexed_files": {}}
        
        try:
            with open(cache_path, 'rb') as f:
                data = msgpack.unpackb(f.read(), raw=False)
            branch_data = data.get(branch, {})
            # Ensure all keys exist
            return {
                "ignore_files": branch_data.get("ignore_files", {}),
                "indexed_files": branch_data.get("indexed_files", {}),
                "chunk_count": branch_data.get("chunk_count", 0)
            }
        except Exception as e:
            logger.warning(f"Failed to load metadata cache: {e}")
            return {"ignore_files": {}, "indexed_files": {}}
    
    def save_metadata_cache(
        self, 
        branch: str, 
        ignore_files: Dict[str, Dict[str, Any]],
        indexed_files: Dict[str, Dict[str, Any]],
        chunk_count: int = 0
    ):
        """
        Save ALL metadata to local msgpack cache (ultra-fast).
        
        Args:
            branch: Git branch name
            ignore_files: Dict of {file_path: {"mtime": ..., "size": ..., "hash": ...}}
            indexed_files: Dict of {file_path: {"mtime": ..., "size": ..., "hash": ...}}
            chunk_count: Total chunk count (to avoid ChromaDB query on next load)
        """
        cache_path = self._get_metadata_cache_path()
        cache_path.parent.mkdir(parents=True, exist_ok=True)
        
        try:
            # Load existing cache (may have other branches)
            if cache_path.exists():
                with open(cache_path, 'rb') as f:
                    data = msgpack.unpackb(f.read(), raw=False)
            else:
                data = {}
            
            # Update this branch's data
            data[branch] = {
                "ignore_files": ignore_files,
                "indexed_files": indexed_files,
                "chunk_count": chunk_count
            }
            
            # Write atomically
            temp_path = cache_path.with_suffix('.tmp')
            with open(temp_path, 'wb') as f:
                f.write(msgpack.packb(data, use_bin_type=True))
            temp_path.replace(cache_path)
            
            logger.debug(f"Saved metadata cache: {len(indexed_files)} indexed files, {len(ignore_files)} ignore files")
        except Exception as e:
            logger.warning(f"Failed to save metadata cache: {e}")
    
    def _on_file_event(self, event: FileSystemEvent):
        """Handle file system event (called by watchdog)."""
        if event.is_directory:
            return
        
        file_path = Path(event.src_path)
        
        # Check if this is an ignore file (.gitignore or .fskbignore)
        if file_path.name in [".gitignore", ".fskbignore"]:
            # Handle ignore file change via callback (thread-safe)
            if event.event_type in ["created", "modified", "deleted"]:
                if self._on_ignore_change_callback:
                    # Call the callback directly from this thread
                    # The callback will handle scheduling in the event loop
                    try:
                        import asyncio
                        loop = asyncio.get_event_loop()
                        # Schedule coroutine in thread-safe way
                        asyncio.run_coroutine_threadsafe(
                            self._check_ignore_file_change(file_path),
                            loop
                        )
                    except RuntimeError:
                        # Event loop not available, log and skip
                        logger.debug(f"Event loop not available for ignore file change: {file_path}")
            return
        
        # Check if should ignore
        if self.ignore_matcher and self.ignore_matcher.should_ignore(file_path):
            return
        
        # Determine event type
        if event.event_type == "created":
            event_type = "created"
        elif event.event_type == "modified":
            event_type = "modified"
        elif event.event_type == "deleted":
            event_type = "deleted"
        elif event.event_type == "moved":
            event_type = "moved"
        else:
            return
        
        # Add to pending events (debouncing)
        self._pending_events[file_path] = (event_type, datetime.now())
    
    async def _process_pending_events(self):
        """Process pending events with debouncing."""
        while self._running:
            try:
                await asyncio.sleep(self.debounce_delay)
                
                if not self._pending_events:
                    continue
                
                now = datetime.now()
                cutoff = now - timedelta(seconds=self.debounce_delay)
                
                # Process events that are old enough
                to_process = []
                for file_path, (event_type, timestamp) in list(self._pending_events.items()):
                    if timestamp <= cutoff:
                        to_process.append((file_path, event_type))
                        del self._pending_events[file_path]
                
                # Process each event
                for file_path, event_type in to_process:
                    if event_type != "deleted" and not self.is_text_file(file_path):
                        continue
                    
                    try:
                        await self.callback(file_path, event_type)
                    except Exception as e:
                        logger.error(f"Error in file watcher callback for {file_path}: {e}")
            
            except asyncio.CancelledError:
                break
            except Exception as e:
                logger.error(f"Error processing pending events: {e}")
    
    def is_text_file(self, file_path: Path) -> bool:
        """
        Check if a file is a text file we should process.
        
        Args:
            file_path: Path to file
        
        Returns:
            True if file should be processed
        """
        try:
            # Check if file exists
            if not file_path.exists():
                return False
            
            # Quick check: skip if in problematic directories
            path_parts = file_path.parts
            skip_dirs = {'node_modules', 'bin', 'obj', '.git', '__pycache__', '.venv', 'venv'}
            if any(part in skip_dirs for part in path_parts):
                return False
            
            # Check file size (must be fast)
            file_size = file_path.stat().st_size
            if file_size > self.max_file_size_bytes:
                return False
            
            # Skip empty files
            if file_size == 0:
                return False
            
            # Check extension first (fast and reliable)
            ext = file_path.suffix.lower()
            if ext in self.text_extensions:
                return True
            
            # For files without recognized extensions, try content detection
            # Limit to reasonable size to avoid performance issues
            if file_size < 100000:  # Only check content for files < 100KB
                return self._detect_text_by_content(file_path)
            
            return False
        
        except Exception as e:
            # Don't log every error - too verbose during scanning
            return False
    
    def _detect_text_by_content(self, file_path: Path) -> bool:
        """
        Detect if file is text by examining content.
        Uses a robust approach that doesn't rely on external libraries.
        """
        try:
            # Read a sample of the file
            with open(file_path, "rb") as f:
                sample = f.read(8192)
            
            if not sample:
                return False
            
            # Method 1: Check for null bytes (binary indicator)
            # Binary files typically have many null bytes
            null_count = sample.count(b'\x00')
            if null_count > len(sample) * 0.05:  # More than 5% null bytes
                return False
            
            # Method 2: Check for common binary file signatures
            # These are magic numbers at the start of files
            binary_signatures = [
                b'\x7fELF',      # ELF executables
                b'MZ',           # Windows executables
                b'\x89PNG',      # PNG images
                b'\xff\xd8\xff', # JPEG images
                b'GIF8',         # GIF images
                b'%PDF',         # PDF files
                b'PK\x03\x04',   # ZIP files
                b'PK\x05\x06',   # ZIP files
                b'PK\x07\x08',   # ZIP files
                b'\x1f\x8b',     # GZIP files
                b'BM',           # BMP images
                b'II*\x00',      # TIFF images (little-endian)
                b'MM\x00*',      # TIFF images (big-endian)
                b'RIFF',         # WAV, AVI, WebP
                b'\x00\x00\x01\xba', # MPEG
                b'\x00\x00\x01\xb3', # MPEG
                b'ftyp',         # MP4, M4A, etc (offset 4)
            ]
            
            for sig in binary_signatures:
                if sample.startswith(sig):
                    return False
                # Check at offset 4 for some formats
                if len(sample) > 4 and sample[4:].startswith(sig):
                    return False
            
            # Method 3: Check for control characters (except common whitespace)
            # Text files should mostly be printable characters
            control_chars = 0
            for byte in sample:
                # Allow: tab (9), newline (10), carriage return (13)
                if byte < 32 and byte not in (9, 10, 13):
                    control_chars += 1
            
            # If more than 10% control characters, likely binary
            if control_chars > len(sample) * 0.1:
                return False
            
            # Method 4: Try UTF-8 decode
            # Most text files are UTF-8 or ASCII
            try:
                sample.decode('utf-8')
                return True
            except UnicodeDecodeError:
                pass
            
            # Method 5: Try common encodings
            # Don't use chardet - it's slow and not always reliable
            for encoding in ['latin-1', 'cp1252', 'iso-8859-1']:
                try:
                    sample.decode(encoding)
                    # If it decodes and has reasonable control char ratio, accept it
                    return control_chars < len(sample) * 0.15
                except (UnicodeDecodeError, LookupError):
                    continue
            
            # If we got here, it's ambiguous - default to not text
            return False
            
        except Exception as e:
            # On any error, default to False
            return False
    
    async def scan_existing_files(self, progress_callback=None) -> Set[Path]:
        """
        Scan for existing text files in the root directory.
        
        Args:
            progress_callback: Optional callback(scanned_count, found_count) for progress updates
        
        Returns:
            Set of text file paths
        """
        text_files = set()
        
        logger.info(f"Scanning existing files in {self.root_path}")
        
        try:
            # Run the scanning in a thread pool to not block
            loop = asyncio.get_event_loop()
            
            # Shared state for progress reporting
            progress_data = {"scanned": 0, "found": 0}
            
            def _scan():
                """
                Git-like recursive directory walk with early termination.
                Never descends into ignored directories for massive performance gain.
                """
                import os
                files = set()
                file_hashes = {}
                scanned = 0
                
                def walk_directory(directory: Path):
                    """Recursively walk directory, skipping ignored dirs (like Git does)."""
                    nonlocal scanned
                    
                    try:
                        # Use os.scandir for efficiency (DirEntry has cached stat)
                        with os.scandir(directory) as entries:
                            for entry in entries:
                                entry_path = Path(entry.path)
                                
                                if entry.is_dir(follow_symlinks=False):
                                    scanned += 1
                                    
                                    # Git optimization: Check if dir is ignored BEFORE descending
                                    if self.ignore_matcher and self.ignore_matcher.should_ignore(entry_path):
                                        # Skip entire directory tree (never enumerate its contents)
                                        continue
                                    
                                    # Directory not ignored - recurse
                                    walk_directory(entry_path)
                                
                                elif entry.is_file(follow_symlinks=False):
                                    scanned += 1
                                    
                                    # Check if file is ignored (fast with caching)
                                    if self.ignore_matcher and self.ignore_matcher.should_ignore(entry_path):
                                        continue
                                    
                                    # Check if text file
                                    if self.is_text_file(entry_path):
                                        files.add(entry_path)
                                        
                                        # Compute file hash for Merkle tree
                                        try:
                                            with open(entry_path, "rb") as f:
                                                file_hash = hashlib.sha256(f.read()).hexdigest()
                                                file_hashes[entry_path] = file_hash
                                        except Exception as e:
                                            logger.debug(f"Error hashing {entry_path}: {e}")
                                    
                                    # Update progress every 100 items for smoother UI
                                    if scanned % 100 == 0:
                                        progress_data["scanned"] = scanned
                                        progress_data["found"] = len(files)
                    
                    except PermissionError:
                        # Skip directories we can't read
                        pass
                    except Exception as e:
                        logger.debug(f"Error scanning {directory}: {e}")
                
                # Start recursive walk
                walk_directory(self.root_path)
                
                return files, scanned, file_hashes
            
            # Run in executor with periodic progress updates
            scan_task = loop.run_in_executor(None, _scan)
            
            # Poll for completion and report progress
            last_scanned = 0
            while not scan_task.done():
                await asyncio.sleep(0.5)  # Update every 500ms
                # Report intermediate progress if callback provided
                if progress_callback and progress_data["scanned"] > last_scanned:
                    progress_callback(progress_data["scanned"], progress_data["found"])
                    last_scanned = progress_data["scanned"]
            
            text_files, total_scanned, file_hashes = await scan_task
            
            # Build new Merkle tree
            previous_tree = self._merkle_tree
            new_tree = MerkleTree(self.root_path)
            new_tree.build(text_files, file_hashes)
            
            # Compare with previous tree if available
            if previous_tree:
                added, modified, deleted = new_tree.compare(previous_tree)
                if added or modified or deleted:
                    logger.info(
                        f"Merkle tree changes: "
                        f"{len(added)} added, {len(modified)} modified, {len(deleted)} deleted directories"
                    )
                else:
                    logger.debug("No structural changes detected by Merkle tree")
            
            # Update cached tree and hashes
            self._merkle_tree = new_tree
            self._file_hashes = file_hashes
            
            # Final progress update
            if progress_callback:
                progress_callback(total_scanned, len(text_files))
        
        except Exception as e:
            logger.error(f"Error scanning files: {e}")
        
        logger.info(f"Found {len(text_files)} text files in {self.root_path} (scanned {total_scanned} items)")
        return text_files
    
    def get_changed_directories(self) -> Set[Path]:
        """
        Get directories that have changed since last scan (using Merkle tree).
        Useful for targeted re-indexing.
        
        Returns:
            Set of directory paths that have changes
        """
        if not self._merkle_tree:
            return set()
        
        # This would need a new scan to compare against
        # For now, return empty - can be enhanced later
        return set()
    
    def get_file_hash(self, file_path: Path) -> Optional[str]:
        """Get cached hash for a file, if available."""
        return self._file_hashes.get(file_path)


class _FileEventHandler(FileSystemEventHandler):
    """Watchdog event handler that delegates to FileWatcher."""
    
    def __init__(self, watcher: FileWatcher):
        self.watcher = watcher
    
    def on_any_event(self, event: FileSystemEvent):
        """Handle any file system event."""
        self.watcher._on_file_event(event)

