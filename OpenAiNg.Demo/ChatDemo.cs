using System.Text;
using Newtonsoft.Json;
using OpenAiNg.Chat;
using OpenAiNg.ChatFunctions;
using OpenAiNg.Common;

namespace OpenAiNg.Demo;

public static class ChatDemo
{
    public static async Task<ChatResult> Completion()
    {
        ChatResult result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Model = Models.Model.GPT4_Turbo_Preview,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRole.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRole.User, "2+2=?")
            ]
        });

        return result;
    }


    public static async Task Anthropic()
    {
       Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
       {
           Model = Models.Model.Claude3Sonnet
       });
       chat.AppendSystemMessage("You are a dog and you woof");
       chat.AppendUserInput("Who are you?");

       string? str = await chat.GetResponseFromChatbotAsync();
    }


    public static async Task<string> StreamWithFunctions()
    {
        StringBuilder sb = new StringBuilder();
        
        Conversation chat = Program.Connect().Chat.CreateConversation();
        chat.RequestParameters.Tools = [
            new Tool(new ToolFunction("get_weather", "gets the current weather"))
        ];
        chat.OnAfterToolsCall = async (result) =>
        {
            string? str = await chat.GetResponseFromChatbotAsync();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        
        Guid msgId = Guid.NewGuid(); 
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today?", msgId);
        
        await chat.StreamResponseEnumerableFromChatbotAsyncWithFunctions(msgId, (x) =>
        {
            sb.Append(x);
            return Task.CompletedTask;
        }, functions =>
        {
            List<FunctionResult> results = [];
            
            foreach (FunctionCall fn in functions)
            {
                results.Add(new FunctionResult(fn.Name, new
                {
                    result = "ok",
                    weather = "A mild rain is expected around noon."
                }));
            }

            return Task.FromResult(results);
        }, null, null);


        string response = sb.ToString();
        return response;
    }
}