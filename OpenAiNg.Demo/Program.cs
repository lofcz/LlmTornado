using Newtonsoft.Json;
using OpenAiNg.Code;

namespace OpenAiNg.Demo;

    
public enum Demos
{
    Unknown,
    ChatVision,
    ChatVisionBase64,
    AssistantList,
    AssistantCreate,
    AssistantCreateWithCustomFunction,
    AssistantRetrieve,
    AssistantModify,
    AssistantDelete,
    FilesUpload,
    ImagesGenerate,
    AssistantCreateWithFile,
    AssistantListFiles,
    AssistantAttachFile,
    AssistantRetriveFile,
    AssistantRemoveFile,
    ThreadsCreate,
    ThreadsRetrieve,
    ThreadsModify,
    ThreadsDelete,
    ThreadsCreateMessage,
    ChatCompletion,
    ChatStreamWithFunctions,
    ChatAnthropic,
    ChatStreamingAnthropic,
    ChatAzure,
    ChatOpenAiFunctions,
    ChatAnthropicFunctions,
    ChatAnthropicFailFunctions,
    Last
}

public class Program
{
    private static Demos selectedDemo = Demos.Unknown;
    private static Keys ApiKeys { get; set; }

    class AzureKey
    {
        public string Version { get; set; }
        public string ApiUrlFormat { get; set; }
        public string Key { get; set; }
    }
    
    class Keys
    {
        public string OpenAi { get; set; }
        public string Anthropic { get; set; }
        public AzureKey Azure { get; set; }
    }

    public static OpenAiApi Connect(LLmProviders provider = LLmProviders.OpenAi)
    {
        if (provider is LLmProviders.AzureOpenAi)
        {
            return new OpenAiApi(ApiKeys.Azure.Key)
            {
                ApiVersion = ApiKeys.Azure.Version,
                ApiUrlFormat = ApiKeys.Azure.ApiUrlFormat
            };
        }
        
        return new OpenAiApi(provider is LLmProviders.OpenAi ? ApiKeys.OpenAi : ApiKeys.Anthropic);
    }

    public static async Task<bool> SetupApi()
    {
        string? projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            Console.WriteLine("Failed to read project directory path, see Program.cs, SetupApi()");
            return false;
        }

        if (!File.Exists($"{projectDirectory}\\apiKey.json"))
        {
            Console.WriteLine("Please copy and paste apiKeyPrototype.json file in the same folder, rename the copy as apiKey.json and replace the string inside with your API key");
            return false;
        }

        string apiKey = await File.ReadAllTextAsync($"{projectDirectory}\\apiKey.json");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("API key not set, please place your API key in apiKey.json file");
            return false;
        }

        ApiKeys = JsonConvert.DeserializeObject<Keys>(apiKey) ?? throw new Exception("Invalid content of apiKey.json");
        return true;
    }

    public static Func<Task>? GetDemo(Demos demo)
    {
        Func<Task>? task = demo switch
        {
            Demos.ChatVisionBase64 => VisionDemo.VisionBase64,
            Demos.ChatVision => VisionDemo.VisionBase64,
            Demos.AssistantList => AssistantsDemo.List,
            Demos.AssistantCreate => AssistantsDemo.Create,
            Demos.AssistantCreateWithCustomFunction => AssistantsDemo.CreateWithCustomFunction,
            Demos.AssistantRetrieve => AssistantsDemo.Retrieve,
            Demos.AssistantModify => AssistantsDemo.Modify,
            Demos.AssistantDelete => AssistantsDemo.Delete,
            Demos.FilesUpload => FilesDemo.Upload,
            Demos.ImagesGenerate => ImagesDemo.Generate,
            Demos.AssistantCreateWithFile => AssistantsDemo.CreateWithFile,
            Demos.AssistantListFiles => AssistantsDemo.ListFiles,
            Demos.AssistantAttachFile => AssistantsDemo.AttachFile,
            Demos.AssistantRetriveFile => AssistantsDemo.RetrieveFile,
            Demos.AssistantRemoveFile => AssistantsDemo.RemoveFile,
            Demos.ThreadsCreate => ThreadsDemo.Create,
            Demos.ThreadsRetrieve => ThreadsDemo.Retrieve,
            Demos.ThreadsModify => ThreadsDemo.Modify,
            Demos.ThreadsDelete => ThreadsDemo.Delete,
            Demos.ThreadsCreateMessage => ThreadsDemo.CreateMessage,
            Demos.ChatCompletion => ChatDemo.Completion,
            Demos.ChatStreamWithFunctions => ChatDemo.StreamWithFunctions,
            Demos.ChatAnthropic => ChatDemo.Anthropic,
            Demos.ChatStreamingAnthropic => ChatDemo.AnthropicStreaming,
            Demos.ChatAzure => ChatDemo.Azure,
            Demos.ChatOpenAiFunctions => ChatDemo.OpenAiFunctions,
            Demos.ChatAnthropicFunctions => ChatDemo.AnthropicFunctions,
            Demos.ChatAnthropicFailFunctions => ChatDemo.AnthropicFailFunctions,
            _ => null
        };

        return task;
    }
    
    public static async Task Main(string[] args)
    {
        if (!await SetupApi())
        {
            return;
        }

        selectedDemo = Demos.Last - 1;
        Func<Task>? task = GetDemo(selectedDemo);

        if (task is not null)
        {
            await task.Invoke();
        }
    }
}