using System.ComponentModel;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Demo;

public class InfraDemo : DemoBase
{
    
    [TornadoTest]
    public static async Task TornadoFunction()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((string location) =>
                {
                    return "";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });
        Console.Write(serialized);
    }
}