"""
Quick recovery utilities for handling app restarts gracefully.
"""

import asyncio
from pathlib import Path
from typing import Dict, Set
from loguru import logger


class RecoveryManager:
    """Manages quick recovery of indexing state after restarts."""
    
    def __init__(self, data_dir: Path):
        self.data_dir = Path(data_dir)
        self.state_file = self.data_dir / "indexing_state.json"
    
    async def save_state(self, roots: Dict[Path, Dict]):
        """
        Save current indexing state for quick recovery.
        
        Args:
            roots: Dictionary of root paths to their state
        """
        try:
            import json
            
            state = {
                "version": "1.0",
                "roots": []
            }
            
            for root_path, root_state in roots.items():
                root_info = {
                    "path": str(root_path),
                    "branch": root_state.current_branch,
                    "files_scanned": root_state.stats.files_scanned,
                    "files_indexed": root_state.stats.files_indexed,
                    "chunks_created": root_state.stats.chunks_created,
                }
                state["roots"].append(root_info)
            
            # Write atomically
            temp_file = self.state_file.with_suffix(".tmp")
            await asyncio.to_thread(
                lambda: temp_file.write_text(json.dumps(state, indent=2))
            )
            await asyncio.to_thread(
                lambda: temp_file.replace(self.state_file)
            )
            
            logger.debug(f"Saved indexing state: {len(roots)} roots")
        
        except Exception as e:
            logger.error(f"Error saving indexing state: {e}")
    
    async def load_state(self) -> Dict[str, Dict]:
        """
        Load previous indexing state.
        
        Returns:
            Dictionary of root paths to their previous state
        """
        try:
            if not self.state_file.exists():
                return {}
            
            import json
            
            content = await asyncio.to_thread(
                lambda: self.state_file.read_text()
            )
            state = json.loads(content)
            
            if state.get("version") != "1.0":
                logger.warning(f"Unknown state file version: {state.get('version')}")
                return {}
            
            roots = {}
            for root_info in state.get("roots", []):
                path = root_info.get("path")
                if path:
                    roots[path] = root_info
            
            logger.info(f"Loaded previous state: {len(roots)} roots")
            return roots
        
        except Exception as e:
            logger.error(f"Error loading indexing state: {e}")
            return {}
    
    async def cleanup_state(self):
        """Remove state file."""
        try:
            if self.state_file.exists():
                await asyncio.to_thread(self.state_file.unlink)
        except Exception as e:
            logger.error(f"Error cleaning up state: {e}")

