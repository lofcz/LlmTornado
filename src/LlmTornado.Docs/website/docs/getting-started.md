# Getting Started

Welcome to the LlmTornado documentation! This comprehensive guide will walk you through installation, setup, and your first AI-powered application using LlmTornado.

## Why LlmTornado?

LlmTornado is a powerful, flexible C# library that simplifies AI integration by:
- **Supporting 15+ AI providers** with a unified API
- **Offering agentic frameworks** for complex workflows
- **Providing multimodal capabilities** (text, images, audio, video)
- **Enabling MCP integration** for standardized tool communication
- **Ensuring enterprise readiness** with comprehensive error handling

## Installation

### Core Package

Install the main LlmTornado package via NuGet:

```bash
dotnet add package LlmTornado
```

Or using Package Manager Console:

```powershell
Install-Package LlmTornado
```

### Optional Addons

Enhance LlmTornado with additional packages:

```bash
# Agentic framework for building autonomous AI agents
dotnet add package LlmTornado.Agents

# Model Context Protocol (MCP) integration
dotnet add package LlmTornado.Mcp

# Productivity and quality of life enhancements
dotnet add package LlmTornado.Contrib

# Vector database support
dotnet add package LlmTornado.VectorDatabases
dotnet add package LlmTornado.VectorDatabases.ChromaDB
```

## Quick Start

### Basic Chat Completion

Get started with a simple chat completion using OpenAI:

```csharp
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;

// Initialize with your API key
TornadoApi api = new TornadoApi(new[]
{
    new ProviderAuthentication(LLmProviders.OpenAi, "YOUR_OPENAI_API_KEY")
});

// Create and execute a chat request
ChatResult? result = await api.Chat.CreateChatCompletion(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.Turbo,
    Messages = [
        new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
        new ChatMessage(ChatMessageRoles.User, "What is 2+2?")
    ]
});

// Display the response
Console.WriteLine(result?.Choices?[0].Message?.Content);
// Output: "2+2 equals 4."
```

### Using Conversations

For more natural interactions, use the conversation API:

```csharp
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;

TornadoApi api = new TornadoApi("YOUR_OPENAI_API_KEY");

// Create a conversation
Conversation conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35.Turbo
});

conversation.AddSystemMessage("You are a helpful assistant.");

// Add messages and get responses
conversation.AddUserMessage("What is async/await in C#?");
string? response = await conversation.GetResponse();
Console.WriteLine(response);

// Continue the conversation
conversation.AddUserMessage("Can you show me an example?");
response = await conversation.GetResponse();
Console.WriteLine(response);
```

### Multi-Provider Support

Switch between providers effortlessly:

```csharp
// Initialize with multiple providers
TornadoApi api = new TornadoApi([
    new (LLmProviders.OpenAi, "OPENAI_KEY"),
    new (LLmProviders.Anthropic, "ANTHROPIC_KEY"),
    new (LLmProviders.Google, "GOOGLE_KEY"),
    new (LLmProviders.Groq, "GROQ_KEY")
]);

// Use OpenAI
Conversation openAiChat = api.Chat.CreateConversation(ChatModel.OpenAi.Gpt4.O);
await openAiChat.AppendUserInput("Hello!").GetResponse();

// Switch to Anthropic
Conversation claudeChat = api.Chat.CreateConversation(ChatModel.Anthropic.Claude37.Sonnet);
await claudeChat.AppendUserInput("Hello!").GetResponse();

// Use Google Gemini
Conversation geminiChat = api.Chat.CreateConversation(ChatModel.Google.Gemini.Gemini2Flash001);
await geminiChat.AppendUserInput("Hello!").GetResponse();
```

## Configuration

### API Keys

#### Option 1: Direct Initialization
```csharp
TornadoApi api = new TornadoApi("YOUR_API_KEY");
```

#### Option 2: Multiple Providers
```csharp
TornadoApi api = new TornadoApi([
    new (LLmProviders.OpenAi, "OPENAI_KEY"),
    new (LLmProviders.Anthropic, "ANTHROPIC_KEY"),
    new (LLmProviders.Google, "GOOGLE_KEY")
]);
```

#### Option 3: Environment Variables
```csharp
// Set environment variable
Environment.SetEnvironmentVariable("OPENAI_API_KEY", "your-key");

// Use without explicit key
TornadoApi api = new TornadoApi(LLmProviders.OpenAi);
```

### Custom Endpoints

For self-hosted or custom providers:

```csharp
// Ollama example
TornadoApi api = new TornadoApi(new Uri("http://localhost:11434"));

Conversation chat = api.Chat.CreateConversation(new ChatModel("llama3:8b"));
await chat.AppendUserInput("Hello!").GetResponse();
```

