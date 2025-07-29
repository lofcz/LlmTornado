<template>
  <div class="mystical-landing">
    <!-- Full page blue-gray mystical background -->
    <div class="mystical-background">
      <!-- Subtle moondust shimmer on sides -->
      <div class="moondust-left"></div>
      <div class="moondust-right"></div>
    </div>

    <!-- Central Eclipse Panel -->
    <div class="eclipse-panel">
      <!-- Core Features -->
      <section class="core-features">
        <div class="section-header">
          <h2 class="mystical-section-title">Built for the <span class="power-text">Future</span> of Development</h2>
          <p class="mystical-section-desc">
            Every line of code crafted with precision. Every API call optimized for performance. 
            Every feature designed to amplify your creative potential.
          </p>
        </div>

        <div class="features-constellation">
          <div class="feature-orb">
            <div class="orb-core">
              <div class="orb-icon">🌙</div>
            </div>
            <div class="orb-content">
              <h3>Lunar Intelligence</h3>
              <p>Harness the mystical power of AI with intuitive patterns that feel natural and powerful.</p>
            </div>
          </div>

          <div class="feature-orb">
            <div class="orb-core">
              <div class="orb-icon">⚡</div>
            </div>
            <div class="orb-content">
              <h3>Lightning Velocity</h3>
              <p>Experience blazing-fast performance with intelligent caching and async patterns.</p>
            </div>
          </div>

          <div class="feature-orb">
            <div class="orb-core">
              <div class="orb-icon">🔮</div>
            </div>
            <div class="orb-content">
              <h3>Infinite Possibilities</h3>
              <p>Adapt to any use case with extensible patterns and mystical flexibility.</p>
            </div>
          </div>

          <div class="feature-orb">
            <div class="orb-core">
              <div class="orb-icon">🌟</div>
            </div>
            <div class="orb-content">
              <h3>Stellar Architecture</h3>
              <p>Built with cosmic foresight for the future of AI development and integration.</p>
            </div>
          </div>

          <div class="feature-orb">
            <div class="orb-core">
              <div class="orb-icon">🔗</div>
            </div>
            <div class="orb-content">
              <h3>Function Calling</h3>
              <p>Seamlessly integrate external tools and APIs with native function calling support.</p>
            </div>
          </div>

          <div class="feature-orb">
            <div class="orb-core">
              <div class="orb-icon">🌊</div>
            </div>
            <div class="orb-content">
              <h3>Streaming Capabilities</h3>
              <p>Deliver real-time results with powerful, easy-to-use streaming APIs.</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Mystical Code Showcase -->
      <section class="code-mystique">
        <div class="code-altar">
          <div class="code-header">
            <div class="code-orbs">
              <div class="code-orb red"></div>
              <div class="code-orb amber"></div>
              <div class="code-orb emerald"></div>
            </div>
            <span class="code-filename">main.cs</span>
          </div>
          <div class="code-content">
            <pre class="mystical-code"><code><span class="code-keyword">var</span> <span class="code-var">api</span> <span class="code-op">=</span> <span class="code-keyword">new</span> <span class="code-class">TornadoApi</span><span class="code-punct">();</span>

<span class="code-keyword">var</span> <span class="code-var">response</span> <span class="code-op">=</span> <span class="code-keyword">await</span> <span class="code-var">api</span><span class="code-punct">.</span><span class="code-prop">Chat</span>
  <span class="code-punct">.</span><span class="code-method">CreateChatCompletionAsync</span><span class="code-punct">(</span>
    <span class="code-string">"Tell me about quantum computing"</span>
  <span class="code-punct">);</span>

