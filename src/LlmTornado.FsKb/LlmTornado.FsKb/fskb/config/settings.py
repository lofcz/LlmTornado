"""
Configuration settings using Pydantic for validation and type safety.
"""

from pathlib import Path
from typing import List, Literal, Optional
from pydantic import BaseModel, Field, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict
from loguru import logger
import json


class EmbeddingConfig(BaseModel):
    """Embedding provider configuration."""
    
    provider: Literal["local", "openai", "voyage", "cohere", "google", "anthropic"] = "local"
    model: str = "jinaai/jina-code-embeddings-0.5b"  # Default: Jina Code Embeddings 0.5B (494M params, 896 dim, 32768 context, 15+ languages, task-specific)
    api_key: Optional[str] = None
    batch_size: int = 8  # Larger batch with INT8 quantization (uses ~2-3GB VRAM on RTX 2080)
    
    @field_validator("api_key")
    @classmethod
    def validate_api_key(cls, v, info):
        """Validate API key is present for non-local providers."""
        provider = info.data.get("provider")
        if provider != "local" and not v:
            logger.warning(f"No API key provided for provider: {provider}")
        return v


class ChunkingConfig(BaseModel):
    """Text chunking configuration."""
    
    chunk_size: int = Field(default=3000, ge=100, le=8000)  # Characters per chunk (Jina supports 8K tokens ~6-7K chars)
    chunk_overlap: int = Field(default=500, ge=0, le=2000)  # Overlap for context (increased for larger chunks)
    separators: List[str] = ["\n\n", "\n", ". ", " ", ""]  # Split priority
    
    @field_validator("chunk_overlap")
    @classmethod
    def validate_overlap(cls, v, info):
        """Ensure overlap is less than chunk size."""
        chunk_size = info.data.get("chunk_size", 512)
        if v >= chunk_size:
            raise ValueError(f"chunk_overlap ({v}) must be less than chunk_size ({chunk_size})")
        return v


class ResourceConfig(BaseModel):
    """Resource management configuration."""
    
    max_cpu_percent: float = Field(default=50.0, ge=1.0, le=100.0)
    max_memory_mb: int = Field(default=2048, ge=256)
    max_workers: Optional[int] = None  # None = auto-detect based on CPU cores
    idle_timeout_seconds: int = Field(default=300, ge=60)  # Unload inactive indices after 5 min
    debounce_delay_ms: int = Field(default=500, ge=100, le=5000)


class IndexingConfig(BaseModel):
    """File indexing configuration."""
    
    text_extensions: List[str] = [
        ".py", ".js", ".ts", ".jsx", ".tsx", ".java", ".cpp", ".c", ".h", ".hpp",
        ".cs", ".go", ".rs", ".rb", ".php", ".swift", ".kt", ".scala",
        ".md", ".txt", ".rst", ".tex", ".org",
        ".html", ".css", ".scss", ".sass", ".less",
        ".json", ".yaml", ".yml", ".toml", ".xml", ".ini", ".cfg", ".conf",
        ".sql", ".sh", ".bash", ".zsh", ".fish", ".ps1",
        ".vue", ".svelte", ".astro",  # Modern JS frameworks
    ]
    max_file_size_mb: int = Field(default=10, ge=1, le=100)
    respect_gitignore: bool = True
    use_fskbignore: bool = True
    
    # Directories to always skip (in addition to gitignore)
    skip_directories: List[str] = [
        "node_modules", ".git", "__pycache__", ".venv", "venv", "env",
        "bin", "obj", ".vs", ".vscode", "target", "build", "dist",
        ".next", ".nuxt", ".output", "coverage", ".pytest_cache",
        ".vitepress", "cache", ".cache", "assets",
    ]


class SearchConfig(BaseModel):
    """Search configuration."""
    
    top_k: int = Field(default=10, ge=1, le=100)
    min_similarity: float = Field(default=0.3, ge=0.0, le=1.0)  # Increased from 0.0 to filter low-quality results
    context_lines_before: int = Field(default=2, ge=0, le=10)
    context_lines_after: int = Field(default=2, ge=0, le=10)


