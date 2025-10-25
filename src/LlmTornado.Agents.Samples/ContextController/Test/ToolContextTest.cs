using LlmTornado.Chat.Models;
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
namespace LlmTornado.Agents.Samples.ContextController;

public static class ToolContextTest
{
    public static async Task TestToolContextService()
    {
        ContextContainer contextContainer = new ContextContainer();
        contextContainer.Goal = "Assist the user with their requests using appropriate tools and models.";
        contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
            ChatMessageRoles.System,
            contextContainer.Goal
        ));
        contextContainer.ChatMessages.Add(new LlmTornado.Chat.ChatMessage(
            ChatMessageRoles.User,
            "What is the weather like today?"
        ));

        var api = new TornadoApi([
                        new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                new ProviderAuthentication(LLmProviders.Anthropic, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                        ]);

        ToolContextService toolContextService = new ToolContextService(api, contextContainer);

        toolContextService.AddToolToLibrary(
            toolKey: "get_weather",
            tool: new Tool(new ToolFunction("get_weather", "Gets details of the weather", new
            {
                type = "object",
                properties = new
                {
                    location = new
                    {
                        type = "string",
                        description = "location to get weather"
                    }
                },
                required = new List<string> { "location" }
            })),
            description: "Fetches the current weather information for a specified location.");

        toolContextService.AddToolToLibrary(
            toolKey: "get_order",
            tool: new Tool(new ToolFunction("get_order", "Gets details of the order", new
            {
                type = "object",
                properties = new
                {
                    id = new
                    {
                        type = "string",
                        description = "id of the order to fetch"
                    }
                },
                required = new List<string> { "location" }
            })),
            description: "Fetches the current order information for a specified id.");

        toolContextService.AddToolToLibrary(
            toolKey: "get_location",
            tool: new Tool(new ToolFunction("get_location", "Gets location of user")),
            description: "Get the users location");

        toolContextService.AddToolToLibrary(
            toolKey: "order_taco_bell",
            tool: new Tool(new ToolFunction("order_tacos", "Gets tacos for user")),
            description: "Orders the users tacos from taco bell.");

        var tools = await toolContextService.GetToolContext();

        Console.WriteLine("Selected Tools:");
        foreach (var tool in tools)
        {
            Console.WriteLine($"- {tool.ToolName}");
        }
    }
}