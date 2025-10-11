using A2A;
using A2A.AspNetCore;
using LlmTornado;
using LlmTornado.A2A;
using LlmTornado.A2A.AgentServer;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;



//Requires docker environment variable OPENAI_API_KEY to be set in Launch settings or in run command
//Sample Agent Server using LlmTornado and A2A
BaseA2ATornadoRuntimeConfiguration agentRuntime = new A2ATornadoAgentSample().Build(); //Replace this to customize or modify files directly.

#region API Configuration
TaskManager taskManager = new TaskManager();
// Create and register the specified agent runtime
agentRuntime.Attach(taskManager);
var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("A2AAgentServer"))
    .WithTracing(tracing => tracing
        .AddSource(TaskManager.ActivitySource.Name)
        .AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
        .AddSource(agentRuntime.ActivitySource.Name)
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
