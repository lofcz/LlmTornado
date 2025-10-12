# LlmTornado.Microsoft.Extensions.AI

Microsoft.Extensions.AI provider implementation for LlmTornado, enabling seamless integration with the Microsoft AI abstractions.

## Overview

This package provides `IChatClient` and `IEmbeddingGenerator` implementations that work with LlmTornado's multi-provider API, allowing you to use any of the 100+ supported AI providers through the standardized Microsoft.Extensions.AI interfaces.

## Features

- ✅ Full `IChatClient` implementation with streaming support
- ✅ Full `IEmbeddingGenerator` implementation
- ✅ Full `IImageGenerator` implementation
- ✅ OpenTelemetry instrumentation for observability
- ✅ Automatic message content conversion (text, images, files)
- ✅ Tool/function calling support
- ✅ Support for all LlmTornado providers (OpenAI, Anthropic, Google, DeepSeek, etc.)

## Installation

```bash
dotnet add package LlmTornado.Microsoft.Extensions.AI
```

## Usage

### Chat Client

```csharp
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Microsoft.Extensions.AI;
using Microsoft.Extensions.AI;
using System.Linq;

// Initialize the LlmTornado API
var api = new TornadoApi("your-api-key");

// Create a chat client using the extension method
IChatClient chatClient = api.AsChatClient(ChatModel.OpenAi.Gpt4);

// Use the chat client
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "What is the capital of France?")
};

var response = await chatClient.GetResponseAsync(messages);
Console.WriteLine(response.Contents.OfType<TextContent>().FirstOrDefault()?.Text);
```

### Streaming Chat

```csharp
await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
{
    foreach (var content in update.Contents)
    {
        if (content is TextContent textContent)
        {
            Console.Write(textContent.Text);
        }
    }
}
```

### Image Generation

```csharp
using LlmTornado.Images;

// Create an image generator
var imageGenerator = api.AsImageGenerator(ImageModels.DallE3);

// Generate an image
var imageResponse = await imageGenerator.GenerateImageAsync(
    "A cat wearing a top hat",
    new ImageGenerationOptions { ImageSize = new System.Drawing.Size(1024, 1024) });

var imageUrl = imageResponse.Contents.OfType<UriContent>().FirstOrDefault()?.Uri;
Console.WriteLine($"Generated image URL: {imageUrl}");
```

### Embedding Generator

```csharp
using LlmTornado.Embedding.Models;
using System;

// Create an embedding generator
var embeddingGenerator = api.AsEmbeddingGenerator(
    EmbeddingModel.OpenAi.TextEmbedding3Small,
    dimensions: 1536);

// Generate embeddings
var texts = new[] { "Hello world", "AI is amazing" };
var embeddings = await embeddingGenerator.GenerateAsync(texts);

foreach (var embedding in embeddings)
{
    var vector = embedding.AsReadOnlySpan();
    Console.WriteLine($"Embedding: {string.Join(", ", vector.Slice(0, Math.Min(5, vector.Length)))}...");
}
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add chat client
services.AddTornadoChatClient(
    api,
    ChatModel.OpenAi.Gpt4,
    new ChatRequest { Temperature = 0.7 });

// Add embedding generator
services.AddTornadoEmbeddingGenerator(
    api,
    EmbeddingModel.OpenAi.TextEmbedding3Small,
    dimensions: 1536);

// Add image generator
services.AddTornadoImageGenerator(api, ImageModels.DallE3);

var serviceProvider = services.BuildServiceProvider();

// Use from DI
var chatClient = serviceProvider.GetRequiredService<IChatClient>();
var embeddingGenerator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
var imageGenerator = serviceProvider.GetRequiredService<IImageGenerator>();
```

### Multi-Modal Content

```csharp
using Microsoft.Extensions.AI;
using System.Linq;

var messages = new List<ChatMessage>
{
    new(ChatRole.User, new AIContent[]
    {
        new TextContent("What's in this image?"),
        new UriContent("https://example.com/image.jpg", "image/jpeg")
    })
};

var response = await chatClient.GetResponseAsync(messages);
```

### Function/Tool Calling

```csharp
var options = new ChatOptions
{
    Tools = new List<AITool>
    {
        AIFunctionFactory.Create(
            (string location) => $"The weather in {location} is sunny.",
            name: "get_weather",
            description: "Gets the current weather for a location")
    }
};

var response = await chatClient.GetResponseAsync(messages, options);
```

## OpenTelemetry Support

The implementation includes built-in OpenTelemetry instrumentation:

- Activity source: `LlmTornado.Microsoft.Extensions.AI.Chat`
- Activity source: `LlmTornado.Microsoft.Extensions.AI.Embeddings`
- Spans include: model, token usage, finish reasons, and error tracking

```csharp
using OpenTelemetry.Trace;

services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("LlmTornado.Microsoft.Extensions.AI.Chat")
        .AddSource("LlmTornado.Microsoft.Extensions.AI.Embeddings")
        .AddConsoleExporter());
```

## Supported Providers

This implementation supports all providers available in LlmTornado:

- OpenAI
- Anthropic (Claude)
- Google (Gemini)
- Azure OpenAI
- DeepSeek
- Mistral
- Cohere
- xAI (Grok)
- Perplexity
- Groq
- And 90+ more providers!

Simply change the model to switch providers:

```csharp
// OpenAI
var openaiClient = api.AsChatClient(ChatModel.OpenAi.Gpt4);

// Anthropic
var anthropicClient = api.AsChatClient(ChatModel.Anthropic.Claude37Sonnet);

// Google
var googleClient = api.AsChatClient(ChatModel.Google.Gemini20Flash);
```

## License

This project is licensed under CC0-1.0.

## Links

- [LlmTornado GitHub](https://github.com/lofcz/LlmTornado)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/advanced/sample-implementations)
