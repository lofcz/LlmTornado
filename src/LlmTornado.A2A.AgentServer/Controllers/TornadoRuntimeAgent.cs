using A2A;
using A2A.AspNetCore;
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
using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.A2A.AgentServer;

/// <summary>
/// Wraps Semantic Kernel-based agents to handle Travel related tasks
/// </summary>
public class TornadoRuntimeAgent : IDisposable
{
    public static readonly ActivitySource ActivitySource = new("A2A.TornadoRuntimeAgent", "1.0.0");
    //private readonly ILogger _logger;
    private readonly ChatRuntime _agent;
    private ITaskManager? _taskManager;
    private IRuntimeConfiguration _runtimeConfig;
    private AgentTask _currentTask;
    private CancellationToken _cancellationToken;
    /// <summary>
    /// Initializes a new instance of the SemanticKernelTravelAgent
    /// </summary>
    /// <param name="logger">Logger for the agent</param>
    public TornadoRuntimeAgent(IRuntimeConfiguration runtimeConfig)
    {
        //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeConfig = runtimeConfig ?? throw new ArgumentNullException(nameof(runtimeConfig));

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
        taskManager.OnMessageReceived = ProcessMessageAsync;
    }

    private async Task<A2AResponse> ProcessMessageAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new AgentMessage()
            {
                Role = MessageRole.Agent,
                MessageId = Guid.NewGuid().ToString(),
                ContextId = messageSendParams.Message.ContextId,
                Parts = [new TextPart() {
                    Text = "Operation cancelled."
                }]
            };
        }

        // Process the message
        var messageText = messageSendParams.Message.Parts.OfType<TextPart>().First().Text;

        ChatMessage response = await _agent.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, messageText));

        List<Part> parts = new List<Part>();


        if(response.Content != null)
        {
            parts.Add(new TextPart() { Text = response.Content });
        }
        else
        {
            foreach (var part in response.Parts)
            {
                if (part.Text != null)
                {
                    parts.Add(new TextPart() { Text = part.Text });
                }
            }
        }

        // Create and return an artifact
        return new AgentMessage()
        {
            Role = MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = messageSendParams.Message.ContextId,
            Parts = parts
        };
    }

    public async Task ExecuteAgentTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        _currentTask = task;
        if (_taskManager == null)
        {
            throw new InvalidOperationException("TaskManager is not attached.");
        }

        await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working, cancellationToken: cancellationToken);

        // Get message from the user
        var userMessage = task.History!.Last().Parts.First().AsTextPart().Text;

        var artifact = new Artifact();

        _agent.RuntimeConfiguration.OnRuntimeEvent += ProcessRuntimeStreamingEvent;

        // Get the response from the agent
        
        ChatMessage response = await _agent.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, userMessage ?? "Hello"));

        // Return as artifacts
        await _taskManager.UpdateStatusAsync(_currentTask.Id, TaskState.Completed, cancellationToken: cancellationToken);
    }

    public async ValueTask ProcessRuntimeStreamingEvent(ChatRuntimeEvents evt)
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
                        await _taskManager.ReturnArtifactAsync(_currentTask.Id, new Artifact()
                        {
                            Parts = [new TextPart() {
                                Text = deltaTextEvent.DeltaText ?? ""
                            }]
                        }, _cancellationToken);
                    }
                }
            }
        }
    }

    public static ValueTask ProcessRuntimeOrchestrationEvent(ChatRuntimeEvents evt)
    {
        if (evt.EventType == ChatRuntimeEventTypes.Orchestration)
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

    public List<string> SupportedContentTypes { get; } = ["text", "text/plain"];

    private ChatRuntime InitializeAgent()
    {
        try
        {
            ChatRuntime runtime = new ChatRuntime(_runtimeConfig);
            return runtime;
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Failed to initialize TornadoRuntimeAgent");
            throw;
        }
    }
}


