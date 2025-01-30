using LlmTornado.Assistants;
using LlmTornado.Common;
using LlmTornado.Models;
using File = LlmTornado.Files.File;

namespace LlmTornado.Demo;

public static class AssistantsDemo
{
    private static string GenerateName() => $"demo_assistant_{DateTime.Now.Ticks}";
    private static Assistant? generatedAssistant;

    public static async Task<IReadOnlyList<Assistant>> List()
    {
        HttpCallResult<ListResponse<Assistant>> response = await Program.Connect().Assistants.ListAssistantsAsync();
        Console.WriteLine(response.Response);
        return response.Data!.Items;
    }

    public static async Task<Assistant?> Create()
    {
        HttpCallResult<Assistant> response = await Program.Connect().Assistants
            .CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, GenerateName(), "test model",
                "system prompt"));
        generatedAssistant = response.Data!;
        Console.WriteLine(response.Response);
        return response.Data;
    }

    public static async Task CreateWithCustomFunction()
    {
        //TODO
    }

    public static async Task CreateFileSearchAssistant()
    {
        //TODO
    }

    public static async Task CreateWithCodeInterpreter()
    {
        //TODO
    }

    public static async Task<Assistant?> Retrieve()
    {
        if (generatedAssistant is null)
        {
            await Create();
        }

        HttpCallResult<Assistant> response =
            await Program.Connect().Assistants.RetrieveAssistantAsync(generatedAssistant!.Id);
        Console.WriteLine(response.Response);
        return response.Data;
    }

    public static async Task<Assistant?> Modify()
    {
        if (generatedAssistant is null)
        {
            await Create();
        }

        generatedAssistant!.Description = "modified description";
        HttpCallResult<Assistant> response =
            await Program.Connect().Assistants.ModifyAssistantAsync(generatedAssistant, generatedAssistant);
        Console.WriteLine(response.Response);
        return response.Data;
    }

    public static async Task<bool> Delete()
    {
        if (generatedAssistant is null)
        {
            await Create();
        }

        HttpCallResult<bool> response = await Program.Connect().Assistants.DeleteAssistantAsync(generatedAssistant!);
        Console.WriteLine(response.Response);
        return response.Data;
    }

    public static async Task DeleteAllDemoAssistants()
    {
        IReadOnlyList<Assistant> assistants = await List();
        List<Task<HttpCallResult<bool>>> tasks = (from assistant in assistants
            where assistant.Name.StartsWith("demo_assistant")
            select Program.Connect().Assistants.DeleteAssistantAsync(assistant.Id)).ToList();
        Console.WriteLine($"Deleting {tasks.Count} assistants...");
        await Task.WhenAll(tasks);
    }
}