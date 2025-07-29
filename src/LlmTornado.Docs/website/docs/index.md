---
layout: home

hero:
  name: "LlmTornado"
  text: "Your Gateway to AI."
  tagline: Harness the power of generative AI with a library that's as powerful as it is elegant. Built for developers who want to innovate without compromise.
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/lofcz/LlmTornado

features:
  - title: "⚡ Intuitive Design"
    details: A clean, modern API that gets you from idea to implementation in record time. Experience the perfect balance of simplicity and power.
  - title: "🚀 Performance-Optimized"
    details: Engineered for speed and efficiency with advanced caching, streaming, and async patterns. Your applications will fly.
  - title: "🔮 Extensible & Flexible"
    details: Easily customizable to fit the unique needs of your projects. From simple chatbots to complex AI workflows.
---

<style>
:root {
  --vp-home-hero-name-color: transparent;
  --vp-home-hero-name-background: -webkit-linear-gradient(120deg, #f8fafc 10%, #e2e8f0 50%, #cbd5e1 90%);
  
  --vp-home-hero-image-background-image: linear-gradient(-45deg, #0f172a 0%, #1e293b 25%, #334155 50%, #475569 75%);
  --vp-home-hero-image-filter: blur(44px);
}

@media (min-width: 640px) {
  :root {
    --vp-home-hero-image-filter: blur(56px);
  }
}

@media (min-width: 960px) {
  :root {
    --vp-home-hero-image-filter: blur(68px);
  }
}

/* Dark luxury hero styling */
.VPHero {
  background: 
    radial-gradient(ellipse at top, rgba(15, 23, 42, 0.9) 0%, rgba(15, 23, 42, 1) 70%),
    linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%);
  position: relative;
  overflow: hidden;
}

.VPHero::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: 
    radial-gradient(circle at 25% 25%, rgba(148, 163, 184, 0.05) 0%, transparent 50%),
    radial-gradient(circle at 75% 75%, rgba(148, 163, 184, 0.03) 0%, transparent 50%);
  animation: mysticalPulse 8s ease-in-out infinite alternate;
}

@keyframes mysticalPulse {
  0% { opacity: 0.3; }
  100% { opacity: 0.7; }
}

.VPHero .container {
  position: relative;
  z-index: 1;
}

/* Elegant feature cards */
.VPFeatures {
  background: linear-gradient(180deg, rgba(15, 23, 42, 0.8), transparent 100%) !important;
  padding: 2rem 0 4rem 0;
}

.VPFeature {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
}

.VPFeatures .VPFeature {
   border: none;
}

.VPFeature::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 1px;
  background: linear-gradient(90deg, transparent, rgba(148, 163, 184, 0.4), transparent);
  opacity: 0;
  transition: opacity 0.3s ease;
}

.VPFeature:hover::before {
  opacity: 1;
}

.VPFeature:hover {
  transform: translateY(-4px);
  background: rgba(51, 65, 85, 0.8);
}

/* Refined typography */
.VPHero .name {
  font-weight: 800;
  letter-spacing: -0.025em;
  text-shadow: 0 0 30px rgba(248, 250, 252, 0.1);
}

.VPHero .text {
  font-weight: 500;
  background: linear-gradient(120deg, #cbd5e1, #94a3b8);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  text-shadow: none;
}

.VPHero .tagline {
  color: #94a3b8;
  font-size: 1.125rem;
  line-height: 1.7;
  max-width: 640px;
  margin: 0;
  font-weight: 400;
  text-align: left;
}

/* Sophisticated button styling */
.VPButton.brand {
  background: linear-gradient(135deg, rgba(148, 163, 184, 0.1), rgba(71, 85, 105, 0.2));
  border: 1px solid rgba(148, 163, 184, 0.2);
  color: #f8fafc;
  position: relative;
  overflow: hidden;
  backdrop-filter: blur(8px);
  transition: all 0.3s ease;
}

.VPButton.brand::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(248, 250, 252, 0.1), transparent);
  transition: left 0.6s ease;
}

.VPButton.brand:hover {
  background: linear-gradient(135deg, rgba(148, 163, 184, 0.15), rgba(71, 85, 105, 0.25));
  border-color: rgba(148, 163, 184, 0.3);
  transform: translateY(-1px);
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.3);
}

.VPButton.brand:hover::before {
  left: 100%;
}

.VPButton.alt {
  border: 1px solid rgba(71, 85, 105, 0.3);
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(8px);
  color: #cbd5e1;
  transition: all 0.3s ease;
}

.VPButton.alt:hover {
  border-color: rgba(148, 163, 184, 0.4);
  background: rgba(30, 41, 59, 0.6);
  transform: translateY(-1px);
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.2);
}

/* Feature content styling */
.VPFeature .title {
  color: #f1f5f9;
  font-weight: 600;
  margin-bottom: 0.75rem;
}

.VPFeature .details {
  color: #94a3b8;
  line-height: 1.6;
  font-weight: 400;
}
</style>

<LandingPageSections />
