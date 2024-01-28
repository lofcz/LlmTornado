namespace OpenAiNg.Demo;

internal class Program
{
    private static Demos selectedDemo = Demos.Unknown;
    private static string ApiKey { get; set; }

    public static OpenAiApi Connect()
    {
        return new OpenAiApi(ApiKey);
    }

    private static async Task<bool> SetupApi()
    {
        string? projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            Console.WriteLine("Failed to read project directory path, see Program.cs, SetupApi()");
            Console.ReadKey();
            return false;
        }

        if (!File.Exists($"{projectDirectory}\\apiKey.json"))
        {
            Console.WriteLine("Please copy and paste apiKeyPrototype.json file in the same folder, rename the copy as apiKey.json and replace the string inside with your API key");
            Console.ReadKey();
            return false;
        }

        string apiKey = (await File.ReadAllTextAsync($"{projectDirectory}\\apiKey.json")).Replace("\"", "");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("API key not set, please place your API key in apiKey.json file");
            Console.ReadKey();
            return false;
        }

        ApiKey = apiKey;
        return true;
    }

    public static async Task Main(string[] args)
    {
        if (!await SetupApi()) return;

        selectedDemo = Demos.Last - 1;

        Task? task = selectedDemo switch
        {
            Demos.ChatVisionBase64 => VisionDemo.VisionBase64(),
            Demos.ChatVision => VisionDemo.VisionBase64(),
            Demos.AssistantList => AssistantsDemo.List(),
            Demos.AssistantCreate => AssistantsDemo.Create(),
            Demos.AssistantCreateWithCustomFunction => AssistantsDemo.CreateWithCustomFunction(),
            Demos.AssistantRetrieve => AssistantsDemo.Retrieve(),
            Demos.AssistantModify => AssistantsDemo.Modify(),
            Demos.AssistantDelete => AssistantsDemo.Delete(),
            Demos.FilesUpload => FilesDemo.Upload(),
            Demos.ImagesGenerate => ImagesDemo.Generate(),
            Demos.AssistantCreateWithFile => AssistantsDemo.CreateWithFile(),
            Demos.AssistantListFiles => AssistantsDemo.ListFiles(),
            Demos.AssistantAttachFile => AssistantsDemo.AttachFile(),
            Demos.AssistantRetriveFile => AssistantsDemo.RetrieveFile(),
            Demos.AssistantRemoveFile => AssistantsDemo.RemoveFile(),
            Demos.ThreadsCreate => ThreadsDemo.Create(),
            _ => null
        };

        if (task is not null) await task;
    }

    private enum Demos
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
        Last
    }
}