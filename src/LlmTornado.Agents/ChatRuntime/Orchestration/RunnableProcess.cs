namespace LlmTornado.Agents.ChatRuntime.Orchestration;

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

    public object BaseResult { get; set; } = new object();

    /// <summary>
    /// Time Execution process has Started
    /// </summary>
    public DateTime? StartTime { get; set; }

    public TimeSpan RunnerExecutionTime { get; set; }


    //public object Result { get; set; }
    public RunnableProcess() { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="runner">Runnable instance associated with this process</param>
    /// <param name="inputValue">Input value for the process</param>
    /// <param name="maxReruns">Maximum number of reruns allowed</param>
    public RunnableProcess(OrchestrationRunnableBase runner , object inputValue, int maxReruns = 3)
    {
        Runner = runner;
        BaseInput = inputValue;
        MaxReruns = maxReruns;
    }

    /// <summary>
    /// Get the execution time of the runner process.
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    public void SetExecutionTime(DateTime startTime, DateTime endTime)
    {
        RunnerExecutionTime = endTime - startTime;
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


    /// <summary>
    /// Initializes a new instance of the <see cref="RunnableProcess{T}"/> class with the specified runnable, input, and
    /// maximum rerun count.
    /// </summary>
    /// <param name="runnable">The orchestration runnable that defines the process logic to be executed.</param>
    /// <param name="input">The input data of type <typeparamref name="T"/> required by the process. Cannot be <see langword="null"/>.</param>
    /// <param name="maxReruns">The maximum number of times the process can be rerun in case of failure. Defaults to 3.</param>
    public RunnableProcess(OrchestrationRunnableBase runnable, T input, int maxReruns = 3) : base(runnable, input!, maxReruns)
    {
        Input = input!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnableProcess{T}"/> class with the specified runnable, input,
    /// identifier, and maximum rerun count.
    /// </summary>
    /// <param name="runnable">The orchestration runnable that defines the process logic to be executed.</param>
    /// <param name="input">The input data of type <typeparamref name="T"/> required by the process. Cannot be <see langword="null"/>.</param>
    /// <param name="id">A unique identifier for the process. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="maxReruns">The maximum number of times the process can be rerun in case of failure. Defaults to 3.</param>
    public RunnableProcess(OrchestrationRunnableBase runnable, T input, string id, int maxReruns = 3) : base(runnable, input!, maxReruns)
    {
        Input = input!;
        Id = id;
    }
}


/// <summary>
/// Represents a process that operates on a specific state with a generic input type.
/// </summary>
/// <remarks>This class extends the <see cref="StateProcess"/> to handle operations with a specific input
/// type. It provides functionality to create state results with the specified type.</remarks>
/// <typeparam name="T">The type of the input and result associated with the state process.</typeparam>
public class RunnableProcess<TInput, TOutput> : RunnableProcess
{
    /// <summary>
    /// Gets or sets the input value of type <typeparamref name="T"/>.
    /// </summary>
    public TInput Input { get => (TInput)BaseInput; set => BaseInput = value!; }

    public TOutput Result { get => (TOutput)BaseResult; set => BaseResult = value!; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnableProcess{T}"/> class with the specified runnable, input, and
    /// maximum rerun count.
    /// </summary>
    /// <param name="runnable">The orchestration runnable that defines the process logic to be executed.</param>
    /// <param name="input">The input data of type <typeparamref name="T"/> required by the process. Cannot be <see langword="null"/>.</param>
    /// <param name="maxReruns">The maximum number of times the process can be rerun in case of failure. Defaults to 3.</param>
    public RunnableProcess(OrchestrationRunnableBase runnable, TInput input, int maxReruns = 3) : base(runnable, input!, maxReruns)
    {
        Input = input!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnableProcess{T}"/> class with the specified runnable, input,
    /// identifier, and maximum rerun count.
    /// </summary>
    /// <param name="runnable">The orchestration runnable that defines the process logic to be executed.</param>
    /// <param name="input">The input data of type <typeparamref name="T"/> required by the process. Cannot be <see langword="null"/>.</param>
    /// <param name="id">A unique identifier for the process. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="maxReruns">The maximum number of times the process can be rerun in case of failure. Defaults to 3.</param>
    public RunnableProcess(OrchestrationRunnableBase runnable, TInput input, string id, int maxReruns = 3) : base(runnable, input!, maxReruns)
    {
        Input = input!;
        Id = id;
    }
}