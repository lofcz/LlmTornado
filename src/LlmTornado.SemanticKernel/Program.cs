using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;

namespace LlmTornado.SemanticKernel;

public class Program
{
    public enum TestEnum
    {
        Opt1,
        Opt2,
        Opt3
    }
    
    class ApiKeys
    {
        public string OpenAi { get; set; }
    }
    
    public static async Task Main(string[] args)
    {
        ApiKeys apiKeys = JsonConvert.DeserializeObject<ApiKeys>(await File.ReadAllTextAsync("apiKey.json"));
        
        KernelFunction method = KernelFunctionFactory.CreateFromMethod(([Description("hello")] string text, string enm) =>
        {
            return 10;
        }, functionName: "test", parameters: [
            new KernelParameterMetadata(new KernelParameterMetadata("text")
            {
                ParameterType = typeof(string)
            }),
            new KernelParameterMetadata(new KernelParameterMetadata("enm")
            {
                Description = "choose from limited options", 
                ParameterType = typeof(TestEnum),
                Schema = KernelJsonSchema.Parse("""
                                                {
                                                    "type": "string",
                                                    "enum": ["Option1", "Option2"],
                                                    "description": "Limited enum values"
                                                }
                                                """)
            })
        ]);
        
        Kernel kernel = Kernel.CreateBuilder()
            .AddOpenAIChatClient("gpt-4.1", apiKeys.OpenAi)
            .Build();
        
        kernel.Plugins.AddFromFunctions("myPlugin", [ method ]);
        
        string userPrompt = "Použij funkci test a nastav text na 'ahoj' a enm na 'Option1'.";

        FunctionResult result = await kernel.InvokePromptAsync(userPrompt, new KernelArguments(new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
            }));

        int z = 0;
    }
}