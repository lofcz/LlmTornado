"""
FSKB - File System Knowledge Base
Main entry point for the application.
"""

import sys
import asyncio
import argparse
from pathlib import Path
from loguru import logger

# Configure logging
from fskb.config import get_settings

settings = get_settings()
log_file = settings.storage.log_dir / "fskb.log"

# Remove default logger to avoid double logging
logger.remove()

# Add console logger
logger.add(
    lambda msg: print(msg, end=""),
    level="INFO",
    format="{time:HH:mm:ss.SSS} | {level: <8} | {message}\n"
)

# Add file logger with more detail
logger.add(
    log_file,
    rotation="10 MB",
    retention="7 days",
    level="DEBUG",
    format="{time:YYYY-MM-DD HH:mm:ss} | {level} | {name}:{function}:{line} | {message}"
)

from fskb.utils import ResourceManager
from fskb.indexing import EmbeddingProvider, IndexingEngine
from fskb.storage import ChromaStore
from fskb.search import QueryEngine
from fskb.mcp_server import MCPServer


def parse_args():
    """Parse command line arguments."""
    parser = argparse.ArgumentParser(description="FSKB - File System Knowledge Base")
    parser.add_argument(
        "--mcp",
        action="store_true",
        help="Run as MCP server (stdio mode)"
    )
    parser.add_argument(
        "--no-gui",
        action="store_true",
        help="Run without GUI (headless mode)"
    )
    parser.add_argument(
        "--config",
        type=Path,
        help="Path to config file"
    )
    parser.add_argument(
        "--add-root",
        type=Path,
        action="append",
        dest="roots",
        help="Add root directory to index (can be specified multiple times)"
    )
    
    return parser.parse_args()


async def run_mcp_server(
    settings,
    indexing_engine: IndexingEngine,
    query_engine: QueryEngine,
):
    """Run the application as an MCP server."""
    logger.info("Starting MCP server mode")
    
    try:
        mcp_server = MCPServer(
            settings=settings,
            indexing_engine=indexing_engine,
            query_engine=query_engine,
        )
        
        await mcp_server.run()
    
    except KeyboardInterrupt:
        logger.info("MCP server interrupted by user")
    except Exception as e:
        logger.error(f"Error in MCP server: {e}")
        raise


async def run_headless(
    settings,
    indexing_engine: IndexingEngine,
    roots: list[Path] = None,
):
    """Run in headless mode (no GUI)."""
    logger.info("Starting headless mode")
    
    try:
        # Add roots if specified (use create_task to avoid blocking)
        if roots:
            for root in roots:
                asyncio.create_task(indexing_engine.add_root(root))
        else:
            # Add configured roots
            for root in settings.roots:
                asyncio.create_task(indexing_engine.add_root(root))
        
        # Give tasks a moment to start
        await asyncio.sleep(0.5)
        
        # Keep running
        logger.info("Indexing engine running. Press Ctrl+C to stop.")
        await asyncio.Event().wait()
    
    except KeyboardInterrupt:
        logger.info("Headless mode interrupted by user")


def run_gui(settings):
    """Run with GUI."""
    logger.info("Starting GUI mode")
    
    try:
        from PyQt6.QtWidgets import QApplication
        from fskb.gui import MainWindow
        import qasync
        
        app = QApplication(sys.argv)
        
        # Use qasync for asyncio integration with Qt
        loop = qasync.QEventLoop(app)
        asyncio.set_event_loop(loop)
        
        # Define async initialization and cleanup
        async def async_gui_main():
            # Initialize components in the qasync event loop
            (
                resource_manager,
                embedding_provider,
                chroma_store,
                indexing_engine,
                query_engine,
            ) = await initialize_components(settings)
            
            try:
                # Create main window
                window = MainWindow(
                    settings=settings,
                    indexing_engine=indexing_engine,
                    query_engine=query_engine,
                    resource_manager=resource_manager,
                )
                window.show()
                
                # Load configured roots (use create_task to avoid blocking event loop)
                for root in settings.roots:
                    asyncio.create_task(indexing_engine.add_root(root))
                
                # Give tasks a moment to start
                await asyncio.sleep(0.5)
                
                # Keep running until window closes
                while not app.closingDown():
                    await asyncio.sleep(0.1)
            
            finally:
                # Non-blocking cleanup with timeout
                try:
                    await asyncio.wait_for(
                        cleanup_components(resource_manager, indexing_engine, chroma_store),
                        timeout=10.0  # Max 10 seconds for cleanup
                    )
                except asyncio.TimeoutError:
                    logger.warning("Cleanup timed out, exiting anyway")
                except Exception as e:
                    logger.error(f"Error during cleanup: {e}", exc_info=False)
        
        with loop:
            try:
                loop.run_until_complete(async_gui_main())
            except RuntimeError as e:
                # Event loop stopped (normal exit via quit())
                if "Event loop stopped" in str(e):
                    logger.info("Application closed by user")
                else:
                    raise
    
    except ImportError as e:
        logger.error(f"GUI dependencies not available: {e}")
        logger.info("Install PyQt6 and qasync: pip install PyQt6 qasync")
        sys.exit(1)
    except KeyboardInterrupt:
        logger.info("GUI interrupted by user")