<span class="code-comment">// Intelligence at your fingertips</span></code></pre>
          </div>
        </div>
      </section>

      <!-- Cosmic Statistics -->
      <section class="cosmic-stats">
        <div class="stats-constellation">
          <div class="stat-orb">
            <div class="stat-number">
              <div>99.9%</div>
            </div>
            <div class="stat-label">Cosmic Reliability</div>
            <div class="stat-aura"></div>
          </div>
          <div class="stat-orb">
            <div class="stat-number">
              <div>50ms</div>
            </div>
            <div class="stat-label">Lightning Speed</div>
            <div class="stat-aura"></div>
          </div>
          <div class="stat-orb">
            <div class="stat-number">
              <div>∞</div>
            </div>
            <div class="stat-label">Infinite Power</div>
            <div class="stat-aura"></div>
          </div>
        </div>
      </section>

      <!-- Developer Testimonials -->
      <section class="testimonials-mystique">
        <div class="section-header">
          <h2 class="mystical-section-title">Voices from the <span class="power-text">Digital Realm</span></h2>
        </div>
        <div class="testimonials-grid">
          <div class="testimonial-card">
            <div class="testimonial-content">
              "LlmTornado transformed our AI integration from complex chaos to elegant simplicity."
            </div>
            <div class="testimonial-author">
              <div class="author-avatar">🧙‍♂️</div>
              <div class="author-info">
                <div class="author-name">Alex Chen</div>
                <div class="author-title">Senior AI Engineer</div>
              </div>
            </div>
          </div>
          <div class="testimonial-card">
            <div class="testimonial-content">
              "The perfect balance of power and elegance. It's like having AI magic at your fingertips."
            </div>
            <div class="testimonial-author">
              <div class="author-avatar">🌟</div>
              <div class="author-info">
                <div class="author-name">Sarah Kim</div>
                <div class="author-title">Tech Lead</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Mystical Call to Action -->
      <section class="mystical-cta">
        <div class="cta-content">
          <h2 class="cta-title">Ready to Harness the <span class="power-text">Primordial Power</span>?</h2>
          <p class="cta-description">
            Join the developers who've discovered the perfect balance of cosmic power and elegant simplicity in AI development.
          </p>
          <div class="cta-buttons">
            <a href="/getting-started" class="btn-mystical primary large">
              <span class="btn-text">Begin Your Journey</span>
              <div class="btn-aura"></div>
            </a>
            <a href="https://github.com/lofcz/LlmTornado" class="btn-mystical secondary large">
              <span class="btn-text">Explore the Code</span>
              <div class="btn-aura"></div>
            </a>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
/* Mystical Animations */
@keyframes mystical-float {
  0%, 100% { transform: translateY(0px) rotate(0deg) scale(1); }
  33% { transform: translateY(-15px) rotate(120deg) scale(1.05); }
  66% { transform: translateY(-8px) rotate(240deg) scale(0.95); }
}

@keyframes cosmic-pulse {
  0%, 100% { opacity: 0.8; transform: scale(1); }
  50% { opacity: 0.8; transform: scale(1.1); }
}

@keyframes aurora-flow {
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
}

@keyframes golden-glow {
  0%, 100% { box-shadow: 0 0 20px rgba(251, 191, 36, 0.3); }
  50% { box-shadow: 0 0 40px rgba(251, 191, 36, 0.6), 0 0 60px rgba(251, 191, 36, 0.2); }
}

/* Base Mystical Layout */
.mystical-landing {
  position: relative;
  min-height: 100vh;
  overflow-x: hidden;
}

