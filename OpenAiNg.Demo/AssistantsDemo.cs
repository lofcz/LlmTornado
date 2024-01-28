using OpenAiNg.Assistants;
using OpenAiNg.Common;
using OpenAiNg.Models;
using File = OpenAiNg.Files.File;

namespace OpenAiNg.Demo;

public static class AssistantsDemo
{
    public static async Task List()
    {
        HttpCallResult<ListResponse<AssistantResponse>> response = await Program.Connect().Assistants.ListAssistantsAsync(new ListQuery(1));
        HttpCallResult<ListResponse<AssistantResponse>> response2 = await Program.Connect().Assistants.ListAssistantsAsync(new ListQuery(1, after: response.Data?.LastId));
        AssistantResponse? first = response.Data?.Items.FirstOrDefault();
    }

    public static async Task<AssistantResponse?> Create()
    {
        HttpCallResult<AssistantResponse> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval
        }));

        return result.Data;
    }

    public static async Task CreateWithCustomFunction()
    {
        HttpCallResult<AssistantResponse> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<Tool>
        {
            new(new ToolFunction("my_function", "test function", new
            {
                type = "object",
                properties = new
                {
                    arg1 = new
                    {
                        type = "string",
                        description = "argument 1 description"
                    }
                },
                required = new List<string> { "arg1" }
            }))
        }));
    }

    public static async Task Retrieve()
    {
        HttpCallResult<AssistantResponse>? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval,
            Tool.CodeInterpreter
        }));

        HttpCallResult<AssistantResponse> retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(result.Data.Id);
    }

    public static async Task Modify()
    {
        HttpCallResult<AssistantResponse>? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval,
            Tool.CodeInterpreter
        }));

        HttpCallResult<AssistantResponse>? modifyResult = await Program.Connect().Assistants.ModifyAssistantAsync(result.Data?.Id, new CreateAssistantRequest(result.Data, name: "my model renamed"));
        HttpCallResult<AssistantResponse> retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(modifyResult.Data?.Id);
    }

    public static async Task Delete()
    {
        HttpCallResult<AssistantResponse> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval,
            Tool.CodeInterpreter
        }));

        HttpCallResult<bool> deleted = await Program.Connect().Assistants.DeleteAssistantAsync(result.Data?.Id);

        HttpCallResult<AssistantResponse> retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(result.Data?.Id);
    }

    public static async Task<AssistantResponse?> CreateWithFile()
    {
        File? file = await FilesDemo.Upload();

        HttpCallResult<AssistantResponse> result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval
        }, [file?.Id]));

        return result.Data;
    }

    public static async Task ListFiles()
    {
        AssistantResponse? assistant = await CreateWithFile();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);
    }

    public static async Task AttachFile()
    {
        AssistantResponse? assistant = await Create();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);

        result = await Program.Connect().Assistants.ListFilesAsync(assistant);
    }

    public static async Task RetrieveFile()
    {
        AssistantResponse? assistant = await Create();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);

        HttpCallResult<AssistantFileResponse> retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);
    }

    public static async Task RemoveFile()
    {
        AssistantResponse? assistant = await Create();
        HttpCallResult<ListResponse<AssistantFileResponse>> result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);

        HttpCallResult<AssistantFileResponse> retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);

        await Program.Connect().Assistants.RemoveFileAsync(assistant.Id, file);

        retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);
    }
}