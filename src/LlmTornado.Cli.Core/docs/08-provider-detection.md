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
    A["1. Anthropic<br/>Claude 4.6 Opus"] --> B["2. OpenAI<br/>GPT-5.2"]
    B --> C["3. Google<br/>Gemini 3 Pro Preview"]
    C --> D["4. xAI<br/>Grok 4"]
    D --> E["5. DeepSeek<br/>Chat"]
    E --> F["6. Groq<br/>Llama 4 Maverick"]
    F --> G["7. Mistral<br/>Mistral Medium"]
```

### Default Models per Provider

| Provider | Default Model |
|----------|--------------|
| Anthropic | Claude 4.6 Opus |
| OpenAI | GPT-5.2 |
| Google | Gemini 3 Pro Preview |
| xAI | Grok 4 |
| DeepSeek | Chat |
| Groq | Llama 4 Maverick |
| Mistral | Mistral Medium 2508 |
| Cohere | Command A 0325 |
| Perplexity | Sonar Pro |

## Optimizer Model Selection

The optimizer model is used for internal tasks like tool optimization (see [09-tool-optimization.md](09-tool-optimization.md)). It prioritizes **cheapest/fastest** models:

```mermaid
graph LR
    A["1. Google<br/>Gemini 2.5 Flash"] --> B["2. OpenAI<br/>O4 Mini"]
    B --> C["3. Anthropic<br/>Claude 4 Sonnet"]
    C --> D["4. Groq<br/>Llama 4 Scout"]
    D --> E["5. DeepSeek<br/>Chat"]
    E --> F["6. Mistral<br/>Mistral Large"]
    F --> G["7. xAI<br/>Grok 4.1 Fast"]
```

**Fallback**: If none of the priority providers are available, the first detected provider's default model is used.

## Available Models per Provider

Each detected provider exposes a curated list of models the user can switch to at runtime:

```mermaid
graph TD
    subgraph "OpenAI"
        OA1["GPT-5.2"]
        OA2["GPT-5.2 Pro"]
        OA3["GPT-5.1"]
        OA4["GPT-5.1 Codex Max"]
        OA5["O4 Mini"]
        OA6["O3"]
    end

    subgraph "Anthropic"
        AN1["Claude 4.6 Opus"]
        AN2["Claude 4.5 Opus"]
        AN3["Claude 4.5 Sonnet"]
        AN4["Claude 4 Sonnet"]
    end

    subgraph "Google"
        GO1["Gemini 3 Pro Preview"]
        GO2["Gemini 3 Flash Preview"]
        GO3["Gemini 2.5 Pro"]
        GO4["Gemini 2.5 Flash"]
    end

    subgraph "xAI"
        XA1["Grok 4"]
        XA2["Grok 4 Fast Reasoning"]
        XA3["Grok 4.1 Fast Reasoning"]
    end

    subgraph "DeepSeek"
        DS1["Chat"]
        DS2["Reasoner"]
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
| **Mistral** | Mistral Medium 2508, Magistral Medium 2509, Mistral Large 2512 |
| **Cohere** | Command A 0325, Command A Reasoning 2508, Command A Vision 2507 |
| **Perplexity** | Sonar Pro, Sonar Default, Sonar Deep Research |

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
