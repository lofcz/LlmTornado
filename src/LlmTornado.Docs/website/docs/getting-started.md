# Getting started

LlmTornado is a .NET library for using multiple model providers through one API. Start with the core package, add the Agents package only when you need an agent loop, tools, guardrails, or persisted agent history.

## Install

```powershell
dotnet add package LlmTornado
dotnet add package LlmTornado.Agents
```

`LlmTornado.Agents` is optional. Other integrations, including MCP and vector databases, are published as separate packages.

## Configure a provider

Keep credentials outside source control. This example reads an OpenAI key from the environment and names the provider explicitly:

```csharp
using LlmTornado;
using LlmTornado.Code;

string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Set OPENAI_API_KEY first.");

TornadoApi api = new TornadoApi(
    new ProviderAuthentication(LLmProviders.OpenAi, apiKey));
```

For several providers, pass a collection of `ProviderAuthentication` values. The model selected for each request determines which credentials and endpoint provider are used.

```csharp
TornadoApi api = new TornadoApi([
    new ProviderAuthentication(LLmProviders.OpenAi, openAiKey),
    new ProviderAuthentication(LLmProviders.Anthropic, anthropicKey),
    new ProviderAuthentication(LLmProviders.Google, googleKey)
]);
```

See [Custom providers](/advanced-and-preview/custom-providers) for authenticated self-hosted or OpenAI-compatible endpoints.

## Send a chat request

```csharp
using LlmTornado.Chat;
using LlmTornado.Chat.Models;

Conversation conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt41.V41Mini
});

conversation.AddSystemMessage("You are a concise C# assistant.");
conversation.AddUserMessage("Explain async streams in two sentences.");

ChatRichResponse response = await conversation.GetResponseRich();
Console.WriteLine(response.Text);
```

The `Conversation` instance retains its message history. Add another user message and call `GetResponseRich()` again to continue the same chat.

## Use the Responses API

OpenAI's Responses API supports response chaining, reasoning configuration, built-in tools, and typed streaming events. Use `api.Responses` directly when you need those features.

```csharp
using LlmTornado.Responses;

ResponseResult result = await api.Responses.CreateResponse(new ResponseRequest
{
    Model = ChatModel.OpenAi.Gpt41.V41Mini,
    Instructions = "Be concise.",
    InputItems = [
        new ResponseInputMessage(ChatMessageRoles.User, "What is dependency injection?")
    ]
});

Console.WriteLine(result.OutputText);
```

Read the [Responses overview](/llmtornado/responses/overview) before using Responses-only models. The higher-level chat API automatically routes models whose metadata declares Responses support without Chat support; see [Codex routing](/advanced-and-preview/codex-routing).

## Run an agent

```csharp
using LlmTornado.Agents;

TornadoAgent agent = new TornadoAgent(
    client: api,
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful C# assistant.");

Conversation result = await agent.Run("Explain the SOLID principles.");
Console.WriteLine(result.Messages.Last().Content);
```

Agent runs accept cancellation, maximum-turn controls, error behavior, streaming callbacks, and token telemetry. See [Agent runner](/agents/tornado-agent/tornado-runner).

## Where to go next

- [Chat basics](/llmtornado/chat/basics)
- [Responses overview](/llmtornado/responses/overview)
- [Agents](/agents/getting-started)
- [Streaming](/llmtornado/chat/streaming)
- [Function calling](/llmtornado/chat/functions)
- [Persistent conversations](/agents/tornado-agent/persistent-conversation)

The Assistants, Threads, and Completions sections are retained for existing integrations and are marked **Legacy** in navigation. Prefer Chat, Responses, and Agents for new work.

## Help and examples

- [Demo project](https://github.com/lofcz/LlmTornado/tree/master/src/LlmTornado.Demo)
- [GitHub issues](https://github.com/lofcz/LlmTornado/issues)
- [NuGet package](https://www.nuget.org/packages/LlmTornado)
