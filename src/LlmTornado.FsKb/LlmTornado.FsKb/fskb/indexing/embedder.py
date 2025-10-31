"""
Embedding generation using local models and API providers via litellm.
"""

import asyncio
from typing import List, Optional
import numpy as np
from loguru import logger
from tenacity import retry, stop_after_attempt, wait_exponential
from ..config import Settings

# Import for local embeddings
try:
    from sentence_transformers import SentenceTransformer
    SENTENCE_TRANSFORMERS_AVAILABLE = True
except ImportError:
    SENTENCE_TRANSFORMERS_AVAILABLE = False
    logger.warning("sentence-transformers not available, local embeddings disabled")

# Import for API embeddings
try:
    import litellm
    LITELLM_AVAILABLE = True
except ImportError:
    LITELLM_AVAILABLE = False
    logger.warning("litellm not available, API embeddings disabled")


class EmbeddingProvider:
    """Unified interface for embedding generation."""
    
    def __init__(self, settings: Settings, resource_manager=None):
        self.settings = settings
        self.provider = settings.embedding.provider
        self.model = settings.embedding.model
        self.api_key = settings.embedding.api_key
        self.batch_size = settings.embedding.batch_size
        self.resource_manager = resource_manager  # For UI interaction detection
        
        self._local_model: Optional[SentenceTransformer] = None
        self._dimension: Optional[int] = None
        
        self._initialize()
    
    def _initialize(self):
        """Initialize the embedding provider."""
        if self.provider == "local":
            self._initialize_local()
        else:
            self._initialize_api()
    
    def _initialize_local(self):
        """Initialize local sentence-transformers model."""
        if not SENTENCE_TRANSFORMERS_AVAILABLE:
            raise RuntimeError("sentence-transformers not installed for local embeddings")
        
        logger.info(f"Loading local embedding model: {self.model}")
        
        try:
            # Detect CUDA availability
            import torch
            device = "cuda" if torch.cuda.is_available() else "cpu"
            
            if device == "cuda":
                gpu_name = torch.cuda.get_device_name(0)
                gpu_memory = torch.cuda.get_device_properties(0).total_memory / 1024**3
                logger.info(f"GPU detected: {gpu_name} ({gpu_memory:.1f} GB)")
            else:
                logger.info("No GPU detected, using CPU")
            
            # Load model with explicit device
            # trust_remote_code=True needed for Jina and some other models
            self._local_model = SentenceTransformer(
                self.model, 
                device=device,
                trust_remote_code=True
            )
            self._dimension = self._local_model.get_sentence_embedding_dimension()
            
            logger.info(
                f"Local model loaded: {self.model}, "
                f"dimension: {self._dimension}, "
                f"device: {device}"
            )
        
        except Exception as e:
            logger.error(f"Error loading local model: {e}")
            raise
    
    def _initialize_api(self):
        """Initialize API provider via litellm."""
        if not LITELLM_AVAILABLE:
            raise RuntimeError("litellm not installed for API embeddings")
        
        if not self.api_key:
            logger.warning(f"No API key provided for {self.provider}")
        
        # Set API key for litellm
        if self.provider == "openai":
            import openai
            openai.api_key = self.api_key
        
        logger.info(f"API provider initialized: {self.provider}/{self.model}")
        
        # Dimension will be determined on first embedding
        self._dimension = None
    
    @property
    def dimension(self) -> int:
        """Get embedding dimension."""
        if self._dimension is None:
            # For API providers, we need to get dimension from first embedding
            if self.provider != "local":
                # Common dimensions for popular models
                dimension_map = {
                    "text-embedding-3-small": 1536,
                    "text-embedding-3-large": 3072,
                    "text-embedding-ada-002": 1536,
                    "voyage-2": 1024,
                    "voyage-large-2": 1536,
                }
                self._dimension = dimension_map.get(self.model, 1536)  # Default
        
        return self._dimension or 1536
    
    async def embed_texts(self, texts: List[str]) -> List[np.ndarray]:
        """
        Generate embeddings for a list of texts.
        Handles batching automatically.
        
        Args:
            texts: List of text strings to embed
        
        Returns:
            List of embedding vectors as numpy arrays
        """
        if not texts:
            return []
        
        try:
            if self.provider == "local":
                return await self._embed_local(texts)
            else:
                return await self._embed_api(texts)
        
        except Exception as e:
            logger.error(f"Error generating embeddings: {e}")
            # Return zero vectors as fallback
            return [np.zeros(self.dimension, dtype=np.float32) for _ in texts]
    
    async def _embed_local(self, texts: List[str]) -> List[np.ndarray]:
        """Generate embeddings using local model."""
        if not self._local_model:
            raise RuntimeError("Local model not initialized")
        
        # Run in executor to not block event loop
        loop = asyncio.get_event_loop()
        
        # Process in batches
        all_embeddings = []
        total_batches = (len(texts) + self.batch_size - 1) // self.batch_size
        logger.debug(f"Generating embeddings for {len(texts)} texts in {total_batches} batches")
        
        # Adaptive timeout: longer for first batch (model warm-up/GPU initialization)
        first_batch_timeout = 120.0  # 2 minutes for first batch
        normal_timeout = 60.0  # 1 minute for subsequent batches
        
        for i in range(0, len(texts), self.batch_size):
            batch = texts[i:i + self.batch_size]
            batch_num = i // self.batch_size + 1
            
            # Use longer timeout for first batch (GPU warm-up)
            timeout = first_batch_timeout if batch_num == 1 else normal_timeout
            
            # Run encoding in thread pool with cancellation support
            try:
                import time
                start_time = time.time()
                
                embeddings = await asyncio.wait_for(
                    loop.run_in_executor(None, self._local_model.encode, batch),
                    timeout=timeout
                )
                
                elapsed = time.time() - start_time
                logger.debug(f"Batch {batch_num}/{total_batches} completed in {elapsed:.1f}s")
                
                # Warn if batch is slow (but didn't timeout)
                if elapsed > 30.0:
                    logger.warning(
                        f"Batch {batch_num}/{total_batches} took {elapsed:.1f}s. "
                        f"Consider reducing batch_size from {self.batch_size} in config"
                    )
                
                # Convert to numpy and move to CPU immediately to prevent GPU memory accumulation
                if hasattr(embeddings, 'cpu'):
                    # PyTorch tensor
                    embeddings_np = embeddings.cpu().numpy().astype(np.float32)
                elif isinstance(embeddings, list) and len(embeddings) > 0 and hasattr(embeddings[0], 'cpu'):
                    # List of PyTorch tensors
                    embeddings_np = [emb.cpu().numpy().astype(np.float32) for emb in embeddings]
                else:
                    # Already numpy
                    embeddings_np = [np.array(emb, dtype=np.float32) for emb in embeddings]
                
                all_embeddings.extend(embeddings_np)
                
                # Clear GPU cache after each batch to prevent fragmentation
                try:
                    import torch
                    if torch.cuda.is_available():
                        torch.cuda.empty_cache()
                except:
                    pass
            except asyncio.TimeoutError:
                logger.error(
                    f"Embedding batch {batch_num}/{total_batches} timed out after {timeout}s "
                    f"(batch_size={self.batch_size}, model={self.model}). "
                    f"Suggestions: 1) Reduce batch_size in config, 2) Check GPU memory, "
                    f"3) Update GPU drivers"
                )
                # Add zero vectors for failed batch
                all_embeddings.extend([
                    np.zeros(self.dimension, dtype=np.float32)
                    for _ in batch
                ])
            except asyncio.CancelledError:
                logger.info("Embedding cancelled")
                raise
            
            # Yield control frequently to keep UI responsive
            # Check if UI is active and yield more frequently
            ui_active = False
            if self.resource_manager and hasattr(self.resource_manager, 'is_ui_active'):
                ui_active = self.resource_manager.is_ui_active()
            
            # Yield every batch if UI is active, every 2 batches otherwise
            if ui_active or i % (self.batch_size * 2) == 0:
                await asyncio.sleep(0)  # Yield to event loop
        
        return all_embeddings  # Already numpy arrays from immediate conversion
    
    @retry(
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=2, max=10)
    )
    async def _embed_api(self, texts: List[str]) -> List[np.ndarray]:
        """Generate embeddings using API via litellm."""
        if not LITELLM_AVAILABLE:
            raise RuntimeError("litellm not installed")
        
        all_embeddings = []
        
        # Process in batches
        for i in range(0, len(texts), self.batch_size):
            batch = texts[i:i + self.batch_size]
            
            try:
                # Use litellm's embedding function
                # Format: provider/model
                model_name = f"{self.provider}/{self.model}" if self.provider != "openai" else self.model
                
                response = await asyncio.to_thread(
                    litellm.embedding,
                    model=model_name,
                    input=batch,
                    api_key=self.api_key
                )
                
                # Extract embeddings
                embeddings = [np.array(item["embedding"], dtype=np.float32) for item in response.data]
                all_embeddings.extend(embeddings)
                
                # Update dimension if not set
                if self._dimension is None and embeddings:
                    self._dimension = len(embeddings[0])
                
                # Yield control and respect rate limits
                await asyncio.sleep(0.1)
            
            except Exception as e:
                logger.error(f"Error in API embedding batch: {e}")
                # Add zero vectors for failed batch
                all_embeddings.extend([
                    np.zeros(self.dimension, dtype=np.float32)
                    for _ in batch
                ])
        
        return all_embeddings
    
    async def embed_single(self, text: str) -> np.ndarray:
        """Embed a single text string."""
        embeddings = await self.embed_texts([text])
        return embeddings[0] if embeddings else np.zeros(self.dimension, dtype=np.float32)

