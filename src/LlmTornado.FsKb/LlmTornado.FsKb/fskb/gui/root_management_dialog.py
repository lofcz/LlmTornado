"""
Root Management Dialog for managing individual root directories.
"""

import asyncio
from pathlib import Path
from typing import Optional
from PyQt6.QtWidgets import (
    QDialog, QVBoxLayout, QHBoxLayout, QPushButton, QLabel,
    QTabWidget, QWidget, QGroupBox, QFormLayout, QTreeWidget,
    QTreeWidgetItem, QTextEdit, QMessageBox, QProgressBar,
    QLineEdit, QMenu, QTreeWidgetItemIterator
)
from PyQt6.QtCore import Qt, QTimer
from PyQt6.QtGui import QIcon
from loguru import logger


class RootManagementDialog(QDialog):
    """Dialog for managing a root directory's indexing and ignore patterns."""
    
    def __init__(self, parent, root_path: Path, indexing_engine, settings):
        super().__init__(parent)
        self.root_path = root_path
        self.indexing_engine = indexing_engine
        self.settings = settings
        self.root_state = self.indexing_engine.roots.get(root_path)
        
        if not self.root_state:
            QMessageBox.warning(self, "Error", f"Root not found: {root_path}")
            self.reject()
            return
        
        self.setWindowTitle(f"Manage Root - {root_path.name}")
        self.setMinimumSize(700, 500)
        self.resize(800, 600)
        
        self._setup_ui()
        
        # Update stats every 500ms
        self.stats_timer = QTimer()
        self.stats_timer.timeout.connect(self._update_stats)
        self.stats_timer.start(500)
    
    def _setup_ui(self):
        """Setup the UI layout."""
        layout = QVBoxLayout()
        
        # Tabs
        self.tabs = QTabWidget()
        self.tabs.addTab(self._create_status_tab(), "Status")
        self.tabs.addTab(self._create_ignore_tab(), "Ignore Patterns")
        layout.addWidget(self.tabs)
        
        # Close button
        button_layout = QHBoxLayout()
        button_layout.addStretch()
        close_btn = QPushButton("Close")
        close_btn.clicked.connect(self.accept)
        button_layout.addWidget(close_btn)
        layout.addLayout(button_layout)
        
        self.setLayout(layout)
    
    def _create_status_tab(self) -> QWidget:
        """Create the status tab with stats and pause/resume."""
        widget = QWidget()
        layout = QVBoxLayout()
        
        # Root info
        info_group = QGroupBox("Root Information")
        info_layout = QFormLayout()
        
        self.path_label = QLabel(str(self.root_path))
        info_layout.addRow("Path:", self.path_label)
        
        self.branch_label = QLabel(self.root_state.current_branch)
        info_layout.addRow("Branch:", self.branch_label)
        
        info_group.setLayout(info_layout)
        layout.addWidget(info_group)
        
        # Statistics
        stats_group = QGroupBox("Indexing Statistics")
        stats_layout = QFormLayout()
        
        self.scanned_label = QLabel(str(self.root_state.stats.files_scanned))
        stats_layout.addRow("Files Scanned:", self.scanned_label)
        
        self.indexed_label = QLabel(str(self.root_state.stats.files_indexed))
        stats_layout.addRow("Files Indexed:", self.indexed_label)
        
        self.chunks_label = QLabel(str(self.root_state.stats.chunks_created))
        stats_layout.addRow("Chunks Created:", self.chunks_label)
        
        self.queue_label = QLabel(str(self.indexing_engine._indexing_queue.qsize()))
        stats_layout.addRow("Queue Size:", self.queue_label)
        
        self.errors_label = QLabel(str(self.root_state.stats.errors))
        stats_layout.addRow("Errors:", self.errors_label)
        
        self.status_label = QLabel(self.root_state.stats.current_file or "Idle")
        stats_layout.addRow("Current:", self.status_label)
        
        stats_group.setLayout(stats_layout)
        layout.addWidget(stats_group)
        
        # Pause/Resume button
        button_layout = QHBoxLayout()
        self.pause_resume_btn = QPushButton("Resume" if self.root_state.paused else "Pause")
        self.pause_resume_btn.clicked.connect(self._toggle_pause)
        self.pause_resume_btn.setMinimumWidth(120)
        button_layout.addWidget(self.pause_resume_btn)
        button_layout.addStretch()
        layout.addLayout(button_layout)
        
        layout.addStretch()
        
        widget.setLayout(layout)
        return widget
    
    def _create_ignore_tab(self) -> QWidget:
        """Create the ignore patterns tab with file tree."""
        widget = QWidget()
        layout = QVBoxLayout()
        
        # Info label
        info_label = QLabel(
            "This tab shows all files and their ignore status. "
            "You can edit .fskbignore to customize which files are indexed."
        )
        info_label.setWordWrap(True)
        layout.addWidget(info_label)
        
        # Buttons
        button_layout = QHBoxLayout()
        
        edit_fskbignore_btn = QPushButton("Edit .fskbignore")
        edit_fskbignore_btn.clicked.connect(self._edit_fskbignore)
        button_layout.addWidget(edit_fskbignore_btn)
        
        view_gitignore_btn = QPushButton("View .gitignore")
        view_gitignore_btn.clicked.connect(self._view_gitignore)
        button_layout.addWidget(view_gitignore_btn)
        
        refresh_btn = QPushButton("Refresh")
        refresh_btn.clicked.connect(lambda: asyncio.create_task(self._refresh_file_tree_async()))
        button_layout.addWidget(refresh_btn)
        
        button_layout.addStretch()
        layout.addLayout(button_layout)
        
        # Search/filter box
        search_layout = QHBoxLayout()
        search_label = QLabel("Filter:")
        self.search_box = QLineEdit()
        self.search_box.setPlaceholderText("Search files/folders...")
        self.search_box.textChanged.connect(self._filter_tree)
        self.search_box.setClearButtonEnabled(True)
        search_layout.addWidget(search_label)
        search_layout.addWidget(self.search_box)
        layout.addLayout(search_layout)
        
        # Progress bar for loading
        self.file_tree_progress = QProgressBar()
        self.file_tree_progress.setTextVisible(True)
        self.file_tree_progress.setFormat("Loading: %v / %m files")
        self.file_tree_progress.hide()  # Hidden by default
        layout.addWidget(self.file_tree_progress)
        
        # Status label for loading
        self.file_tree_status = QLabel("")
        layout.addWidget(self.file_tree_status)
        
        # File tree
        self.file_tree = QTreeWidget()
        self.file_tree.setHeaderLabels(["File/Folder", "Status", "Chunks", "Reason", "Actions"])
        self.file_tree.setColumnWidth(0, 350)
        self.file_tree.setColumnWidth(1, 100)
        self.file_tree.setColumnWidth(2, 70)
        self.file_tree.setColumnWidth(3, 150)
        self.file_tree.setColumnWidth(4, 80)
        self.file_tree.itemClicked.connect(self._on_tree_item_clicked)
        self.file_tree.setContextMenuPolicy(Qt.ContextMenuPolicy.CustomContextMenu)
        self.file_tree.customContextMenuRequested.connect(self._show_context_menu)
        
        # Track which items have buttons already
        self._items_with_buttons = set()
        
        # Connect to viewport events for lazy button creation
        self.file_tree.viewport().installEventFilter(self)
        self.file_tree.itemExpanded.connect(lambda: QTimer.singleShot(0, self._create_visible_buttons))
        self.file_tree.verticalScrollBar().valueChanged.connect(lambda: QTimer.singleShot(0, self._create_visible_buttons))
        
        layout.addWidget(self.file_tree)
        
        # Load file tree asynchronously (don't block!)
        # Use a timer to trigger async load after dialog is shown
        QTimer.singleShot(100, lambda: asyncio.create_task(self._refresh_file_tree_async()))
        
        widget.setLayout(layout)
        return widget
    
    def _update_stats(self):
        """Update statistics display."""
        if not self.root_state:
            return
        
        try:
            self.scanned_label.setText(str(self.root_state.stats.files_scanned))
            self.indexed_label.setText(str(self.root_state.stats.files_indexed))
            self.chunks_label.setText(str(self.root_state.stats.chunks_created))
            self.queue_label.setText(str(self.indexing_engine._indexing_queue.qsize()))
            self.errors_label.setText(str(self.root_state.stats.errors))
            self.status_label.setText(self.root_state.stats.current_file or "Idle")
            
            # Update pause/resume button
            self.pause_resume_btn.setText("Resume" if self.root_state.paused else "Pause")
            
        except Exception as e:
            logger.error(f"Error updating stats: {e}")
    
    def _toggle_pause(self):
        """Toggle pause/resume for this root."""
        is_paused = self.indexing_engine.is_root_paused(self.root_path)
        
        if is_paused:
            success = self.indexing_engine.resume_root(self.root_path)
            if success:
                self.pause_resume_btn.setText("Pause")
                logger.info(f"Resumed indexing for {self.root_path}")
        else:
            success = self.indexing_engine.pause_root(self.root_path)
            if success:
                self.pause_resume_btn.setText("Resume")
                logger.info(f"Paused indexing for {self.root_path}")
    
    async def _refresh_file_tree_async(self):
        """Refresh the file tree showing indexed and ignored files (async, non-blocking, progressive)."""
        # Clear tree, button tracking, and show progress
        self.file_tree.clear()
        self._items_with_buttons.clear()
        self.file_tree_progress.show()
        self.file_tree_progress.setMaximum(0)  # Indeterminate initially
        self.file_tree_status.setText("Loading chunk counts...")
        
        try:
            # Get chunk counts per file (single efficient query)
            branch_name = self.root_state.git_tracker.get_current_branch() if self.root_state.git_tracker else "main"
            file_chunk_counts = await self.indexing_engine.chroma_store.get_file_chunk_counts(
                self.root_path, 
                branch_name
            )
            logger.debug(f"Loaded chunk counts for {len(file_chunk_counts)} files")
            
            # Create root item immediately
            self.file_tree_status.setText("Scanning files...")
            root_item = QTreeWidgetItem(self.file_tree, [str(self.root_path.name), "", "", ""])
            root_item.setExpanded(True)
            
            # Group by directory
            dir_items = {self.root_path: root_item}
            
            # Scan and process files progressively (stream updates as files are found)
            files_processed = 0
            batch_size = 50  # Process in batches
            
            # Use a queue for streaming results from scanner to UI
            import queue
            import threading
            file_queue = queue.Queue(maxsize=10)  # Buffer up to 10 batches
            
            def scan_files_to_queue():
                """Scan files and push batches to queue (runs in thread pool)."""
                try:
                    current_batch = []
                    
                    # OPTIMIZATION: Reuse already-indexed files (MUCH faster than rglob)
                    # These are already known to be text files
                    for rel_path_str in self.root_state.indexed_files.keys():
                        file_path = self.root_path / rel_path_str
                        if file_path.exists():
                            current_batch.append(file_path)
                            if len(current_batch) >= batch_size:
                                file_queue.put(current_batch)
                                current_batch = []
                    
                    # Also add any files currently in the indexing queue (pending files)
                    # These are text files that haven't been indexed yet
                    # (We can't easily access the queue, so we'll do a quick scan for unindexed files)
                    
                    # Push remaining files
                    if current_batch:
                        file_queue.put(current_batch)
                finally:
                    # Signal completion
                    file_queue.put(None)
            
            # Start scanning in background thread
            scan_thread = threading.Thread(target=scan_files_to_queue, daemon=True)
            scan_thread.start()
            
            # Update status - now scanning
            self.file_tree_status.setText("Scanning files...")
            
            # Process batches as they arrive (streaming!)
            loop = asyncio.get_event_loop()
            while True:
                # Check queue without blocking too long
                try:
                    batch_files = await loop.run_in_executor(None, lambda: file_queue.get(timeout=0.1))
                except queue.Empty:
                    await asyncio.sleep(0.05)  # Brief pause before next check
                    continue  # No batch yet, keep waiting
                
                # None signals end of scanning
                if batch_files is None:
                    break
                
                # Process this batch
                for file_path in batch_files:
                    # Get relative path
                    rel_path = file_path.relative_to(self.root_path)
                    rel_path_str = str(rel_path).replace('\\', '/')  # Normalize to forward slashes
                    
                    # Check if file is indexed
                    is_indexed = rel_path_str in self.root_state.indexed_files
                    
                    # Check if file is ignored
                    is_ignored = self.root_state.ignore_matcher.should_ignore(file_path)
                    
                    # Get chunk count for this file
                    chunk_count = file_chunk_counts.get(rel_path_str, 0)
                    chunk_str = str(chunk_count) if chunk_count > 0 else ""
                    
                    # Determine status and reason
                    if is_indexed:
                        status = "✓ Indexed"
                        reason = ""
                    elif is_ignored:
                        status = "✗ Ignored"
                        reason = "Ignored by pattern"  # Simplified - don't read files
                    else:
                        status = "Pending"
                        reason = "Not yet indexed"
                    
                    # Create or get parent directory item
                    parent_dir = file_path.parent
                    if parent_dir not in dir_items:
                        # Create directory items recursively
                        parts = list(parent_dir.relative_to(self.root_path).parts)
                        current_path = self.root_path
                        current_item = root_item
                        
                        for part in parts:
                            current_path = current_path / part
                            if current_path not in dir_items:
                                dir_item = QTreeWidgetItem(current_item, [part, "", "", ""])
                                dir_items[current_path] = dir_item
                                current_item = dir_item
                            else:
                                current_item = dir_items[current_path]
                    
                    # Add file item
                    parent_item = dir_items.get(parent_dir, root_item)
                    file_item = QTreeWidgetItem(parent_item, [file_path.name, status, chunk_str, reason, ""])
                    
                    # Store file path in item data for later retrieval
                    file_item.setData(0, Qt.ItemDataRole.UserRole, str(file_path))
                    file_item.setData(0, Qt.ItemDataRole.UserRole + 1, is_indexed)  # Store indexed status
                    
                    # Color code by status
                    if is_indexed:
                        file_item.setForeground(1, Qt.GlobalColor.green)
                        if chunk_count > 0:
                            file_item.setForeground(2, Qt.GlobalColor.green)
                    elif is_ignored:
                        file_item.setForeground(1, Qt.GlobalColor.red)
                    else:
                        file_item.setForeground(1, Qt.GlobalColor.yellow)
                    
                    # Don't create QPushButton for each file (too expensive with thousands of files)
                    # Users can right-click or double-click to reindex
                    
                    files_processed += 1
                
                # Update progress after each batch
                self.file_tree_status.setText(f"Loaded {files_processed} files...")
                
                # Yield to event loop
                await asyncio.sleep(0)
            
            # Done!
            self.file_tree_progress.hide()
            self.file_tree_status.setText(f"✓ Loaded {files_processed} files")
            
            # Create buttons for initially visible items
            QTimer.singleShot(0, self._create_visible_buttons)
            
            # Auto-hide status after 3 seconds
            QTimer.singleShot(3000, lambda: self.file_tree_status.setText(""))
            
        except Exception as e:
            logger.error(f"Error refreshing file tree: {e}")
            self.file_tree.clear()
            error_item = QTreeWidgetItem(self.file_tree, [f"Error: {str(e)}", "", "", ""])
            self.file_tree_progress.hide()
            self.file_tree_status.setText(f"❌ Error: {str(e)}")
    
    def _get_ignore_reason(self, file_path: Path) -> str:
        """Try to determine why a file is ignored."""
        # Check if it matches a pattern from .gitignore or .fskbignore
        # This is a simplified check - the actual ignore matcher handles the complexity
        try:
            # Check .fskbignore
            fskbignore = self.root_path / ".fskbignore"
            if fskbignore.exists():
                with open(fskbignore, 'r', encoding='utf-8') as f:
                    for line in f:
                        line = line.strip()
                        if line and not line.startswith('#'):
                            if file_path.match(line):
                                return f".fskbignore: {line}"
            
            # Check .gitignore
            gitignore = self.root_path / ".gitignore"
            if gitignore.exists():
                with open(gitignore, 'r', encoding='utf-8') as f:
                    for line in f:
                        line = line.strip()
                        if line and not line.startswith('#'):
                            if file_path.match(line):
                                return f".gitignore: {line}"
            
            return "Ignored by pattern"
        except Exception as e:
            return f"Error: {e}"
    
    def _edit_fskbignore(self):
        """Open .fskbignore for editing."""
        fskbignore_path = self.root_path / ".fskbignore"
        
        # Create if it doesn't exist
        if not fskbignore_path.exists():
            try:
                fskbignore_path.write_text("# Add patterns to ignore (one per line)\n", encoding='utf-8')
            except Exception as e:
                QMessageBox.warning(self, "Error", f"Failed to create .fskbignore: {e}")
                return
        
        # Open editor dialog
        self._open_file_editor(fskbignore_path, "Edit .fskbignore")
    
    def _view_gitignore(self):
        """View .gitignore (read-only)."""
        gitignore_path = self.root_path / ".gitignore"
        
        if not gitignore_path.exists():
            QMessageBox.information(self, "Not Found", ".gitignore does not exist in this root.")
            return
        
        # Open viewer dialog (read-only)
        self._open_file_editor(gitignore_path, "View .gitignore", read_only=True)
    
    def _open_file_editor(self, file_path: Path, title: str, read_only: bool = False):
        """Open a simple text editor dialog for a file."""
        dialog = QDialog(self)
        dialog.setWindowTitle(title)
        dialog.setMinimumSize(600, 400)
        dialog.resize(700, 500)
        
        layout = QVBoxLayout()
        
        # Text editor
        text_edit = QTextEdit()
        text_edit.setReadOnly(read_only)
        
        try:
            content = file_path.read_text(encoding='utf-8')
            text_edit.setPlainText(content)
        except Exception as e:
            text_edit.setPlainText(f"Error reading file: {e}")
            text_edit.setReadOnly(True)
        
        layout.addWidget(text_edit)
        
        # Buttons
        button_layout = QHBoxLayout()
        
        if not read_only:
            save_btn = QPushButton("Save")
            save_btn.clicked.connect(lambda: self._save_file(file_path, text_edit.toPlainText(), dialog))
            button_layout.addWidget(save_btn)
        
        close_btn = QPushButton("Close")
        close_btn.clicked.connect(dialog.accept)
        button_layout.addWidget(close_btn)
        
        button_layout.addStretch()
        layout.addLayout(button_layout)
        
        dialog.setLayout(layout)
        dialog.exec()
    
    def _save_file(self, file_path: Path, content: str, dialog: QDialog):
        """Save content to a file."""
        try:
            file_path.write_text(content, encoding='utf-8')
            QMessageBox.information(self, "Success", f"File saved: {file_path.name}")
            dialog.accept()
            
            # Refresh file tree after saving
            asyncio.create_task(self._refresh_file_tree_async())
        except Exception as e:
            QMessageBox.warning(self, "Error", f"Failed to save file: {e}")
    
    def _on_tree_item_clicked(self, item: QTreeWidgetItem, column: int):
        """Handle tree item click - show chunks for indexed files."""
        # Get file path from item data
        file_path_str = item.data(0, Qt.ItemDataRole.UserRole)
        is_indexed = item.data(0, Qt.ItemDataRole.UserRole + 1)
        
        if not file_path_str or not is_indexed:
            return  # Only show chunks for indexed files
        
        file_path = Path(file_path_str)
        
        # Show chunks for this file
        asyncio.create_task(self._show_file_chunks(file_path))
    
    async def _show_file_chunks(self, file_path: Path):
        """Show all chunks for a file, ordered by line numbers."""
        try:
            # Get relative path
            rel_path = file_path.relative_to(self.root_path)
            rel_path_str = str(rel_path).replace('\\', '/')
            
            # Query ChromaDB for all chunks of this file
            collection = await self.indexing_engine.chroma_store.get_or_create_collection(self.root_path)
            
            # Get chunks from ChromaDB (check both path formats for old data)
            loop = asyncio.get_event_loop()
            rel_path_backslash = str(rel_path).replace('/', '\\')
            results = await loop.run_in_executor(
                None,
                lambda: collection.get(
                    where={
                        "$and": [
                            {"branch": self.root_state.current_branch},
                            {
                                "$or": [
                                    {"file_path": rel_path_str},  # New format
                                    {"file_path": rel_path_backslash}  # Old format
                                ]
                            }
                        ]
                    },
                    include=["metadatas", "documents"]
                )
            )
            
            if not results or not results['ids']:
                QMessageBox.information(self, "No Chunks", f"No chunks found for {file_path.name}")
                return
            
            # Extract chunks and sort by line_start
            chunks = []
            for i in range(len(results['ids'])):
                metadata = results['metadatas'][i]
                content = results['documents'][i]
                # FIXED: Use correct metadata keys (line_start, not start_line)
                line_start = metadata.get('line_start', 0)
                line_end = metadata.get('line_end', 0)
                chunks.append({
                    'start_line': line_start,
                    'end_line': line_end,
                    'content': content,
                    'content_hash': metadata.get('content_hash', '')  # Full hash for debugging
                })
            
            # Sort by start_line
            chunks.sort(key=lambda x: x['start_line'])
            
            # Create dialog to show chunks
            dialog = QDialog(self)
            dialog.setWindowTitle(f"Chunks: {file_path.name}")
            dialog.setMinimumSize(800, 600)
            
            layout = QVBoxLayout()
            
            # Info label
            info_label = QLabel(f"File: {rel_path_str}\nTotal chunks: {len(chunks)}")
            info_label.setWordWrap(True)
            layout.addWidget(info_label)
            
            # Text browser to show chunks
            text_browser = QTextEdit()
            text_browser.setReadOnly(True)
            text_browser.setStyleSheet("""
                QTextEdit {
                    background-color: #1e1e1e;
                    color: #d4d4d4;
                    font-family: 'Consolas', 'Monaco', monospace;
                    font-size: 10pt;
                }
            """)
            
            # Build HTML with all chunks (bright yellow dividers, gray text)
            html_parts = ['<pre style="color: #b0b0b0; margin: 0;">']  # Grayer text
            for i, chunk in enumerate(chunks, 1):
                # Bright yellow divider
                start = chunk['start_line']
                end = chunk['end_line']
                # Fix: If start/end are 0, try to infer from content
                if start == 0 and end == 0:
                    line_count = chunk['content'].count('\n') + 1
                    start = 1
                    end = line_count
                
                divider = f"=== Chunk {i}/{len(chunks)} === Lines {start}-{end} === Hash: {chunk['content_hash']} ==="
                html_parts.append(f'<span style="color: #ffff00; font-weight: bold;">{divider}</span>\n')
                # Escape HTML entities in content
                import html as html_lib
                escaped_content = html_lib.escape(chunk['content'])
                html_parts.append(escaped_content)
                html_parts.append("\n\n")
            
            html_parts.append('</pre>')
            text_browser.setHtml(''.join(html_parts))
            layout.addWidget(text_browser)
            
            # Close button
            close_btn = QPushButton("Close")
            close_btn.clicked.connect(dialog.close)
            layout.addWidget(close_btn)
            
            dialog.setLayout(layout)
            
            # Keep reference to prevent garbage collection
            self._chunk_viewer_dialog = dialog
            
            # Show non-blocking to avoid event loop issues
            dialog.show()
            dialog.raise_()
            dialog.activateWindow()
            
        except Exception as e:
            logger.error(f"Error showing file chunks: {e}")
            QMessageBox.warning(self, "Error", f"Failed to load chunks: {e}")
    
    def _reindex_file(self, file_path_str: str):
        """Queue a file for reindexing."""
        try:
            file_path = Path(file_path_str)
            
            # Confirm action
            reply = QMessageBox.question(
                self,
                "Reindex File",
                f"Reindex {file_path.name}?\n\nThis will delete existing chunks and re-create them.",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
                QMessageBox.StandardButton.No
            )
            
            if reply != QMessageBox.StandardButton.Yes:
                return
            
            # Queue file for reindexing (priority 0 = highest, modified triggers re-indexing)
            rel_path = file_path.relative_to(self.root_path)
            priority_item = (0, self.indexing_engine._queue_counter, (self.root_path, rel_path, "modified"))
            self.indexing_engine._queue_counter += 1
            
            # Put in queue asynchronously
            asyncio.create_task(self.indexing_engine._indexing_queue.put(priority_item))
            
            logger.info(f"Queued {rel_path} for reindexing with priority 0")
            QMessageBox.information(self, "Queued", f"{file_path.name} queued for reindexing")
            
        except Exception as e:
            logger.error(f"Error reindexing file: {e}")
            QMessageBox.warning(self, "Error", f"Failed to queue file: {e}")
    
    def eventFilter(self, obj, event):
        """Filter events to detect when viewport is painted."""
        if obj == self.file_tree.viewport():
            if event.type() in (event.Type.Paint, event.Type.Resize):
                # Create buttons for visible items after paint
                QTimer.singleShot(0, self._create_visible_buttons)
        return super().eventFilter(obj, event)
    
    def _create_visible_buttons(self):
        """Create buttons only for visible items in the tree (lazy loading)."""
        try:
            viewport_rect = self.file_tree.viewport().rect()
            
            # Iterate through all items
            iterator = QTreeWidgetItemIterator(self.file_tree)
            while iterator.value():
                item = iterator.value()
                
                # Check if item is visible
                item_rect = self.file_tree.visualItemRect(item)
                if item_rect.isValid() and viewport_rect.intersects(item_rect):
                    # Get file path to check if this is a file (not directory)
                    file_path_str = item.data(0, Qt.ItemDataRole.UserRole)
                    if file_path_str and id(item) not in self._items_with_buttons:
                        # Create button for this item
                        reindex_btn = QPushButton("↻")
                        reindex_btn.setToolTip("Reindex this file")
                        reindex_btn.setMaximumWidth(30)
                        reindex_btn.clicked.connect(lambda checked, fp=file_path_str: self._reindex_file(fp))
                        self.file_tree.setItemWidget(item, 4, reindex_btn)
                        
                        # Mark as having button
                        self._items_with_buttons.add(id(item))
                
                iterator += 1
        except Exception as e:
            logger.debug(f"Error creating visible buttons: {e}")
    
    def _show_context_menu(self, position):
        """Show context menu for file tree items."""
        item = self.file_tree.itemAt(position)
        if not item:
            return
        
        # Get file path from item data
        file_path_str = item.data(0, Qt.ItemDataRole.UserRole)
        if not file_path_str:
            return  # Directory item, no actions
        
        is_indexed = item.data(0, Qt.ItemDataRole.UserRole + 1)
        
        menu = QMenu(self)
        
        if is_indexed:
            # Show chunks action
            show_chunks_action = menu.addAction("Show Chunks")
            show_chunks_action.triggered.connect(lambda: asyncio.create_task(self._show_file_chunks(Path(file_path_str))))
            
            menu.addSeparator()
        
        # Reindex action
        reindex_action = menu.addAction("Reindex File")
        reindex_action.triggered.connect(lambda: self._reindex_file(file_path_str))
        
        menu.exec(self.file_tree.viewport().mapToGlobal(position))
    
    def _filter_tree(self, search_text: str):
        """Filter tree items based on search text."""
        if not search_text:
            # Show all items
            iterator = QTreeWidgetItemIterator(self.file_tree)
            while iterator.value():
                item = iterator.value()
                item.setHidden(False)
                iterator += 1
            return
        
        search_lower = search_text.lower()
        
        def should_show_item(item: QTreeWidgetItem) -> bool:
            """Recursively determine if item or any child matches search."""
            # Check if this item's text matches
            item_text = item.text(0).lower()
            if search_lower in item_text:
                return True
            
            # Check children
            for i in range(item.childCount()):
                child = item.child(i)
                if should_show_item(child):
                    return True
            
            return False
        
        # Hide/show items based on search
        iterator = QTreeWidgetItemIterator(self.file_tree)
        while iterator.value():
            item = iterator.value()
            should_show = should_show_item(item)
            item.setHidden(not should_show)
            
            # Expand item if it has matching children
            if should_show and item.childCount() > 0:
                item.setExpanded(True)
            
            iterator += 1
    
    def closeEvent(self, event):
        """Handle dialog close."""
        self.stats_timer.stop()
        event.accept()

