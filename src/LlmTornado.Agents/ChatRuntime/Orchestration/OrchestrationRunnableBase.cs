using LlmTornado.Agents.DataModels;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;

/// <summary>
/// Represents the base class for defining a state within a state machine.
/// </summary>
/// <remarks>The <see cref="OrchestrationRunnableBase"/> class provides a framework for managing state transitions, input
/// and output processing, and state invocation within a state machine. It includes properties and methods for
/// handling state-specific logic, such as entering and exiting states, checking conditions, and managing
/// transitions.</remarks>
public abstract class OrchestrationRunnableBase 
{
    public string RunnableName { get; set; } = "Runnable";
    /// <summary>
    /// Used to limit the number of times to rerun the state.
    /// </summary>
    public bool BeingReran = false;

    /// <summary>
    /// State identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();


    /// <summary>
    /// Input processes that the state has to process this tick.
    /// </summary>
    private List<RunnableProcess> _baseProcesses { get; set; } = new List<RunnableProcess>();


    ///// <summary>
    ///// Not used.
    ///// </summary>
    internal List<OrchestrationAdvancer<object>> BaseAdvancers { get; set; } = new List<OrchestrationAdvancer<object>>();

    
    public Orchestration? Orchestrator { get; set; }


    /// <summary>
    /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation requests.
    /// </summary>
    public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

    /// <summary>
    /// Internal invoke to abstract BaseState from Type specifics.
    /// </summary>
    /// <returns></returns>
    /// 
    internal abstract ValueTask Invoke();

    /// <summary>
    /// Adds state Process to the required state.
    /// </summary>
    /// <remarks>This method is abstract and must be implemented by derived classes to define the
    /// specific behavior of entering a new state.</remarks>
    /// <param name="input">The input that influences the state transition. Can be null if no specific input is required for the
    /// transition.</param>
    /// <returns></returns>
    internal abstract ValueTask _InitializeRunnable(RunnableProcess? input);

    /// <summary>
    /// Transitions the current state to an exit state asynchronously.
    /// </summary>
    /// <remarks>This method should be implemented to handle any necessary cleanup or finalization
    /// tasks when exiting a state. It is called as part of the state transition process.</remarks>
    /// <returns>A task that represents the asynchronous operation of exiting the state.</returns>
    internal abstract ValueTask _CleanupRunnable();

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
    public bool SingleInvokeForProcesses { get; set; } = false;

    /// <summary>
    /// Used to set if this state is okay not to have transitions.
    /// </summary>
    public bool AllowDeadEnd { get; set; } = false;

    /// <summary>
    /// Whether this state is thread safe to run in parallel.
    /// </summary>
    public bool IsThreadSafe { get; set; } = false;  


    /// <summary>
    /// Evaluates and returns a list of runtime processes that meet specific conditions.
    /// </summary>
    /// <returns>A list of <see cref="RunnableProcess"/> objects that satisfy the defined conditions.  The list will be empty if
    /// no conditions are met.</returns>
    internal abstract List<RunnableProcess>? CanAdvance();

    /// <summary>
    /// Cancels the execution of the current state and any associated operations.
    /// </summary>
    public void Cancel()
    {
        cts.Cancel();
    }

    internal void AddAdvancer<TOutput>(OrchestrationAdvancer<TOutput> advancer)
    {
        AdvancementRequirement<object> advancementRequirement = (object input) => advancer.InvokeMethod((TOutput)input);
        BaseAdvancers.Add(new OrchestrationAdvancer<object>(advancementRequirement, advancer.NextRunnable));
    }

    internal object[] GetBaseResults()
    {
        return _baseProcesses.Where(p => p.BaseResult is not null).Select(p => p.BaseResult!).ToArray();
    }

    internal List<RunnableProcess> GetRunnableProcesses()
    {
        return _baseProcesses;
    }

    internal List<RunnableProcess<TInput, TOutput>> GetBaseRunnableProcesses<TInput, TOutput>()
    {
        return _baseProcesses.Select(process => process.ReturnProcess<TInput, TOutput>()).ToList();
    }

    internal void AddBaseRunnableProcess(RunnableProcess process)
    {
        _baseProcesses.Add(process);
    }

    internal void AddBaseRunnableProcess<TInput, TOutput>(RunnableProcess<TInput, TOutput> process)
    {
        _baseProcesses.Add(process);
    }

    internal void UpdateBaseRunnableProcess(string id, object result)
    {
        var existingProcess = _baseProcesses.FirstOrDefault(p => p.Id == id);
        if (existingProcess != null)
        {
            existingProcess.BaseResult = result;
        }
    }

    internal void UpdateBaseRunnableProcess(string id, RunnableProcess process)
    {
        var existingProcess = _baseProcesses.FirstOrDefault(p => p.Id == id);
        if (existingProcess != null)
        {
            existingProcess.BaseResult = process.BaseResult;
            existingProcess.TokenUsage = process.TokenUsage;
            existingProcess.RunnableExecutionTime = process.RunnableExecutionTime;
            existingProcess.StartTime = process.StartTime;
        }
    }

    internal void OverrideBaseRunnableProcess(string id, RunnableProcess process)
    {
        var existingProcess = _baseProcesses.FirstOrDefault(p => p.Id == id);
        if (existingProcess != null)
        {
            _baseProcesses.Remove(existingProcess);
            process.Id = id;
            _baseProcesses.Add(process);
        }
    }

    internal void ClearProcessTokenUsage(string processId)
    {
        RunnableProcess? process = _baseProcesses.FirstOrDefault(p => p.Id == processId);
        if (process != null)
        {
            process.TokenUsage = 0;
        }
    }

    internal void ClearAllProcessTokenUsage()
    {
        foreach (var process in _baseProcesses)
        {
            process.TokenUsage = 0;
        }
    }
}