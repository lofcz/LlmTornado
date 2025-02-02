# Feature Matrix

✅ - Supported  
⚠️ - Partially supported/under construction  
❌ - Not supported  
_Empty fields mean the feature is unsupported by the provider._

## Inference

|               | OpenAI | Anthropic | Google | Cohere | DeepSeek | Groq | xAI | Custom* |
|-------|-----------|-----------|-----------| -----------| -----------| -----------| -----------| -----------|
|**Audio**      | ✅ |    |     |     |   |     |     |     |
|**Chat**       | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |  ✅ |
|**Caching**    |    |    |  ❌ |     |    |    |     |     |
|**Embeddings** | ✅ |    | ❌  | ❌ |    |    | ❌  |    |
|**Fine-Tuning**| ✅ |    | ❌  |    |    |    |      |    |
|**Batch**      | ❌ | ❌ | ❌ | ❌ |    | ❌ |     |    |
|**Files**      | ✅ |    | ✅ |     |    |     |     |    |
|**Uploads**    | ❌ |    |     |    |    |     |     |    |
|**Images**     | ✅ |    |     |    |    |     |     |    |
|**Models**     | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
|**Moderation** | ✅ |    |     |    |    |     |     |    |

_*Custom means any OpenAI compatible provider, such as Azure OpenAI, Ollama, KoboldCpp, etc._

## OpenAI Specific

 Assistants | Threads | Messages | Runs | Run steps | Vector stores | Vector store files | Vector store file batches | Realtime |
|-----------|------------|---------|----------|------| ---------------|-------------------|-------------------------|-----------|
| ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ✅ | ✅ | ✅  | ❌ |
