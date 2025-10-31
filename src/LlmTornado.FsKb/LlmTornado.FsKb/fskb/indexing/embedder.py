"""
Embedding generation using local models and API providers via litellm.
"""

import asyncio
from typing import List, Optional
import numpy as np
from loguru import logger
from tenacity import retry, stop_after_attempt, wait_exponential
from ..config import Settings

# Jina Code Embeddings task configurations (official from Jina docs)
# For code search, we index chunks with "Candidate code snippet", so we can only
# use tasks that also expect code snippets (nl2code, code2code)
INSTRUCTION_CONFIGS = {
    "nl2code": {
        "query": "Find the most relevant code snippet given the following query:\n",
        "passage": "Candidate code snippet:\n",
        "description": "Natural language to code - works for ALL queries (questions, descriptions, etc.)"
    },
    "code2code": {
        "query": "Find an equivalent code snippet given the following code snippet:\n",
        "passage": "Candidate code snippet:\n",
        "description": "Code to code search - paste code to find similar implementations"
    }
}

# Note: QA, code2nl, code2completion tasks are NOT suitable for code search
# because they expect different passage types (answers, comments, completions)
# Since we index CODE CHUNKS, we must use nl2code or code2code

# Default task
DEFAULT_TASK = "nl2code"

def detect_task_from_query(query: str) -> str:
    """
    Auto-detect the best task based on query patterns.
    
    For code search:
    - nl2code: Natural language queries (questions, descriptions, keywords)
    - code2code: When query looks like actual code
    """
    query_lower = query.lower().strip()
    
    # If query looks like code (has braces, parentheses, semicolons)
    if any(c in query for c in ['{', '}', '()', '=>', ';', 'def ', 'class ', 'function ']):
        return "code2code"
    
    # Default to nl2code for all natural language (including questions!)
    return "nl2code"

# Import for local embeddings
try:
    from transformers import AutoModel, AutoTokenizer
    import torch
    import torch.nn.functional as F
    TRANSFORMERS_AVAILABLE = True
