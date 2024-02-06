using OpenAiNg.Chat;
using OpenAiNg.ChatFunctions;

namespace OpenAiNg.Demo;

public static class ChatDemo
{
    public static async Task Completion()
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

        int y = 0;
    }
}