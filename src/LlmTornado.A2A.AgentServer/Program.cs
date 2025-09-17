using A2A;
using A2A.AspNetCore;
using LlmTornado;
using LlmTornado.A2A;
using LlmTornado.A2A.AgentServer;
using LlmTornado.Agents.ChatRuntime;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Collections.Concurrent;


/// This is the main entry point for the A2A Agent Server application
/// This Server is used for hosting agents on the docker container
/// Hosting project handles the lifecycle of the containers and communication to the containers
ConcurrentDictionary<string, IRuntimeConfiguration> configurations = new();
RegisterRuntimeConfiguration<ChatBotAgent>();

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("A2AAgentServer"))
    .WithTracing(tracing => tracing
        .AddSource(TaskManager.ActivitySource.Name)
        .AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
        .AddSource(A2ATornadoRuntimeConfiguration.ActivitySource.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        })
    );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LlmTornado Agents API", Version = "v1" });
    // Note: XML comments file generation can be enabled in project properties if needed
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add health endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }));

IRuntimeConfiguration configurationType;

//A2A Setup
// Create and register the specified agent
var taskManager = new TaskManager();
var configType = GetConfigurationTypeFromArgs(args);

if(configType != null && configType != "ChatBotAgent")
{
    configurationType = Activator.CreateInstance(configurations[configType].GetType()) as IRuntimeConfiguration ?? new ChatBotAgent();
}
else
{
   configurationType = new ChatBotAgent(); // Default to ChatBotAgent if no valid config type is provided
}

var chatAgent = new A2ATornadoRuntimeConfiguration(configurationType);

chatAgent.Attach(taskManager);
app.MapA2A(taskManager, "/agent");
app.MapWellKnownAgentCard(taskManager, "/agent");
app.MapHttpA2A(taskManager, "/agent");

//A2A Setup End



app.Run();

static string? GetConfigurationTypeFromArgs(string[] args)
{
    // Look for --agent parameter
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "--configType" || args[i] == "-ct")
        {
            return args[i + 1];
        }
    }

    // Default to ChatBot if no config type specified
    Console.WriteLine("No config type specified. Use --configType or -ct parameter to specify config type. Defaulting to 'ChatBot'.");
    return null;
}

void RegisterRuntimeConfiguration<T>() where T : IRuntimeConfiguration
{
    configurations.AddOrUpdate(typeof(T).Name, Activator.CreateInstance<T>(), (key, oldValue) => Activator.CreateInstance<T>());
    Console.WriteLine("Registered runtime configuration: {0}", typeof(T).Name);
}