except ImportError:
    TRANSFORMERS_AVAILABLE = False
    logger.warning("transformers/torch not available, local embeddings disabled")

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
        
        self._local_model: Optional[any] = None  # AutoModel for transformers
        self._tokenizer: Optional[any] = None   # AutoTokenizer for transformers
        self._device: Optional[str] = None
        self._dimension: Optional[int] = None
        
        self._initialize()
    
    def _initialize(self):
        """Initialize the embedding provider."""
        if self.provider == "local":
            self._initialize_local()
        else:
            self._initialize_api()
    
    def _initialize_local(self):
        """Initialize local transformers model (Jina Code Embeddings with last-token pooling and INT8 quantization)."""
        if not TRANSFORMERS_AVAILABLE:
            raise RuntimeError("transformers/torch not installed for local embeddings")
        
        logger.info(f"Loading local embedding model: {self.model}")
        
        try:
            # Detect CUDA availability
            device = "cuda" if torch.cuda.is_available() else "cpu"
            self._device = device
            
            if device == "cuda":
                gpu_name = torch.cuda.get_device_name(0)
                gpu_memory = torch.cuda.get_device_properties(0).total_memory / 1024**3
                logger.info(f"GPU detected: {gpu_name} ({gpu_memory:.1f} GB)")
            else:
                logger.info("No GPU detected, using CPU")
            
            # Load tokenizer with left padding (required for last-token pooling)
            self._tokenizer = AutoTokenizer.from_pretrained(
                self.model,
                padding_side='left',
                trust_remote_code=True
            )
            
            # Load model with INT8 quantization on GPU for 2-3x speedup
            if device == "cuda":
                # Check GPU compute capability (Flash Attention 2 needs Ampere+ = 8.0+)
                gpu_compute_capability = torch.cuda.get_device_capability(0)
                gpu_cc = float(f"{gpu_compute_capability[0]}.{gpu_compute_capability[1]}")
                supports_flash_attn = gpu_cc >= 8.0  # Ampere (RTX 3000+) or newer
                
                if not supports_flash_attn:
                    logger.info(f"GPU compute capability {gpu_cc} < 8.0 (Flash Attention 2 requires Ampere/RTX 3000+ or newer)")
                
                try:
                    from transformers import BitsAndBytesConfig
                    
                    quantization_config = BitsAndBytesConfig(
                        load_in_8bit=True,
                        bnb_8bit_compute_dtype=torch.bfloat16  # Use BF16 for compute
                    )
                    
                    # Only try Flash Attention 2 on Ampere+ GPUs
                    if supports_flash_attn:
                        try:
                            self._local_model = AutoModel.from_pretrained(
                                self.model,
                                quantization_config=quantization_config,
                                device_map="auto",  # Automatically place on GPU
                                attn_implementation='flash_attention_2',
                                trust_remote_code=True
                            )
                            logger.info("Model loaded with INT8 quantization + Flash Attention 2 (3-4x faster)")
                        except Exception as e:
                            logger.warning(f"Flash Attention 2 failed ({e}), using standard attention")
                            self._local_model = AutoModel.from_pretrained(
                                self.model,
                                quantization_config=quantization_config,
                                device_map="auto",
                                trust_remote_code=True
                            )
                            logger.info("Model loaded with INT8 quantization (2-3x faster)")
                    else:
                        # RTX 2080 and older: INT8 without Flash Attention
                        self._local_model = AutoModel.from_pretrained(
                            self.model,
                            quantization_config=quantization_config,
                            device_map="auto",
                            trust_remote_code=True
                        )
                        logger.info("Model loaded with INT8 quantization (2-3x faster)")
                    
                except (ImportError, Exception) as e:
                    if "bitsandbytes" in str(e).lower() or isinstance(e, ImportError):
                        logger.warning("bitsandbytes not available, falling back to BF16")
                    else:
                        logger.warning(f"INT8 quantization failed: {e}, falling back to BF16")
                    
                    # Fallback to BF16 without quantization
                    try:
                        self._local_model = AutoModel.from_pretrained(
                            self.model,
                            dtype=torch.bfloat16,  # Use 'dtype' instead of deprecated 'torch_dtype'
                            attn_implementation='flash_attention_2',
                            trust_remote_code=True
                        ).to(device)
                        logger.info("Model loaded with BF16 + Flash Attention 2")
                    except Exception as e:
                        if "flash_attn" in str(e).lower():
                            logger.warning("Flash Attention 2 not available, using eager attention")
                            self._local_model = AutoModel.from_pretrained(
                                self.model,
                                dtype=torch.bfloat16,
                                trust_remote_code=True
                            ).to(device)
                            logger.info("Model loaded with BF16 (standard attention)")
                        else:
                            raise
            else:
                # CPU: use FP32
                self._local_model = AutoModel.from_pretrained(
                    self.model,
                    dtype=torch.float32,  # Use 'dtype' instead of deprecated 'torch_dtype'
                    trust_remote_code=True
                ).to(device)
                logger.info("Model loaded with FP32 (CPU)")
            
            # Get embedding dimension from model config
            self._dimension = self._local_model.config.hidden_size
            
            logger.info(
                f"Local model loaded: {self.model}, "
                f"dimension: {self._dimension}, "
                f"device: {device}"
            )
        
        except Exception as e:
            logger.error(f"Error loading local model: {e}")
            raise
    
    def _last_token_pool(self, last_hidden_states: torch.Tensor, attention_mask: torch.Tensor) -> torch.Tensor:
        """
        Last-token pooling for Jina Code Embeddings.
        Required for models that use causal LM training.
        """
        left_padding = (attention_mask[:, -1].sum() == attention_mask.shape[0])
        if left_padding:
            return last_hidden_states[:, -1]
        else:
            sequence_lengths = attention_mask.sum(dim=1) - 1
            batch_size = last_hidden_states.shape[0]
            return last_hidden_states[torch.arange(batch_size, device=last_hidden_states.device), sequence_lengths]
    
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
    
    async def embed_texts(self, texts: List[str], is_query: bool = False, task: str = None) -> List[np.ndarray]:
        """
        Generate embeddings for a list of texts.
        Handles batching automatically.
        
        Args:
            texts: List of text strings to embed
            is_query: True if embedding search queries, False if embedding code chunks/passages
            task: Task type (qa, nl2code, code2code) or None for auto-detect
        
        Returns:
            List of embedding vectors as numpy arrays
        """
        if not texts:
            return []
        
        try:
            if self.provider == "local":
                return await self._embed_local(texts, is_query, task)
            else:
                return await self._embed_api(texts)
        
        except Exception as e:
            logger.error(f"Error generating embeddings: {e}")
            # Return zero vectors as fallback
            return [np.zeros(self.dimension, dtype=np.float32) for _ in texts]
    
    async def _embed_local(self, texts: List[str], is_query: bool = False, task: str = None) -> List[np.ndarray]:
        """Generate embeddings using local model with instruction prefixes and last-token pooling."""
        if not self._local_model or not self._tokenizer:
            raise RuntimeError("Local model not initialized")
        
        # Auto-detect task for queries if not specified
        if is_query and task is None and len(texts) == 1:
            task = detect_task_from_query(texts[0])
            logger.debug(f"Auto-detected task: {task} for query: {texts[0][:50]}...")
        
        # Use default task if still not set
        if task is None:
            task = DEFAULT_TASK
        
        # Get instruction config for the selected task
        config = INSTRUCTION_CONFIGS.get(task, INSTRUCTION_CONFIGS[DEFAULT_TASK])
        
        # Add instruction prefix based on query vs passage
        instruction = config["query"] if is_query else config["passage"]
        texts_with_instruction = [f"{instruction}{text}" for text in texts]
        
        # Run in executor to not block event loop
        loop = asyncio.get_event_loop()
        
        # Process in batches
        all_embeddings = []
        total_batches = (len(texts_with_instruction) + self.batch_size - 1) // self.batch_size
        logger.debug(f"Generating embeddings for {len(texts)} texts in {total_batches} batches")
        
        # Adaptive timeout: longer for first batch (model warm-up/GPU initialization)
        first_batch_timeout = 120.0  # 2 minutes for first batch
        normal_timeout = 60.0  # 1 minute for subsequent batches
        
        def encode_batch(batch_texts):
            """Encode a batch using transformers with last-token pooling."""
            # Tokenize
            batch_dict = self._tokenizer(
                batch_texts,
                padding=True,
                truncation=True,
                max_length=8192,  # Keep at 8192 for RTX 2080 constraint
                return_tensors="pt",
            )
            batch_dict = {k: v.to(self._device) for k, v in batch_dict.items()}
            
            # Generate embeddings
            with torch.no_grad():
                outputs = self._local_model(**batch_dict)
                # Use last-token pooling instead of mean pooling
                embeddings = self._last_token_pool(outputs.last_hidden_state, batch_dict['attention_mask'])
            
            return embeddings
        
        for i in range(0, len(texts_with_instruction), self.batch_size):
            batch = texts_with_instruction[i:i + self.batch_size]
            batch_num = i // self.batch_size + 1
            
            # Use longer timeout for first batch (GPU warm-up)
            timeout = first_batch_timeout if batch_num == 1 else normal_timeout
            
            # Run encoding in thread pool with cancellation support
            try:
                import time
                start_time = time.time()
                
                embeddings = await asyncio.wait_for(
                    loop.run_in_executor(None, encode_batch, batch),
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
                embeddings_np = embeddings.cpu().numpy().astype(np.float32)
                all_embeddings.extend([emb for emb in embeddings_np])
                
                # Clear GPU cache after each batch to prevent fragmentation
                if torch.cuda.is_available():
                    torch.cuda.empty_cache()
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
    
    async def embed_single(self, text: str, is_query: bool = False, task: str = None) -> np.ndarray:
        """Embed a single text string."""
        embeddings = await self.embed_texts([text], is_query=is_query, task=task)
        return embeddings[0] if embeddings else np.zeros(self.dimension, dtype=np.float32)

