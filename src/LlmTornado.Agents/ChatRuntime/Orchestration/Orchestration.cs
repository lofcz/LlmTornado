using LlmTornado.Chat;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;

/// <summary>
/// Represents a state machine that manages the execution of state processes with support for concurrency and
/// cancellation. [SUGGEST USING RuntimeMachine&lt;TInput, TOutput&gt;]
/// </summary>
/// <remarks>The <see cref="RuntimeMachine"/> class provides mechanisms to initialize, run, and manage state
/// processes. It supports concurrent execution of processes up to a specified maximum number of threads, and allows
/// for graceful stopping and cancellation of operations. The state machine can be reset and reused for multiple
/// runs.</remarks>
public class Orchestration
{
    /// <summary>
    /// Gets or sets the initial state of the system or process.
    /// </summary>
    public OrchestrationRunnableBase InitialRunnable { get; protected set; }
    /// <summary>
    /// Triggers events related to the state machine, such as state transitions and errors.
    /// </summary>
    public Action<OrchestrationEvent>? OnOrchestrationEvent { get; set; } 

    /// <summary>
    /// List of processes that will be run in the state machine this tick.
    /// </summary>
    public List<RunnableProcess> CurrentRunnableProcesses { get; private set; } = new List<RunnableProcess>();

    private List<RunnableProcess> newRunnableProcesses = new List<RunnableProcess>();
    /// <summary>
    /// Trigger to stop the state machine.
    /// </summary>
    public CancellationTokenSource StopTrigger = new CancellationTokenSource();

    private bool _isCompleted = false;

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
    public bool IsCompleted { get => _isCompleted; }

    /// <summary>
    /// Gets or sets the final result of the computation.
    /// </summary>
    public List<object>? BaseFinalResult { get; set; }

    // [consideration] I actually don't have this set anywhere.. I need this to have all the states but states are internally linked to each other.
    /// <summary>
    /// Gets or sets the collection of states.
    /// </summary>
    public Dictionary<string, OrchestrationRunnableBase> Runnables { get; set; } = new Dictionary<string, OrchestrationRunnableBase>();

