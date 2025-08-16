namespace LlmTornado.Agents.Runtime;

public interface IRunner
{
    /// <summary>
    /// Gets the identifier of the state.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Get the current StateMachine instance associated with this state.
    /// </summary>
    public ProcessRuntime? ActiveRuntime { get; set; }
}

/// <summary>
/// Represents the base class for defining a state within a state machine.
/// </summary>
/// <remarks>The <see cref="BaseRunner"/> class provides a framework for managing state transitions, input
/// and output processing, and state invocation within a state machine. It includes properties and methods for
/// handling state-specific logic, such as entering and exiting states, checking conditions, and managing
/// transitions.</remarks>
public abstract class BaseRunner : IRunner
{

    /// <summary>
    /// Used to limit the number of times to rerun the state.
    /// </summary>
    public bool BeingReran = false;

    /// <summary>
    /// State identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets a list of results extracted from the output results.
    /// </summary>
    internal List<object> BaseOutput => BaseOutputResults.Select(output => output.ResultObject).ToList();

    /// <summary>
    /// Output results of the state, containing processed results from the state invocation.
    /// </summary>
    internal List<RunnerResult> BaseOutputResults { get; set; } = new List<RunnerResult>();

    /// <summary>
    /// Gets or sets the list of input objects the state has to process this tick.
    /// </summary>
    internal List<object> BaseInput { get; set; } = new List<object>();

    /// <summary>
    /// Input processes that the state has to process this tick.
    /// </summary>
    internal List<RunnableProcess> BaseInputProcesses { get; set; } = new List<RunnableProcess>();

    /// <summary>
    /// Gets or sets the list of routes.
    /// </summary>
    internal List<RuntimeAdvancer<object>> BaseAdvancers { get; set; } = new List<RuntimeAdvancer<object>>();

    
    public ProcessRuntime? ActiveRuntime { get; set; }


    /// <summary>
    /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation requests.
    /// </summary>
    public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

    /// <summary>
    /// Internal invoke to abstract BaseState from Type specifics.
    /// </summary>
    /// <returns></returns>
    /// 
    internal abstract Task _Invoke();

    /// <summary>
    /// Adds state Process to the required state.
    /// </summary>
    /// <remarks>This method is abstract and must be implemented by derived classes to define the
    /// specific behavior of entering a new state.</remarks>
    /// <param name="input">The input that influences the state transition. Can be null if no specific input is required for the
    /// transition.</param>
    /// <returns></returns>
    internal abstract Task _InitializeRunnable(RunnableProcess? input);

    /// <summary>
    /// Transitions the current state to an exit state asynchronously.
    /// </summary>
    /// <remarks>This method should be implemented to handle any necessary cleanup or finalization
    /// tasks when exiting a state. It is called as part of the state transition process.</remarks>
    /// <returns>A task that represents the asynchronous operation of exiting the state.</returns>
    internal abstract Task _CleanupRunnable();

    /// <summary>
    /// Retrieves the type of input that this state can process.
    /// </summary>
    /// <returns>A <see cref="Type"/> object representing the input type that this instance is designed to handle.</returns>
    public abstract Type GetInputType();
    /// <summary>
    /// Gets the output type produced by this state.
    /// </summary>
    /// <returns>A <see cref="Type"/> representing the output type.</returns>
    public abstract Type GetOutputType();

    /// <summary>
    /// Gets or sets a value indicating whether parallel transitions are allowed.
    /// </summary>
    public bool AllowsParallelAdvances { get; set; } = false;


    /// <summary>
    /// property to combine input into a single process to avoid running multiple threads for each input.
    /// </summary>
    public bool CombineInput { get; set; } = false;

    /// <summary>
    /// Used to set if this state is okay not to have transitions.
    /// </summary>
    public bool IsDeadEnd { get; set; } = false;

    /// <summary>
    /// Evaluates and returns a list of runtime processes that meet specific conditions.
    /// </summary>
    /// <returns>A list of <see cref="RunnableProcess"/> objects that satisfy the defined conditions.  The list will be empty if
    /// no conditions are met.</returns>
    internal abstract List<RunnableProcess>? CanAdvance();

    public void Cancel()
    {
        cts.Cancel();
    }
}