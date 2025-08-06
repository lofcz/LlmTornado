<!-- markdownlint-disable MD033 -->
# Chat Models

## Overview

Below is an interactive list of providers. The code samples are pre-rendered (hidden) by VitePress so they share the same Shiki highlighting as the rest of the docs.

<!-- Hidden code snippets (pre-rendered by VitePress) -->

<div id="snippet-openai" style="display:none;">

```csharp
// Connect to OpenAI with API key
var api = new TornadoApi("your-openai-api-key", LLmProviders.OpenAi);

// Create conversation with streaming
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.O
});

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}
```

</div>

<div id="snippet-google" style="display:none;">

```csharp
// Connect to Google with API key
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
}
```

</div>

<div id="snippet-anthropic" style="display:none;">

```csharp
// Connect to Anthropic with API key
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
}
```

</div>

<div id="snippet-xAi" style="display:none;">

```csharp
// Connect to xAI with API key
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
}
```

</div>

<div id="snippet-cohere" style="display:none;">

```csharp
// Connect to Cohere with API key
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
}
```

</div>

<div id="snippet-ollama" style="display:none;">

```csharp
// Connect to Ollama (default port 11434)
var api = new TornadoApi(new Uri("http://localhost:11434"));

// Create conversation with a local model
var conversation = api.Chat.CreateConversation(new ChatModel("llama3:8b"));

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}
```

</div>

<div id="snippet-vllm" style="display:none;">

```csharp
// Connect to vLLM (default port 8000)
var api = new TornadoApi(new Uri("http://localhost:8000"));

// Create conversation with a vLLM model
var conversation = api.Chat.CreateConversation(new ChatModel("meta-llama/Meta-Llama-3-8B-Instruct"));

// Stream response to console
await foreach (var chunk in conversation.StreamResponse())
{
    Console.Write(chunk.Content);
}
```

</div>

<div id="snippet-groq" style="display:none;">

```csharp
// Connect to Groq with API key
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
}
```

</div>

<div id="snippet-deepseek" style="display:none;">

```csharp
// Connect to DeepSeek with API key
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
}
```

</div>

<div id="snippet-mistral" style="display:none;">

```csharp
// Connect to Mistral with API key
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
}
```

</div>

<div id="snippet-openrouter" style="display:none;">

```csharp
// Connect to OpenRouter with API key
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
}
```

</div>

<div id="snippet-perplexity" style="display:none;">

```csharp
// Connect to Perplexity with API key
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
}
```

</div>

<!-- Provider selector component -->

<ProviderSelector />
