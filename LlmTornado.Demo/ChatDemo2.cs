using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;

namespace LlmTornado.Demo;

public static partial class ChatDemo
{
    [TornadoTest]
    public static async Task MistralLarge()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.PixtralLarge
        });
        
        chat.AppendUserInput("You are a dog, sound authentic. Who are you?");
        string? str = await chat.GetResponse();

        Console.WriteLine("Mistral:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task MistralLargeStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge
        });
        
        chat.AppendUserInput("You are a dog, sound authentic. Who are you?");

        Console.WriteLine("Mistral:");
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = (token) =>
            {
                Console.Write(token);
                return ValueTask.CompletedTask;
            },
            BlockFinishedHandler = (block) =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task MistralChatStreamingTools()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge,
            Tools =
            [
                new Tool(new ToolFunction("get_weather", "gets the current weather in a given city", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The city for which the weather information is required."
                        }
                    },
                    required = new List<string> { "city" }
                }))
            ]
        });
        
        chat.OnAfterToolsCall = async (result) =>
        {
            Console.WriteLine();
            await GetNextResponse();
        };
        
        chat.AppendUserInput([
            new ChatMessagePart("Check the weather today in Paris")
        ]);

        Console.WriteLine("Mistral:");
        await GetNextResponse();

        async Task GetNextResponse()
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                FunctionCallHandler = (fns) =>
                {
                    foreach (FunctionCall fn in fns)
                    {
                        fn.Result = new FunctionResult(fn.Name, new
                        {
                            result = "ok",
                            weather = "A mild rain is expected around noon."
                        });
                    }
                
                    return ValueTask.CompletedTask;
                },
                MessageTokenHandler = (token) =>
                {
                    Console.Write(token);
                    return ValueTask.CompletedTask;
                },
                BlockFinishedHandler = (block) =>
                {
                    Console.WriteLine();
                    return ValueTask.CompletedTask;
                }
            });
        }
    }
}