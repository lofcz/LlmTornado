using LlmTornado.Chat;
using LlmTornado.StateMachines;

namespace LlmTornado.Agents.AgentStates;

public interface IAgentStateMachine
{
    /// <summary>
    /// Gets or sets the <see cref="ControllerAgent"/> responsible for controlling the system's operations.
    /// </summary>
    public ControllerAgent ControlAgent { get; set; }
    /// <summary>
    /// Collection of <see cref="ModelItem"/> shared across the agent's state machine.
    /// </summary>
    public List<ChatMessage> SharedModelItems { get; set; }
}

public abstract class AgentStateMachine<TInput, TOutput> : StateMachine<TInput, TOutput>, IAgentStateMachine
{
    public ControllerAgent ControlAgent { get; set; }
    public List<ChatMessage> SharedModelItems { get; set; } = [];

    public AgentStateMachine(ControllerAgent agent) {

        ControlAgent = agent;
        InitializeStates();
        OnBegin += AddToControl; //Add active state machine to the control agent when it begins execution
        //Add OnFinish and CancellationTriggered events to remove from control
        FinishedTriggered += RemoveFromControl; // Remove active state machine from the control agent when it finishes execution
        CancellationTriggered += RemoveFromControl; // Remove active state machine from the control agent when cancellation is triggered
        CancellationTriggered += CancelTriggered; // Cancel all active states when cancellation is triggered

        //Add new States Event Handlers for Verbose and Streaming Callbacks from State
        OnStateEntered += (state) =>
        {
            if(state.State is IAgentState agentState)
            {
                ControlAgent.VerboseCallback += agentState.RunnerVerboseCallbacks;
                ControlAgent.StreamingCallback += agentState.StreamingCallbacks;
            }
        };

        //Remove Verbose and Streaming Callbacks from State when exited
        OnStateExited += (state) =>
        {
            if (state is IAgentState agentState)
            {
                ControlAgent.VerboseCallback -= agentState.RunnerVerboseCallbacks;
                ControlAgent.StreamingCallback -= agentState.StreamingCallbacks;
            }
        };
    }

    /// <summary>
    /// Adds the current state machine to the control agent for management and debug.
    /// </summary>
    /// <remarks>This method registers the state machine with the control agent, enabling it to manage
    /// the state machine's lifecycle.</remarks>
    private void AddToControl()
    {
        ControlAgent.AddStateMachine(this);
    }

    /// <summary>
    /// Removes the current state machine from the control agent.
    /// </summary>
    /// <remarks>This method should be called when the state machine is no longer needed to ensure it
    /// is properly deregistered from the control agent.</remarks>
    private void RemoveFromControl()
    {
        ControlAgent.RemoveStateMachine(this);
    }

    /// <summary>
    /// Cancels the operation for each agent state in the collection of states.
    /// </summary>
    /// <remarks>This method iterates through all states and cancels the operation for those that
    /// implement the <see cref="IAgentState"/> interface by invoking the <see
    /// cref="System.Threading.CancellationTokenSource.Cancel"/> method on their cancellation token
    /// source.</remarks>
    private void CancelTriggered()
    {
        foreach (BaseState state in States)
        {
            if (state is IAgentState agentState)
            {
                agentState.CancelTokenSource.Cancel();
            }
        }
    }

    /// <summary>
    /// Initializes the states required for the operation of the implementing class.
    /// </summary>
    /// <remarks>This method must be called before any state-dependent operations are performed.
    /// Implementations should ensure that all necessary states are set up and ready for use.</remarks>
    public abstract void InitializeStates();
}