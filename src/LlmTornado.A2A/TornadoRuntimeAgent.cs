using A2A;
using LlmTornado.A2A.ChatBot;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace LlmTornado.A2A;



#region Semantic Kernel Agent

/// <summary>
/// Wraps Semantic Kernel-based agents to handle Travel related tasks
/// </summary>
public class TornadoRuntimeAgent : IDisposable
{
    public static readonly ActivitySource ActivitySource = new("A2A.TornadoRuntimeAgent", "1.0.0");

    /// <summary>
    /// Initializes a new instance of the SemanticKernelTravelAgent
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="logger">Logger for the agent</param>
    public TornadoRuntimeAgent(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Initialize the agent
        _agent = InitializeAgent();
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Attach(ITaskManager taskManager)
    {
        _taskManager = taskManager;
        taskManager.OnTaskCreated = ExecuteAgentTaskAsync;
        taskManager.OnTaskUpdated = ExecuteAgentTaskAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    public async Task ExecuteAgentTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        if (_taskManager == null)
        {
            throw new InvalidOperationException("TaskManager is not attached.");
        }

        await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working, cancellationToken: cancellationToken);

        // Get message from the user
        var userMessage = task.History!.Last().Parts.First().AsTextPart().Text;

        var artifact = new Artifact();

        _agent.RuntimeConfiguration.OnRuntimeEvent += ProcessRuntimeEvent;

        // Get the response from the agent
        
        ChatMessage response = await _agent.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, userMessage ?? "Hello"));

        var content = response.Content;
        artifact.Parts.Add(new TextPart() { Text = content! });

        // Return as artifacts
        await _taskManager.ReturnArtifactAsync(task.Id, artifact, cancellationToken);
        await _taskManager.UpdateStatusAsync(task.Id, TaskState.Completed, cancellationToken: cancellationToken);
    }

    public static ValueTask ProcessRuntimeEvent(ChatRuntimeEvents evt)
    {
        if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
        {
            if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
            {
                if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                {
                    if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                    {
                        Console.Write(deltaTextEvent.DeltaText);
                    }
                }
            }
        }
        else if (evt.EventType == ChatRuntimeEventTypes.Orchestration)
        {
            if (evt is ChatRuntimeOrchestrationEvent orchestrationEvt)
            {
                if (orchestrationEvt.OrchestrationEventData is OnVerboseOrchestrationEvent verbose)
                {
                    Console.WriteLine(verbose.Message);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    public static Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AgentCard>(cancellationToken);
        }

        var capabilities = new AgentCapabilities()
        {
            Streaming = false,
            PushNotifications = false,
        };

        var chattingSkill= new AgentSkill()
        {
            Id = "chatting_skill",
            Name = "Chatting feature",
            Description = "Agent to chat with and search the web.",
            Tags = ["chat", "websearch", "llm-tornado"],
            Examples =
            [
                "Hello, what's up?",
                "What is the weather like in boston?",
            ],
        };

        return Task.FromResult(new AgentCard()
        {
            Name = "Tornado Agent",
            Description = "Agent to chat with and search the web",
            Url = agentUrl,
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [chattingSkill],
        });
    }

    #region private
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ChatRuntime _agent;
    private ITaskManager? _taskManager;

    public List<string> SupportedContentTypes { get; } = ["text", "text/plain"];

    private ChatRuntime InitializeAgent()
    {
        try
        {
            TornadoApi client = new TornadoApi(Code.LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            ChatBotAgent chatbotConfig = new ChatBotAgent();
            OrchestrationRuntimeConfiguration config = chatbotConfig.BuildSimpleAgent(client, streaming: true, conversationFile: "Conversation1.json");
            ChatRuntime runtime = new ChatRuntime(config);
            return runtime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize TornadoRuntimeAgent");
            throw;
        }
    }

    #endregion
}
#endregion