    /// <summary>
    /// Gets or sets a value indicating whether the steps of the process should be recorded.
    /// </summary>
    public bool RecordSteps { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection of steps, where each step is represented as a list of runnable processes.
    /// </summary>
    public ConcurrentDictionary<int, List<RunnerRecord>> RunSteps { get; set; } = new ConcurrentDictionary<int, List<RunnerRecord>>();

    private int _stepCounter = 0;

    private bool _isInitialized = false;

    public Orchestration()
    {

    }

    /// <summary>
    /// Marks the current operation as finished.
    /// </summary>
    /// <remarks>Sets the <see cref="IsCompleted"/> property to <see langword="true"/>, indicating that
    /// the operation is complete.</remarks>
    public void HasCompletedSuccessfully() { _isCompleted = true; }

    /// <summary>
    /// Used to stop the state machine and cancel any ongoing operations.
    /// </summary>
    public void Cancel() => StopTrigger.Cancel();

    /// <summary>
    /// Add a record of the current step in the orchestration process.
    /// </summary>
    /// <param name="record"></param>
    public void AddRecordStep(RunnerRecord record)
    {
        if (!RecordSteps) return;

        RunSteps.AddOrUpdate(_stepCounter, new List<RunnerRecord>() { record }, (key, existingList) =>
        {
            existingList.Add(record);
            return existingList;
        });
    }
    /// <summary>
    /// Asynchronously exits all active runnables.
    /// </summary>
    /// <remarks>This method initiates the exit sequence for each active runnable and waits for all
    /// runnables to complete their exit operations.  It runs the exit operations concurrently to improve
    /// performance.</remarks>
    /// <returns></returns>
    private async Task ExitActiveRunnables()
    {
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Exiting all runnables...")); //Invoke the exit state machine event

        List<Task> Tasks = new List<Task>();
        CurrentRunnableProcesses.ForEach(process => {
            Tasks.Add(Task.Run(async () => await BeginExit(process)));
        });
        await Task.WhenAll(Tasks);
        Tasks.Clear();

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Finished Exiting all runnables!"));
    }

    private async Task BeginExit(RunnableProcess process)
    {
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Exiting  ..."));
        await process.Runner._CleanupRunnable();
        OnOrchestrationEvent?.Invoke(new OnFinishedRunnableEvent(process.Runner)); //Invoke the exit state machine event
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Orchestration Exited."));
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
        if (CurrentRunnableProcesses.Count == 0) { HasCompletedSuccessfully(); return; }

        _stepCounter++;

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Processing {CurrentRunnableProcesses.Count} active processes..."));
        OnOrchestrationEvent?.Invoke(new OnTickOrchestrationEvent()); //Invoke the tick state machine event
        
        List<Task> Tasks = new List<Task>();
        CurrentRunnableProcesses.ForEach(process => Tasks.Add(Task.Run(async () =>
        {
            OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Invoking state: {process.Runner.GetType().Name}"));
            OnOrchestrationEvent?.Invoke(new OnStartedRunnableEvent(process.Runner)); //Invoke the state entered event
            await process.Runner.Invoke(); //Invoke the state process

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
        process.Runner.Orchestrator ??= this; //Set the current state machine if not already set
        CurrentRunnableProcesses.Add(process);

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Entering state: {process.Runner.GetType().Name}"));
        OnOrchestrationEvent?.Invoke(new OnStartedRunnableEvent(process.Runner));
        //Internal lock on access to state
        await process.Runner._InitializeRunnable(process); //preset input

        _isInitialized = true; //Set the initialized flag to true after the process is initialized
    }

    /// <summary>
    /// Initializes all new state processes asynchronously.
    /// </summary>
    /// <remarks>This method clears the current active processes and initializes each new state
    /// process concurrently. After initialization, it ensures that only distinct state processes, based on their
    /// state ID, remain active.</remarks>
    /// <returns></returns>
    private async Task InitilizeAllNewProcesses()
    {
        //Clear all of the active processes
        CurrentRunnableProcesses?.Clear();

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Initializing {newRunnableProcesses.Count} new state processes..."));

        //If there are no new processes, return
        if (newRunnableProcesses.Count == 0) { HasCompletedSuccessfully(); return; }

        //Initialize each new state process concurrently
        List<Task> Tasks = new List<Task>();
        foreach (RunnableProcess process in newRunnableProcesses)
        {
            Tasks.Add(Task.Run(async () => await InitilizeProcess(process)));
        }
        await Task.WhenAll(Tasks);
        Tasks.Clear();

        //This is to remove running the same state twice with two processes.. it gets input from _EnterRuntime
        // Replace the DistinctBy line with this GroupBy approach which is compatible with .NET Standard 2.0
        CurrentRunnableProcesses = CurrentRunnableProcesses?
            .GroupBy(state => state.Runner.Id)
            .Select(g => g.First())
            .ToList()!;

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Initialization Complete."));
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
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Validating Runtime conditions for transitions"));

        newRunnableProcesses.Clear();
        CurrentRunnableProcesses.ForEach(process => {
            newRunnableProcesses.AddRange(process.Runner.CanAdvance() ?? new List<RunnableProcess>());
        });

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Finished Validations"));
    }

    /// <summary>
    /// Determines whether the current operation is finished and triggers the necessary actions if so.
    /// </summary>
    /// <remarks>If the operation is finished, this method will asynchronously exit all processes and
    /// invoke the <see cref="OnFinishedTriggered"/> event.</remarks>
    /// <returns><see langword="true"/> if the operation is finished and the exit processes have been triggered; otherwise,
    /// <see langword="false"/>.</returns>
    private async Task<bool> CheckIfCompleted()
    {
        if (IsCompleted)
        {
            await ExitActiveRunnables();
            OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Runtime Machine Finished."));
            OnOrchestrationEvent?.Invoke(new OnFinishedOrchestrationEvent()); //Invoke the finished state machine event
            return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether the cancellation has been requested and handles the cancellation process.
    /// </summary>
    /// <remarks>If the cancellation is requested, this method triggers the <see
    /// cref="OnCancelledOrchestrationEvent"/> event, exits all processes asynchronously, and returns <see
    /// langword="true"/>. Otherwise, it returns <see langword="false"/>.</remarks>
    /// <returns><see langword="true"/> if the cancellation has been requested and handled; otherwise, <see
    /// langword="false"/>.</returns>
    private async Task<bool> CheckIfCancelled()
    {
        if (StopTrigger.IsCancellationRequested)
        {
            //what if invoking the state? How do we handle that?
            await ExitActiveRunnables();
            OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Runtime Machine Cancelled."));
            OnOrchestrationEvent?.Invoke(new OnCancelledOrchestrationEvent()); //Invoke the cancelled state machine event
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resets the current state machine, preparing the system for a new execution cycle.
    /// </summary>
    /// <remarks>This method sets the <see cref="IsCompleted"/> flag to <see langword="false"/>, resets
    /// the stop trigger,  and clears the list of active processes. It should be called before starting a new run to
    /// ensure  that the system is in a clean state.</remarks>
    private void ResetRun()
    {
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Resetting RuntimeMachine"));
        _isCompleted = false;
        _stepCounter = 0;
        StopTrigger = new CancellationTokenSource(); //Reset the stop trigger
        CurrentRunnableProcesses.Clear();
        newRunnableProcesses.Clear();
    }


    /// <summary>
    /// Sets the initial state for the entry point of the state machine.
    /// </summary>
    /// <param name="initialRunnable">The initial state to be set. Must have an input type assignable to <typeparamref name="TInput"/>.</param>
    /// <exception cref="InvalidCastException">Thrown if the input type of <paramref name="initialRunnable"/> is not assignable to <typeparamref
    /// name="TInput"/>.</exception>
    public virtual void SetEntryRunnable(OrchestrationRunnableBase initialRunnable)
    {
        InitialRunnable = initialRunnable;
    }

    internal virtual async Task Initialize(object? input = null)
    {
        ResetRun(); //Reset the state machine before running

        newRunnableProcesses.Add(new RunnableProcess(InitialRunnable, input));

        //Initialize the process with the starting state and input
        await InitilizeAllNewProcesses();

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Initilization Completed!")); //Invoke the begin state machine event
    }

    /// <summary>
    /// Invokes the state machine asynchronously, starting from the initial state and processing the provided input.
    /// </summary>
    /// <param name="input"> Input result for the Orchestration</param>
    /// <returns></returns>
    public async Task InvokeAsync(object? input = null)
    {
        await Initialize(input);
        await RunToCompletion();
    }

    internal async Task RunToCompletion()
    {
        if(!_isInitialized)
        {
            throw new InvalidOperationException("Runtime has not been initialized. Call InitializeRuntime() before running to completion.");
        }

        OnOrchestrationEvent?.Invoke(new OnBeginOrchestrationEvent()); //Invoke the begin state machine event

        while (!IsCompleted && !StopTrigger.IsCancellationRequested)
        {
            await InvokeStep();
        }
    }

    private async Task InvokeStep()
    {
        if(!_isInitialized)
        {
            throw new InvalidOperationException("Runtime has not been initialized. Call InitializeRuntime() before invoking steps.");
        }

        //Collect each state Result
        await ProcessTick();

        //stop the state machine if needed & exit all states
        if(await CheckIfCompleted()) return;


        if(await CheckIfCancelled()) return;

        //Create List of transitions to new states from conditional movement
        GetNewProcesses();

        //Exit the current Processes
        await ExitActiveRunnables();

        //Reset Active Processes Here
        await InitilizeAllNewProcesses();
    }

    internal void OnStartingRunnableProcess(RunnableProcess process)
    {
        OnOrchestrationEvent?.Invoke(new OnStartedRunnableProcessEvent(process));
    }

    internal void OnFinishedRunnableProcess(RunnableProcess process)
    {
        OnOrchestrationEvent?.Invoke(new OnFinishedRunnableProcessEvent(process));
    }

    /// <summary>
    /// Gets the final results of the state machine run as a list of objects.
    /// </summary>
    /// <returns></returns>
    public virtual List<object>? GetResults()
    {
        return this.BaseFinalResult;
    }

    /// <summary>
    /// try to get the results of the state machine run as a list of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual bool TryGetResults<T>(out List<T> value)
    {
        try
        {
            if (BaseFinalResult is List<T> result)
            {
                value = result;
                return true;
            }
            else
            {
                value = default!;
                return false;
            }
        }
        catch (Exception ex)
        {
            value = default!;
            OnOrchestrationEvent?.Invoke(new OnErrorOrchestrationEvent(ex));
            return false;
        }
    }
}

/// <summary>
/// Represents a state machine that processes inputs of type <typeparamref name="TInput"/> and produces outputs of
/// type <typeparamref name="TOutput"/>.
/// </summary>
/// <remarks>The <see cref="Orchestration{TInput, TOutput}"/> class allows for the execution of a series of
/// states, starting from a specified entry state and concluding at a result state.</remarks>
/// <typeparam name="TInput">The type of input that the state machine processes.</typeparam>
/// <typeparam name="TOutput">The type of output that the state machine produces.</typeparam>
public class Orchestration<TInput, TOutput> : Orchestration
{
    /// <summary>
    /// Executes the asynchronous operation, initializing with the specified input and running to completion.
    /// </summary>
    /// <param name="input">The input parameter used to initialize the operation. Can be <see langword="null"/> or the default value of
    /// <typeparamref name="TInput"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(TInput? input = default)
    {
        await Initialize(input);
        await RunToCompletion();
    }

    /// <summary>
    /// Result of the state machine run, containing a Array of outputs of type <typeparamref name="TOutput"/>.
    /// </summary>
    public TOutput[]? Results => RunnableWithResult.GetBaseResults().ToList().Select(result=> (TOutput)result).ToArray();

    /// <summary>
    /// Retrieves the results as a list of objects.
    /// </summary>
    /// <remarks>This method converts the items in the <c>Results</c> collection to a list of objects. If
    /// <c>Results</c> is <see langword="null"/>, the method returns <see langword="null"/>.</remarks>
    /// <returns>A list of objects converted from the <c>Results</c> collection, or <see langword="null"/> if <c>Results</c> is
    /// <see langword="null"/>.</returns>
    public override List<object>? GetResults()
    {
        return RunnableWithResult.GetBaseResults().ToList()!;
    }

    /// <summary>
    /// Gets or sets the result state of the operation.
    /// </summary>
    public OrchestrationRunnableBase RunnableWithResult { get; private set; }

    public Orchestration() { }

    /// <summary>
    /// Executes the state machine starting from the specified start state and processes the given input.
    /// </summary>
    /// <param name="input">The input data to be processed by the state machine.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results produced by the state machine.
    /// Each result corresponds to an output from the state machine, and may be null if no output is produced for a
    /// particular state transition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the start state or result state is not set before execution.</exception>
    private async Task Initialize(TInput? input = default)
    {
        //Input validation before running the state machine
        if (InitialRunnable == null)
        {
            throw new InvalidOperationException("Need to Set a Start Runtime for the Resulting RuntimeMachine");
        }

        if (RunnableWithResult == null)
        {
            throw new InvalidOperationException("Need to Set a Result Runtime for the Resulting RuntimeMachine");
        }

        await base.Initialize(input);
    }


    /// <summary>
    /// Sets the initial state for the entry point of the state machine.
    /// </summary>
    /// <param name="initialRunnable">The initial state to be set. Must have an input type assignable to <typeparamref name="TInput"/>.</param>
    /// <exception cref="InvalidCastException">Thrown if the input type of <paramref name="initialRunnable"/> is not assignable to <typeparamref
    /// name="TInput"/>.</exception>
    public override void SetEntryRunnable(OrchestrationRunnableBase initialRunnable)
    {
        if (!typeof(TInput).IsAssignableFrom(initialRunnable.GetInputType()))
        {
            throw new InvalidCastException($"Entry Runtime {initialRunnable.ToString()} with Input type of {initialRunnable.GetInputType()} Requires Input Type of {typeof(TInput)}");
        }

        InitialRunnable = initialRunnable;
    }

    /// <summary>
    /// Sets the output state for the current state machine.
    /// </summary>
    /// <remarks>This method updates the current output state, ensuring type compatibility with the
    /// expected output type.</remarks>
    /// <param name="finalRunnable">The state to be set as the output. Must have an output type assignable to <typeparamref name="TOutput"/>.</param>
    /// <exception cref="InvalidCastException">Thrown if the output type of <paramref name="finalRunnable"/> is not assignable to <typeparamref
    /// name="TOutput"/>.</exception>
    public void SetRunnableWithResult(OrchestrationRunnableBase finalRunnable)
    {
        if (!typeof(TOutput).IsAssignableFrom(finalRunnable.GetOutputType()))
        {
            throw new InvalidCastException($"Output Runtime {finalRunnable.ToString()} with Output type of {finalRunnable.GetOutputType()} Requires Cast of Output Type to {typeof(TOutput)}");
        }

        RunnableWithResult = finalRunnable;
    }

    public override bool TryGetResults<T>(out List<T> value)
    {
        if(typeof(T) == typeof(TOutput))
        {
            if (BaseFinalResult is List<T> result)
            {
                value = result;
                return true;
            }
            else
            {
                value = default!;
                return false;
            }
        }

        value = default!;
        return false;
    }
}

