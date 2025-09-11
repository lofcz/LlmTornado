using A2A;
using A2A.AspNetCore;
using LlmTornado;
using LlmTornado.A2A;
using LlmTornado.A2A.AgentServer;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LlmTornado Agents API", Version = "v1" });
    // Note: XML comments file generation can be enabled in project properties if needed
});

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("A2AAgentServer"))
    .WithTracing(tracing => tracing
        .AddSource(TaskManager.ActivitySource.Name)
        .AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
        .AddSource(TornadoRuntimeAgent.ActivitySource.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        })
    );

var app = builder.Build();

app.UseHttpsRedirection();

// Add health endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }));

// Create and register the specified agent
var taskManager = new TaskManager();

TornadoApi client = new TornadoApi(LlmTornado.Code.LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
var chatAgent = new TornadoRuntimeAgent(new ChatBotAgent());
chatAgent.Attach(taskManager);
app.MapA2A(taskManager, "/chat");
app.MapWellKnownAgentCard(taskManager, "/chat");
app.MapHttpA2A(taskManager, "/chat");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();