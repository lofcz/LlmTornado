using A2A;
using A2A.AspNetCore;
using LlmTornado;
using LlmTornado.A2A;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

//Requires docker environment variable OPENAI_API_KEY to be set in Launch settings or in run command
TornadoApi client = new TornadoApi(LlmTornado.Code.LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "");

string instructions = @"
You are an expert assistant designed to help users with a variety of tasks.
You can perform tasks such as answering questions, providing recommendations, and assisting with problem-solving.
You should always strive to provide accurate and helpful information to the user.
";

TornadoAgent Agent =  new TornadoAgent(
    client: client,
    model: ChatModel.OpenAi.Gpt5.V5,
    name: "Assistant",
    instructions: instructions,
    streaming: true);

IRuntimeConfiguration runtimeConfig = new SingletonRuntimeConfiguration(Agent); //Add your Runtime Configuration here


BasicA2ATornadoRuntimeConfiguration agentRuntime = new BasicA2ATornadoRuntimeConfiguration(
    runtimeConfig: runtimeConfig,  
    name: "LlmTornado.A2A.AgentServer", 
    version:"1.0.0"
    );

// Create and register the specified agent runtime
var taskManager = new TaskManager();
agentRuntime.Attach(taskManager);

#region API Configuration
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


/// <summary>
/// Wraps Llm Runtime agents to handle Travel related tasks
/// </summary>
public class BasicA2ATornadoRuntimeConfiguration : BaseA2ATornadoRuntimeConfiguration
{
    /// <summary>
    /// Initializes a new instance of the A2ATornadoRuntimeService
    /// </summary>
    public BasicA2ATornadoRuntimeConfiguration(IRuntimeConfiguration runtimeConfig, string name, string version) : base(runtimeConfig, name, version) { }

    /// <summary>
    /// Defines a static Agent Card for the agent
    /// </summary>
    /// <returns></returns>
    public override AgentCard DescribeAgentCard(string agentUrl)
    {
        AgentCapabilities capabilities = new AgentCapabilities()
        {
            Streaming = true,
            PushNotifications = false,
        };

        AgentSkill chattingSkill = new AgentSkill()
        {
            Id = "chatting_skill",
            Name = "Chatting feature",
            Description = "Agent to chat with and search the web.",
            Tags = ["chat",  "llm-tornado"],
            Examples =
            [
                "Hello, what's up?",
            ],
        };

        return new AgentCard()
        {
            Name = AgentName,
            Description = "Agent to chat with and search the web",
            Url = agentUrl, // Placeholder URL
            Version = AgentVersion,
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [chattingSkill],
        };
    }
}
