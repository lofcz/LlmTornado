namespace LlmTornado.Agents.Orchestration.Core;

/// <summary>
/// Represents a process that manages the execution state and input for a state-based operation.
/// </summary>
/// <remarks>The <see cref="StateProcess"/> class provides functionality to manage the state of an
/// operation, including the ability to rerun the operation a specified number of times. It also allows for the
/// creation of state results and the conversion to a typed state process.</remarks>
public class RunnableProcess
{
    /// <summary>
    /// Max alled reruns for this process.
    /// </summary>
    public int MaxReruns { get; set; } = 3;
    /// <summary>
    /// Current rerun attempts for this process.
    /// </summary>
    private int rerunAttempts { get; set; } = 0;
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>
    /// Get the State for this process.
    /// </summary>
    public OrchestrationRunnableBase Runner { get; set; }

    /// <summary>
    /// Gets or sets the input object to process.
    /// </summary>
    public object BaseInput { get; set; } = new object();

    //public object Result { get; set; }
    public RunnableProcess() { }

    public RunnableProcess(OrchestrationRunnableBase runner , object inputValue, int maxReruns = 3)
    {
        Runner = runner;
        BaseInput = inputValue;
        MaxReruns = maxReruns;
    }

    /// <summary>
    /// Determines whether another attempt can be made based on the current number of rerun attempts.
    /// </summary>
    /// <returns><see langword="true"/> if the number of rerun attempts is less than the maximum allowed reruns; otherwise,
    /// <see langword="false"/>.</returns>
    public bool CanReAttempt()
    {
        rerunAttempts++;
        return rerunAttempts < MaxReruns;
    }

    /// <summary>
    /// Creates a new <see cref="RunnerResult"/> instance associated with the current state.
    /// </summary>
    /// <param name="result">The result object to be encapsulated within the <see cref="StateResult"/>.</param>
    /// <returns>A <see cref="RunnerResult/> containing the specified result object and the current state's identifier.</returns>
    public RunnerResult CreateRunnerResult(object result)
    {
        return new RunnerResult(Id, result);
    }

    /// <summary>
    /// Retrieves a new instance of <see cref="StateProcess{T}"/> initialized with the current state and input.
    /// </summary>
    /// <remarks>Used for Rerun generation</remarks>
    /// <typeparam name="T">The type of the input used to initialize the process.</typeparam>
    /// <returns>A <see cref="StateProcess{T}"/> object initialized with the current state and input of type <typeparamref
    /// name="T"/>.</returns>
    public RunnableProcess<T> GetProcess<T>()
    {
        return new RunnableProcess<T>(Runner, (T)BaseInput, Id);
    }

    public void Cancel()
    {
        Runner.cts.Cancel();
    }
}

/// <summary>
/// Represents a process that operates on a specific state with a generic input type.
/// </summary>
/// <remarks>This class extends the <see cref="StateProcess"/> to handle operations with a specific input
/// type. It provides functionality to create state results with the specified type.</remarks>
/// <typeparam name="T">The type of the input and result associated with the state process.</typeparam>
public class RunnableProcess<T> : RunnableProcess
{
    /// <summary>
    /// Gets or sets the input value of type <typeparamref name="T"/>.
    /// </summary>
    public T Input { get => (T)BaseInput; set => BaseInput = value!; }

    public RunnableProcess(OrchestrationRunnableBase runnable, T input, int maxReruns = 3) : base(runnable, input!, maxReruns)
    {
        Input = input!;
    }

    public RunnableProcess(OrchestrationRunnableBase runnable, T input, string id, int maxReruns = 3) : base(runnable, input!, maxReruns)
    {
        Input = input!;
        Id = id;
    }

    /// <summary>
    /// Creates a new <see cref="StateResult{T}"/> instance with the specified result.
    /// </summary>
    /// <param name="result">The result value to be encapsulated within the <see cref="RunnerResult{T}"/>.</param>
    /// <returns>A <see cref="RunnerResult{T}"/> containing the specified result and the current state ID.</returns>
    public RunnerResult<T> CreateRunnerResult(T result)
    {
        return new RunnerResult<T>(Id, result);
    }
}