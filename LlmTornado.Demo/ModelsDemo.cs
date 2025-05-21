using LlmTornado.Code;
using LlmTornado.Models;

namespace LlmTornado.Demo;

public class ModelsDemo
{
    static async Task GetProviderModels(LLmProviders provider)
    {
        List<RetrievedModel>? models = await Program.Connect().Models.GetModels(provider);

        Console.WriteLine("Available models");
        Console.WriteLine("------------------------");
        
        foreach (RetrievedModel model in models.OrderByDescending(x => x.Created))
        {
            Console.WriteLine(model);
        }
    }
    
    [TornadoTest]
    public static async Task GetModelsOpenAi()
    {
        await GetProviderModels(LLmProviders.OpenAi);
    }
    
    [TornadoTest]
    public static async Task GetModelsAnthropic()
    {
        await GetProviderModels(LLmProviders.Anthropic);
    }
    
    [TornadoTest]
    public static async Task GetModelsGoogle()
    {
        await GetProviderModels(LLmProviders.Google);
    }
    
    [TornadoTest]
    public static async Task GetModelsCohere()
    {
        await GetProviderModels(LLmProviders.Cohere);
    }
}