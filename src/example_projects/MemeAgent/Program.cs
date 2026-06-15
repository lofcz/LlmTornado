using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Mcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// Setup: TornadoApi configured with provider(s)
TornadoApi api = new(new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")));

// User input
Console.Write("Meme theme: ");
string theme = Console.ReadLine() ?? "AI Agents";

// Create orchestration configuration
MemeOrchestration config = new(api, theme);
ChatRuntime runtime = new(config);

// Execute the orchestration graph
ChatMessage result = await runtime.InvokeAsync(new ChatMessage(ChatMessageRoles.User, theme));
Console.WriteLine($"\n{result.Content}");

Console.ReadLine();

// --- Orchestration Configuration ---
class MemeOrchestration : OrchestrationRuntimeConfiguration
{
    private MCPServer? _mcpServer;
    private readonly TornadoApi _api;
    private readonly string _theme;
    
    public MemeOrchestration(TornadoApi api, string theme)
    {
        _api = api;
        _theme = theme;
        
        // Build orchestration using fluent builder
        new OrchestrationBuilder(this)
            .WithRuntimeProperty("theme", theme)
            .WithRuntimeProperty("api", api)
            .WithRuntimeProperty("iteration", 0)
            .WithRuntimeInitializer(SetupOrchestration)
            .Build();
    }
    
    private ValueTask SetupOrchestration(OrchestrationRuntimeConfiguration config)
    {
        // Define runnables (graph nodes)
        var entry = new EntryRunnable(config);
        var selector = new TemplateSelectorRunnable(_api, config);
        var textGen = new TextGeneratorRunnable(_api, config);
        var validator = new ValidatorRunnable(_api, config);
        var retry = new RetryDecisionRunnable(config);
        var complete = new CompletionRunnable(config);
        complete.AllowDeadEnd = true;
        
        // Wire the graph using fluent builder
        new OrchestrationBuilder(config)
            .SetEntryRunnable(entry)
            .SetOutputRunnable(complete)
            .AddAdvancer<string>(entry, (theme) => !string.IsNullOrEmpty(theme), selector)
            .AddAdvancer<TemplateInfo>(selector, (template) => template.TemplateId != null, textGen)
            .AddAdvancer<MemeData>(textGen, validator)
            .AddAdvancer<ValidationResult>(validator, (v) => v.Approved, complete)
            .AddAdvancer<ValidationResult>(validator, (v) => !v.Approved && (int)config.RuntimeProperties["iteration"] < 3, retry)
            .AddAdvancer<ValidationResult>(validator, (v) => !v.Approved && (int)config.RuntimeProperties["iteration"] >= 3, complete)
            .AddAdvancer<TemplateInfo>(retry, textGen)
            .Build();
        
        return ValueTask.CompletedTask;
    }
    
    public async ValueTask<MCPServer> GetMcpServerAsync()
    {
        if (_mcpServer == null)
        {
            _mcpServer = MCPToolkits.Meme();
            await _mcpServer.InitializeAsync();
        }
        return _mcpServer;
    }
}

// --- Graph Nodes (Runnables) ---
record TemplateInfo(string TemplateId, int LineCount);
record MemeData(string Url, string[] Text);
record ValidationResult(bool Approved, double Score, string[] Issues);

// Output schemas for structured responses
record MemeTextOutput(string[] TextLines);
record ValidationOutput(bool Approved, double Score, string[] Issues);

class EntryRunnable(Orchestration orch) : OrchestrationRunnable<ChatMessage, string>(orch)
{
    public override ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process) =>
        ValueTask.FromResult((string)Orchestrator.RuntimeProperties["theme"]);
}

class TemplateSelectorRunnable(TornadoApi api, Orchestration orch) 
    : OrchestrationRunnable<string, TemplateInfo>(orch)
{
    public override async ValueTask<TemplateInfo> Invoke(RunnableProcess<string, TemplateInfo> process)
    {
        var config = (MemeOrchestration)Orchestrator;
        MCPServer mcp = await config.GetMcpServerAsync();
        
        TornadoAgent agent = new(api, ChatModel.OpenAi.Gpt4.O, "Selector",
            $"Pick a funny meme template for: {process.Input}");
        agent.AddTool(mcp.AllowedTornadoTools.ToArray());
        
        string? templateId = null;
        int lineCount = 2;
        agent.AddTool(new Tool((string id, int lines) => {
            templateId = id; lineCount = lines; agent.Cancel();
            return "Selected";
        }, "confirm_template"));
        
        await agent.Run("Select template", maxTurns: 5);
        Console.WriteLine($"✓ Template: {templateId} ({lineCount} lines)");
        
        TemplateInfo template = new(templateId ?? "drake", lineCount);
        Orchestrator.RuntimeProperties["template"] = template; // Store for retries
        return template;
    }
}

class TextGeneratorRunnable(TornadoApi api, Orchestration orch) 
    : OrchestrationRunnable<TemplateInfo, MemeData>(orch)
{
    public override async ValueTask<MemeData> Invoke(RunnableProcess<TemplateInfo, MemeData> process)
    {
        string theme = (string)Orchestrator.RuntimeProperties["theme"];
        int iteration = (int)Orchestrator.RuntimeProperties["iteration"];
        string feedback = iteration > 0 ? "Previous failed validation. Be funnier!" : "";
        
        TornadoAgent agent = new(api, ChatModel.OpenAi.Gpt4.O, "TextGen",
            $"Generate {process.Input.LineCount} SHORT meme lines about: {theme}. Max 6 words/line. {feedback}",
            outputSchema: typeof(MemeTextOutput));
        
        Conversation conv = await agent.Run($"Create meme text for: {theme}");
        var result = conv.Messages[^1].Content?.ParseJson<MemeTextOutput>();
        string[] lines = result.TextLines;
        
        // Build meme URL
        string[] transformed = lines.Select(t => t.Replace(" ", "_").Replace("?", "~q")).ToArray();
        string url = $"http://localhost:5000/images/{process.Input.TemplateId}/{string.Join("/", transformed)}.png";
        
        Console.WriteLine($"📝 Text: {string.Join(" / ", lines)}");
        Orchestrator.RuntimeProperties["memeUrl"] = url;
        return new(url, lines);
    }
}

