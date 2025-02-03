using LlmTornado.Caching;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Demo;

public class CachingDemo
{
    [TornadoTest]
    public static async Task<HttpCallResult<CachedContentInformation>> Create()
    {
        string text = await File.ReadAllTextAsync("Static/Files/a11.txt");

        HttpCallResult<CachedContentInformation> cachingResult = await Program.Connect().Caching.Create(new CreateCachedContentRequest(60, ChatModel.Google.Gemini.Gemini15Pro002, [
            new CachedContent([
                new ChatMessagePart(text)
            ], CachedContentRoles.User)
        ]));
        
        Console.WriteLine(cachingResult.Data.Name);
        Console.WriteLine(cachingResult.Data.ExpireTime);
        
        return cachingResult;
    }

    [TornadoTest]
    public static async Task List()
    {
        HttpCallResult<CachedContentInformation> created = await Create();
        HttpCallResult<CachedContentList> listed = await Program.Connect().Caching.List();
        
        Console.WriteLine($"Listed:");

        foreach (var item in listed.Data.CachedContents)
        {
            Console.WriteLine($"{item.Name} - {item.ExpireTime}");
        }
    }
    
    [TornadoTest]
    public static async Task Get()
    {
        HttpCallResult<CachedContentInformation> created = await Create();
        HttpCallResult<CachedContentInformation> retrieved = await Program.Connect().Caching.Get(created.Data.Name);
        
        Console.WriteLine($"Get:");
        Console.WriteLine($"{retrieved.Data.Name} - {retrieved.Data.ExpireTime}");
    }
    
    [TornadoTest]
    public static async Task Patch()
    {
        HttpCallResult<CachedContentInformation> created = await Create();
        HttpCallResult<CachedContentInformation> patched = await Program.Connect().Caching.Patch(created.Data.Name, 300);
        
        Console.WriteLine($"Patched:");
        Console.WriteLine($"{patched.Data.Name} - {patched.Data.ExpireTime}");
    }
    
    [TornadoTest]
    public static async Task Delete()
    {
        HttpCallResult<CachedContentInformation> created = await Create();
        
        HttpCallResult<bool> deleted = await Program.Connect().Caching.Delete(created.Data.Name);

        if (deleted.Ok)
        {
            Console.WriteLine($"Cached resource deleted");
        }

        try
        {
            await Program.Connect().Caching.Delete(created.Data.Name);
        }
        catch (Exception e) // we forced exception on http level
        {
            Console.WriteLine($"Cached resource already deleted");
        }
    }
}