using LlmTornado.Caching;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Demo;

public class CachingDemo
{
    public static async Task<HttpCallResult<CachedContentInformation>> Create()
    {
        string text = await File.ReadAllTextAsync("Static/Files/a11.txt");

        HttpCallResult<CachedContentInformation> cachingResult = await Program.Connect().Caching.Create(new CreateCachedContentRequest(60, ChatModel.Google.Gemini.Gemini15Pro002, [
            new CachedContent([
                new ChatMessagePart(text)
            ], CachedContentRoles.User)
        ]));

        return cachingResult;
    }
}