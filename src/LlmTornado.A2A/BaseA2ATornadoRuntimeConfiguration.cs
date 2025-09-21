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
using System.IO;
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
        _agent.RuntimeConfiguration.OnRuntimeEvent += RuntimeEventHandler;
        _agent.RuntimeConfiguration.OnRuntimeRequestEvent += HandleRuntimePermissionRequest;
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
        taskManager.OnTaskCreated = StartAgentTaskAsync;
        taskManager.OnTaskUpdated = UpdateAgentTaskAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    /// <summary>
    /// Process a task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public abstract Task StartAgentTaskAsync(AgentTask task, CancellationToken cancellationToken);


    /// <summary>
    /// Update an existing task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task UpdateAgentTaskAsync(AgentTask task, CancellationToken cancellationToken);


    /// <summary>
    /// Defines a static Agent Card for the agent
    /// </summary>
    /// <returns></returns>
    public abstract AgentCard DescribeAgentCard(string agentUrl);


    public virtual async ValueTask<bool> HandleRuntimePermissionRequest(string request)
    {
        Artifact artifact = new Artifact()
        {
            Description = "Permission Request Event",
            Parts = new List<Part>()
        };
        artifact.Parts.Add(new TextPart()
        {
            Text = request
        });

        await _taskManager?.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken)!;
        await _taskManager?.UpdateStatusAsync(_currentTask.Id, TaskState.InputRequired, cancellationToken: _cancellationToken)!;

        return true;
    }

    public async ValueTask RuntimeEventHandler(ChatRuntimeEvents evt)
    {
        if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
        {
            await ProcessRunnerEvents(evt);
        }
        else if (evt.EventType == ChatRuntimeEventTypes.Orchestration)
        {
            await ProcessRuntimeOrchestrationEvent(evt);
        }
    }

    /// <summary>
    /// Process runtime events from runner
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
    public virtual async ValueTask ProcessRunnerEvents(ChatRuntimeEvents evt)
    {
        if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
        {

            if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
            {
                Artifact artifact = new Artifact()
                {
                    Description = evt.EventType.ToString(),
                    Parts = new List<Part>()
                };
                if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                {
                    if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                    {
                        artifact.Description = streamEvt.ModelStreamingEvent.EventType.ToString();
                        artifact.Parts.Add(new TextPart()
                        {
                            Text = deltaTextEvent.DeltaText ?? ""
                        });
                        Console.Write(deltaTextEvent.DeltaText);
                    }
                }
                else if (runnerEvt.AgentRunnerEvent is AgentRunnerToolInvokedEvent toolInvokedEvt)
                {
                    artifact.Description = "Tool Invoked";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $@"
{toolInvokedEvt.ToolCalled.Name} was invoked.

with Arguments = {toolInvokedEvt.ToolCalled.Arguments}"
                    });
                }
                else if(runnerEvt.AgentRunnerEvent is AgentRunnerToolCompletedEvent toolCompletedEvt)
                {
                    artifact.Description = "Tool Completed";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $@"{toolCompletedEvt.ToolCall.Name} has completed.
With Results: {toolCompletedEvt.ToolCall.Result.RemoteContent?.ToString() ?? toolCompletedEvt.ToolCall.Result.Content}"
                    });
                }
                else if(runnerEvt.AgentRunnerEvent is AgentRunnerGuardrailTriggeredEvent guardrailTriggeredEvent)
                {
                    artifact.Description = "Guardrail Triggered";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $@"Guardrail was triggered. Reason: {guardrailTriggeredEvent.Reason}"
                    });
                    await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
                    await _taskManager.UpdateStatusAsync(_currentTask.Id, TaskState.Rejected, final: true, cancellationToken: _cancellationToken);
                    await Task.Delay(100); // Adjust the delay as needed
                }

                await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
                await Task.Delay(100); // Adjust the delay as needed
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
            Artifact artifact = new Artifact()
            {
                ArtifactId = Guid.NewGuid().ToString(),
                Description = "Agent Orchestration Event",
                Parts = new List<Part>()
            };

            if (evt is ChatRuntimeOrchestrationEvent orchestrationEvt)
            {
                if (orchestrationEvt.OrchestrationEventData is OnVerboseOrchestrationEvent verbose)
                {
                    artifact.Description = "Agent Verbose Orchestration Event";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = verbose.Message ?? ""
                    });                  
                }
                else if (orchestrationEvt.OrchestrationEventData is OnErrorOrchestrationEvent error)
                {
                    artifact.Description = "Agent Orchestration Error";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = error.Exception?.ToString() ?? "Unknown error"
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnCancelledOrchestrationEvent cancelled)
                {
                    artifact.Description = "Agent Orchestration Cancelled";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = "The orchestration was cancelled."
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnBeginOrchestrationEvent started)
                {
                    artifact.Description = "Agent Orchestration Started";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = "The orchestration has started."
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnFinishedOrchestrationEvent completed)
                {
                    artifact.Description = "Agent Orchestration Completed";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = "The orchestration has completed."
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnInitializedOrchestrationEvent initialized)
                {
                    artifact.Description = "Agent Orchestration Initialized";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = "The orchestration has been initialized."
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnStartedRunnableEvent startedRunnable)
                {
                    artifact.Description = "Agent Orchestration Started Runnable";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $"The orchestration has started runnable: {startedRunnable.RunnableBase.RunnableName}"
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnFinishedRunnableEvent completedRunnable)
                {
                    artifact.Description = "Agent Orchestration Completed Runnable";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $"The orchestration has completed runnable: {completedRunnable.Runnable.RunnableName}"
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnStartedRunnableProcessEvent startedRunnableProcess)
                {
                    artifact.Description = "Agent Started Runnable with process";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $@"
Agent Has Started Runnable {startedRunnableProcess.RunnableProcess.Runner.RunnableName} with process ID: {startedRunnableProcess.RunnableProcess.Id}  
Input Variables: {JsonSerializer.Serialize(startedRunnableProcess.RunnableProcess.BaseInput)}
"
                    });
                }
                else if (orchestrationEvt.OrchestrationEventData is OnFinishedRunnableProcessEvent finishedRunnableProcess)
                {
                    artifact.Description = "Agent Finished Runnable with process";
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $@"
Agent Has Finished Runnable {finishedRunnableProcess.RunnableProcess.Runner.RunnableName} with process ID: {finishedRunnableProcess.RunnableProcess.Id}  

Process Duration: {finishedRunnableProcess.RunnableProcess.RunnableExecutionTime.TotalSeconds} seconds

Token Usage: {finishedRunnableProcess.RunnableProcess.TokenUsage}

Result Variables: {JsonSerializer.Serialize(finishedRunnableProcess.RunnableProcess.BaseResult)}
"
                    });
                }   
                else if (orchestrationEvt.OrchestrationEventData is OrchestrationEvent unknown)
                {
                    artifact.Description = unknown.Type;
                    artifact.Parts.Add(new TextPart()
                    {
                        Text = $"Received an orchestration event of type: {unknown.Type}"
                    });
                }

            }

            await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
            await Task.Delay(100); // Adjust the delay as needed
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


