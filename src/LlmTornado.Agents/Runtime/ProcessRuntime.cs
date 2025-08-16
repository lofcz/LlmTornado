using System.Collections.Concurrent;
using System.Reflection;

namespace LlmTornado.Agents.Runtime;

/// <summary>
/// Represents a state machine that manages the execution of state processes with support for concurrency and
/// cancellation. [SUGGEST USING RuntimeMachine&lt;TInput, TOutput&gt;]
/// </summary>
/// <remarks>The <see cref="RuntimeMachine"/> class provides mechanisms to initialize, run, and manage state
/// processes. It supports concurrent execution of processes up to a specified maximum number of threads, and allows
/// for graceful stopping and cancellation of operations. The state machine can be reset and reused for multiple
/// runs.</remarks>
public class ProcessRuntime
{
    /// <summary>
    /// Triggers events related to the state machine, such as state transitions and errors.
    /// </summary>
    public Action<RuntimeEvent>? OnRuntimeEvent { get; set; } 

    /// <summary>
    /// List of processes that will be run in the state machine this tick.
    /// </summary>
    public List<RunnableProcess> CurrentRunnerProcesses { get; private set; } = new List<RunnableProcess>();

    private List<RunnableProcess> newRunnerProcesses = new List<RunnableProcess>();
    /// <summary>
    /// Trigger to stop the state machine.
    /// </summary>
    public CancellationTokenSource StopTrigger = new CancellationTokenSource();

    private bool _isFinished = false;

    /// <summary>
    /// You can use this to store runtime properties that you want to access later or in other states.
    /// </summary>
    public ConcurrentDictionary<string, object> RuntimeProperties { get; set; } = new ConcurrentDictionary<string, object>();

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> used to signal cancellation of the current operation.
    /// </summary>
    public CancellationToken CancelToken => StopTrigger.Token;

    /// <summary>
    /// Gets or sets a value indicating whether the process is complete.
    /// </summary>
    public bool IsFinished { get => _isFinished; }

    /// <summary>
    /// Gets or sets the final result of the computation.
    /// </summary>
    public object? BaseFinalResult { get; set; }

    /// <summary>
    /// Gets or sets the collection of states.
    /// </summary>
    public List<BaseRunner> Runners { get; set; } = new List<BaseRunner>();

    /// <summary>
    /// Gets or sets a value indicating whether the steps of the process should be recorded.
    /// </summary>
    public bool RecordSteps { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection of steps, where each step is represented as a list of state processes.
    /// </summary>
    public List<List<RunnableProcess>> Steps { get; set; } = new List<List<RunnableProcess>>();

    private bool isInitialized = false;

    public ProcessRuntime()
    {

    }

    /// <summary>
    /// Marks the current operation as finished.
    /// </summary>
    /// <remarks>Sets the <see cref="IsFinished"/> property to <see langword="true"/>, indicating that
    /// the operation is complete.</remarks>
    public void FinalizeRuntime() { _isFinished = true; }

    /// <summary>
    /// Used to stop the state machine and cancel any ongoing operations.
    /// </summary>
    public void CancelRuntime() => StopTrigger.Cancel();

    /// <summary>
    /// Asynchronously exits all active processes.
    /// </summary>
    /// <remarks>This method initiates the exit sequence for each active process and waits for all
    /// processes to complete their exit operations.  It runs the exit operations concurrently to improve
    /// performance.</remarks>
    /// <returns></returns>
    private async Task ExitActiveProcesses()
    {
        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Exiting all processes...")); //Invoke the exit state machine event

        List<Task> Tasks = new List<Task>();

        //Exit all processes
        CurrentRunnerProcesses.ForEach(process => {
            Tasks.Add(Task.Run(async () => await BeginExit(process)));
        });

        await Task.WhenAll(Tasks);
        Tasks.Clear();

        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Finished Exiting all processes!"));
    }

    private async Task BeginExit(RunnableProcess process)
    {
        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Exiting Runtime..."));
        await process.Runner._CleanupRunnable();
        OnRuntimeEvent?.Invoke(new OnRunnableFinishedEvent(process.Runner)); //Invoke the exit state machine event
        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("RuntimeExited."));
    }

