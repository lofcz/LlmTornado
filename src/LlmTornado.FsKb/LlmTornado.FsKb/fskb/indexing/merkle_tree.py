"""
Merkle tree for efficient directory-level change detection.
Allows quickly identifying which directories have changed.
"""

import hashlib
from pathlib import Path
from typing import Dict, Optional, Set, Tuple
from loguru import logger


class MerkleNode:
    """A node in the Merkle tree (file or directory)."""
    
    def __init__(self, path: Path, is_dir: bool):
        self.path = path
        self.is_dir = is_dir
        self.hash: Optional[str] = None
        self.children: Dict[str, 'MerkleNode'] = {}
    
    def compute_hash(self, file_hashes: Dict[Path, str]) -> str:
        """
        Compute hash for this node.
        For files: use the provided file hash
        For directories: hash of sorted child hashes
        """
        if not self.is_dir:
            # File node - use provided hash
            self.hash = file_hashes.get(self.path, "")
            return self.hash
        
        # Directory node - hash all children
        child_hashes = []
        for name in sorted(self.children.keys()):
            child = self.children[name]
            child_hash = child.compute_hash(file_hashes)
            child_hashes.append(f"{name}:{child_hash}")
        
        # Hash the concatenated child hashes
        combined = "|".join(child_hashes)
        self.hash = hashlib.sha256(combined.encode('utf-8')).hexdigest()
        return self.hash
    
    def __repr__(self) -> str:
        return f"MerkleNode({self.path.name}, dir={self.is_dir}, hash={self.hash[:8] if self.hash else 'None'})"


class MerkleTree:
    """
    Merkle tree for efficient directory-level change detection.
    
    Each file gets a hash, each directory gets a hash of its children.
    Comparing trees identifies changed directories without scanning unchanged ones.
    """
    
    def __init__(self, root_path: Path):
        self.root_path = root_path
        self.root_node = MerkleNode(root_path, is_dir=True)
    
    def build(self, file_paths: Set[Path], file_hashes: Dict[Path, str]):
        """
        Build Merkle tree from file paths and their hashes.
        
        Args:
            file_paths: Set of file paths (relative to root)
            file_hashes: Map of file path -> content hash
        """
        # Reset tree
        self.root_node = MerkleNode(self.root_path, is_dir=True)
        
        # Add all files to tree
        for file_path in file_paths:
            self._add_file(file_path)
        
        # Compute hashes bottom-up
        self.root_node.compute_hash(file_hashes)
        
        logger.debug(f"Built Merkle tree with {len(file_paths)} files, root hash: {self.root_node.hash[:8]}")
    
    def _add_file(self, file_path: Path):
        """Add a file to the tree, creating parent directories as needed."""
        # Get path relative to root
        try:
            rel_path = file_path.relative_to(self.root_path)
        except ValueError:
            rel_path = file_path
        
        parts = rel_path.parts
        if not parts:
            return
        
        # Navigate/create directory structure
        current_node = self.root_node
        for i, part in enumerate(parts):
            is_last = (i == len(parts) - 1)
            is_dir = not is_last
            
            if part not in current_node.children:
                node_path = self.root_path / Path(*parts[:i+1])
                current_node.children[part] = MerkleNode(node_path, is_dir=is_dir)
            
            current_node = current_node.children[part]
    
    def compare(self, other: 'MerkleTree') -> Tuple[Set[Path], Set[Path], Set[Path]]:
        """
        Compare with another Merkle tree to find changes.
        
        Args:
            other: Previous Merkle tree to compare against
        
        Returns:
            Tuple of (added_paths, modified_paths, deleted_paths) - all at directory level
        """
        added = set()
        modified = set()
        deleted = set()
        
        # Compare recursively
        self._compare_nodes(
            self.root_node,
            other.root_node,
            added,
            modified,
            deleted
        )
        
        return added, modified, deleted
    
    def _compare_nodes(
        self,
        new_node: MerkleNode,
        old_node: Optional[MerkleNode],
        added: Set[Path],
        modified: Set[Path],
        deleted: Set[Path],
    ):
        """Recursively compare nodes."""
        # If old node doesn't exist, entire subtree is new
        if old_node is None:
            added.add(new_node.path)
            return
        
        # If hashes match, subtree unchanged
        if new_node.hash == old_node.hash:
            return
        
        # If not a directory, it's modified
        if not new_node.is_dir:
            modified.add(new_node.path)
            return
        
        # Directory changed - recurse into children
        all_children = set(new_node.children.keys()) | set(old_node.children.keys())
        
        for child_name in all_children:
            new_child = new_node.children.get(child_name)
            old_child = old_node.children.get(child_name)
            
            if new_child is None:
                # Child deleted
                deleted.add(old_child.path)
            elif old_child is None:
                # Child added
                added.add(new_child.path)
            else:
                # Child exists in both - compare
                self._compare_nodes(new_child, old_child, added, modified, deleted)
    
    def get_changed_directories(self, other: 'MerkleTree') -> Set[Path]:
        """
        Get set of directories that have changed.
        
        Args:
            other: Previous Merkle tree
        
        Returns:
            Set of directory paths that have changes
        """
        changed_dirs = set()
        self._find_changed_dirs(self.root_node, other.root_node, changed_dirs)
        return changed_dirs
    
    def _find_changed_dirs(
        self,
        new_node: MerkleNode,
        old_node: Optional[MerkleNode],
        changed_dirs: Set[Path],
    ):
        """Recursively find changed directories."""
        # If old doesn't exist or hashes differ
        if old_node is None or new_node.hash != old_node.hash:
            if new_node.is_dir:
                changed_dirs.add(new_node.path)
            else:
                # For files, add parent directory
                changed_dirs.add(new_node.path.parent)
            return
        
        # If directory and hashes match, no changes in subtree
        if new_node.is_dir and new_node.hash == old_node.hash:
            return
        
        # Recurse into children
        for child_name, new_child in new_node.children.items():
            old_child = old_node.children.get(child_name) if old_node else None
            self._find_changed_dirs(new_child, old_child, changed_dirs)

