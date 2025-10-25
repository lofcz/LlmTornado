// See https://aka.ms/new-console-template for more information
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

ContextContainer contextContainer = new ContextContainer();
contextContainer.Goal = "Create a C# lib for AI Context Engineering Toolkit";
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.System,
    contextContainer.Goal
));
contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
    ChatMessageRoles.User,
    "Create a C# lib for AI Context Engineering Toolkit"
));

var api = new TornadoApi([
                new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                new ProviderAuthentication(LLmProviders.Anthropic, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                ]);


