using OpenAiNg.Audio;
using OpenAiNg.Chat;
using OpenAiNg.Code;
using OpenAiNg.Models;

namespace OpenAiNg.Demo;

public static class VisionDemo
{
    public static async Task Vision()
    {
        ChatResult result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatMessage[]
        {
            new ChatMessage(ChatMessageRole.User, [
                new ChatMessagePart(new Uri("https://encrypted-tbn3.gstatic.com/images?q=tbn:ANd9GcSGfpQ3m-QWiXgCBJJbrcUFdNdWAhj7rcUqjeNUC6eKcXZDAtWm")),
            ]),
            new ChatMessage(ChatMessageRole.User, "What is on this image?")
        }, Model.GPT4_Vision_Preview, max_tokens: 256);

        Console.WriteLine(result.Choices[0].Message.Content);
        Console.ReadKey();
        
        int z = 0;
    }
}