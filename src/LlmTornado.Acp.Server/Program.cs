using LlmTornado.Acp;
using LlmTornado.Acp.Server;
using LlmTornado.Cli.Core.Providers;

// All diagnostic output goes to stderr so stdout stays clean for JSON-RPC
Console.Error.WriteLine("[ACP] LlmTornado ACP Server starting...");

ProviderDetectionResult? detection = ProviderDetector.Detect();

if (detection is null)
{
    Console.Error.WriteLine("[ACP] ERROR: No LLM provider API keys found.");
    Console.Error.WriteLine("[ACP] Set at least one provider environment variable:");
    Console.Error.WriteLine("[ACP]   OPENAI_API_KEY, ANTHROPIC_API_KEY, GOOGLE_API_KEY,");
    Console.Error.WriteLine("[ACP]   GROQ_API_KEY, COHERE_API_KEY, MISTRAL_API_KEY,");
    Console.Error.WriteLine("[ACP]   DEEPSEEK_API_KEY, XAI_API_KEY, PERPLEXITY_API_KEY,");
    Console.Error.WriteLine("[ACP]   OPENROUTER_API_KEY, DEEPINFRA_API_KEY");
    return 1;
}

string providers = string.Join(", ", detection.Providers.Select(p => p.Provider));
Console.Error.WriteLine($"[ACP] Detected providers: {providers}");
Console.Error.WriteLine($"[ACP] Active model: {detection.ActiveModel.Name}");

if (detection.OptimizerModel is not null)
{
    Console.Error.WriteLine($"[ACP] Optimizer model: {detection.OptimizerModel.Name}");
}

Console.Error.WriteLine("[ACP] Listening on stdin/stdout...");

TornadoAcpRuntime runtime = new(detection);
AcpJsonRpcServer server = new(runtime, Console.OpenStandardInput(), Console.OpenStandardOutput());

await server.RunAsync();

return 0;