class StorageConfig(BaseModel):
    """Storage configuration."""
    
    use_chromadb: bool = True  # Use ChromaDB instead of manual FAISS+SQLite
    data_dir: Path = Path.home() / ".fskb" / "data"
    log_dir: Path = Path.home() / ".fskb" / "logs"
    config_dir: Path = Path.home() / ".fskb"
    
    def __init__(self, **data):
        super().__init__(**data)
        # Ensure directories exist
        self.config_dir.mkdir(parents=True, exist_ok=True)
        self.data_dir.mkdir(parents=True, exist_ok=True)
        self.log_dir.mkdir(parents=True, exist_ok=True)


class Settings(BaseSettings):
    """Main application settings."""
    
    model_config = SettingsConfigDict(
        env_prefix="FSKB_",
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
    )
    
    # Sub-configurations
    embedding: EmbeddingConfig = Field(default_factory=EmbeddingConfig)
    chunking: ChunkingConfig = Field(default_factory=ChunkingConfig)
    resource: ResourceConfig = Field(default_factory=ResourceConfig)
    indexing: IndexingConfig = Field(default_factory=IndexingConfig)
    search: SearchConfig = Field(default_factory=SearchConfig)
    storage: StorageConfig = Field(default_factory=StorageConfig)
    
    # Root paths to index
    roots: List[Path] = Field(default_factory=list)
    
    # GUI settings
    gui_enabled: bool = True
    minimize_to_tray: bool = True
    show_notifications: bool = True
    show_debug_panel: bool = False  # Debug panel for testing chunk similarity
    
    # Internal - stores where config was loaded from
    _config_path: Optional[Path] = None
    
    def add_root(self, root_path: Path):
        """Add a root path and save config."""
        root_path = root_path.resolve()
        if root_path not in self.roots:
            self.roots.append(root_path)
            self.save_to_file()
            logger.info(f"Added root to config: {root_path}")
    
    def remove_root(self, root_path: Path):
        """Remove a root path and save config."""
        root_path = root_path.resolve()
        if root_path in self.roots:
            self.roots.remove(root_path)
            self.save_to_file()
            logger.info(f"Removed root from config: {root_path}")
    
    @classmethod
    def load_from_file(cls, config_path: Path) -> "Settings":
        """Load settings from a JSON or YAML file."""
        if not config_path.exists():
            logger.info(f"Config file not found: {config_path}, using defaults")
            return cls()
        
        with open(config_path, "r", encoding="utf-8") as f:
            if config_path.suffix in [".yaml", ".yml"]:
                try:
                    import yaml
                    data = yaml.safe_load(f)
                except ImportError:
                    logger.error("PyYAML not installed, cannot load YAML config")
                    return cls()
            else:
                data = json.load(f)
        
        return cls(**data)
    
    def save_to_file(self, config_path: Optional[Path] = None):
        """Save settings to a JSON file."""
        if config_path is None:
            # Use stored config path or default
            if hasattr(self, '_config_path') and self._config_path:
                config_path = self._config_path
            else:
                config_path = self.storage.config_dir / "config.json"
        
        config_path.parent.mkdir(parents=True, exist_ok=True)
        
        # Convert to dict and handle Path objects
        data = self.model_dump(mode="json", exclude={'_config_path'})
        
        with open(config_path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
        
        logger.info(f"Settings saved to {config_path}")


# Singleton settings instance
_settings: Optional[Settings] = None


def get_settings(config_path: Optional[Path] = None) -> Settings:
    """Get or create the global settings instance."""
    global _settings
    
    if _settings is None:
        if config_path is None:
            # Try local config first (for dev), then user config
            local_config = Path("config.json")
            user_config = Path.home() / ".fskb" / "config.json"
            
            if local_config.exists():
                config_path = local_config
                logger.info(f"Loading config from: {local_config.absolute()}")
            else:
                config_path = user_config
                logger.info(f"Loading config from: {user_config}")
        
        _settings = Settings.load_from_file(config_path)
        
        # Store the config path for future saves
        _settings._config_path = config_path
    
    return _settings


def reload_settings(config_path: Optional[Path] = None):
    """Reload settings from file."""
    global _settings
    _settings = None
    return get_settings(config_path)

