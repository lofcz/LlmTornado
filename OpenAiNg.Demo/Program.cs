namespace OpenAiNg.Demo;

class Program
{
    private static string ApiKey { get; set; }

    public static OpenAiApi Connect()
    {
        return new OpenAiApi(ApiKey);
    }

    static bool SetupApi()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPEN_AI_NG_DEMO_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("API key not set, please create an environment variable OPEN_AI_NG_DEMO_KEY and set it to your API key.");
            Console.ReadKey();
            return false;
        }

        ApiKey = apiKey;
        return true;
    }
    
    static async Task Main(string[] args)
    {
        if (!SetupApi())
        {
            return;
        }

        await SpeechDemo.Tts();
    }
}