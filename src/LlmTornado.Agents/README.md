# LlmTornado Agents

## Overview
LlmTornado Agents is a framework designed to facilitate the creation and management of AI agents that can perform complex tasks by leveraging large language models (LLMs). The framework provides a structured approach to building agents that can interact with various tools, manage state, and execute tasks in a modular fashion.

## Getting Started

```csharp
using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Agents.DataModels;

TornadoApi client = new TornadoApi("your_api_key");

TornadoAgent agent = new TornadoAgent(client, ChatModel.OpenAi.Gpt41.V41Mini, "You are a useful assistant.");

Conversation result = await agent.RunAsync("What is 2+2?");

Console.WriteLine(result.Messages.Last().Content);
```

## TODO
* [ ] Need Vector Store Features
* [ ] Ability to see which states take longest and cost the most
* [ ] More comprehensive examples in the repo
* [ ] Observability as a whole
* [ ] Api for Runtime