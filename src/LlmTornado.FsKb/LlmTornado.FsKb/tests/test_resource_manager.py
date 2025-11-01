"""Tests for resource management."""

import pytest
import asyncio
from fskb.config import Settings
from fskb.utils import ResourceManager


@pytest.fixture
def settings():
    """Create settings for testing."""
    settings = Settings()
    settings.resource.max_cpu_percent = 50.0
    settings.resource.max_memory_mb = 1024
    return settings


@pytest.mark.asyncio
async def test_resource_manager_initialization(settings):
    """Test resource manager initialization."""
    manager = ResourceManager(settings)
    
    assert manager.max_cpu_percent == 50.0
    assert manager.max_memory_mb == 1024


@pytest.mark.asyncio
async def test_resource_monitoring(settings):
    """Test resource monitoring."""
    manager = ResourceManager(settings)
    
    await manager.start_monitoring()
    
    # Wait a bit for monitoring to start
    await asyncio.sleep(2.5)
    
    # Check stats are being collected
    stats = manager.get_stats()
    assert "cpu_percent" in stats
    assert "memory_mb" in stats
    assert stats["cpu_percent"] >= 0
    assert stats["memory_mb"] > 0
    
    await manager.stop_monitoring()


@pytest.mark.asyncio
async def test_optimal_worker_count(settings):
    """Test optimal worker count calculation."""
    manager = ResourceManager(settings)
    
    count = manager.get_optimal_worker_count()
    assert count >= 1
    assert count <= 4


@pytest.mark.asyncio
async def test_wait_if_throttled(settings):
    """Test throttling behavior."""
    manager = ResourceManager(settings)
    
    # Should not block when not throttled
    await manager.wait_if_throttled()
    
    # Should always yield
    await manager.wait_if_throttled(yield_anyway=True)

