using LlmTornado.Assistants;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Models;
using LlmTornado.VectorStores;

namespace LlmTornado.Demo;

public static class AssistantsDemo
{
    private static string GenerateName() => $"demo_assistant_{DateTime.Now.Ticks}";
    private static Assistant? generatedAssistant;

    [TornadoTest]
    public static async Task<IReadOnlyList<Assistant>> List()
    {
        HttpCallResult<ListResponse<Assistant>> response = await Program.Connect().Assistants.ListAssistantsAsync();
        Console.WriteLine(response.Response);
        return response.Data!.Items;
    }

    [TornadoTest]
    public static async Task<Assistant?> Create()
    {
        HttpCallResult<Assistant> response = await Program.Connect().Assistants
            .CreateAssistantAsync(new CreateAssistantRequest(ChatModel.OpenAi.Gpt4.O241120, GenerateName(), "test model",
                "system prompt"));
        generatedAssistant = response.Data!;
        Console.WriteLine(response.Response);
        return response.Data;
    }

    [TornadoTest]
    public static async Task<Assistant> CreateFunctionAssistant()
    {
        HttpCallResult<Assistant> response = await Program.Connect().Assistants.CreateAssistantAsync(
            new CreateAssistantRequest(
                null,
                GenerateName(),
                "Function Demo Assistant",
                "You are a helpful assistant with the ability to call functions related to weather")
            {
                Tools = new List<AssistantTool>
                {
                    new AssistantToolFunction(new ToolFunctionConfig(
                        "get_weather",
                        "Get current temperature for a given city",
                        new
                        {
                            type = "object",
                            properties = new
                            {
                                location = new
                                {
                                    type = "string",
                                    description = "City and Country"
                                }
                            },
                            required = new List<string> {"location"},
                            additionalProperties = false
                        }
                    )),
                    new AssistantToolFunction(new ToolFunctionConfig(
                        "get_humidity",
                        "Get current humidity for a given city",
                        new
                        {
                            type = "object",
                            properties = new
                            {
                                location = new
                                {
                                    type = "string",
                                    description = "City and Country"
                                }
                            },
                            required = new List<string> {"location"},
                            additionalProperties = false
                        }
                    ))
                }
            });

        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<Assistant> CreateFileSearchAssistant()
    {
        VectorStoreFile vectorStoreFile = await VectorStoreDemo.CreateVectorStoreFile();

        HttpCallResult<Assistant> response = await Program.Connect().Assistants.CreateAssistantAsync(
            new CreateAssistantRequest(
                null,
                GenerateName(),
                "FileSearch Demo Assistant",
                "You are a helpful assistant with the ability to search files.")
            {
                Tools = new List<AssistantTool>
                {
                    new AssistantToolFileSearch()
                },
                ToolResources = new ToolResources
                {
                    FileSearch = new FileSearchConfig
                    {
                        FileSearchFileIds = new List<string> {vectorStoreFile.VectorStoreId}
                    }
                }
            });

        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<Assistant> CreateWithCodeInterpreter()
    {
        VectorStoreFile vectorStoreFile = await VectorStoreDemo.CreateVectorStoreFile();

        HttpCallResult<Assistant> response = await Program.Connect().Assistants.CreateAssistantAsync(
            new CreateAssistantRequest(
                null,
                GenerateName(),
                "Code Interpreter Demo Assistant",
                "You are a helpful assistant with code interpretation capabilities.")
            {
                Tools = new List<AssistantTool>
                {
                    AssistantToolCodeInterpreter.Inst
                },
                ToolResources = new ToolResources
                {
                    CodeInterpreter = new CodeInterpreterConfig
                    {
                        CodeInterpreterFileIds = new List<string> {vectorStoreFile.Id}
                    }
                }
            });

        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<Assistant?> Retrieve()
    {
        generatedAssistant ??= await Create();

        HttpCallResult<Assistant> response =
            await Program.Connect().Assistants.RetrieveAssistantAsync(generatedAssistant!.Id);
        Console.WriteLine(response.Response);
        return response.Data;
    }

    [TornadoTest]
    public static async Task<Assistant?> Modify()
    {
        generatedAssistant ??= await Create();

        CreateAssistantRequest modifyRequest = new CreateAssistantRequest(generatedAssistant!)
        {
            Description = "modified description"
        };

        HttpCallResult<Assistant> response =
            await Program.Connect().Assistants.ModifyAssistantAsync(generatedAssistant!.Id, modifyRequest);
        Console.WriteLine(response.Response);
        return response.Data;
    }

    [TornadoTest]
    public static async Task<bool> Delete()
    {
        generatedAssistant ??= await Create();

        HttpCallResult<bool> response = await Program.Connect().Assistants.DeleteAssistantAsync(generatedAssistant!);
        Console.WriteLine(response.Response);
        generatedAssistant = null;
        return response.Data;
    }

    [TornadoTest]
    public static async Task DeleteAllDemoAssistants()
    {
        IReadOnlyList<Assistant> assistants = await List();
        List<Task<HttpCallResult<bool>>> tasks = (from assistant in assistants
            where assistant.Name.StartsWith("demo_assistant")
            select Program.Connect().Assistants.DeleteAssistantAsync(assistant.Id)).ToList();
        Console.WriteLine($"Deleting {tasks.Count} assistants...");
        generatedAssistant = null;
        await Task.WhenAll(tasks);
    }
}