using A2A;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Chat;

namespace LlmTornado.A2A;

/// <summary>
/// Wraps Llm Runtime agents to handle Travel related tasks
/// </summary>
public class A2ATornadoRuntimeConfiguration : BaseA2ATornadoRuntimeConfiguration
{
    /// <summary>
    /// Initializes a new instance of the A2ATornadoRuntimeService
    /// </summary>
    public A2ATornadoRuntimeConfiguration(IRuntimeConfiguration runtimeConfig, string name, string version) : base(runtimeConfig, name, version) { }

    /// <summary>
    /// Process a task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override async Task StartAgentTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        if (_taskManager == null)
        {
            throw new InvalidOperationException("TaskManager is not attached.");
        }

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
    /// Process a task assigned to the agent with streaming output
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override async Task UpdateAgentTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        await StartAgentTaskAsync(task, cancellationToken);
    }

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
            Tags = ["chat", "websearch", "llm-tornado"],
            Examples =
            [
                "Hello, what's up?",
            "What is the weather like in boston?",
        ],
        };

        return new AgentCard()
        {
            Name = "Tornado Agent",
            Description = "Agent to chat with and search the web",
            Url = agentUrl, // Placeholder URL
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [chattingSkill],
        };
    }
}
