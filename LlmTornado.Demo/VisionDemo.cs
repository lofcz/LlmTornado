using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Images;
using LlmTornado.Models;

namespace LlmTornado.Demo;

public static class VisionDemo
{
    public static async Task Vision()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletion([
            new ChatMessage(ChatMessageRoles.User, [new ChatMessagePart(new Uri("https://encrypted-tbn3.gstatic.com/images?q=tbn:ANd9GcSGfpQ3m-QWiXgCBJJbrcUFdNdWAhj7rcUqjeNUC6eKcXZDAtWm"))]), new ChatMessage(ChatMessageRoles.User, "What is on this image?")
        ], ChatModel.OpenAi.Gpt4.VisionPreview, maxTokens: 256);

        Console.WriteLine(result?.Choices?[0].Message?.Content);
    }

    public static async Task VisionBase64()
    {
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/catBoi.jpg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";

        ChatResult? result = await Program.Connect().Chat.CreateChatCompletion([
            new ChatMessage(ChatMessageRoles.User, [new ChatMessagePart(base64, ImageDetail.Auto)]), new ChatMessage(ChatMessageRoles.User, "What is on this image?")
        ], ChatModel.OpenAi.Gpt4.VisionPreview, maxTokens: 256);

        Console.WriteLine(result?.Choices?[0].Message?.Content);
    }
}