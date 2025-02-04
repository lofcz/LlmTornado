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
|**Embeddings** | ✅ |    | ❌  | ❌ |    |    | ❌  |    |
|**Fine-Tuning**| ✅ |    | ❌  |    |    |    |      |    |
|**Batch**      | ❌ | ❌ | ❌ | ❌ |    | ❌ |     |    |
|**Files**      | ✅ |    | ✅ |     |    |     |     |    |
|**Uploads**    | ❌ |    |     |    |    |     |     |    |
|**Images**     | ✅ |    |     |    |    |     |     |    |
|**Models**     | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
|**Moderation** | ✅ |    |     |    |    |     |     |    |
|**Tokenize**   |    | ❌ |     | ❌ |    |     |     |    |

_*Custom means any OpenAI compatible provider, such as Azure OpenAI, Ollama, KoboldCpp, etc._

## OpenAI Specific

 Assistants | Threads | Messages | Runs | Run steps | Vector stores | Vector store files | Vector store file batches | Realtime |
|-----------|------------|---------|----------|------| ---------------|-------------------|-------------------------|-----------|
| ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ✅ | ✅ | ✅  | ❌ |

## Google Specific

 Caching* | Semantic Retrieval 
|-----------|------------ |
| ✅ | ❌ |  

_*Other providers expose caching as part of `Chat`, or don't offer the feature._


## Cohere Specific

 Rerank | Embed Jobs* | Classify* | Datasets* | Connectors* |
|-----------|------------ | ------------ | ------------ | ------------ |
| ❌ | ❌ |  ❌ | ❌ | ❌ | ❌

_*`/v1` APIs, future support unsure, probably won't be implemented._