/* Full Page Mystical Background */
.mystical-background {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: linear-gradient(180deg, 
    rgba(30, 41, 59, 0.2) 0%, 
    rgba(30, 41, 59, 0.5) 25%, 
    #1e293b 50%, 
    #0f172a 75%, 
    #020617 100%);
  z-index: -2;
}

/* Subtle Moondust Shimmer */
.moondust-left {
  position: absolute;
  top: 0;
  left: 0;
  width: 200px;
  height: 100%;
  background: linear-gradient(90deg, 
    rgba(148, 163, 184, 0.03) 0%, 
    rgba(148, 163, 184, 0.01) 50%, 
    transparent 100%);
  animation: mystical-float 60s ease-in-out infinite;
}

.moondust-right {
  position: absolute;
  top: 0;
  right: 0;
  width: 200px;
  height: 100%;
  background: linear-gradient(270deg, 
    rgba(148, 163, 184, 0.03) 0%, 
    rgba(148, 163, 184, 0.01) 50%, 
    transparent 100%);
  animation: mystical-float 70s ease-in-out infinite reverse;
}

/* Central Eclipse Panel */
.eclipse-panel {
  position: relative;
  max-width: 1200px;
  margin: 0 auto;
  background: linear-gradient(180deg,
    rgba(30, 41, 59, 0.1) 0%,
    rgba(30, 41, 59, 0.3) 30%,
    rgba(30, 41, 59, 0.4) 50%,
    rgba(15, 23, 42, 0.6) 70%,
    rgba(2, 6, 23, 0.8) 100%);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(148, 163, 184, 0.1);
  border-radius: 24px;
  margin-top: 0;
  margin-bottom: 2rem;
  overflow: hidden;
  z-index: 1;
}

.eclipse-panel::before {
  content: '';
  position: absolute;
  top: -1px;
  left: -1px;
  right: -1px;
  bottom: -1px;
  background: linear-gradient(45deg, rgba(59, 130, 246, 0.15), rgba(139, 92, 246, 0.15), rgba(71, 85, 105, 0.15), rgba(59, 130, 246, 0.15));
  border-radius: 24px;
  z-index: -1;
}

/* Hero Section */
.hero-section {
  padding: 4rem 2rem;
  text-align: center;
}

.mystical-title {
  font-size: 4rem;
  font-weight: 900;
  background: linear-gradient(135deg, #f8fafc 0%, #cbd5e1 50%, #94a3b8 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 1rem;
  text-shadow: 0 0 30px rgba(248, 250, 252, 0.3);
}

.mystical-subtitle {
  font-size: 2.5rem;
  font-weight: 700;
  color: #e2e8f0;
  margin-bottom: 2rem;
  opacity: 0.9;
}

.mystical-description {
  font-size: 1.25rem;
  color: #94a3b8;
  max-width: 600px;
  margin: 0 auto 3rem;
  line-height: 1.7;
}

.hero-buttons {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

/* Latest Update Banner */
.latest-update-section {
  padding: 0 2rem 2rem;
  display: flex;
  justify-content: center;
}

.update-banner {
  position: relative;
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 2rem;
  background: linear-gradient(135deg, rgba(251, 191, 36, 0.1) 0%, rgba(245, 158, 11, 0.15) 100%);
  border: 1px solid rgba(251, 191, 36, 0.2);
  border-radius: 12px;
  backdrop-filter: blur(8px);
  animation: golden-glow 3s ease-in-out infinite;
}

.update-icon {
  font-size: 1.5rem;
  color: #fbbf24;
}

.update-content {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.update-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: #fbbf24;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.update-text {
  color: #e2e8f0;
  font-size: 0.875rem;
}

/* Section Headers */
.section-header {
  text-align: center;
  margin-bottom: 3rem;
}

.mystical-section-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: #f8fafc;
  margin-bottom: 1rem;
}

.power-text {
  background: linear-gradient(135deg, #3b82f6, #8b5cf6);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.elegance-text {
  background: linear-gradient(135deg, #60a5fa, #a78bfa);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.mystical-section-desc {
  font-size: 1.125rem;
  color: #94a3b8;
  max-width: 700px;
  margin: 0 auto;
  line-height: 1.6;
}

/* Features Section */
.primordial-features {
  padding: 3rem 2rem;
}

.features-constellation {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 2rem;
  max-width: 1000px;
  margin: 0 auto;
}

.feature-orb {
  position: relative;
  padding: 2rem;
  background: radial-gradient(circle at center, rgba(30, 41, 59, 0.4) 0%, rgba(15, 23, 42, 0.6) 100%);
  border: 1px solid rgba(148, 163, 184, 0.1);
  border-radius: 16px;
  backdrop-filter: blur(12px);
  transition: all 0.3s ease;
  text-align: center;
}

.feature-orb:hover {
  transform: translateY(-8px);
  border-color: rgba(148, 163, 184, 0.2);
  background: radial-gradient(circle at center, rgba(30, 41, 59, 0.1) 0%, rgba(15, 23, 42, 0.7) 100%);
}

.orb-core {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 4rem;
  height: 4rem;
  margin: 0 auto 1.5rem;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.2) 0%, rgba(139, 92, 246, 0.1) 100%);
  border-radius: 50%;
  animation: cosmic-pulse 4s ease-in-out infinite;
}

.orb-icon {
  font-size: 2rem;
}

.orb-content h3 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #f1f5f9;
  margin-bottom: 0.75rem;
}

.orb-content p {
  color: #94a3b8;
  line-height: 1.6;
}

/* Code Section */
.code-mystique {
  padding: 3rem 2rem;
}

.code-altar {
  max-width: 600px;
  margin: 0 auto;
  background: radial-gradient(ellipse at center, rgba(15, 23, 42, 0.8) 0%, rgba(2, 6, 23, 0.9) 100%);
  border: 1px solid rgba(148, 163, 184, 0.15);
  border-radius: 16px;
  overflow: hidden;
  backdrop-filter: blur(16px);
}

.code-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.5rem;
  background: rgba(2, 6, 23, 0.6);
  border-bottom: 1px solid rgba(148, 163, 184, 0.1);
}