async def initialize_components(settings):
    """Initialize all application components."""
    logger.info("Initializing components...")
    
    # Resource manager
    resource_manager = ResourceManager(settings)
    await resource_manager.start_monitoring()
    
    # Embedding provider (pass resource_manager for UI interaction detection)
    embedding_provider = EmbeddingProvider(settings, resource_manager=resource_manager)
    
    # Storage
    chroma_store = ChromaStore(
        data_dir=settings.storage.data_dir,
        embedding_dimension=embedding_provider.dimension,
    )
    
    # Indexing engine
    indexing_engine = IndexingEngine(
        settings=settings,
        resource_manager=resource_manager,
        embedding_provider=embedding_provider,
        chroma_store=chroma_store,
    )
    await indexing_engine.start()
    
    # Query engine
    query_engine = QueryEngine(
        settings=settings,
        embedding_provider=embedding_provider,
        chroma_store=chroma_store,
    )
    
    logger.info("Components initialized successfully")
    
    return resource_manager, embedding_provider, chroma_store, indexing_engine, query_engine


async def cleanup_components(
    resource_manager,
    indexing_engine,
    chroma_store,
):
    """Cleanup components on shutdown with UI responsiveness."""
    logger.info("Cleaning up components...")
    
    try:
        # Save state first (fast, should complete quickly)
        from fskb.indexing import RecoveryManager
        recovery = RecoveryManager(indexing_engine.settings.storage.data_dir)
        try:
            await asyncio.wait_for(recovery.save_state(indexing_engine.roots), timeout=2.0)
        except asyncio.TimeoutError:
            logger.warning("State save timed out")
        
        # Stop components with frequent yields
        await asyncio.sleep(0)  # Yield immediately
        
        # Stop indexing engine (now has timeout)
        await indexing_engine.stop()
        await asyncio.sleep(0)  # Yield
        
        # Stop resource monitoring
        await resource_manager.stop_monitoring()
        await asyncio.sleep(0)  # Yield
        
        # Close ChromaDB
        await chroma_store.close()
        
    except Exception as e:
        logger.error(f"Error during cleanup: {e}", exc_info=False)
    
    logger.info("Cleanup complete")


async def async_main():
    """Async main function."""
    args = parse_args()
    
    # Load settings
    if args.config:
        settings = get_settings(args.config)
    else:
        settings = get_settings()
    
    logger.info("FSKB starting...")
    logger.info(f"Data directory: {settings.storage.data_dir}")
    logger.info(f"Embedding provider: {settings.embedding.provider}/{settings.embedding.model}")
    
    # Initialize components
    (
        resource_manager,
        embedding_provider,
        chroma_store,
        indexing_engine,
        query_engine,
    ) = await initialize_components(settings)
    
    try:
        if args.mcp:
            # MCP server mode
            await run_mcp_server(settings, indexing_engine, query_engine)
        elif args.no_gui:
            # Headless mode
            await run_headless(settings, indexing_engine, args.roots)
        else:
            # GUI mode (needs to be run in sync context)
            run_gui(settings, indexing_engine, query_engine, resource_manager)
    
    finally:
        await cleanup_components(resource_manager, indexing_engine, chroma_store)
        logger.info("FSKB stopped")


def main():
    """Main entry point."""
    try:
        # Check if running in GUI mode (no --mcp or --no-gui flags)
        args = parse_args()
        
        if not args.mcp and not args.no_gui:
            # Load settings first
            if args.config:
                settings = get_settings(args.config)
            else:
                settings = get_settings()
            
            # For GUI mode, pass settings and let run_gui initialize components
            # This ensures components are initialized in the qasync event loop
            try:
                run_gui(settings)
            finally:
                pass  # Cleanup handled by run_gui
        else:
            # MCP or headless mode - use asyncio
            asyncio.run(async_main())
    
    except KeyboardInterrupt:
        logger.info("Application interrupted by user")
    except RuntimeError as e:
        # Handle graceful shutdown from GUI close
        if "Event loop stopped" in str(e):
            logger.info("Application closed gracefully")
        else:
            logger.exception(f"Fatal error: {e}")
            sys.exit(1)
    except Exception as e:
        logger.exception(f"Fatal error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
