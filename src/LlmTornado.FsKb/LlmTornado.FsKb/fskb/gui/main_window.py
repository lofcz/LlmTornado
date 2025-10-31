"""
Main GUI window for FSKB using PyQt6.
"""

import sys
import asyncio
from pathlib import Path
from typing import Optional
from PyQt6.QtWidgets import (
    QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QPushButton,
    QLabel, QLineEdit, QTextEdit, QTableWidget, QTableWidgetItem,
    QFileDialog, QMenuBar, QMenu, QSystemTrayIcon, QMessageBox,
    QProgressBar, QGroupBox, QTabWidget, QDialog, QFormLayout,
    QSpinBox, QDoubleSpinBox, QComboBox, QCheckBox
)
from PyQt6.QtCore import Qt, QTimer, pyqtSignal, QObject
from PyQt6.QtGui import QIcon, QAction
from loguru import logger

from ..config import Settings
from ..indexing import IndexingEngine
from ..search import QueryEngine
from ..utils import ResourceManager


class SignalEmitter(QObject):
    """Helper class for thread-safe signal emission."""
    stats_updated = pyqtSignal(dict)
    search_completed = pyqtSignal(list)
    error_occurred = pyqtSignal(str)


class SettingsDialog(QDialog):
    """Settings dialog for configuration."""
    
    def __init__(self, settings: Settings, parent=None):
        super().__init__(parent)
        self.settings = settings
        self.setWindowTitle("FSKB Settings")
        self.setModal(True)
        self.resize(500, 400)
        
        self._setup_ui()
    
    def _setup_ui(self):
        """Setup UI components."""
        layout = QVBoxLayout()
        
        # Embedding settings
        embedding_group = QGroupBox("Embedding Provider")
        embedding_layout = QFormLayout()
        
        self.provider_combo = QComboBox()
        self.provider_combo.addItems(["local", "openai", "voyage", "cohere", "google"])
        self.provider_combo.setCurrentText(self.settings.embedding.provider)
        embedding_layout.addRow("Provider:", self.provider_combo)
        
        self.model_input = QLineEdit(self.settings.embedding.model)
        embedding_layout.addRow("Model:", self.model_input)
        
        self.api_key_input = QLineEdit(self.settings.embedding.api_key or "")
        self.api_key_input.setEchoMode(QLineEdit.EchoMode.Password)
        embedding_layout.addRow("API Key:", self.api_key_input)
        
        embedding_group.setLayout(embedding_layout)
        layout.addWidget(embedding_group)
        
        # Resource settings
        resource_group = QGroupBox("Resource Limits")
        resource_layout = QFormLayout()
        
        self.max_cpu_spin = QDoubleSpinBox()
        self.max_cpu_spin.setRange(1.0, 100.0)
        self.max_cpu_spin.setValue(self.settings.resource.max_cpu_percent)
        self.max_cpu_spin.setSuffix(" %")
        resource_layout.addRow("Max CPU:", self.max_cpu_spin)
        
        self.max_memory_spin = QSpinBox()
        self.max_memory_spin.setRange(256, 16384)
        self.max_memory_spin.setValue(self.settings.resource.max_memory_mb)
        self.max_memory_spin.setSuffix(" MB")
        resource_layout.addRow("Max Memory:", self.max_memory_spin)
        
        resource_group.setLayout(resource_layout)
        layout.addWidget(resource_group)
        
        # Chunking settings
        chunk_group = QGroupBox("Text Chunking")
        chunk_layout = QFormLayout()
        
        self.chunk_size_spin = QSpinBox()
        self.chunk_size_spin.setRange(100, 2000)
        self.chunk_size_spin.setValue(self.settings.chunking.chunk_size)
        chunk_layout.addRow("Chunk Size:", self.chunk_size_spin)
        
        self.chunk_overlap_spin = QSpinBox()
        self.chunk_overlap_spin.setRange(0, 500)
        self.chunk_overlap_spin.setValue(self.settings.chunking.chunk_overlap)
        chunk_layout.addRow("Chunk Overlap:", self.chunk_overlap_spin)
        
        chunk_group.setLayout(chunk_layout)
        layout.addWidget(chunk_group)
        
        # Buttons
        button_layout = QHBoxLayout()
        save_btn = QPushButton("Save")
        save_btn.clicked.connect(self.accept)
        cancel_btn = QPushButton("Cancel")
        cancel_btn.clicked.connect(self.reject)
        button_layout.addStretch()
        button_layout.addWidget(save_btn)
        button_layout.addWidget(cancel_btn)
        layout.addLayout(button_layout)
        
        self.setLayout(layout)
    
    def get_settings(self) -> Settings:
        """Get updated settings."""
        self.settings.embedding.provider = self.provider_combo.currentText()
        self.settings.embedding.model = self.model_input.text()
        self.settings.embedding.api_key = self.api_key_input.text() or None
        self.settings.resource.max_cpu_percent = self.max_cpu_spin.value()
        self.settings.resource.max_memory_mb = self.max_memory_spin.value()
        self.settings.chunking.chunk_size = self.chunk_size_spin.value()
        self.settings.chunking.chunk_overlap = self.chunk_overlap_spin.value()
        return self.settings


