using LlmTornado.Chat.Models;
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Chat.Models;
namespace LlmTornado.Agents.Samples.ContextController;

public static class ModelContextTest
{
    public static async Task TestModelContextService()
    {
        ContextContainer contextContainer = new ContextContainer();
        contextContainer.Goal = "Assist the user with their requests using appropriate tools and models.";
        contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
            ChatMessageRoles.System,
            contextContainer.Goal
        ));
        contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
            ChatMessageRoles.User,
            "What is the weather like today in New York City?"
        ));

        ModelContextService modelContextService = new ModelContextService(
            new TornadoApi([
                new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
        new ProviderAuthentication(LLmProviders.Anthropic, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                ]),
            contextContainer);

        modelContextService.AddModelToLibrary("expensive", ChatModel.OpenAi.Gpt5.V5, "Best for general purpose tasks with high accuracy.");
        modelContextService.AddModelToLibrary("cheap", ChatModel.OpenAi.Gpt4.Turbo, "Good for less complex tasks where cost is a concern.");
        modelContextService.AddModelToLibrary("ethical", ChatModel.Anthropic.Claude35.SonnetLatest, "Useful for tasks requiring strong safety and ethical considerations.");
        modelContextService.AddModelToLibrary("thinking", ChatModel.OpenAi.O3.V3, "Well-rounded and powerful model across domains. It sets a new standard for math, science, coding, and visual reasoning tasks. It also excels at technical writing and instruction-following. Use it to think through multi-step problems that involve analysis across text, code, and images");

        ChatModel selectedModel = await modelContextService.GetModelContext();

        Console.WriteLine($"Selected Model: {selectedModel.GetApiName}");
    }
}