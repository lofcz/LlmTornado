using OpenAiNg.Assistants;
using OpenAiNg.Common;
using OpenAiNg.Models;
using File = OpenAiNg.Files.File;

namespace OpenAiNg.Demo;

public class AssistantsDemo
{
    public static async Task List()
    {
        ListResponse<AssistantResponse>? response = await Program.Connect().Assistants.ListAssistantsAsync(new ListQuery(1));
        ListResponse<AssistantResponse>? response2 = await Program.Connect().Assistants.ListAssistantsAsync(new ListQuery(1, after: response?.LastId));
        AssistantResponse? first = response?.Items.FirstOrDefault();
    }

    public static async Task<AssistantResponse?> Create()
    {
        AssistantResponse? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval
        }));

        return result;
    }
    
    public static async Task CreateWithCustomFunction()
    {
        AssistantResponse? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<Tool>
        {
            new Tool(new ToolFunction("my_function", "test function", new
            {
                type = "object",
                properties = new {
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
        AssistantResponse? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval,
            Tool.CodeInterpreter
        }));

        AssistantResponse? retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(result?.Id);
    }
    
    public static async Task Modify()
    {
        AssistantResponse? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval,
            Tool.CodeInterpreter
        }));

        AssistantResponse? modifyResult = await Program.Connect().Assistants.ModifyAssistantAsync(result?.Id, new CreateAssistantRequest(result, name: "my model renamed"));
        
        AssistantResponse? retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(modifyResult?.Id);
    }
    
    public static async Task Delete()
    {
        AssistantResponse? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "retrieve model", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval,
            Tool.CodeInterpreter
        }));

        bool deleted = await Program.Connect().Assistants.DeleteAssistantAsync(result?.Id);
        
        AssistantResponse? retrievalResult = await Program.Connect().Assistants.RetrieveAssistantAsync(result?.Id);
    }

    public static async Task<AssistantResponse?> CreateWithFile()
    {
        File? file = await FilesDemo.Upload();
        
        AssistantResponse? result = await Program.Connect().Assistants.CreateAssistantAsync(new CreateAssistantRequest(Model.GPT35_Turbo_1106, "model1", "test model", "system prompt", new List<Tool>
        {
            Tool.Retrieval
        }, [ file?.Id ]));

        return result;
    }
    
    public static async Task ListFiles()
    {
        AssistantResponse? assistant = await CreateWithFile();
        ListResponse<AssistantFileResponse>? result = await Program.Connect().Assistants.ListFilesAsync(assistant);
    }
    
    public static async Task AttachFile()
    {
        AssistantResponse? assistant = await Create();
        ListResponse<AssistantFileResponse>? result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);
        
        result = await Program.Connect().Assistants.ListFilesAsync(assistant);
    }
    
    public static async Task RetrieveFile()
    {
        AssistantResponse? assistant = await Create();
        ListResponse<AssistantFileResponse>? result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);
        
        AssistantFileResponse? retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);
    }
    
    public static async Task RemoveFile()
    {
        AssistantResponse? assistant = await Create();
        ListResponse<AssistantFileResponse>? result = await Program.Connect().Assistants.ListFilesAsync(assistant);

        File? file = await FilesDemo.Upload();
        await Program.Connect().Assistants.AttachFileAsync(assistant.Id, file);
        
        AssistantFileResponse? retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);

        await Program.Connect().Assistants.RemoveFileAsync(assistant.Id, file);
        
        retrieveResult = await Program.Connect().Assistants.RetrieveFileAsync(assistant.Id, file.Id);
    }
}