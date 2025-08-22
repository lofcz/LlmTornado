using LlmTornado.Chat;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace LlmTornado.Agents.Orchestration.Core;

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
    public List<List<RunnableProcess>> RunSteps { get; set; } = new List<List<RunnableProcess>>();

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
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Exiting Runtime..."));
        await process.Runner._CleanupRunnable();
        OnOrchestrationEvent?.Invoke(new OnFinishedRunnableEvent(process.Runner)); //Invoke the exit state machine event
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("RuntimeExited."));
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

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Processing {CurrentRunnableProcesses.Count} active processes..."));
        OnOrchestrationEvent?.Invoke(new OnTickOrchestrationEvent()); //Invoke the tick state machine event
        
        List<Task> Tasks = new List<Task>();
        CurrentRunnableProcesses.ForEach(process => Tasks.Add(Task.Run(async () =>
        {
            OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Invoking state: {process.Runner.GetType().Name}"));
            OnOrchestrationEvent?.Invoke(new OnStartedRunnableEvent(process)); //Invoke the state entered event
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
        OnOrchestrationEvent?.Invoke(new OnStartedRunnableEvent(process));
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
        CurrentRunnableProcesses.Clear();
        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent($"Initializing {newRunnableProcesses.Count} new state processes..."));

        //If there are no new processes, return
        if (newRunnableProcesses.Count == 0) { HasCompletedSuccessfully(); return; }

        //Record Step
        if (RecordSteps) { RunSteps.Add(newRunnableProcesses); }

        //Initialize each new state process concurrently
        List<Task> Tasks = new List<Task>();
        foreach (RunnableProcess stateProcess in newRunnableProcesses)
        {
            Tasks.Add(Task.Run(async () => await InitilizeProcess(stateProcess)));
        }
        await Task.WhenAll(Tasks);
        Tasks.Clear();

        //This is to remove running the same state twice with two processes.. it gets input from _EnterRuntime
        // Replace the DistinctBy line with this GroupBy approach which is compatible with .NET Standard 2.0
        CurrentRunnableProcesses = CurrentRunnableProcesses
            .GroupBy(state => state.Runner.Id)
            .Select(g => g.First())
            .ToList();

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

    public virtual async Task Initialize(OrchestrationRunnableBase initialRunner, object? input = null)
    {
        if (initialRunner == null)
            throw new ArgumentNullException(nameof(initialRunner), "Start state cannot be null");

        ResetRun(); //Reset the state machine before running

        newRunnableProcesses.Add(new RunnableProcess(initialRunner, input));

        //Initialize the process with the starting state and input
        await InitilizeAllNewProcesses();

        OnOrchestrationEvent?.Invoke(new OnVerboseOrchestrationEvent("Initilization Completed!")); //Invoke the begin state machine event
    }

    public async Task InvokeAsync(object? input = null)
    {
        await Initialize(InitialRunnable, input);
        await RunToCompletion();
    }

    private async Task RunToCompletion()
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

    public void AddRunnable(string name, OrchestrationRunnableBase runnable)
    {
        Runnables.Add(name, runnable);
    }

    public void RemoveRunnable(string name)
    {
        Runnables.Remove(name);
    }

    public void ClearRunnables()
    {
        Runnables.Clear();
    }

    public virtual List<object>? GetResults()
    {
        return this.BaseFinalResult;
    }

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
    public List<TOutput>? Results => RunnableWithResult.BaseOutput.ConvertAll(item => (TOutput)item)!;

    public override List<object>? GetResults()
    {
        return Results?.ConvertAll(item => (object?)item)!;
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
    public async Task Initialize( object? input = null)
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

        await base.Initialize(InitialRunnable, input);
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