    /// <summary>
    /// Processes each active process asynchronously, ensuring that the number of concurrent executions is limited
    /// by the thread limiter.
    /// </summary>
    /// <remarks>This method runs each active process in parallel, respecting the concurrency limits
    /// imposed by the <c>threadLimitor</c>. It waits for all processes to complete before returning.</remarks>
    /// <returns></returns>
    private async Task ProcessTick()
    {
        //If there are no processes, return
        if (CurrentRunnerProcesses.Count == 0) { _isFinished = true; return; }

        OnRuntimeEvent?.Invoke(new OnTickRuntimeEvent()); //Invoke the tick state machine event
        List<Task> Tasks = new List<Task>();
        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent($"Processing {CurrentRunnerProcesses.Count} active processes..."));

        CurrentRunnerProcesses.ForEach(process => Tasks.Add(Task.Run(async () =>
        {
            OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent($"Invoking state: {process.Runner.GetType().Name}"));
            OnRuntimeEvent?.Invoke(new OnRunnableStartedEvent(process)); //Invoke the state entered event
            await process.Runner._Invoke(); //Invoke the state process

        })));

        //Wait for collection
        await Task.WhenAll(Tasks);
        Tasks.Clear();
    }

    /// <summary>
    /// Initializes the specified state process by setting its current state machine and adding it to the active
    /// processes.
    /// </summary>
    /// <remarks>This method ensures thread-safe access to the state machine by using a semaphore. It
    /// sets the current state machine for the process if it is not already set and adds the process to the list of
    /// active processes. After initialization, it enters the state of the process.</remarks>
    /// <param name="process">The state process to initialize. This parameter cannot be null.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Thrown when process or process.Runnable is null.</exception>
    private async Task InitilizeProcess(RunnableProcess process)
    {
        if (process?.Runner == null)
            throw new ArgumentNullException(nameof(process), "Process and its Runtime cannot be null");

        //Gain access to state machine
        process.Runner.ActiveRuntime ??= this; //Set the current state machine if not already set
        CurrentRunnerProcesses.Add(process);

        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent($"Entering state: {process.Runner.GetType().Name}"));
        OnRuntimeEvent?.Invoke(new OnRunnableStartedEvent(process));
        //Internal lock on access to state
        await process.Runner._InitializeRunnable(process); //preset input

        isInitialized = true; //Set the initialized flag to true after the process is initialized
    }

    /// <summary>
    /// Initializes all new state processes asynchronously.
    /// </summary>
    /// <remarks>This method clears the current active processes and initializes each new state
    /// process concurrently. After initialization, it ensures that only distinct state processes, based on their
    /// state ID, remain active.</remarks>
    /// <param name="newRuntimeProcesses">A list of <see cref="RunnableProcess"/> objects representing the new state processes to be initialized.</param>
    /// <returns></returns>
    private async Task InitilizeAllNewProcesses()
    {
        //Clear all of the active processes
        CurrentRunnerProcesses.Clear();
        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent($"Initializing {newRunnerProcesses.Count} new state processes..."));
        //If there are no new processes, return
        if (newRunnerProcesses.Count == 0) { _isFinished = true; return; }

        //Record Step
        if (RecordSteps) { Steps.Add(newRunnerProcesses); }


        //Initialize each new state process concurrently
        List<Task> Tasks = new List<Task>();
        foreach (RunnableProcess stateProcess in newRunnerProcesses)
        {
            Tasks.Add(Task.Run(async () => await InitilizeProcess(stateProcess)));
        }
        await Task.WhenAll(Tasks);
        Tasks.Clear();

        //This is to remove running the same state twice with two processes.. it gets input from _EnterRuntime
        // Replace the DistinctBy line with this GroupBy approach which is compatible with .NET Standard 2.0
        CurrentRunnerProcesses = CurrentRunnerProcesses
            .GroupBy(state => state.Runner.Id)
            .Select(g => g.First())
            .ToList();

        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent($"Initialization Complete."));
    }

    /// <summary>
    /// Retrieves a list of new state processes based on the current conditions.
    /// </summary>
    /// <remarks>This method evaluates each active process and collects new state processes that meet
    /// specific conditions. The returned list may be empty if no new state processes are identified.</remarks>
    /// <returns>A list of <see cref="RunnableProcess"/> objects representing the new state processes that meet the specified
    /// conditions. The list will be empty if no new processes are found.</returns>
    private void GetNewProcesses()
    {
        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Validating Runtime conditions for transitions"));

        newRunnerProcesses.Clear();
        CurrentRunnerProcesses.ForEach(process => {
            newRunnerProcesses.AddRange(process.Runner.CanAdvance() ?? new List<RunnableProcess>());
        });

        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Finished Validations"));
    }

    /// <summary>
    /// Determines whether the current operation is finished and triggers the necessary actions if so.
    /// </summary>
    /// <remarks>If the operation is finished, this method will asynchronously exit all processes and
    /// invoke the <see cref="OnFinishedTriggered"/> event.</remarks>
    /// <returns><see langword="true"/> if the operation is finished and the exit processes have been triggered; otherwise,
    /// <see langword="false"/>.</returns>
    private async Task<bool> CheckIfFinished()
    {
        if (IsFinished)
        {
            await ExitActiveProcesses();
            OnRuntimeEvent?.Invoke(new OnFinishedRuntimeEvent()); //Invoke the finished state machine event
            OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Runtime Machine Finished."));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether the cancellation has been requested and handles the cancellation process.
    /// </summary>
    /// <remarks>If the cancellation is requested, this method triggers the <see
    /// cref="OnCancellationTriggered"/> event, exits all processes asynchronously, and returns <see
    /// langword="true"/>. Otherwise, it returns <see langword="false"/>.</remarks>
    /// <returns><see langword="true"/> if the cancellation has been requested and handled; otherwise, <see
    /// langword="false"/>.</returns>
    private async Task<bool> CheckIfCancelled()
    {
        if (StopTrigger.IsCancellationRequested)
        {
            //what if invoking the state? How do we handle that?
            await ExitActiveProcesses();
            OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Runtime Machine Cancelled."));
            OnRuntimeEvent?.Invoke(new OnCancelledRuntimeEvent()); //Invoke the cancelled state machine event
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resets the current state machine, preparing the system for a new execution cycle.
    /// </summary>
    /// <remarks>This method sets the <see cref="IsFinished"/> flag to <see langword="false"/>, resets
    /// the stop trigger,  and clears the list of active processes. It should be called before starting a new run to
    /// ensure  that the system is in a clean state.</remarks>
    private void ResetRun()
    {
        OnRuntimeEvent?.Invoke(new OnVerboseRuntimeEvent("Resetting RuntimeMachine"));
        _isFinished = false;
        StopTrigger = new CancellationTokenSource(); //Reset the stop trigger
        CurrentRunnerProcesses.Clear();
        newRunnerProcesses.Clear();
    }


    public virtual async Task InitializeRuntime(BaseRunner initialRunner, object? input = null)
    {
        if (initialRunner == null)
            throw new ArgumentNullException(nameof(initialRunner), "Start state cannot be null");

        OnRuntimeEvent?.Invoke(new OnBeginRuntimeEvent()); //Invoke the begin state machine event

        ResetRun(); //Reset the state machine before running

        newRunnerProcesses.Add(new RunnableProcess(initialRunner, input));

        //Initialize the process with the starting state and input
        await InitilizeAllNewProcesses();
    }

    internal async Task InvokeStep()
    {
        if(!isInitialized)
        {
            throw new InvalidOperationException("Runtime has not been initialized. Call InitializeRuntime() before invoking steps.");
        }

        //Collect each state Result
        await ProcessTick();

        //stop the state machine if needed & exit all states
        if(await CheckIfFinished())
        {
            return;
        }   

        if(await CheckIfCancelled())
        {
            return;
        }

        //Create List of transitions to new states from conditional movement
        GetNewProcesses();

        //Exit the current Processes
        await ExitActiveProcesses();

        //Reset Active Processes Here
        await InitilizeAllNewProcesses();
    }
}

/// <summary>
/// Represents a state machine that processes inputs of type <typeparamref name="TInput"/> and produces outputs of
/// type <typeparamref name="TOutput"/>.
/// </summary>
/// <remarks>The <see cref="Runtime{TInput, TOutput}"/> class allows for the execution of a series of
/// states, starting from a specified entry state and concluding at a result state.</remarks>
/// <typeparam name="TInput">The type of input that the state machine processes.</typeparam>
/// <typeparam name="TOutput">The type of output that the state machine produces.</typeparam>
public class Runtime<TInput, TOutput> : ProcessRuntime
{
    /// <summary>
    /// Provides a mechanism for comparing two <see cref="RunnableOutputCollection{TOutput}"/> objects based on their
    /// index values.
    /// </summary>
    /// <remarks>This comparer is used to sort or order <see cref="RunnableOutputCollection{TOutput}"/>
    /// instances by their index. If either collection has an index of zero, the collections are considered
    /// equal.</remarks>
    class IndexSorter : IComparer<RunnableOutputCollection<TOutput?>>
    {
        public int Compare(RunnableOutputCollection<TOutput?>? x, RunnableOutputCollection<TOutput?>? y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            if (x.Index == 0 || y.Index == 0)
            {
                return 0;
            }

            // CompareTo() method
            return x.Index.CompareTo(y.Index);
        }
    }

    /// <summary>
    /// Result of the state machine run, containing a list of outputs of type <typeparamref name="TOutput"/>.
    /// </summary>
    public List<TOutput>? Results => ResultRuntime.BaseOutput.ConvertAll(item => (TOutput)item)!;

    /// <summary>
    /// Gets or sets the initial state of the system or process.
    /// </summary>
    public BaseRunner StartRuntime { get; private set; }
    /// <summary>
    /// Gets or sets the result state of the operation.
    /// </summary>
    public BaseRunner ResultRuntime { get; private set; }

    public Runtime() { }

    /// <summary>
    /// Executes the state machine starting from the specified start state and processes the given input.
    /// </summary>
    /// <param name="input">The input data to be processed by the state machine.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results produced by the state machine.
    /// Each result corresponds to an output from the state machine, and may be null if no output is produced for a
    /// particular state transition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the start state or result state is not set before execution.</exception>
    public override async Task InitializeRuntime(BaseRunner initialRunner, object? input = null)
    {
        //Input validation before running the state machine
        if (StartRuntime == null)
        {
            throw new InvalidOperationException("Need to Set a Start Runtime for the Resulting RuntimeMachine");
        }

        if (ResultRuntime == null)
        {
            throw new InvalidOperationException("Need to Set a Result Runtime for the Resulting RuntimeMachine");
        }

        await base.InitializeRuntime(initialRunner, input);
    }

    /// <summary>
    /// Sets the initial state for the entry point of the state machine.
    /// </summary>
    /// <param name="initialRunner">The initial state to be set. Must have an input type assignable to <typeparamref name="TInput"/>.</param>
    /// <exception cref="InvalidCastException">Thrown if the input type of <paramref name="initialRunner"/> is not assignable to <typeparamref
    /// name="TInput"/>.</exception>
    public void SetEntryRuntime(BaseRunner initialRunner)
    {
        if (!typeof(TInput).IsAssignableFrom(initialRunner.GetInputType()))
        {
            throw new InvalidCastException($"Entry Runtime {initialRunner.ToString()} with Input type of {initialRunner.GetInputType()} Requires Input Type of {typeof(TInput)}");
        }

        StartRuntime = initialRunner;
    }

    /// <summary>
    /// Sets the output state for the current state machine.
    /// </summary>
    /// <remarks>This method updates the current output state, ensuring type compatibility with the
    /// expected output type.</remarks>
    /// <param name="lastRunner">The state to be set as the output. Must have an output type assignable to <typeparamref name="TOutput"/>.</param>
    /// <exception cref="InvalidCastException">Thrown if the output type of <paramref name="lastRunner"/> is not assignable to <typeparamref
    /// name="TOutput"/>.</exception>
    public void SetOutputRuntime(BaseRunner lastRunner)
    {
        if (!typeof(TOutput).IsAssignableFrom(lastRunner.GetOutputType()))
        {
            throw new InvalidCastException($"Output Runtime {lastRunner.ToString()} with Output type of {lastRunner.GetOutputType()} Requires Cast of Output Type to {typeof(TOutput)}");
        }

        ResultRuntime = lastRunner;
    }

}

/// <summary>
/// Represents a collection of output results from a run, along with an index indicating the position of the input array.
/// </summary>
/// <remarks>This class is used to store and manage the results of a run operation, providing both the
/// results and the index of the run. It can be used to track multiple runs and their respective outputs.</remarks>
/// <typeparam name="TOutput">The type of the output results contained in the collection.</typeparam>
public class RunnableOutputCollection<TOutput>
{
    public int Index { get; set; } = 0;
    public List<TOutput> Results { get; set; }

    public RunnableOutputCollection() { }

    public RunnableOutputCollection(int index, List<TOutput> results)
    {
        Index = index;
        Results = results;
    }
}