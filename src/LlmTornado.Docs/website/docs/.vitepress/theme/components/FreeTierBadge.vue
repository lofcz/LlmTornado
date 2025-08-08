<template>
  <div class="free-tier-badge">
    <span class="free-tier-icon">🎁</span>
    <span class="free-tier-text">Free Tier Available</span>
    <div class="free-tier-tooltip">
      <div><strong>Models:</strong> {{ models.join(', ') }}</div>
      <div><strong>Limits:</strong> {{ limits }}</div>
      <div><strong>Quota:</strong> {{ quota }}</div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'FreeTierBadge',
  props: {
    models: {
      type: Array,
      required: true
    },
    limits: {
      type: String,
      required: true
    },
    quota: {
      type: String,
      required: true
    }
  }
}
</script>

<style scoped>
.free-tier-badge {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.25rem 0.75rem;
  background: rgba(74, 222, 128, 0.1);
  border: 1px solid rgba(74, 222, 128, 0.3);
  border-radius: 16px;
  font-size: 0.875rem;
  color: #4ade80;
  cursor: help;
}

.free-tier-badge:hover .free-tier-tooltip {
  visibility: visible;
  opacity: 1;
}

.free-tier-tooltip {
  position: absolute;
  bottom: 100%;
  left: 0;
  width: 220px;
  padding: 0.75rem;
  background: var(--vp-c-bg);
  border: 1px solid var(--vp-c-divider);
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  visibility: hidden;
  opacity: 0;
  transition: all 0.2s ease;
  z-index: 10;
}

.free-tier-tooltip div {
  margin-bottom: 0.25rem;
}

.free-tier-tooltip div:last-child {
  margin-bottom: 0;
}

.free-tier-tooltip strong {
  color: var(--vp-c-text-1);
}
</style>