## Common Use Cases

### 1. Simple Q&A Bot

```csharp
TornadoApi api = new TornadoApi("YOUR_API_KEY");

while (true)
{
    Console.Write("You: ");
    string? userInput = Console.ReadLine();
    
    if (string.IsNullOrEmpty(userInput) || userInput == "exit")
        break;
    
    Conversation chat = api.Chat.CreateConversation(ChatModel.OpenAi.Gpt35Turbo);
    string? response = await chat.AppendUserInput(userInput).GetResponse();
    
    Console.WriteLine($"AI: {response}");
}
```

### 2. Streaming Responses

```csharp
TornadoApi api = new TornadoApi("YOUR_API_KEY");

Conversation chat = api.Chat.CreateConversation(ChatModel.OpenAi.Gpt4.O);

await chat
    .AppendSystemMessage("You are a creative storyteller.")
    .AppendUserInput("Tell me a short story about a robot.")
    .StreamResponse(Console.Write);
```

### 3. Function Calling

```csharp
// Define a tool function
string GetCurrentWeather(string location)
{
    return $"Weather in {location}: Sunny, 72°F";
}

TornadoApi api = new TornadoApi("YOUR_API_KEY");

Conversation chat = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.O,
    Tools = [
        new Tool((string location, ToolArguments args) => GetCurrentWeather(location),
            "get_weather",
            "Get the current weather for a location")
    ]
});

await chat.AppendUserInput("What's the weather like in San Francisco?").GetResponseRich();
```

### 4. Structured Output

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string City { get; set; }
}

TornadoApi api = new TornadoApi("YOUR_API_KEY");

Conversation chat = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.O,
    ResponseFormat = ChatRequestResponseFormats.Json
});

chat.AppendSystemMessage("Extract person info from the message. Return JSON with name, age, and city.");
chat.AppendUserInput("John is 30 years old and lives in New York.");

ChatRichResponse response = await chat.GetResponseRich();
Person? person = JsonConvert.DeserializeObject<Person>(response.Content);
```

### 5. Building an Agent

```csharp
using LlmTornado.Agents;

TornadoApi api = new TornadoApi("YOUR_API_KEY");

TornadoAgent agent = new TornadoAgent(
    client: api,
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful coding assistant specialized in C#."
);

Conversation result = await agent.RunAsync("Explain the SOLID principles");
Console.WriteLine(result.Messages.Last().Content);
```

## Next Steps

Now that you've got the basics, explore more advanced features:

### Core Features
- [Chat Basics](./1.%20LlmTornado/1.%20Chat/1.%20basics.md) - Deep dive into chat functionality
- [Structured Output](./1.%20LlmTornado/1.%20Chat/3.%20structured-output.md) - Enforce JSON schemas
- [Function Calling](./1.%20LlmTornado/1.%20Chat/4.%20functions.md) - Enable AI to use tools
- [Models](./1.%20LlmTornado/1.%20Chat/5.%20models.md) - Explore all supported models
- [Streaming](./1.%20LlmTornado/1.%20Chat/6.%20streaming.md) - Real-time response streaming

### Advanced Topics
- [Agents](./2.%20Agents/1.%20Getting-Started.md) - Build autonomous AI agents
- [MCP Integration](./3.%20MPC/MPC.md) - Model Context Protocol support
- [Vector Databases](./6.%20VectorDatabases/1.%20Getting-Started.md) - Semantic search and embeddings

## Troubleshooting

### Common Issues

#### API Key Errors
```
Error: Invalid API key
```
**Solution**: Verify your API key is correct and has proper permissions

#### Model Not Found
```
Error: Model 'xyz' not found
```
**Solution**: Check the [Feature Matrix](https://github.com/lofcz/LlmTornado/blob/master/FeatureMatrix.md) for supported models

#### Rate Limiting
```
Error: Rate limit exceeded
```
**Solution**: Implement retry logic with exponential backoff or upgrade your API plan

### Getting Help

- **Documentation**: Browse the complete documentation in the sidebar
- **GitHub Issues**: Report bugs or request features at [GitHub](https://github.com/lofcz/LlmTornado/issues)
- **Examples**: Check the [Demo project](https://github.com/lofcz/LlmTornado/tree/master/src/LlmTornado.Demo) for more examples

## Additional Resources

- [Official Repository](https://github.com/lofcz/LlmTornado)
- [NuGet Package](https://www.nuget.org/packages/LlmTornado)
- [Feature Matrix](https://github.com/lofcz/LlmTornado/blob/master/FeatureMatrix.md)
- [Release Notes](https://github.com/lofcz/LlmTornado/releases)
