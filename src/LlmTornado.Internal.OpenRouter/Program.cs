using LlmTornado.Code;
using LlmTornado.Models;

namespace LlmTornado.Internal.OpenRouter;

class Program
{
    static async Task Main(string[] args)
    {
        // no auth needed
        List<RetrievedModel>? models = await new TornadoApi(LLmProviders.OpenRouter).Models.GetModels(LLmProviders.OpenRouter);

        int z = 0;
        
        Console.WriteLine("Hello, World!");
    }
}