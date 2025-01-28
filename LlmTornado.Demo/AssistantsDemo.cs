using LlmTornado.Assistants;
using LlmTornado.Common;
using LlmTornado.Models;
using File = LlmTornado.Files.File;

namespace LlmTornado.Demo;

public static class AssistantsDemo
{
    public static async Task List()
    {
        HttpCallResult<ListResponse<Assistant>> response = await Program.Connect().Assistants.ListAssistantsAsync(new ListQuery(1));
        HttpCallResult<ListResponse<Assistant>> response2 = await Program.Connect().Assistants.ListAssistantsAsync(new ListQuery(1, after: response.Data?.LastId));
        Assistant? first = response.Data?.Items.FirstOrDefault();
    }

    public static async Task<Assistant?> Create()
    {
        HttpCallResult<Assistant> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<AssistantTool>
        {
            AssistantToolCodeInterpreter.Default
        }));

        return result.Data;
    }

    public static async Task CreateWithCustomFunction()
    {
        //TODO
        // HttpCallResult<Assistant> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<Tool>
        // {
        //     new(new ToolFunction("my_function", "test function", new
        //     {
        //         type = "object",
        //         properties = new
        //         {
        //             arg1 = new
        //             {
        //                 type = "string",
        //                 description = "argument 1 description"
        //             }
        //         },
        //         required = new List<string> { "arg1" }
        //     }))
        // }));
    }

    public static async Task Retrieve()
    {
        HttpCallResult<Assistant>? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<AssistantTool>
        {
            AssistantToolFileSearch.Default
        }));

        HttpCallResult<Assistant> retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(result.Data.Id);
    }

    public static async Task Modify()
    {
        HttpCallResult<Assistant>? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<AssistantTool>
        {
            AssistantToolFileSearch.Default
        }));

        HttpCallResult<Assistant>? modifyResult = await Program.Connect().Assistants.ModifyAssistantAsync(result.Data?.Id, new CreateAssistantRequest(result.Data, name: "my model renamed"));
        HttpCallResult<Assistant> retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(modifyResult.Data?.Id);
    }

    public static async Task Delete()
    {
        HttpCallResult<Assistant> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<AssistantTool>
        {
            AssistantToolFileSearch.Default
        }));

        HttpCallResult<bool> deleted = await Program.Connect().Assistants.DeleteAssistantAsync(result.Data?.Id);

        HttpCallResult<Assistant> retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(result.Data?.Id);
    }

    public static async Task<Assistant?> CreateWithFile()
    {
        File? file = await FilesDemo.Upload();

        HttpCallResult<Assistant> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<AssistantTool>
        {
            AssistantToolFileSearch.Default
        }, new ToolResources()
        {
            FileSearch = null // TODO
        }));

        return result.Data;
    }

    public static async Task ListFiles()
    {
        Assistant? assistant = await CreateWithFile();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);
    }

    public static async Task AttachFile()
    {
        Assistant? assistant = await Create();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);

        result = await Program.Connect().Assistants.ListFilesAsync(assistant);
    }

    public static async Task RetrieveFile()
    {
        Assistant? assistant = await Create();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);

        HttpCallResult<AssistantFileResponse> retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);
    }

    public static async Task RemoveFile()
    {
        Assistant? assistant = await Create();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);

        HttpCallResult<AssistantFileResponse> retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);

        await Program.Connect().Assistants.RemoveFileAsync(assistant.Id, file);

        retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);
    }
}