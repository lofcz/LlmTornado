using LlmTornado.Acp;
using LlmTornado.Acp.Server;

// All diagnostic output goes to stderr so stdout stays clean for JSON-RPC
Console.Error.WriteLine("[ACP] LlmTornado ACP Server starting...");

string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
string model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4.1-nano";

if (string.IsNullOrWhiteSpace(apiKey))
{
    // Search several locations for apiKey.json
    string[] searchPaths =
    [
        Path.Combine(AppContext.BaseDirectory, "apiKey.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "apiKey.json"),
        // When launched via "dotnet run", look relative to the project file
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LlmTornado.Demo", "apiKey.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "LlmTornado.Demo", "apiKey.json")
    ];

    foreach (string candidate in searchPaths)
    {
        string fullPath = Path.GetFullPath(candidate);

        if (!File.Exists(fullPath))
        {
            continue;
        }

        try
        {
            string json = await File.ReadAllTextAsync(fullPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("OpenAi", out var prop))
            {
                apiKey = prop.GetString();

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.Error.WriteLine($"[ACP] Loaded API key from: {fullPath}");
                    break;
                }
            }
        }
        catch
        {
            // Try next candidate
        }
    }
}

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("[ACP] ERROR: No OpenAI API key found.");
    Console.Error.WriteLine("[ACP] Set OPENAI_API_KEY environment variable, or place apiKey.json next to the executable.");
    return 1;
}

Console.Error.WriteLine($"[ACP] Using model: {model}");
Console.Error.WriteLine("[ACP] Listening on stdin/stdout...");

TornadoAcpRuntime runtime = new(apiKey, model);
AcpJsonRpcServer server = new(runtime, Console.OpenStandardInput(), Console.OpenStandardOutput());

await server.RunAsync();

return 0;
