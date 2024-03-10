using OpenAiNg.Chat;
using OpenAiNg.Images;
using OpenAiNg.Models;

namespace OpenAiNg.Demo;

public static class VisionDemo
{
    public static async Task Vision()
    {
        ChatResult result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatMessage[]
        {
            new(ChatMessageRole.User, [
                new ChatMessagePart(new Uri("https://encrypted-tbn3.gstatic.com/images?q=tbn:ANd9GcSGfpQ3m-QWiXgCBJJbrcUFdNdWAhj7rcUqjeNUC6eKcXZDAtWm"))
            ]),
            new(ChatMessageRole.User, "What is on this image?")
        }, Model.GPT4_Vision_Preview, max_tokens: 256);

        Console.WriteLine(result.Choices[0].Message.Content);
    }

    public static async Task VisionBase64()
    {
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/catBoi.jpg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";

        ChatResult result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatMessage[]
        {
            new(ChatMessageRole.User, [
                new ChatMessagePart(base64, ImageDetail.Auto)
            ]),
            new(ChatMessageRole.User, "What is on this image?")
        }, Model.GPT4_Vision_Preview, max_tokens: 256);

        Console.WriteLine(result.Choices[0].Message.Content);
    }
}