class MainWindow(QMainWindow):
    """Main window for FSKB application."""
    
    def __init__(
        self,
        settings: Settings,
        indexing_engine: IndexingEngine,
        query_engine: QueryEngine,
        resource_manager: ResourceManager,
    ):
        super().__init__()
        
        self.settings = settings
        self.indexing_engine = indexing_engine
        self.query_engine = query_engine
        self.resource_manager = resource_manager
        
        # Signal emitter for async updates
        self.signals = SignalEmitter()
        self.signals.stats_updated.connect(self._on_stats_updated)
        self.signals.search_completed.connect(self._on_search_completed)
        self.signals.error_occurred.connect(self._on_error)
        
        # Cache indexing status for instant closeEvent checks
        self._cached_queue_size = 0
        self._cached_has_indexing = False
        
        # UI interaction tracking for responsiveness
        self._ui_interaction_timer = QTimer()
        self._ui_interaction_timer.setSingleShot(True)
        self._ui_interaction_timer.timeout.connect(self._on_ui_idle)
        self._ui_active = False
        
        # Setup UI
        self.setWindowTitle("FSKB - File System Knowledge Base")
        self.resize(1000, 700)
        
        self._setup_ui()
        self._setup_menu()
        self._setup_system_tray()
        
        # Track previous stats for diffing (initialize BEFORE any UI updates!)
        self._last_stats_snapshot = {}
        self._system_is_idle = False
        
        # Do an immediate update to show any existing roots
        self._update_ui_stats()
        
        self.stats_timer = QTimer()
        self.stats_timer.timeout.connect(self._update_stats)
        self.stats_timer.start(500)  # Initial fast rate
        
        # Process events timer to keep UI responsive
        self.event_timer = QTimer()
        self.event_timer.timeout.connect(lambda: None)  # Just process events
        self.event_timer.start(50)  # Every 50ms for better responsiveness
        
        # Install event filter to detect all UI interactions
        self.installEventFilter(self)
        
        logger.info("GUI initialized")
    
    def _setup_ui(self):
        """Setup the main UI."""
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        
        layout = QVBoxLayout()
        
        # Tabs
        tabs = QTabWidget()
        
        # Roots tab
        roots_tab = self._create_roots_tab()
        tabs.addTab(roots_tab, "Indexed Roots")
        
        # Search tab
        search_tab = self._create_search_tab()
        tabs.addTab(search_tab, "Search")
        
        # Stats tab
        stats_tab = self._create_stats_tab()
        tabs.addTab(stats_tab, "Statistics")
        
        layout.addWidget(tabs)
        
        # Status bar
        self.statusBar().showMessage("Ready")
        
        central_widget.setLayout(layout)
    
    def _create_roots_tab(self) -> QWidget:
        """Create the roots management tab."""
        widget = QWidget()
        layout = QVBoxLayout()
        
        # Buttons
        button_layout = QHBoxLayout()
        add_btn = QPushButton("Add Root")
        add_btn.clicked.connect(self._add_root)
        remove_btn = QPushButton("Remove Root")
        remove_btn.clicked.connect(self._remove_root)
        button_layout.addWidget(add_btn)
        button_layout.addWidget(remove_btn)
        button_layout.addStretch()
        layout.addLayout(button_layout)
        
        # Roots table
        self.roots_table = QTableWidget()
        self.roots_table.setColumnCount(9)
        self.roots_table.setHorizontalHeaderLabels([
            "Root Path", "Branch", "Scanned", "Indexed", "Chunks", "Queued", "Status", "ETA", "Actions"
        ])
        self.roots_table.setEditTriggers(QTableWidget.EditTrigger.NoEditTriggers)  # Make non-editable
        
        # Set column widths and text alignment - optimized to prevent horizontal scrollbar
        header = self.roots_table.horizontalHeader()
        header.setStretchLastSection(False)  # Don't stretch last column
        header.setSectionResizeMode(0, header.ResizeMode.Stretch)  # Root path stretches
        header.setSectionResizeMode(1, header.ResizeMode.ResizeToContents)  # Branch
        header.setSectionResizeMode(2, header.ResizeMode.ResizeToContents)  # Scanned
        header.setSectionResizeMode(3, header.ResizeMode.ResizeToContents)  # Indexed
        header.setSectionResizeMode(4, header.ResizeMode.ResizeToContents)  # Chunks
        header.setSectionResizeMode(5, header.ResizeMode.ResizeToContents)  # Queued
        header.setSectionResizeMode(6, header.ResizeMode.Interactive)  # Status - user can resize
        header.setSectionResizeMode(7, header.ResizeMode.ResizeToContents)  # ETA
        header.setSectionResizeMode(8, header.ResizeMode.ResizeToContents)  # Actions
        
        # Set initial column widths to fit without horizontal scroll
        self.roots_table.setColumnWidth(1, 80)   # Branch
        self.roots_table.setColumnWidth(2, 70)   # Scanned
        self.roots_table.setColumnWidth(3, 70)   # Indexed
        self.roots_table.setColumnWidth(4, 70)   # Chunks
        self.roots_table.setColumnWidth(5, 70)   # Queued
        self.roots_table.setColumnWidth(6, 200)  # Status
        self.roots_table.setColumnWidth(7, 70)   # ETA
        self.roots_table.setColumnWidth(8, 80)   # Actions (Manage button)
        
        # Apply dark theme styling to make text legible
        self.roots_table.setStyleSheet("""
            QTableWidget {
                background-color: #1e1e1e;
                color: #d4d4d4;
                gridline-color: #3e3e3e;
                border: 1px solid #3e3e3e;
            }
            QTableWidget::item {
                background-color: #1e1e1e;
                color: #d4d4d4;
                padding: 5px;
                text-align: left;
            }
            QTableWidget::item:selected {
                background-color: #094771;
                color: #ffffff;
            }
            QTableWidget::item:hover {
                background-color: #2a2a2a;
            }
            QHeaderView::section {
                background-color: #252526;
                color: #cccccc;
                padding: 5px;
                border: 1px solid #3e3e3e;
                font-weight: bold;
            }
        """)
        
        # Disable text elision (ellipsis) for specific columns to show full text
        self.roots_table.setTextElideMode(Qt.TextElideMode.ElideNone)
        
        layout.addWidget(self.roots_table)
        
        widget.setLayout(layout)
        return widget
    
    def _create_search_tab(self) -> QWidget:
        """Create the search tab."""
        widget = QWidget()
        layout = QVBoxLayout()
        
        # Query input
        query_layout = QHBoxLayout()
        query_layout.addWidget(QLabel("Query:"))
        self.query_input = QLineEdit()
        self.query_input.setPlaceholderText("Enter search query...")
        self.query_input.returnPressed.connect(self._perform_search)
        # Track typing for UI responsiveness
        self.query_input.textChanged.connect(self._on_ui_interaction)
        self.query_input.installEventFilter(self)
        query_layout.addWidget(self.query_input)
        
        search_btn = QPushButton("Search")
        search_btn.clicked.connect(self._perform_search)
        query_layout.addWidget(search_btn)
        
        layout.addLayout(query_layout)
        
        # Root selection and search settings on same row
        root_settings_layout = QHBoxLayout()
        root_settings_layout.addWidget(QLabel("Root:"))
        self.root_combo = QComboBox()
        self.root_combo.setMinimumWidth(200)
        root_settings_layout.addWidget(self.root_combo)
        
        root_settings_layout.addStretch()  # Push settings to the right
        
        root_settings_layout.addWidget(QLabel("Max Results:"))
        self.top_k_spin = QSpinBox()
        self.top_k_spin.setMinimum(1)
        self.top_k_spin.setMaximum(100)
        self.top_k_spin.setValue(self.settings.search.top_k)
        self.top_k_spin.setMinimumWidth(60)
        self.top_k_spin.valueChanged.connect(self._on_search_settings_changed)
        root_settings_layout.addWidget(self.top_k_spin)
        
        root_settings_layout.addWidget(QLabel("Min Similarity:"))
        self.min_similarity_spin = QDoubleSpinBox()
        self.min_similarity_spin.setMinimum(0.0)
        self.min_similarity_spin.setMaximum(1.0)
        self.min_similarity_spin.setSingleStep(0.05)
        self.min_similarity_spin.setValue(self.settings.search.min_similarity)
        self.min_similarity_spin.setMinimumWidth(60)
        self.min_similarity_spin.valueChanged.connect(self._on_search_settings_changed)
        root_settings_layout.addWidget(self.min_similarity_spin)
        
        layout.addLayout(root_settings_layout)
        
        # Results table
        self.results_table = QTableWidget()
        self.results_table.setColumnCount(4)
        self.results_table.setHorizontalHeaderLabels([
            "File", "Lines", "Score", "Preview"
        ])
        self.results_table.setEditTriggers(QTableWidget.EditTrigger.NoEditTriggers)  # Make non-editable
        self.results_table.itemDoubleClicked.connect(self._on_result_double_clicked)
        self.results_table.setWordWrap(True)  # Enable word wrap for preview
        self.results_table.verticalHeader().setDefaultSectionSize(60)  # Taller rows for preview
        
        # Set column resize modes - will be intelligently sized after first search
        results_header = self.results_table.horizontalHeader()
        results_header.setSectionResizeMode(0, results_header.ResizeMode.Interactive)  # File - user can resize
        results_header.setSectionResizeMode(1, results_header.ResizeMode.Interactive)  # Lines - auto-sized, then user can resize
        results_header.setSectionResizeMode(2, results_header.ResizeMode.Interactive)  # Score - auto-sized, then user can resize
        results_header.setSectionResizeMode(3, results_header.ResizeMode.Interactive)  # Preview - auto-sized, then user can resize
        
        # Apply dark theme styling to make text legible
        self.results_table.setStyleSheet("""
            QTableWidget {
                background-color: #1e1e1e;
                color: #d4d4d4;
                gridline-color: #3e3e3e;
                border: 1px solid #3e3e3e;
            }
            QTableWidget::item {
                background-color: #1e1e1e;
                color: #d4d4d4;
                padding: 5px;
                text-align: left;
            }
            QTableWidget::item:selected {
                background-color: #094771;
                color: #ffffff;
            }
            QTableWidget::item:hover {
                background-color: #2a2a2a;
            }
            QHeaderView::section {
                background-color: #252526;
                color: #cccccc;
                padding: 5px;
                border: 1px solid #3e3e3e;
                font-weight: bold;
            }
        """)
        
        # Disable text elision (ellipsis) to show full text
        self.results_table.setTextElideMode(Qt.TextElideMode.ElideNone)
        
        layout.addWidget(self.results_table)
        
        widget.setLayout(layout)
        return widget
    
    def _create_stats_tab(self) -> QWidget:
        """Create the statistics tab."""
        widget = QWidget()
        layout = QVBoxLayout()
        
        # Resource usage
        resource_group = QGroupBox("Resource Usage")
        resource_layout = QVBoxLayout()
        
        cpu_layout = QHBoxLayout()
        cpu_layout.addWidget(QLabel("CPU:"))
        self.cpu_progress = QProgressBar()
        self.cpu_label = QLabel("0%")
        cpu_layout.addWidget(self.cpu_progress)
        cpu_layout.addWidget(self.cpu_label)
        resource_layout.addLayout(cpu_layout)
        
        mem_layout = QHBoxLayout()
        mem_layout.addWidget(QLabel("Memory:"))
        self.mem_progress = QProgressBar()
        self.mem_label = QLabel("0 MB")
        mem_layout.addWidget(self.mem_progress)
        mem_layout.addWidget(self.mem_label)
        resource_layout.addLayout(mem_layout)
        
        resource_group.setLayout(resource_layout)
        layout.addWidget(resource_group)
        
        # Indexing stats
        stats_group = QGroupBox("Indexing Statistics")
        stats_layout = QFormLayout()
        
        self.files_scanned_label = QLabel("0")
        stats_layout.addRow("Files Scanned:", self.files_scanned_label)
        
        self.files_indexed_label = QLabel("0")
        stats_layout.addRow("Files Indexed:", self.files_indexed_label)
        
        self.chunks_label = QLabel("0")
        stats_layout.addRow("Chunks Created:", self.chunks_label)
        
        self.queue_label = QLabel("0")
        stats_layout.addRow("Queue Size:", self.queue_label)
        
        self.errors_label = QLabel("0")
        stats_layout.addRow("Errors:", self.errors_label)
        
        stats_group.setLayout(stats_layout)
        layout.addWidget(stats_group)
        
        layout.addStretch()
        
        widget.setLayout(layout)
        return widget
    
    def _setup_menu(self):
        """Setup menu bar."""
        menubar = self.menuBar()
        
        # File menu
        file_menu = menubar.addMenu("File")
        
        settings_action = QAction("Settings", self)
        settings_action.triggered.connect(self._show_settings)
        file_menu.addAction(settings_action)
        
        file_menu.addSeparator()
        
        exit_action = QAction("Exit", self)
        exit_action.triggered.connect(self.close)
        file_menu.addAction(exit_action)
        
        # Help menu
        help_menu = menubar.addMenu("Help")
        
        about_action = QAction("About", self)
        about_action.triggered.connect(self._show_about)
        help_menu.addAction(about_action)
    
    def _setup_system_tray(self):
        """Setup system tray icon."""
        if not self.settings.minimize_to_tray:
            return
        
        # Only setup tray if we have an icon
        # For now, skip tray icon to avoid the warning
        # self.tray_icon = QSystemTrayIcon(self)
        # Would need: self.tray_icon.setIcon(QIcon("path/to/icon.png"))
        pass
    
    def _add_root(self):
        """Add a new root directory."""
        directory = QFileDialog.getExistingDirectory(
            self,
            "Select Root Directory",
            str(Path.home())
        )
        
        if directory:
            root_path = Path(directory)
            asyncio.create_task(self._add_root_async(root_path))
    
    async def _add_root_async(self, root_path: Path):
        """Async add root."""
        try:
            self.statusBar().showMessage(f"Adding root: {root_path} (scanning files...)", 0)
            success = await self.indexing_engine.add_root(root_path)
            if success:
                # Save to config so it persists
                self.settings.add_root(root_path)
                self.statusBar().showMessage(f"Added root: {root_path} - indexing in background", 5000)
                self._update_root_combo()
            else:
                self.statusBar().showMessage(f"Failed to add root: {root_path}", 5000)
                self.signals.error_occurred.emit(f"Failed to add root: {root_path}")
        except Exception as e:
            self.statusBar().showMessage(f"Error: {str(e)}", 5000)
            self.signals.error_occurred.emit(str(e))
    
    def _remove_root(self):
        """Remove selected root."""
        current_row = self.roots_table.currentRow()
        if current_row < 0:
            return
        
        root_path_str = self.roots_table.item(current_row, 0).text()
        root_path = Path(root_path_str)
        
        asyncio.create_task(self._remove_root_async(root_path))
    
    def _open_root_management(self, root_path: Path):
        """Open root management dialog."""
        from .root_management_dialog import RootManagementDialog
        
        dialog = RootManagementDialog(
            parent=self,
            root_path=root_path,
            indexing_engine=self.indexing_engine,
            settings=self.settings
        )
        dialog.exec()
    
    async def _remove_root_async(self, root_path: Path):
        """Async remove root."""
        try:
            success = await self.indexing_engine.remove_root(root_path)
            if success:
                # Remove from config
                self.settings.remove_root(root_path)
                self.statusBar().showMessage(f"Removed root: {root_path}", 3000)
                self._update_root_combo()
        except Exception as e:
            self.signals.error_occurred.emit(str(e))
    
    def _on_search_settings_changed(self):
        """Handle search settings change - persist to config."""
        try:
            self.settings.search.top_k = self.top_k_spin.value()
            self.settings.search.min_similarity = self.min_similarity_spin.value()
            self.settings.save_to_file()
            logger.debug(f"Search settings updated: top_k={self.settings.search.top_k}, min_similarity={self.settings.search.min_similarity}")
        except Exception as e:
            logger.error(f"Error saving search settings: {e}")
    
    def _perform_search(self):
        """Perform semantic search."""
        query = self.query_input.text().strip()
        if not query:
            return
        
        root_str = self.root_combo.currentText()
        if not root_str:
            self.statusBar().showMessage("No root selected", 3000)
            return
        
        root_path = Path(root_str)
        asyncio.create_task(self._search_async(query, root_path))
    
    async def _search_async(self, query: str, root_path: Path):
        """Async search."""
        try:
            root_state = self.indexing_engine.roots.get(root_path)
            if not root_state:
                self.signals.error_occurred.emit("Root not found")
                return
            
            results = await self.query_engine.search(
                query=query,
                root_path=root_path,
                branch_name=root_state.current_branch,
                top_k=self.top_k_spin.value(),
            )
            
            self.signals.search_completed.emit([r.to_dict() for r in results])
        except Exception as e:
            self.signals.error_occurred.emit(str(e))
    
    def _update_stats(self):
        """Update statistics display."""
        try:
            # Directly update UI - don't go through signals
            # This runs in the main Qt thread already
            self._update_ui_stats()
        except Exception as e:
            logger.error(f"Error updating stats: {e}")
    
    def _update_ui_stats(self):
        """Update UI with current stats (runs in Qt main thread) - optimized with diffing."""
        try:
            # Build current snapshot of data for all roots
            current_snapshot = {}
            any_active_work = False
            
            for root_path, root_state in self.indexing_engine.roots.items():
                queued = self.indexing_engine._indexing_queue.qsize()
                
                # Determine if root is in any initialization/scanning phase
                is_initializing = (
                    root_state.stats.files_scanned == 0 and root_state.stats.current_file is not None
                ) or (
                    root_state.stats.current_file and 
                    any(root_state.stats.current_file.startswith(prefix) for prefix in 
                        ["Initializing", "Loading", "Scanning", "Checking", "Queueing"])
                )
                
                # Determine if chunks are being created
                chunks_loading = (
                    root_state.stats.files_indexed > 0 and 
                    root_state.stats.chunks_created == 0 and
                    root_state.stats.current_file is not None
                )
                
                # Calculate status
                if root_state.paused:
                    if queued > 0:
                        status = f"⏸ Paused ({queued} queued)"
                    else:
                        status = "⏸ Paused"
                elif root_state.stats.current_file:
                    if root_state.stats.current_file.startswith(("Initializing", "Loading", "Scanning", "Checking", "Queueing")):
                        status = root_state.stats.current_file
                    else:
                        current_file = Path(root_state.stats.current_file).name
                        status = f"Indexing: {current_file}"
                elif queued > 0:
                    status = f"Indexing... ({queued} queued)"
                elif root_state.stats.files_scanned == 0:
                    status = "Loading..."
                else:
                    status = "✓ Up to date"
                
                # Calculate ETA
                eta_text = "-"
                if not root_state.paused and queued > 0 and root_state.stats.indexing_start_time:
                    import time
                    elapsed = time.time() - root_state.stats.indexing_start_time
                    files_indexed_since_start = root_state.stats.files_indexed - root_state.stats.files_indexed_at_start
                    
                    if elapsed > 5.0 and files_indexed_since_start > 0:
                        rate = files_indexed_since_start / elapsed
                        if rate > 0:
                            eta_seconds = queued / rate
                            if eta_seconds < 60:
                                eta_text = f"{int(eta_seconds)}s"
                            elif eta_seconds < 3600:
                                minutes = int(eta_seconds / 60)
                                seconds = int(eta_seconds % 60)
                                eta_text = f"{minutes}m {seconds}s"
                            else:
                                hours = int(eta_seconds / 3600)
                                minutes = int((eta_seconds % 3600) / 60)
                                eta_text = f"{hours}h {minutes}m"
                
                # Build snapshot for this root
                current_snapshot[str(root_path)] = {
                    "branch": root_state.current_branch,
                    "scanned": "Loading..." if is_initializing else str(root_state.stats.files_scanned),
                    "indexed": "Loading..." if is_initializing else str(root_state.stats.files_indexed),
                    "chunks": "Loading..." if (is_initializing or chunks_loading) else str(root_state.stats.chunks_created),
                    "queued": "Loading..." if is_initializing else str(queued),
                    "status": status,
                    "eta": eta_text,
                }
                
                # Track if any work is active
                if queued > 0 or root_state.stats.current_file:
                    any_active_work = True
            
            # Check if snapshot changed from last update
            if current_snapshot == self._last_stats_snapshot:
                # No changes - skip update to preserve hover states!
                return
            
            # Store snapshot for next comparison
            self._last_stats_snapshot = current_snapshot
            
            # Adjust timer based on activity
            if any_active_work:
                if self._system_is_idle:
                    self._system_is_idle = False
                    self.stats_timer.setInterval(500)  # Fast updates when active
            else:
                if not self._system_is_idle:
                    self._system_is_idle = True
                    self.stats_timer.setInterval(2000)  # Slow updates when idle
            
            # Update table - block signals during update to avoid hover loss
            self.roots_table.blockSignals(True)
            
            # Build mapping of root_path -> row_index
            root_to_row = {}
            for row in range(self.roots_table.rowCount()):
                path_item = self.roots_table.item(row, 0)
                if path_item:
                    root_to_row[path_item.text()] = row
            
            # Update or add rows
            for root_path_str, data in current_snapshot.items():
                if root_path_str in root_to_row:
                    # Update existing row
                    row = root_to_row[root_path_str]
                else:
                    # Add new row
                    row = self.roots_table.rowCount()
                    self.roots_table.insertRow(row)
                    
                    # Set root path (only once, doesn't change)
                    path_item = QTableWidgetItem(root_path_str)
                    path_item.setToolTip(root_path_str)
                    self.roots_table.setItem(row, 0, path_item)
                    
                    # Add manage button (only once)
                    from pathlib import Path as PathLib
                    rp = PathLib(root_path_str)
                    manage_button = QPushButton("Manage")
                    manage_button.setMaximumWidth(80)
                    manage_button.clicked.connect(lambda checked, root=rp: self._open_root_management(root))
                    self.roots_table.setCellWidget(row, 8, manage_button)
                
                # Update cells efficiently (only text, don't recreate items if not needed)
                def update_cell(row, col, text, tooltip=None):
                    item = self.roots_table.item(row, col)
                    if item is None:
                        item = QTableWidgetItem(text)
                        if tooltip:
                            item.setToolTip(tooltip)
                        self.roots_table.setItem(row, col, item)
                    elif item.text() != text:
                        item.setText(text)
                        if tooltip:
                            item.setToolTip(tooltip)
                
                update_cell(row, 1, data["branch"])
                update_cell(row, 2, data["scanned"])
                update_cell(row, 3, data["indexed"])
                update_cell(row, 4, data["chunks"])
                update_cell(row, 5, data["queued"])
                update_cell(row, 6, data["status"], tooltip=data["status"])
                update_cell(row, 7, data["eta"])
            
            self.roots_table.blockSignals(False)
            
            # Update global stats
            stats = self.indexing_engine.get_stats()
            self.files_scanned_label.setText(str(stats.get("files_scanned", 0)))
            self.files_indexed_label.setText(str(stats.get("files_indexed", 0)))
            self.chunks_label.setText(str(stats.get("chunks_created", 0)))
            queue_size = stats.get("queue_size", 0)
            self.queue_label.setText(str(queue_size))
            self.errors_label.setText(str(stats.get("errors", 0)))
            
            # Cache status for instant closeEvent checks
            self._cached_queue_size = queue_size
            self._cached_has_indexing = any(
                root_state.stats.files_indexed < root_state.stats.files_scanned
                for root_state in self.indexing_engine.roots.values()
            )
            
            # Update resource usage
            resource_stats = self.resource_manager.get_stats()
            cpu_percent = resource_stats["cpu_percent"]
            mem_mb = resource_stats["memory_mb"]
            
            self.cpu_progress.setValue(int(cpu_percent))
            self.cpu_label.setText(f"{cpu_percent:.1f}%")
            
            max_mem = self.settings.resource.max_memory_mb
            mem_percent = int((mem_mb / max_mem) * 100)
            self.mem_progress.setValue(min(100, mem_percent))
            self.mem_label.setText(f"{mem_mb:.0f} MB")
            
            # Update root combo box to keep it in sync
            self._update_root_combo()
        
        except Exception as e:
            logger.error(f"Error updating UI stats: {e}", exc_info=True)
    
    def _on_ui_interaction(self):
        """Called when user interacts with UI - prioritize responsiveness."""
        if not self._ui_active:
            self._ui_active = True
            # Notify resource manager to throttle indexing
            if hasattr(self.resource_manager, 'set_ui_active'):
                self.resource_manager.set_ui_active(True)
        
        # Reset idle timer - UI becomes idle after 2 seconds of no interaction
        self._ui_interaction_timer.stop()
        self._ui_interaction_timer.start(2000)  # 2 seconds idle threshold
    
    def _on_ui_idle(self):
        """Called when UI becomes idle - resume full indexing speed."""
        if self._ui_active:
            self._ui_active = False
            # Notify resource manager to resume indexing
            if hasattr(self.resource_manager, 'set_ui_active'):
                self.resource_manager.set_ui_active(False)
    
    def eventFilter(self, source, event):
        """Filter events to detect UI interactions."""
        from PyQt6.QtCore import QEvent
        
        # Detect keyboard events (typing, etc.)
        if event.type() == QEvent.Type.KeyPress:
            self._on_ui_interaction()
        # Detect mouse clicks (but not all mouse moves to avoid spam)
        elif event.type() == QEvent.Type.MouseButtonPress:
            self._on_ui_interaction()
        # Detect scroll wheel
        elif event.type() == QEvent.Type.Wheel:
            self._on_ui_interaction()
        
        return super().eventFilter(source, event)
    
    def _on_stats_updated(self, stats: dict):
        """Handle stats update (from signal)."""
        # Delegate to the same update method
        self._update_ui_stats()
    
    def _on_search_completed(self, results: list):
        """Handle search completion."""
        # Save current selection before updating table
        selected_row = self.results_table.currentRow()
        
        self.results_table.setRowCount(0)
        
        for result in results:
            row = self.results_table.rowCount()
            self.results_table.insertRow(row)
            
            self.results_table.setItem(row, 0, QTableWidgetItem(result["file_path"]))
            self.results_table.setItem(row, 1, QTableWidgetItem(
                f"{result['line_start']}-{result['line_end']}"
            ))
            self.results_table.setItem(row, 2, QTableWidgetItem(f"{result['score']:.3f}"))
            
            # Better preview with more context (300 chars) and proper truncation
            preview = result["content"]
            if len(preview) > 300:
                preview = preview[:300] + "..."
            preview_item = QTableWidgetItem(preview)
            preview_item.setToolTip(result["content"])  # Full content on hover
            self.results_table.setItem(row, 3, preview_item)
        
        # Intelligently resize columns to fit content with focus on path and preview
        if len(results) > 0:
            # First, resize all columns to content
            self.results_table.resizeColumnsToContents()
            
            # Get current widths
            table_width = self.results_table.viewport().width()
            lines_width = self.results_table.columnWidth(1)
            score_width = self.results_table.columnWidth(2)
            
            # Ensure Lines and Score columns aren't too wide (cap at reasonable sizes)
            lines_width = min(lines_width, 100)
            score_width = min(score_width, 80)
            self.results_table.setColumnWidth(1, lines_width)
            self.results_table.setColumnWidth(2, score_width)
            
            # Calculate remaining space for File path and Preview
            remaining_width = table_width - lines_width - score_width - 20  # -20 for margins
            
            # Allocate space: File path gets 30%, Preview gets 70%
            file_width = int(remaining_width * 0.30)
            preview_width = int(remaining_width * 0.70)
            
            # Set minimum widths to ensure usability
            file_width = max(file_width, 200)  # Minimum 200px for file path
            preview_width = max(preview_width, 300)  # Minimum 300px for preview
            
            self.results_table.setColumnWidth(0, file_width)
            self.results_table.setColumnWidth(3, preview_width)
        
        # Restore previous selection if it's still valid
        if selected_row >= 0 and selected_row < self.results_table.rowCount():
            self.results_table.setCurrentCell(selected_row, self.results_table.currentColumn())
        
        self.statusBar().showMessage(f"Found {len(results)} results", 3000)
    
    def _on_error(self, error_msg: str):
        """Handle errors."""
        QMessageBox.critical(self, "Error", error_msg)
    
    def _on_result_double_clicked(self, item):
        """Handle double-click on result item."""
        row = item.row()
        column = item.column()
        
        # Get result data
        file_item = self.results_table.item(row, 0)
        lines_item = self.results_table.item(row, 1)
        score_item = self.results_table.item(row, 2)
        preview_item = self.results_table.item(row, 3)
        
        if not all([file_item, lines_item, score_item, preview_item]):
            return
        
        # If double-clicking preview column, show full content
        if column == 3:
            self._show_chunk_viewer(
                file_path=file_item.text(),
                lines=lines_item.text(),
                score=score_item.text(),
                content=preview_item.toolTip()  # Full content is in tooltip
            )
        else:
            # For other columns, could open file in editor (future feature)
            pass
    
    def _show_chunk_viewer(self, file_path: str, lines: str, score: str, content: str):
        """Show a nice dialog with the full chunk content."""
        dialog = QDialog(self)
        dialog.setWindowTitle("Chunk Viewer")
        dialog.setMinimumSize(700, 500)
        dialog.resize(800, 600)  # Default size
        
        layout = QVBoxLayout()
        
        # Header with metadata
        header_layout = QHBoxLayout()
        header_layout.addWidget(QLabel(f"<b>File:</b> {file_path}"))
        header_layout.addWidget(QLabel(f"<b>Lines:</b> {lines}"))
        header_layout.addWidget(QLabel(f"<b>Score:</b> {score}"))
        header_layout.addStretch()
        layout.addLayout(header_layout)
        
        # Content display
        content_display = QTextEdit()
        content_display.setReadOnly(True)
        content_display.setPlainText(content)
        content_display.setStyleSheet("""
            QTextEdit {
                font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
                font-size: 10pt;
                background-color: #1e1e1e;
                color: #d4d4d4;
                border: 1px solid #3e3e3e;
                padding: 10px;
            }
        """)
        layout.addWidget(content_display)
        
        # Buttons
        button_layout = QHBoxLayout()
        
        # Copy button
        copy_btn = QPushButton("Copy to Clipboard")
        copy_btn.clicked.connect(lambda: self._copy_to_clipboard(content))
        button_layout.addWidget(copy_btn)
        
        button_layout.addStretch()
        
        # Close button
        close_btn = QPushButton("Close")
        close_btn.clicked.connect(dialog.accept)
        button_layout.addWidget(close_btn)
        
        layout.addLayout(button_layout)
        
        dialog.setLayout(layout)
        dialog.exec()
    
    def _copy_to_clipboard(self, text: str):
        """Copy text to clipboard."""
        from PyQt6.QtWidgets import QApplication
        clipboard = QApplication.clipboard()
        clipboard.setText(text)
        self.statusBar().showMessage("Copied to clipboard", 2000)
    
    def _open_result(self):
        """Open selected search result."""
        # Would integrate with system editor
        pass
    
    def _update_root_combo(self):
        """Update root combo box."""
        self.root_combo.clear()
        for root_path in self.indexing_engine.roots.keys():
            self.root_combo.addItem(str(root_path))
    
    def _show_settings(self):
        """Show settings dialog."""
        dialog = SettingsDialog(self.settings, self)
        if dialog.exec() == QDialog.DialogCode.Accepted:
            self.settings = dialog.get_settings()
            # Save settings (will use stored config path)
            self.settings.save_to_file()
            self.statusBar().showMessage("Settings saved", 3000)
    
    def _show_about(self):
        """Show about dialog."""
        QMessageBox.about(
            self,
            "About FSKB",
            "FSKB - File System Knowledge Base\n\n"
            "A lightweight semantic search system for code repositories.\n\n"
            "Features:\n"
            "- Git-aware branch indexing\n"
            "- Semantic search with embeddings\n"
            "- MCP server support\n"
            "- Resource-aware operation\n"
        )
    
    def closeEvent(self, event):
        """Handle window close - graceful shutdown."""
        # If already closing, just accept
        if hasattr(self, '_is_closing') and self._is_closing:
            event.accept()
            return
        
        # Stop timers first to prevent Qt errors during cleanup
        if hasattr(self, 'stats_timer'):
            self.stats_timer.stop()
        if hasattr(self, 'event_timer'):
            self.event_timer.stop()
        
        # Use cached values - no blocking operations!
        queue_size = getattr(self, '_cached_queue_size', 0)
        has_indexing = getattr(self, '_cached_has_indexing', False)
        
        # Show popup immediately - no heavy checks
        if queue_size > 0 or has_indexing:
            reply = QMessageBox.question(
                self,
                "Confirm Exit",
                f"Indexing in progress ({queue_size} files in queue).\n\n"
                "Progress will be saved and resume on next start.\n"
                "Cleanup may take a few seconds.\n"
                "Are you sure you want to exit?",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
            )
            
            if reply == QMessageBox.StandardButton.Yes:
                # Mark as closing and ignore this event - let cleanup trigger actual close
                self._is_closing = True
                event.ignore()
                
                # Hide window to give immediate feedback
                self.hide()
                
                # Trigger async cleanup which will close the app
                asyncio.create_task(self._async_close())
            else:
                # Restart timers if user cancels
                self.stats_timer.start(500)
                self.event_timer.start(50)
                event.ignore()
        else:
            # No active work, can close immediately
            event.accept()
            # But still trigger cleanup for consistency
            if not hasattr(self, '_is_closing'):
                self._is_closing = True
                asyncio.create_task(self._async_close())
    
    async def _async_close(self):
        """Perform async cleanup before closing."""
        try:
            logger.info("Starting graceful shutdown...")
            
            # Signal that we're shutting down (main.py cleanup will handle the rest)
            # Just close the Qt application which will break the event loop
            from PyQt6.QtWidgets import QApplication
            QApplication.instance().quit()
            
        except Exception as e:
            logger.error(f"Error during async close: {e}")
            # Force quit anyway
            from PyQt6.QtWidgets import QApplication
            QApplication.instance().quit()
    

