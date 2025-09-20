using A2A;
using A2A.AspNetCore;
using LlmTornado.A2A;
using LlmTornado.A2A.AgentServer;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


A2ATornadoRuntimeConfiguration agent = new A2ATornadoRuntimeConfiguration(
    runtimeConfig: new ChatBotAgent(),  //Add your Agent Here
    name: "LlmTornado.A2A.AgentServer", 
    version:"1.0.0"
    );

#region API Configuration
// Create and register the specified agent
var taskManager = new TaskManager();
agent.Attach(taskManager);

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("A2AAgentServer"))
    .WithTracing(tracing => tracing
        .AddSource(TaskManager.ActivitySource.Name)
        .AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
        .AddSource(agent.ActivitySource.Name)
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

//A2A API Setup
app.MapA2A(taskManager, "/agent");
app.MapWellKnownAgentCard(taskManager, "/agent");
app.MapHttpA2A(taskManager, "/agent");

//A2A Setup End
app.Run();
#endregion