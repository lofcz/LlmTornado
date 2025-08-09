using LlmTornado.Agents.Orchestration;
using LlmTornado.Chat;
using LlmTornado.StateMachines;

namespace LlmTornado.Agents.AgentStates;

public interface IAgentStateMachine
{

}

public abstract class AgentStateMachine<TInput, TOutput> : StateMachine<TInput, TOutput>, IAgentStateMachine
{
    public AgentStateMachine() {

        InitializeStates();
        OnStateMachineEvent += CancelTriggered; // Cancel all active states when cancellation is triggered
    }

    /// <summary>
    /// Cancels the operation for each agent state in the collection of states.
    /// </summary>
    /// <remarks>This method iterates through all states and cancels the operation for those that
    /// implement the <see cref="IAgentState"/> interface by invoking the <see
    /// cref="System.Threading.CancellationTokenSource.Cancel"/> method on their cancellation token
    /// source.</remarks>
    private void CancelTriggered(StateMachineEvent e)
    {
        if(e is OnCancelledStateMachineEvent)
        {
            foreach (BaseState state in States)
            {
                if (state is IAgentState agentState)
                {
                    agentState.cts.Cancel();
                }
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