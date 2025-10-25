using LlmTornado.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController.Test;

internal class TaskServiceTest
{
    public static async Task Test()
    {
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

        TaskContextService taskContextService = new TaskContextService(api, contextContainer);

        Console.WriteLine("Running task context");

        string task = await taskContextService.GetTaskContext();

        Console.WriteLine($"Current Task: {task}");
    }
}
