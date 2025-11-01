"""
Resource management to keep the system lightweight and responsive.
Monitors CPU and memory usage, throttles operations when necessary.
"""

import asyncio
import psutil
from typing import Optional
from loguru import logger
from ..config import Settings


class ResourceManager:
    """Manages system resources to prevent overload."""
    
    def __init__(self, settings: Settings):
        self.settings = settings
        self.max_cpu_percent = settings.resource.max_cpu_percent
        self.max_memory_mb = settings.resource.max_memory_mb
        self.process = psutil.Process()
        self._monitoring = False
        self._monitor_task: Optional[asyncio.Task] = None
        
        # Stats
        self.current_cpu_percent = 0.0
        self.current_memory_mb = 0.0
        self._should_throttle = False
        
        # UI interaction tracking (for responsiveness)
        # Use threading.Lock for cross-thread safety (Qt thread vs async thread)
        import threading
        self._ui_active = False
        self._ui_active_lock = threading.Lock()
    
    async def start_monitoring(self):
        """Start background monitoring of resources."""
        if self._monitoring:
            return
        
        self._monitoring = True
        self._monitor_task = asyncio.create_task(self._monitor_loop())
        logger.info("Resource monitoring started")
    
    async def stop_monitoring(self):
        """Stop resource monitoring."""
        self._monitoring = False
        if self._monitor_task:
            self._monitor_task.cancel()
            try:
                await self._monitor_task
            except asyncio.CancelledError:
                pass
        logger.info("Resource monitoring stopped")
    
    async def _monitor_loop(self):
        """Background task to monitor resource usage."""
        while self._monitoring:
            try:
                # CPU usage
                self.current_cpu_percent = self.process.cpu_percent(interval=0.1)
                
                # Memory usage
                mem_info = self.process.memory_info()
                self.current_memory_mb = mem_info.rss / 1024 / 1024
                
                # Determine if we should throttle
                self._should_throttle = (
                    self.current_cpu_percent > self.max_cpu_percent or
                    self.current_memory_mb > self.max_memory_mb
                )
                
                if self._should_throttle:
                    logger.debug(
                        f"Throttling: CPU {self.current_cpu_percent:.1f}% "
                        f"(max {self.max_cpu_percent}%), "
                        f"Memory {self.current_memory_mb:.1f}MB "
                        f"(max {self.max_memory_mb}MB)"
                    )
                
                # Check every 2 seconds
                await asyncio.sleep(2.0)
            
            except Exception as e:
                logger.error(f"Error in resource monitoring: {e}")
                await asyncio.sleep(5.0)
    
    async def wait_if_throttled(self, yield_anyway: bool = True):
        """
        Wait if system resources are over limits or UI is active.
        Always yields control to allow other tasks to run.
        
        Args:
            yield_anyway: Even if not throttled, yield control briefly
        """
        # Check if UI is active - prioritize responsiveness
        # Fast read with thread-safe lock
        with self._ui_active_lock:
            ui_active = self._ui_active
        
        if ui_active:
            # UI is active - pause indexing to keep UI responsive
            await asyncio.sleep(0.1)  # 100ms pause for UI responsiveness
        elif self._should_throttle:
            # Wait longer when throttled
            await asyncio.sleep(0.5)
        elif yield_anyway:
            # Always yield control to be responsive
            await asyncio.sleep(0)
    
    def set_ui_active(self, active: bool):
        """Set UI interaction state. Called by GUI when user interacts."""
        # Thread-safe - can be called from Qt thread
        with self._ui_active_lock:
            self._ui_active = active
    
    def is_ui_active(self) -> bool:
        """Check if UI is currently active."""
        with self._ui_active_lock:
            return self._ui_active
    
    def should_throttle(self) -> bool:
        """Check if operations should be throttled."""
        return self._should_throttle
    
    def get_optimal_worker_count(self) -> int:
        """Get optimal number of workers based on CPU cores."""
        if self.settings.resource.max_workers is not None:
            return self.settings.resource.max_workers
        
        # Use half of CPU cores for indexing, min 1, max 4
        cpu_count = psutil.cpu_count(logical=False) or 2
        return max(1, min(4, cpu_count // 2))
    
    def get_stats(self) -> dict:
        """Get current resource usage statistics."""
        return {
            "cpu_percent": round(self.current_cpu_percent, 1),
            "memory_mb": round(self.current_memory_mb, 1),
            "throttled": self._should_throttle,
            "max_cpu_percent": self.max_cpu_percent,
            "max_memory_mb": self.max_memory_mb,
        }
    
    async def __aenter__(self):
        """Context manager entry."""
        await self.start_monitoring()
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        await self.stop_monitoring()

