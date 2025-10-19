using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Models;


namespace LlmTornado.Demo;

public class ModelsDemo : DemoBase
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
    public static async Task AccessAllModels()
    {
        List<IModel> m = ChatModel.Anthropic.AllModels;
        m = ChatModel.OpenAi.AllModels;
        m = ChatModel.Google.AllModels;
        m = ChatModel.Groq.AllModels;
        m = ChatModel.DeepSeek.AllModels;
        m = ChatModel.Perplexity.AllModels;
        m = ChatModel.XAi.AllModels;
        m = ChatModel.Cohere.AllModels;
        m = ChatModel.Mistral.AllModels;
        m = ChatModel.DeepInfra.AllModels;
        m = ChatModel.Blablador.AllModels;
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
    
    [TornadoTest]
    public static async Task GetModelsDeepSeek()
    {
        await GetProviderModels(LLmProviders.DeepSeek);
    }
    
    [TornadoTest]
    public static async Task GetModelsGroq()
    {
        await GetProviderModels(LLmProviders.Groq);
    }
    
    [TornadoTest]
    public static async Task GetModelsMistral()
    {
        await GetProviderModels(LLmProviders.Mistral);
    }
    
    [TornadoTest]
    public static async Task GetModelsXAi()
    {
        await GetProviderModels(LLmProviders.XAi);
    }
    
    [TornadoTest]
    public static async Task GetModelsBlablador()
    {
        await GetProviderModels(LLmProviders.Blablador);
    }
}