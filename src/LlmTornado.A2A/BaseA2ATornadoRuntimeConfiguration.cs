using A2A;
using A2A.AspNetCore;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.A2A;

///SERVER CODE

/// <summary>
/// Wraps Semantic Kernel-based agents to handle Travel related tasks
/// </summary>
public abstract class BaseA2ATornadoRuntimeConfiguration : IA2ARuntimeConfiguration, IDisposable
{
    public readonly ActivitySource ActivitySource;

    //private readonly ILogger _logger;
    protected readonly ChatRuntime _agent;
    protected ITaskManager? _taskManager;
    protected IRuntimeConfiguration _runtimeConfig;
    protected AgentTask _currentTask;
    protected CancellationToken _cancellationToken;

    /// <summary>
    /// Initializes a new instance of the A2ATornadoRuntimeService
    /// </summary>
    public BaseA2ATornadoRuntimeConfiguration(IRuntimeConfiguration runtimeConfig, string name, string version = "1.0.0")
    {
        ActivitySource = new(name, version);
        //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeConfig = runtimeConfig ?? throw new ArgumentNullException(nameof(runtimeConfig));

        // Initialize the agent
        _agent = new ChatRuntime(_runtimeConfig);

        //Setup streaming event handler
        _agent.RuntimeConfiguration.OnRuntimeEvent += ProcessRuntimeStreamingEvent;
        _agent.RuntimeConfiguration.OnRuntimeEvent += ProcessRuntimeOrchestrationEvent;
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
    public virtual void Attach(ITaskManager taskManager)
    {
        _taskManager = taskManager;
        taskManager.OnTaskCreated = ExecuteAgentTaskAsync;
        taskManager.OnTaskUpdated = ExecuteAgentTaskAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    /// <summary>
    /// Process a task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public abstract Task ExecuteAgentTaskAsync(AgentTask task, CancellationToken cancellationToken);

    /// <summary>
    /// Defines a static Agent Card for the agent
    /// </summary>
    /// <returns></returns>
    public abstract AgentCard DescribeAgentCard(string agentUrl);
    /// <summary>
    /// Process runtime events for streaming output
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
    public virtual async ValueTask ProcessRuntimeStreamingEvent(ChatRuntimeEvents evt)
    {
        if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
        {
            if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
            {
                if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                {
                    if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                    {
                        Artifact artifact = new Artifact()
                        {
                            Description = streamEvt.ModelStreamingEvent.EventType.ToString(),
                            Parts = [new TextPart() {
                                Text = deltaTextEvent.DeltaText ?? ""
                            }]
                        };
                        Console.Write(deltaTextEvent.DeltaText);
                        await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
                        await Task.Delay(100); // Adjust the delay as needed
                    }
                }
            }
        }
    }

    /// <summary>
    /// Possibly handle logging
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
    public virtual async ValueTask ProcessRuntimeOrchestrationEvent(ChatRuntimeEvents evt)
    {
        if (evt.EventType == ChatRuntimeEventTypes.Orchestration)
        {
            if (evt is ChatRuntimeOrchestrationEvent orchestrationEvt)
            {
                if (orchestrationEvt.OrchestrationEventData is OnVerboseOrchestrationEvent verbose)
                {
                    Artifact artifact = new Artifact()
                    {
                        ArtifactId = Guid.NewGuid().ToString(),
                        Description = "Agent Orchestration Event",
                        Parts = [new TextPart() {
                                Text = verbose.Message ?? ""
                            }]
                    };
                    await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
                    await Task.Delay(100); // Adjust the delay as needed
                }
            }
        }
    }

    /// <summary>
    /// Generic Agent Card for the agent
    /// </summary>
    /// <param name="agentUrl"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AgentCard>(cancellationToken);
        }

        return Task.FromResult(DescribeAgentCard(agentUrl));
    }
}


