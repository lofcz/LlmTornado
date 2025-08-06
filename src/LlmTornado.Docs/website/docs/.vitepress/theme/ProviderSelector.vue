<template>
  <div class="provider-selector">
    <div class="provider-tabs">
      <button 
        v-for="category in categories" 
        :key="category.id"
        :class="['tab-button', { active: selectedCategory === category.id }]"
        @click="selectedCategory = category.id"
      >
        <span class="tab-icon">{{ category.icon }}</span>
        {{ category.name }}
      </button>
    </div>

    <div class="provider-grid">
      <div 
        v-for="provider in filteredProviders" 
        :key="provider.id"
        :class="['provider-card', { active: selectedProvider === provider.id }]"
        @click="selectedProvider = provider.id"
      >
        <div class="provider-header">
          <div class="provider-icon">
            <span class="icon-emoji">{{ provider.icon }}</span>
          </div>
          <div class="provider-info">
            <h3>{{ provider.name }}</h3>
            <span class="provider-badge" :class="provider.type">
              {{ provider.type }}
            </span>
          </div>
        </div>
        
        <div class="provider-description">
          {{ provider.description }}
        </div>

        <div class="provider-free-tier" v-if="provider.freeTier">
          <FreeTierBadge 
            :models="provider.freeTier.models"
            :limits="provider.freeTier.limits"
            :quota="provider.freeTier.quota"
          />
        </div>

        <div class="provider-features" v-if="provider.features">
          <div class="feature-list">
            <span v-for="feature in provider.features" :key="feature" class="feature-tag">
              {{ feature }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <div class="code-snippet" v-if="selectedProvider">
      <div class="snippet-header">
        <h4>Connect to {{ selectedProviderName }}</h4>
        <div class="snippet-actions">
          <button @click="copyCode" class="copy-button" :class="{ copied: copied }">
            {{ copied ? 'Copied!' : 'Copy' }}
          </button>
        </div>
      </div>
      
      <div class="code-container">
        <pre><code class="language-csharp" v-html="highlightedCode"></code></pre>
      </div>

      <div class="additional-info" v-if="getProviderInfo().additionalInfo">
        <div class="info-section" v-for="(info, index) in getProviderInfo().additionalInfo" :key="index">
          <h5>{{ info.title }}</h5>
          <p>{{ info.content }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, watch } from 'vue'
import { useData } from 'vitepress'
import FreeTierBadge from './components/FreeTierBadge.vue'

export default {
  name: 'ProviderSelector',
  components: {
    FreeTierBadge
  },
  setup() {
    const { isDark } = useData()
    const selectedCategory = ref('cloud')
    const selectedProvider = ref(null)
    const copied = ref(false)

    const categories = [
      { id: 'cloud', name: 'Cloud Providers', icon: '☁️' },
      { id: 'local', name: 'Self-Hosted', icon: '🏠' },
      { id: 'api', name: 'API Services', icon: '🌐' }
    ]

    const providers = [
      // Cloud Providers
      {
        id: 'openai',
        name: 'OpenAI',
        type: 'cloud',
        category: 'cloud',
        icon: '🤖',
        description: 'GPT-4, GPT-3.5, and other advanced models',
        features: ['O Series', 'GPT-4', 'GPT-3.5', 'Multimodal'],
        code: `// Connect to OpenAI with API key
var api = new TornadoApi("your-openai-api-key", LLmProviders.OpenAi);

// Create conversation with streaming
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4o
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from the OpenAI dashboard at platform.openai.com/api-keys'
          },
          {
            title: 'Pricing',
            content: 'OpenAI offers pay-as-you-go pricing with different rates for each model'
          }
        ]
      },
      {
        id: 'google',
        name: 'Google',
        type: 'cloud',
        category: 'cloud',
        icon: '🔍',
        description: 'Gemini series models with multimodal capabilities',
        features: ['Multimodal', 'Reasoning', 'Context Window', 'Safety Features'],
        freeTier: {
          models: ['Gemini 1.0 Pro', 'Gemini 1.5 Flash'],
          limits: '60 requests/minute',
          quota: 'Monthly reset'
        },
        code: `// Connect to Google with API key
var api = new TornadoApi("your-google-api-key", LLmProviders.Google);

// Create conversation with streaming
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.Google.Gemini.Gemini15Pro
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from Google AI Studio at makersuite.google.com/app/apikey'
          },
          {
            title: 'Free Tier Details',
            content: '60 requests/minute for Gemini 1.0 Pro and 1.5 Flash models'
          }
        ]
      },
      {
        id: 'anthropic',
        name: 'Anthropic',
        type: 'cloud',
        category: 'cloud',
        icon: '🛡️',
        description: 'Claude models with strong safety features',
        features: ['Safety', 'Reasoning', 'Long Context', 'Helpful Responses'],
        code: `// Connect to Anthropic with API key
var api = new TornadoApi("your-anthropic-api-key", LLmProviders.Anthropic);

// Create conversation with streaming
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.Anthropic.Claude35Sonnet
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from the Anthropic Console at console.anthropic.com/'
          },
          {
            title: 'Safety Features',
            content: 'Claude models include built-in safety and constitutional AI features'
          }
        ]
      },
      {
        id: 'xAi',
        name: 'xAI',
        type: 'cloud',
        category: 'cloud',
        icon: '𝕏',
        description: 'Groks series models with reasoning capabilities',
        features: ['Reasoning', 'Large Context', 'Web Search', 'Multimodal'],
        code: `// Connect to xAI with API key
var api = new TornadoApi("your-xai-api-key", LLmProviders.XAi);

// Create conversation with Grok model
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.XAi.Grok.Grok2
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from the xAI platform'
          },
          {
            title: 'Model Capabilities',
            content: 'Grok models are designed for reasoning and web search integration'
          }
        ]
      },
      {
        id: 'cohere',
        name: 'Cohere',
        type: 'cloud',
        category: 'cloud',
        icon: '🌊',
        description: 'Command series models with strong reasoning',
        features: ['Reasoning', 'Reranking', 'Embeddings', 'Multilingual'],
        code: `// Connect to Cohere with API key
var api = new TornadoApi("your-cohere-api-key", LLmProviders.Cohere);

// Create conversation with Command model
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.Cohere.Command.CommandRPlus
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from the Cohere dashboard'
          },
          {
            title: 'Special Features',
            content: 'Cohere models excel at reasoning and have built-in reranking capabilities'
          }
        ]
      },
      
      // Self-Hosted Providers
      {
        id: 'ollama',
        name: 'Ollama',
        type: 'local',
        category: 'local',
        icon: '🦙',
        description: 'Local model deployment with various open-source models',
        features: ['Privacy', 'Offline', 'Customizable', 'No API Costs'],
        code: `// Connect to Ollama (default port 11434)
var api = new TornadoApi(new Uri("http://localhost:11434"));

// Create conversation with a local model
var conversation = api.Chat.CreateConversation(new ChatModel("llama3:8b"));

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'Installation',
            content: 'Install Ollama from ollama.com and pull models with: ollama pull llama3'
          },
          {
            title: 'Available Models',
            content: 'Supports LLaMA, Mistral, Code Llama, and many other open-source models'
          }
        ]
      },
      {
        id: 'vllm',
        name: 'vLLM',
        type: 'local',
        category: 'local',
        icon: '⚡',
        description: 'High-performance inference and serving for LLMs',
        features: ['High Performance', 'PagedAttention', 'Tensor Parallelism', 'Continuous Batching'],
        code: `// Connect to vLLM (default port 8000)
var api = new TornadoApi(new Uri("http://localhost:8000"));

// Create conversation with a vLLM model
var conversation = api.Chat.CreateConversation(new ChatModel("meta-llama/Meta-Llama-3-8B-Instruct"));

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'Setup',
            content: 'Install vLLM following the instructions at vllm.ai'
          },
          {
            title: 'Performance',
            content: 'vLLM provides up to 24x higher throughput compared to HuggingFace Transformers'
          }
        ]
      },
      
      // API Services
      {
        id: 'groq',
        name: 'Groq',
        type: 'api',
        category: 'api',
        icon: '🚀',
        description: 'Ultra-fast inference with Llama models',
        features: ['Ultra Low Latency', 'Llama Models', 'Simple API', 'Cost Effective'],
        code: `// Connect to Groq with API key
var api = new TornadoApi("your-groq-api-key", LLmProviders.Groq);

// Create conversation with Llama model
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.Groq.Meta.Llama38b8192
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from console.groq.com'
          },
          {
            title: 'Performance',
            content: 'Groq claims to be the world\'s fastest LLM inference engine'
          }
        ]
      },
      {
        id: 'deepseek',
        name: 'DeepSeek',
        type: 'api',
        category: 'api',
        icon: '🧠',
        description: 'Cost-effective alternatives to major providers',
        features: ['Cost Effective', 'Code Generation', 'Reasoning', 'Chinese Support'],
        code: `// Connect to DeepSeek with API key
var api = new TornadoApi("your-deepseek-api-key", LLmProviders.DeepSeek);

// Create conversation with DeepSeek model
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.DeepSeek.DeepSeek.DeepSeekR1
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from platform.deepseek.com'
          },
          {
            title: 'Specialization',
            content: 'Particularly strong for code generation and programming tasks'
          }
        ]
      },
      {
        id: 'mistral',
        name: 'Mistral',
        type: 'api',
        category: 'api',
        icon: '🌿',
        description: 'High-performance open-source models',
        features: ['Open Source', 'Multilingual', 'Efficient', 'Commercial Friendly'],
        code: `// Connect to Mistral with API key
var api = new TornadoApi("your-mistral-api-key", LLmProviders.Mistral);

// Create conversation with Mistral model
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.Mistral.Mistral.LargeLatest
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from console.mistral.ai'
          },
          {
            title: 'Model Options',
            content: 'Offers various model sizes from 7B to 8x22B parameters'
          }
        ]
      },
      {
        id: 'openrouter',
        name: 'OpenRouter',
        type: 'api',
        category: 'api',
        icon: '🔗',
        description: '400+ models from multiple providers in one API',
        features: ['400+ Models', 'Unified API', 'Model Comparison', 'Cost Optimization'],
        code: `// Connect to OpenRouter with API key
var api = new TornadoApi("your-openrouter-api-key", LLmProviders.OpenRouter);

// Create conversation with any model from OpenRouter
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenRouter.All.Llama38bInstruct
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from openrouter.ai'
          },
          {
            title: 'Model Selection',
            content: 'Access 400+ models from OpenAI, Anthropic, Google, and more through a single API'
          }
        ]
      },
      {
        id: 'perplexity',
        name: 'Perplexity',
        type: 'api',
        category: 'api',
        icon: '❓',
        description: 'Models optimized for search and question answering',
        features: ['Web Search', 'Question Answering', 'Real-time Info', 'Fact Checking'],
        code: `// Connect to Perplexity with API key
var api = new TornadoApi("your-perplexity-api-key", LLmProviders.Perplexity);

// Create conversation with Perplexity model
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.Perplexity.Sonar.Sonar8x7b
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}`,
        additionalInfo: [
          {
            title: 'API Setup',
            content: 'Get your API key from perplexity.ai'
          },
          {
            title: 'Special Features',
            content: 'Models optimized for search and real-time information retrieval'
          }
        ]
      }
    ]

    const filteredProviders = computed(() => {
      return providers.filter(p => p.category === selectedCategory.value)
    })

    const selectedProviderName = computed(() => {
      const provider = providers.find(p => p.id === selectedProvider.value)
      return provider ? provider.name : ''
    })

    const getProviderInfo = () => {
      return providers.find(p => p.id === selectedProvider.value) || {}
    }

    const highlightedCode = computed(() => {
      const provider = getProviderInfo()
      if (!provider.code) return ''
      
      // Simple syntax highlighting for C#
      return provider.code
        .replace(/(\/\/.*$)/gm, '<span class="comment">$1</span>')
        .replace(/(var\s+\w+|new\s+TornadoApi|ChatModel\.|LLmProviders\.)/g, '<span class="keyword">$1</span>')
        .replace(/(string|int|bool|ChatRequest)/g, '<span class="type">$1</span>')
        .replace(/(Model\s*=)/g, '<span class="attribute">$1</span>')
    })

    const copyCode = async () => {
      const provider = getProviderInfo()
      if (!provider.code) return
      
      try {
        await navigator.clipboard.writeText(provider.code)
        copied.value = true
        setTimeout(() => {
          copied.value = false
        }, 2000)
      } catch (err) {
        console.error('Failed to copy code:', err)
      }
    }

    // Auto-select first provider in category
    watch(selectedCategory, (newCategory) => {
      const firstProvider = filteredProviders.value[0]
      if (firstProvider) {
        selectedProvider.value = firstProvider.id
      }
    })

    return {
      selectedCategory,
      selectedProvider,
      copied,
      categories,
      filteredProviders,
      selectedProviderName,
      getProviderInfo,
      highlightedCode,
      copyCode
    }
  }
}
</script>

<style scoped>
.provider-selector {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem 0;
}

.provider-tabs {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
  border-bottom: 2px solid var(--vp-c-divider);
}

.tab-button {
  padding: 0.75rem 1.5rem;
  background: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 500;
  color: var(--vp-c-text-2);
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.tab-button:hover {
  color: var(--vp-c-text-1);
  background: var(--vp-c-bg-soft);
}

.tab-button.active {
  color: var(--vp-c-brand);
  border-bottom-color: var(--vp-c-brand);
}

.tab-icon {
  font-size: 1.2rem;
}

.provider-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.provider-card {
  background: var(--vp-c-bg-soft);
  border: 1px solid var(--vp-c-divider);
  border-radius: 12px;
  padding: 1.5rem;
  cursor: pointer;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.provider-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 4px;
  height: 100%;
  background: var(--vp-c-brand);
  transform: scaleY(0);
  transition: transform 0.3s ease;
}

.provider-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
  border-color: var(--vp-c-brand);
}

.provider-card:hover::before {
  transform: scaleY(1);
}

.provider-card.active {
  border-color: var(--vp-c-brand);
  background: var(--vp-c-brand-light);
}

.provider-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.provider-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: var(--vp-c-bg);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.icon-emoji {
  font-size: 24px;
  line-height: 1;
}

.provider-info h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--vp-c-text-1);
}

.provider-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 500;
  text-transform: uppercase;
}

.provider-badge.cloud {
  background: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.provider-badge.local {
  background: rgba(34, 197, 94, 0.1);
  color: #22c55e;
}

.provider-badge.api {
  background: rgba(168, 85, 247, 0.1);
  color: #a855f7;
}

.provider-description {
  color: var(--vp-c-text-2);
  margin-bottom: 1rem;
  line-height: 1.5;
}

.feature-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.feature-tag {
  padding: 0.25rem 0.75rem;
  background: var(--vp-c-bg);
  border: 1px solid var(--vp-c-divider);
  border-radius: 16px;
  font-size: 0.875rem;
  color: var(--vp-c-text-2);
}

.code-snippet {
  margin-top: 2rem;
  background: var(--vp-c-bg);
  border: 1px solid var(--vp-c-divider);
  border-radius: 12px;
  overflow: hidden;
}

.snippet-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  background: var(--vp-c-bg-soft);
  border-bottom: 1px solid var(--vp-c-divider);
}

.snippet-header h4 {
  margin: 0;
  color: var(--vp-c-text-1);
  font-size: 1rem;
  font-weight: 500;
}

.copy-button {
  padding: 0.5rem 1rem;
  background: var(--vp-c-brand);
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

.copy-button:hover {
  background: var(--vp-c-brand-dark);
}

.copy-button.copied {
  background: var(--vp-c-green);
}

.code-container {
  position: relative;
}

pre {
  margin: 0;
  padding: 1.5rem;
  overflow-x: auto;
  background: var(--vp-c-bg);
}

code {
  font-family: 'Fira Code', 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
  font-size: 0.875rem;
  line-height: 1.5;
}

/* Syntax highlighting */
:deep(.comment) {
  color: var(--vp-c-text-3);
  font-style: italic;
}

:deep(.keyword) {
  color: var(--vp-c-brand);
  font-weight: 600;
}

:deep(.type) {
  color: var(--vp-c-purple);
}

:deep(.attribute) {
  color: var(--vp-c-green);
}

.additional-info {
  padding: 1.5rem;
  border-top: 1px solid var(--vp-c-divider);
}

.info-section {
  margin-bottom: 1.5rem;
}

.info-section:last-child {
  margin-bottom: 0;
}

.info-section h5 {
  margin: 0 0 0.5rem 0;
  color: var(--vp-c-text-1);
  font-size: 0.875rem;
  font-weight: 600;
}

.info-section p {
  margin: 0;
  color: var(--vp-c-text-2);
  font-size: 0.875rem;
  line-height: 1.5;
}

/* Dark mode adjustments */
@media (prefers-color-scheme: dark) {
  .provider-card {
    background: var(--vp-c-bg-mute);
  }
  
  .feature-tag {
    background: var(--vp-c-bg-mute);
  }
}

.provider-free-tier {
  margin-bottom: 1rem;
}
</style>
