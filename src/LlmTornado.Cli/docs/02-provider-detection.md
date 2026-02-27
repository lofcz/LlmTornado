# Stage 2: Provider Detection

## Goal

Auto-detect LLM providers from standard environment variables, build a `TornadoApi` instance with all detected providers, and maintain a registry of available models per provider.

---

## File to Create

### `src/LlmTornado.Cli/ProviderDetector.cs`

---

## Environment Variables to Scan

| Env Var | Provider | Example Model |
|---------|----------|---------------|
| `OPENAI_API_KEY` | `LLmProviders.OpenAi` | `ChatModel.OpenAi.Gpt41.Mini` |
| `ANTHROPIC_API_KEY` | `LLmProviders.Anthropic` | `ChatModel.Anthropic.Claude37.Sonnet` |
| `GOOGLE_API_KEY` | `LLmProviders.Google` | `ChatModel.Google.Gemini.Gemini25Pro` |
| `GROQ_API_KEY` | `LLmProviders.Groq` | `ChatModel.Groq.*` |
| `COHERE_API_KEY` | `LLmProviders.Cohere` | `ChatModel.Cohere.*` |
| `MISTRAL_API_KEY` | `LLmProviders.Mistral` | `ChatModel.Mistral.*` |
| `DEEPSEEK_API_KEY` | `LLmProviders.DeepSeek` | `ChatModel.DeepSeek.*` |
| `XAI_API_KEY` | `LLmProviders.XAi` | `ChatModel.XAi.Grok3.V3` |
| `PERPLEXITY_API_KEY` | `LLmProviders.Perplexity` | `ChatModel.Perplexity.*` |
| `OPENROUTER_API_KEY` | `LLmProviders.OpenRouter` | `ChatModel.OpenRouter.*` |
| `DEEPINFRA_API_KEY` | `LLmProviders.DeepInfra` | `ChatModel.DeepInfra.*` |
| `VOYAGE_API_KEY` | `LLmProviders.Voyage` | (embedding provider) |

---

## Data Model

```csharp
namespace LlmTornado.Cli;

/// <summary>
/// Detected provider with its API key and available models.
/// </summary>
internal sealed class DetectedProvider
{
    public required LLmProviders Provider { get; init; }
    public required string ApiKey { get; init; }
    public required List<ChatModel> Models { get; init; }
    
    /// <summary>
    /// The recommended default model for this provider.
    /// </summary>
    public required ChatModel DefaultModel { get; init; }
}

/// <summary>
/// Result of provider detection: the constructed API client and all detected providers.
/// </summary>
internal sealed class ProviderDetectionResult
{
    public required TornadoApi Api { get; init; }
    public required List<DetectedProvider> Providers { get; init; }
    public required ChatModel ActiveModel { get; set; }
    
    /// <summary>
    /// Flat list of all models across all detected providers.
    /// </summary>
    public List<ChatModel> AllModels => Providers.SelectMany(p => p.Models).ToList();
}
```

---

## Detection Logic

```csharp
internal static class ProviderDetector
{
    /// <summary>
    /// Maps environment variable names to provider enum values.
    /// </summary>
    private static readonly (string EnvVar, LLmProviders Provider)[] ProviderEnvVars =
    [
        ("OPENAI_API_KEY",      LLmProviders.OpenAi),
        ("ANTHROPIC_API_KEY",   LLmProviders.Anthropic),
        ("GOOGLE_API_KEY",      LLmProviders.Google),
        ("GROQ_API_KEY",        LLmProviders.Groq),
        ("COHERE_API_KEY",      LLmProviders.Cohere),
        ("MISTRAL_API_KEY",     LLmProviders.Mistral),
        ("DEEPSEEK_API_KEY",    LLmProviders.DeepSeek),
        ("XAI_API_KEY",         LLmProviders.XAi),
        ("PERPLEXITY_API_KEY",  LLmProviders.Perplexity),
        ("OPENROUTER_API_KEY",  LLmProviders.OpenRouter),
        ("DEEPINFRA_API_KEY",   LLmProviders.DeepInfra),
        ("VOYAGE_API_KEY",      LLmProviders.Voyage),
    ];

    public static ProviderDetectionResult? Detect()
    {
        // 1. Scan env vars, collect ProviderAuthentication objects
        // 2. For each detected provider, build DetectedProvider with models list
        // 3. Construct TornadoApi with multi-provider auth:
        //    new TornadoApi(providerAuths)  // IEnumerable<ProviderAuthentication>
        // 4. Pick default model (priority: Anthropic > OpenAI > Google > first found)
        // 5. Return ProviderDetectionResult or null if nothing found
    }
}
```

---

## TornadoApi Construction

Uses the multi-provider constructor:

```csharp
var providerAuths = detectedProviders.Select(p => 
    new ProviderAuthentication(p.Provider, p.ApiKey)).ToList();

var api = new TornadoApi(providerAuths);
```

This creates a single `TornadoApi` instance that can route requests to any detected provider based on the model being used.

---

## Model Registry

For each provider, populate the available models from the static `ChatModel` hierarchy. Use reflection or a manual mapping:

```csharp
private static List<ChatModel> GetModelsForProvider(LLmProviders provider) => provider switch
{
    LLmProviders.OpenAi => [/* ChatModel.OpenAi.Gpt41.Mini, ChatModel.OpenAi.Gpt41.O, ... */],
    LLmProviders.Anthropic => [/* ChatModel.Anthropic.Claude37.Sonnet, ... */],
    // etc.
};
```

Each provider entry also has a preferred `DefaultModel` used when no model is explicitly selected:

| Provider | Default Model | Rationale |
|----------|---------------|-----------|
| Anthropic | `Claude45.Sonnet` | Best balance of quality + speed + tool use |
| OpenAI | `Gpt51.Mini` | Cost-effective with strong tool use |
| Google | `Gemini25.Pro` | Best Google model for complex tasks |
| xAI | `Grok3.V3` | Flagship model |
| DeepSeek | `V3` or similar | Best available |

**Overall default priority**: If multiple providers detected, pick the first from: Anthropic → OpenAI → Google → xAI → Groq → Mistral → first detected.

---

## `/model` Command Integration

The `ProviderDetectionResult` is passed to `ModelCommand` which uses it to:

1. **`/model list`** — iterate `Providers`, for each show provider name + all models
2. **`/model set <name>`** — search `AllModels` by model name string, update `ActiveModel`
3. Display the current model in the REPL prompt

---

## Error Handling

- If **no providers detected**: print a helpful message listing all supported env vars and exit with code 1
- If a detected key is **empty or whitespace**: skip that provider, log a warning
- If a provider's key is **invalid** (detected during first API call, not at detection time): the chat call will fail with an auth error — handled by the REPL error handler

---

## Example Output

```
Detected providers:
  ✓ Anthropic (ANTHROPIC_API_KEY)  — 8 models available
  ✓ OpenAI (OPENAI_API_KEY)        — 12 models available
  ✗ Google (GOOGLE_API_KEY)         — not set
  ✗ Groq (GROQ_API_KEY)            — not set

Active model: claude-3-7-sonnet (Anthropic)
Type /model list to see all available models.
```

---

## Types Used from LlmTornado

| Type | Namespace | Purpose |
|------|-----------|---------|
| `TornadoApi` | `LlmTornado` | API client construction |
| `ProviderAuthentication` | `LlmTornado` | Per-provider auth |
| `LLmProviders` | `LlmTornado.Code` | Provider enum |
| `ChatModel` | `LlmTornado.Chat` | Model identifiers |
| `ApiAuthentication` | `LlmTornado` | Auth wrapper |