class ValidatorRunnable(TornadoApi api, Orchestration orch) 
    : OrchestrationRunnable<MemeData, ValidationResult>(orch)
{
    public override async ValueTask<ValidationResult> Invoke(RunnableProcess<MemeData, ValidationResult> process)
    {
        using HttpClient http = new();
        byte[] bytes = await http.GetByteArrayAsync(process.Input.Url);
        string base64 = Convert.ToBase64String(bytes);
        
        TornadoAgent agent = new(api, ChatModel.OpenAi.Gpt4.O, "Validator",
            "Rate 0.0-1.0. Approve if >= 0.7. Check: readable? funny? relevant?",
            outputSchema: typeof(ValidationOutput));
        
        Conversation conv = await agent.Run([
            new ChatMessagePart("Validate this meme:"),
            new ChatMessagePart(new ChatImage($"data:image/jpeg;base64,{base64}"))
        ]);
        
        // Check for API errors
        if (conv.Error != null)
        {
            Console.WriteLine($"⚠️ API error: {conv.Error.Response}");
            return new(false, 0.0, [$"API error: {conv.Error.Response}"]);
        }
        
        var result = conv.Messages.Last().Content?.ParseJson<ValidationOutput>();
        
        if (result == null)
        {
            Console.WriteLine("⚠️ Validation failed to parse, defaulting to not approved");
            return new(false, 0.0, ["Failed to parse validation result"]);
        }
        
        Console.WriteLine($"🎯 Score: {result.Score:F2} | Approved: {result.Approved}");
        return new(result.Approved, result.Score, result.Issues);
    }
}

class RetryDecisionRunnable(Orchestration orch) 
    : OrchestrationRunnable<ValidationResult, TemplateInfo>(orch)
{
    public override ValueTask<TemplateInfo> Invoke(RunnableProcess<ValidationResult, TemplateInfo> process)
    {
        int iteration = (int)Orchestrator.RuntimeProperties["iteration"];
        Orchestrator.RuntimeProperties["iteration"] = iteration + 1;
        Console.WriteLine($"🔄 Retry {iteration + 1}/3");
        
        // Retrieve stored template info to reuse for text generation
        TemplateInfo template = (TemplateInfo)Orchestrator.RuntimeProperties["template"];
        return ValueTask.FromResult(template);
    }
}

class CompletionRunnable(Orchestration orch) 
    : OrchestrationRunnable<ValidationResult, ChatMessage>(orch)
{
    public override ValueTask<ChatMessage> Invoke(RunnableProcess<ValidationResult, ChatMessage> process)
    {
        Orchestrator.HasCompletedSuccessfully();
        string url = (string)Orchestrator.RuntimeProperties["memeUrl"];
        string msg = process.Input.Approved 
            ? $"✅ SUCCESS! Meme: {url}" 
            : $"⚠️ Max retries. Best: {url}";
        return ValueTask.FromResult(new ChatMessage(ChatMessageRoles.Assistant, msg));
    }
}

// Compatibility wrapper for OrchestrationBuilder (example only - not shipped publicly)
class OrchestrationBuilder
{
    private readonly OrchestrationRuntimeConfiguration _config;

    public OrchestrationBuilder(OrchestrationRuntimeConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public OrchestrationBuilder SetEntryRunnable(OrchestrationRunnableBase entryRunnable)
    {
        _config.SetEntryRunnable(entryRunnable);
        return this;
    }

    public OrchestrationBuilder SetOutputRunnable(OrchestrationRunnableBase outputRunnable)
    {
        _config.SetRunnableWithResult(outputRunnable);
        return this;
    }

    public OrchestrationBuilder WithRuntimeProperty(string key, object value)
    {
        _config.RuntimeProperties.AddOrUpdate(key, value, (k, v) => value);
        return this;
    }

    public OrchestrationBuilder WithRuntimeInitializer(Func<OrchestrationRuntimeConfiguration, ValueTask> initializer)
    {
        _config.CustomInitialization = initializer;
        return this;
    }

    public OrchestrationBuilder AddAdvancer<T>(OrchestrationRunnableBase fromRunnable, AdvancementRequirement<T> condition, OrchestrationRunnableBase toRunnable)
    {
        // Use dynamic to call AddAdvancer on the generic OrchestrationRunnable<TInput, TOutput>
        dynamic dynRunnable = fromRunnable;
        dynRunnable.AddAdvancer(condition, toRunnable);
        return this;
    }
    
    public OrchestrationBuilder AddAdvancer<T>(OrchestrationRunnableBase fromRunnable, OrchestrationRunnableBase toRunnable)
    {
        // Use dynamic to call AddAdvancer on the generic OrchestrationRunnable<TInput, TOutput>
        dynamic dynRunnable = fromRunnable;
        dynRunnable.AddAdvancer(toRunnable);
        return this;
    }

    public OrchestrationRuntimeConfiguration Build()
    {
        return _config;
    }
}