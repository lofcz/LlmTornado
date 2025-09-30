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
    protected string AgentName { get; set; }
    protected string AgentVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of the A2ATornadoRuntimeService
    /// </summary>
    public BaseA2ATornadoRuntimeConfiguration(IRuntimeConfiguration runtimeConfig, string name, string version = "1.0.0")
    {
        ActivitySource = new(name, version);
        AgentName = name;
        AgentVersion = version;
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
        _taskManager.OnTaskCreated = StartAgentTaskAsync;
        _taskManager.OnTaskUpdated = UpdateAgentTaskAsync;
        _taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    /// <summary>
    /// Process a task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task StartAgentTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        if (_taskManager == null)
        {
            throw new InvalidOperationException("TaskManager is not attached.");
        }

        using var activity = ActivitySource.StartActivity("Invoke", ActivityKind.Server);
        activity?.SetTag("task.id", task.Id);
        activity?.SetTag("message", task.History.Last().ToString());
        activity?.SetTag("state", "working");

        //Shared for the event handler
        _cancellationToken = cancellationToken;

        //Set current task
        _currentTask = task;

        //Check for cancellation
        if (cancellationToken.IsCancellationRequested)
        {
            await _taskManager.UpdateStatusAsync(task.Id, TaskState.Canceled,
                message: new AgentMessage()
                {
                    Role = MessageRole.Agent,
                    MessageId = Guid.NewGuid().ToString(),
                    Parts = [new TextPart() {
                    Text = "Operation cancelled."
                }]
                },
            cancellationToken: cancellationToken);
            return;
        }

        //Send Notify working status
        await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working, cancellationToken: cancellationToken);

        // Get message from the user
        var userMessage = task.History!.Last().ToTornadoMessage();

        // Get the response from the agent
        ChatMessage response = await _agent.InvokeAsync(userMessage);

        // Update the Status to Completed
        await _taskManager.UpdateStatusAsync(_currentTask.Id, TaskState.Completed, message: response.ToA2AAgentMessage(), final: true, cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Update an existing task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task UpdateAgentTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        await StartAgentTaskAsync(task, cancellationToken);
    }


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
        Artifact artifact = evt.ToArtifact();

        if (artifact.Description == "Guardrail Triggered")
        {
            await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
            await _taskManager.UpdateStatusAsync(_currentTask.Id, TaskState.Rejected, final: true, cancellationToken: _cancellationToken);
            await Task.Delay(100); // Adjust the delay as needed
            return;
        }

        using var activity = ActivitySource.StartActivity("Invoke", ActivityKind.Server);
        activity?.SetTag("task.id", _currentTask.Id);
        activity?.SetTag("event", artifact.Description);

        await _taskManager.ReturnArtifactAsync(_currentTask.Id, artifact, _cancellationToken);
        await Task.Delay(100); // Adjust the delay as needed
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


