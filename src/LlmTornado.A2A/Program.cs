using A2A;
using A2A.AspNetCore;
using LlmTornado.A2A;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient()
    .AddLogging()
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("RuntimeAgent");
    })
    .WithTracing(tracing => tracing
        .AddSource(TaskManager.ActivitySource.Name)
        .AddSource(A2AJsonRpcProcessor.ActivitySource.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        })
     );

var app = builder.Build();

var configuration = app.Configuration;
var httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
var logger = app.Logger;

var agent = new TornadoRuntimeAgent(configuration, httpClient, logger);
var taskManager = new TaskManager();
agent.Attach(taskManager);
app.MapA2A(taskManager, string.Empty);
app.MapWellKnownAgentCard(taskManager, string.Empty);

await app.RunAsync();