using A2A;
using A2A.AspNetCore;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.A2A.AgentServer;

/// <summary>
/// Wraps Semantic Kernel-based agents to handle Travel related tasks
/// </summary>
public class A2ATornadoRuntimeController : IDisposable
{
    public static readonly ActivitySource ActivitySource = new("A2A.TornadoRuntimeAgent", "1.0.0");

    //private readonly ILogger _logger;
    private readonly ChatRuntime _agent;
    private ITaskManager? _taskManager;
    private IRuntimeConfiguration _runtimeConfig;
    private AgentTask _currentTask;
    private ConcurrentQueue<Artifact> _artifactQueue = new();
    private CancellationToken _cancellationToken;


    /// <summary>
    /// Initializes a new instance of the SemanticKernelTravelAgent
    /// </summary>
    /// <param name="logger">Logger for the agent</param>
    public A2ATornadoRuntimeController(IRuntimeConfiguration runtimeConfig)
    {
        //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeConfig = runtimeConfig ?? throw new ArgumentNullException(nameof(runtimeConfig));

        // Initialize the agent
        _agent = new ChatRuntime(_runtimeConfig);

        //Setup streaming event handler
        _agent.RuntimeConfiguration.OnRuntimeEvent += ProcessRuntimeStreamingEvent;

    }


    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Interface to the TaskManager
    /// </summary>
    /// <param name="taskManager"></param>
    public void Attach(ITaskManager taskManager)
    {
        _taskManager = taskManager;
        taskManager.OnTaskCreated = ExecuteAgentTaskAsync;
        taskManager.OnTaskUpdated = ExecuteAgentTaskAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
        taskManager.OnMessageReceived = ProcessMessageAsync;
    }

    /// <summary>
    /// Process a message sent to the agent Without streaming
    /// </summary>
    /// <param name="messageSendParams"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<A2AResponse> ProcessMessageAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        //Check for cancellation
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

        // Get the message
        var messageText = messageSendParams.Message.Parts.OfType<TextPart>().First().Text;

        //Invoke Runtime
        ChatMessage response = await _agent.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, messageText));

        // Create and return an artifact
        return response.ToA2AAgentMessage();
    }

    /// <summary>
    /// Process a task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ExecuteAgentTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        if (_taskManager == null)
        {
            throw new InvalidOperationException("TaskManager is not attached.");
        }

        //Shared for the event handler
        _cancellationToken = cancellationToken;
        //Set current task
        _currentTask = task;

        //Send Notify working status
        await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working, cancellationToken: cancellationToken);

        // Get message from the user
        var userMessage = task.History!.Last().Parts.First().AsTextPart().Text;

        // Get the response from the agent
        ChatMessage response = await _agent.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, userMessage ?? "Empty message"));

        // Update the Status to Completed
        await _taskManager.UpdateStatusAsync(_currentTask.Id, TaskState.Completed, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Process runtime events for streaming output
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
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
                        _artifactQueue.Enqueue(new Artifact()
                        {
                            ArtifactId = deltaTextEvent.ItemId,
                            Parts = [new TextPart() {
                                Text = deltaTextEvent.DeltaText ?? ""
                            }]
                        });
                        await RunQueue();
                    }
                }
            }
        }
    }


    private async Task RunQueue()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            if (_artifactQueue.TryDequeue(out var artifact))
            {
                await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
            }
            else
            {
                await Task.Delay(100); // Adjust the delay as needed
            }
        }
    }

    /// <summary>
    /// Possibly handle logging
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Generic Agent Card for the agent
    /// </summary>
    /// <param name="agentUrl"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
}


