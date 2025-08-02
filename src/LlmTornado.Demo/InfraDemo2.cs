using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Infra;
using LlmTornado.Responses;

namespace LlmTornado.Demo;

public class InfraDemo2 : DemoBase
{
    [TornadoTest]
    public static async Task JsonSchema2()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini25Flash,
            Tools =
            [
                new Tool([
                    new ToolParam("name", new ToolParamString()),
                ], "generate_mock_data")
            ],
            ToolChoice = OutboundToolChoice.Required,
            ReasoningBudget = 0
        });

        conversation.AddUserMessage("Use realistic mock data for the provided function.");
        
        ChatRichResponse data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task JsonStructuredSchema2()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini25Flash,
            ResponseFormat = ChatRequestResponseFormats.StructuredJson([
                new ToolParam("name", new ToolParamString("name of a person")),
                new ToolParam("popular_games", new ToolParamListAtomic("a list of popular games", ToolParamAtomicTypes.String))
            ], "generate_mock_data"),
            ReasoningBudget = 0
        });

        conversation.AddUserMessage("Generate realistic mock data.");

        TornadoRequestContent ss = conversation.Serialize();
        
        ChatRichResponse data = await conversation.GetResponseRich();

        int z = 0;
    }
}