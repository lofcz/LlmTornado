namespace OpenAiNg.Demo;

class Program
{
    private static string ApiKey { get; set; }

    public static OpenAiApi Connect()
    {
        return new OpenAiApi(ApiKey);
    }

    static async Task<bool> SetupApi()
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
    
    static async Task Main(string[] args)
    {
        if (!await SetupApi())
        {
            return;
        }

        await VisionDemo.VisionBase64();
    }
}