.code-orbs {
  display: flex;
  gap: 0.5rem;
}

.code-orb {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
  opacity: 0.7;
}

.code-orb.red { background: #ef4444; }
.code-orb.amber { background: #f59e0b; }
.code-orb.emerald { background: #64748b; }

.code-filename {
  color: #94a3b8;
  font-size: 0.875rem;
  font-family: 'JetBrains Mono', monospace;
}

.code-content {
  padding: 2rem;
}

.mystical-code {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-size: 0.875rem;
  line-height: 1.6;
}

.code-keyword { color: #c084fc; }
.code-var { color: #e2e8f0; }
.code-op { color: #94a3b8; }
.code-class { color: #fbbf24; }
.code-punct { color: #94a3b8; }
.code-prop { color: #60a5fa; }
.code-method { color: #34d399; }
.code-string { color: #fb7185; }
.code-comment { color: #64748b; }

/* Stats Section */
.cosmic-stats {
  padding: 3rem 2rem;
}

.stats-constellation {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 2rem;
  max-width: 800px;
  margin: 0 auto;
}

.stat-orb {
  position: relative;
  text-align: center;
  padding: 2.5rem 1.5rem;
  background: radial-gradient(circle at center, rgba(30, 41, 59, 0.2) 0%, rgba(15, 23, 42, 0.4) 100%);
  border: 1px solid rgba(148, 163, 184, 0.08);
  border-radius: 20px;
  backdrop-filter: blur(12px);
  transition: all 0.3s ease;
}

.stat-orb:hover {
  transform: translateY(-6px);
  border-color: rgba(148, 163, 184, 0.15);
}

.stat-number {
  font-size: 3rem;
  font-weight: 900;
  background: linear-gradient(135deg, #f8fafc 0%, #cbd5e1 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
  display: flex;
  justify-content: center;
  min-height: 40px;
}

.stat-number > div {
  margin-top: auto;
  margin-bottom: auto;
}

.stat-label {
  color: #94a3b8;
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.stat-aura {
  position: absolute;
  top: 50%;
  left: 50%;
  width: 100%;
  height: 100%;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.1) 0%, transparent 70%);
  border-radius: 20px;
  transform: translate(-50%, -50%);
  animation: cosmic-pulse 3s ease-in-out infinite;
  z-index: -1;
}

/* Testimonials */
.testimonials-mystique {
  padding: 3rem 2rem;
}

.testimonials-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 2rem;
  max-width: 800px;
  margin: 0 auto;
}

.testimonial-card {
  padding: 2rem;
  background: radial-gradient(ellipse at top, rgba(30, 41, 59, 0.3) 0%, rgba(15, 23, 42, 0.5) 100%);
  border: 1px solid rgba(148, 163, 184, 0.1);
  border-radius: 16px;
  backdrop-filter: blur(12px);
}

.testimonial-content {
  color: #e2e8f0;
  font-size: 1.125rem;
  line-height: 1.6;
  margin-bottom: 1.5rem;
  font-style: italic;
}

.testimonial-author {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.author-avatar {
  font-size: 2rem;
}

.author-info {
  display: flex;
  flex-direction: column;
}

.author-name {
  color: #f1f5f9;
  font-weight: 600;
}

.author-title {
  color: #94a3b8;
  font-size: 0.875rem;
}

/* CTA Section */
.mystical-cta {
  padding: 4rem 2rem;
  text-align: center;
}

.cta-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: #f8fafc;
  margin-bottom: 1.5rem;
}

.cta-description {
  font-size: 1.125rem;
  color: #94a3b8;
  max-width: 600px;
  margin: 0 auto 3rem;
  line-height: 1.6;
}

.cta-buttons {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

/* Mystical Buttons */
.btn-mystical {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.875rem 2rem;
  border-radius: 12px;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.3s ease;
  backdrop-filter: blur(12px);
  overflow: hidden;
}

.btn-mystical.primary {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.2) 0%, rgba(139, 92, 246, 0.2) 100%);
  border: 1px solid rgba(59, 130, 246, 0.3);
  color: #f8fafc;
}

.btn-mystical.primary:hover {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.3) 0%, rgba(139, 92, 246, 0.3) 100%);
  border-color: rgba(59, 130, 246, 0.5);
  transform: translateY(-2px);
  box-shadow: 0 10px 25px -5px rgba(59, 130, 246, 0.3);
}

.btn-mystical.secondary {
  background: rgba(15, 23, 42, 0.6);
  border: 1px solid rgba(71, 85, 105, 0.3);
  color: #cbd5e1;
}

.btn-mystical.secondary:hover {
  background: rgba(30, 41, 59, 0.6);
  border-color: rgba(148, 163, 184, 0.4);
  transform: translateY(-2px);
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.2);
}

.btn-mystical.large {
  padding: 1rem 2.5rem;
  font-size: 1.125rem;
}

.btn-text {
  position: relative;
  z-index: 2;
}

.btn-aura {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: radial-gradient(circle at center, rgba(59, 130, 246, 0.1) 0%, transparent 70%);
  opacity: 0;
  transition: opacity 0.3s ease;
}

.btn-mystical:hover .btn-aura {
  opacity: 1;
}

/* Responsive Design */
@media (max-width: 768px) {
  .mystical-title {
    font-size: 2.5rem;
  }
  
  .mystical-subtitle {
    font-size: 1.75rem;
  }
  
  .mystical-section-title {
    font-size: 2rem;
  }
  
  .cta-title {
    font-size: 2rem;
  }
  
  .eclipse-panel {
    margin: 1rem;
    border-radius: 16px;
  }
  
  .hero-buttons,
  .cta-buttons {
    flex-direction: column;
    align-items: center;
  }
}


@media (max-width: 1200px) {
  .is-home .vp-doc.container {
    padding: 0 !important;
  }

  .is-home .eclipse-panel::before {
    border-radius: 0 !important;
  }

  .is-home .eclipse-panel {
    margin: 0 !important;
  }

  .is-home .core-features {
    padding-left: 15px;
    padding-right: 15px;
  }

  .code-mystique {
    padding: 2rem 15px !important;
  }

  .testimonials-mystique {
    padding: 0 15px !important;
  }

  .testimonials-grid {
    gap: 1rem !important;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)) !important;
  }
  
}
</style>
