# 08 — LLM Provider Detection

The provider detection system automatically discovers available LLM providers by scanning standard environment variables, then selects the best default model and a cheap optimizer model.

## Architecture

```mermaid
classDiagram
    class ProviderDetector {
        -ProviderEnvVars: (string,LLmProviders)[]$
        -DefaultPriority: LLmProviders[]$
        -OptimizerModelPriority: (LLmProviders,Func)[]$
        +Detect() ProviderDetectionResult$
    }

    class ProviderDetectionResult {
        +TornadoApi Api
        +List~DetectedProvider~ Providers
        +ChatModel ActiveModel
        +ChatModel OptimizerModel
    }

    class DetectedProvider {
        +LLmProviders Provider
        +string ApiKey
        +List~ChatModel~ Models
        +ChatModel DefaultModel
    }

    ProviderDetector --> ProviderDetectionResult
    ProviderDetectionResult --> DetectedProvider
```

## Detection Flow

```mermaid
flowchart TD
    Start["ProviderDetector.Detect()"] --> Scan["Scan 12 environment variables"]
    Scan --> Found{"Any keys<br/>found?"}
    Found -->|"No"| Null["Return null<br/>(no providers)"]
    Found -->|"Yes"| Build["Build DetectedProvider list<br/>(key, models, default model)"]
    Build --> API["Create TornadoApi<br/>(all provider authentications)"]
    API --> Active["Select ActiveModel<br/>(by priority)"]
    Active --> Optimizer["Select OptimizerModel<br/>(cheapest available)"]
    Optimizer --> Result["Return ProviderDetectionResult"]
```

## Environment Variables

The detector scans these environment variables in order:

| Environment Variable | Provider |
|---------------------|----------|
| `OPENAI_API_KEY` | OpenAI |
| `ANTHROPIC_API_KEY` | Anthropic |
| `GOOGLE_API_KEY` | Google |
| `GROQ_API_KEY` | Groq |
| `COHERE_API_KEY` | Cohere |
| `MISTRAL_API_KEY` | Mistral |
| `DEEPSEEK_API_KEY` | DeepSeek |
| `XAI_API_KEY` | xAI |
| `PERPLEXITY_API_KEY` | Perplexity |
| `OPENROUTER_API_KEY` | OpenRouter |
| `DEEPINFRA_API_KEY` | DeepInfra |
| `VOYAGE_API_KEY` | Voyage |

## Default Model Selection

When multiple providers are detected, the **active model** is chosen by this priority (best model from the highest-priority available provider):

```mermaid
graph LR
    A["1. Anthropic<br/>Claude Fable 5"] --> B["2. OpenAI<br/>GPT-5.6"]
    B --> C["3. Google<br/>Gemini Pro Latest"]
    C --> D["4. xAI<br/>Grok 4.5"]
    D --> E["5. DeepSeek<br/>Reasoner"]
    E --> F["6. Groq<br/>Llama 4 Maverick"]
    F --> G["7. Mistral<br/>Devstral 2512"]
```

### Default Models per Provider

| Provider | Default Model |
|----------|--------------|
| Anthropic | Claude Fable 5 |
| OpenAI | GPT-5.6 |
| Google | Gemini Pro Latest |
| xAI | Grok 4.5 |
| DeepSeek | Reasoner |
| Groq | Llama 4 Maverick |
| Mistral | Devstral 2512 |
| Cohere | Command A Reasoning 2508 |
| Perplexity | Sonar Reasoning Pro |

## Optimizer Model Selection

The optimizer model is used for internal tasks like tool optimization (see [09-tool-optimization.md](09-tool-optimization.md)). It prioritizes **cheapest/fastest** models:

```mermaid
graph LR
    A["1. Google<br/>Gemini Flash Lite Latest"] --> B["2. OpenAI<br/>GPT-5.6 Luna"]
    B --> C["3. Anthropic<br/>Claude Sonnet 5"]
    C --> D["4. Groq<br/>GPT-OSS 120B"]
    D --> E["5. DeepSeek<br/>Chat"]
    E --> F["6. Mistral<br/>Ministral 14B"]
    F --> G["7. xAI<br/>Grok 4.1 Fast Non-Reasoning"]
```

**Fallback**: If none of the priority providers are available, the first detected provider's default model is used.

## Available Models per Provider

Each detected provider exposes a curated list of models the user can switch to at runtime:

```mermaid
graph TD
    subgraph "OpenAI"
        OA1["GPT-5.6"]
        OA2["GPT-5.6 Sol"]
        OA3["GPT-5.6 Terra"]
        OA4["GPT-5.6 Luna"]
        OA5["GPT-5.5"]
        OA6["GPT-5.4"]
        OA7["GPT-5.3 Codex"]
    end

    subgraph "Anthropic"
        AN1["Claude Fable 5"]
        AN2["Claude Sonnet 5"]
        AN3["Claude Opus 4.8"]
        AN4["Claude Sonnet 4.6"]
        AN5["Claude Haiku 4.5"]
    end

    subgraph "Google"
        GO1["Gemini Pro Latest"]
        GO2["Gemini 3.5 Flash"]
        GO3["Gemini Flash Lite Latest"]
        GO4["Gemini 3.1 Flash Lite"]
        GO5["Gemini 2.5 Pro"]
        GO6["Gemini 2.5 Flash"]
    end

    subgraph "xAI"
        XA1["Grok 4.5"]
        XA2["Grok Build 0.1"]
        XA3["Grok 4.1 Fast Reasoning"]
        XA4["Grok 4"]
        XA5["Grok 4 Fast Reasoning"]
    end

    subgraph "DeepSeek"
        DS1["Reasoner"]
        DS2["Chat"]
    end

    subgraph "Groq"
        GR1["Llama 4 Maverick"]
        GR2["Llama 4 Scout"]
        GR3["Llama 3.3 70B Versatile"]
    end
```

<details>
<summary>Additional providers</summary>

| Provider | Models |
|----------|--------|
| **Mistral** | Devstral 2512, Mistral Medium 2508, Magistral Medium 2509, Mistral Large 2512 |
| **Cohere** | Command A Reasoning 2508, Command A 0325, Command A Vision 2507 |
| **Perplexity** | Sonar Reasoning Pro, Sonar Pro, Sonar Deep Research, Sonar Default |

</details>

## TornadoApi Construction

All detected providers are combined into a single `TornadoApi` instance:

```mermaid
flowchart TD
    D1["Anthropic<br/>key: sk-ant-..."] --> Auth["List&lt;ProviderAuthentication&gt;"]
    D2["OpenAI<br/>key: sk-..."] --> Auth
    D3["Google<br/>key: AI..."] --> Auth

    Auth --> API["new TornadoApi(providerAuths)"]
    API --> Result["Single API client<br/>routes to correct provider<br/>based on model"]
```

This means the application can use any detected model seamlessly — the `TornadoApi` routes requests to the correct provider based on the model being used.

## Integration with AgentBuilder

```mermaid
sequenceDiagram
    participant FE as Frontend (CLI/ACP)
    participant PD as ProviderDetector
    participant AB as AgentBuilder

    FE->>PD: Detect()
    PD-->>FE: {Api, ActiveModel, OptimizerModel, Providers[]}

    FE->>AB: new AgentBuilder(api, activeModel, ..., optimizerModel)
    Note over AB: ActiveModel → used for agent responses
    Note over AB: OptimizerModel → used for ToolOptimizer

    FE->>AB: SetModel(newModel)
    Note over AB: User switches model at runtime
    AB->>AB: Rebuild agent with new model
```
