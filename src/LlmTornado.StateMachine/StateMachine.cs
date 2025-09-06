using System.Collections.Concurrent;

namespace LlmTornado.StateMachines;

/// <summary>
/// Represents a state machine that manages the execution of state processes with support for concurrency and
/// cancellation. [SUGGEST USING StateMachine&lt;TInput, TOutput&gt;]
/// </summary>
/// <remarks>The <see cref="StateMachine"/> class provides mechanisms to initialize, run, and manage state
/// processes. It supports concurrent execution of processes up to a specified maximum number of threads, and allows
/// for graceful stopping and cancellation of operations. The state machine can be reset and reused for multiple
/// runs.</remarks>
public class StateMachine
{
    /// <summary>
    /// Triggers events related to the state machine, such as state transitions and errors.
    /// </summary>
    public Action<StateMachineEvent>? OnStateMachineEvent { get; set; } 

    /// <summary>
    /// List of processes that will be run in the state machine this tick.
    /// </summary>
    public List<StateProcess> ActiveProcesses { get; private set; } = new List<StateProcess>();

    private List<StateProcess> newStateProcesses = new List<StateProcess>();
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
    public List<BaseState> States { get; set; } = new List<BaseState>();

    /// <summary>
    /// Gets or sets a value indicating whether the steps of the process should be recorded.
    /// </summary>
    public bool RecordSteps { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection of steps, where each step is represented as a list of state processes.
    /// </summary>
    public List<List<StateProcess>> Steps { get; set; } = new List<List<StateProcess>>();


    public StateMachine()
    {

    }

    /// <summary>
    /// Marks the current operation as finished.
    /// </summary>
    /// <remarks>Sets the <see cref="IsFinished"/> property to <see langword="true"/>, indicating that
    /// the operation is complete.</remarks>
    public void Finish() { _isFinished = true; }

    /// <summary>
    /// Used to stop the state machine and cancel any ongoing operations.
    /// </summary>
    public void Stop() => StopTrigger.Cancel();

    /// <summary>
    /// Asynchronously exits all active processes.
    /// </summary>
    /// <remarks>This method initiates the exit sequence for each active process and waits for all
    /// processes to complete their exit operations.  It runs the exit operations concurrently to improve
    /// performance.</remarks>
    /// <returns></returns>
    private async Task ExitActiveProcesses()
    {
        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("Exiting all processes...")); //Invoke the exit state machine event

        List<Task> Tasks = new List<Task>();

        //Exit all processes
        ActiveProcesses.ForEach(process => {
            Tasks.Add(Task.Run(async () => await BeginExit(process)));
        });

        await Task.WhenAll(Tasks);
        Tasks.Clear();

        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("Finished Exiting all processes!"));
    }

    private async Task BeginExit(StateProcess process)
    {
        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("Exiting State..."));
        await process.State._ExitState();
        OnStateMachineEvent?.Invoke(new OnStateExitedEvent(process.State)); //Invoke the exit state machine event
        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("StateExited."));
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
        if (ActiveProcesses.Count == 0) { _isFinished = true; return; }

        OnStateMachineEvent?.Invoke(new OnTickStateMachineEvent()); //Invoke the tick state machine event
        List<Task> Tasks = new List<Task>();
        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent($"Processing {ActiveProcesses.Count} active processes..."));

        ActiveProcesses.ForEach(process => Tasks.Add(Task.Run(async () =>
        {
            OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent($"Invoking state: {process.State.GetType().Name}"));
            OnStateMachineEvent?.Invoke(new OnStateEnteredEvent(process)); //Invoke the state entered event
            await process.State._Invoke(); //Invoke the state process

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
    /// <exception cref="ArgumentNullException">Thrown when process or process.State is null.</exception>
    private async Task InitilizeProcess(StateProcess process)
    {
        if (process?.State == null)
            throw new ArgumentNullException(nameof(process), "Process and its State cannot be null");

        //Gain access to state machine
        process.State.CurrentStateMachine ??= this; //Set the current state machine if not already set
        ActiveProcesses.Add(process);

        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent($"Entering state: {process.State.GetType().Name}"));
        OnStateMachineEvent?.Invoke(new OnStateEnteredEvent(process));
        //Internal lock on access to state
        await process.State._EnterState(process); //preset input
    }

    /// <summary>
    /// Initializes all new state processes asynchronously.
    /// </summary>
    /// <remarks>This method clears the current active processes and initializes each new state
    /// process concurrently. After initialization, it ensures that only distinct state processes, based on their
    /// state ID, remain active.</remarks>
    /// <param name="newStateProcesses">A list of <see cref="StateProcess"/> objects representing the new state processes to be initialized.</param>
    /// <returns></returns>
    private async Task InitilizeAllNewProcesses()
    {
        //Clear all of the active processes
        ActiveProcesses.Clear();
        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent($"Initializing {newStateProcesses.Count} new state processes..."));
        //If there are no new processes, return
        if (newStateProcesses.Count == 0) { _isFinished = true; return; }

        //Record Step
        if (RecordSteps) { Steps.Add(newStateProcesses); }


        //Initialize each new state process concurrently
        List<Task> Tasks = new List<Task>();
        foreach (StateProcess stateProcess in newStateProcesses)
        {
            Tasks.Add(Task.Run(async () => await InitilizeProcess(stateProcess)));
        }
        await Task.WhenAll(Tasks);
        Tasks.Clear();

        //This is to remove running the same state twice with two processes.. it gets input from _EnterState
        ActiveProcesses = ActiveProcesses.DistinctBy(state => state.State.Id).ToList();

        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent($"Initialization Complete."));
    }

    /// <summary>
    /// Retrieves a list of new state processes based on the current conditions.
    /// </summary>
    /// <remarks>This method evaluates each active process and collects new state processes that meet
    /// specific conditions. The returned list may be empty if no new state processes are identified.</remarks>
    /// <returns>A list of <see cref="StateProcess"/> objects representing the new state processes that meet the specified
    /// conditions. The list will be empty if no new processes are found.</returns>
    private void GetNewProcesses()
    {
        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("Validating State conditions for transitions"));

        newStateProcesses.Clear();
        ActiveProcesses.ForEach(process => {
            newStateProcesses.AddRange(process.State.CheckConditions() ?? new List<StateProcess>());
        });

        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("Finished Validations"));
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
            OnStateMachineEvent?.Invoke(new OnFinishedStateMachineEvent()); //Invoke the finished state machine event
            OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("State Machine Finished."));
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
            OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("State Machine Cancelled."));
            OnStateMachineEvent?.Invoke(new OnCancelledStateMachineEvent()); //Invoke the cancelled state machine event
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
        OnStateMachineEvent?.Invoke(new OnVerboseStateMachineEvent("Resetting StateMachine"));
        _isFinished = false;
        StopTrigger = new CancellationTokenSource(); //Reset the stop trigger
        ActiveProcesses.Clear();
        newStateProcesses.Clear();
    }

    /// <summary>
    /// Executes the process using the specified initial state and input, and returns the index and resulting
    /// output. This method is an overload that allows for specifying an index and a resulting state for recompiling results from Input Array.
    /// </summary>
    /// <param name="runStartState">The initial state from which the process begins execution.</param>
    /// <param name="input">The input object used during the execution of the process.</param>
    /// <param name="index">An integer representing the index associated with the current execution context.</param>
    /// <param name="ResultingState">The state object that will hold the output after execution completes.</param>
    /// <returns>A tuple containing the index and a list of objects representing the output from the resulting state.</returns>
    internal async Task<(int, List<object>)> Run(BaseState runStartState, object input, int index, BaseState ResultingState)
    {
        await Run(runStartState, input);
        return (index, ResultingState.BaseOutput);
    }

    private async Task InitializeStateMachine(BaseState runStartState, object? input = null)
    {
        if (runStartState == null)
            throw new ArgumentNullException(nameof(runStartState), "Start state cannot be null");

        OnStateMachineEvent?.Invoke(new OnBeginStateMachineEvent()); //Invoke the begin state machine event

        ResetRun(); //Reset the state machine before running

        newStateProcesses.Add(new StateProcess(runStartState, input));

        //Initialize the process with the starting state and input
        await InitilizeAllNewProcesses();
    }

    /// <summary>
    /// Executes the state machine starting from the specified initial state, optionally using the provided input.
    /// </summary>
    /// <remarks>The method initializes the state machine and processes each state until a stop
    /// condition is met.  It handles state transitions and ensures that all processes are properly initialized and
    /// exited.</remarks>
    /// <param name="runStartState">The initial state from which the state machine execution begins. This parameter cannot be null.</param>
    /// <param name="input">An optional input object that can be used by the state machine during execution. This parameter can be null.</param>
    /// <returns>A task that represents the asynchronous operation of running the state machine.</returns>
    /// <exception cref="ArgumentNullException">Thrown when runStartState is null.</exception>
    public async Task Run(BaseState runStartState, object? input = null)
    {
        await InitializeStateMachine(runStartState);
        
        while (true)         //Run the state machine until it is finished or cancelled
        {
            //Collect each state Result
            await ProcessTick();

            //stop the state machine if needed & exit all states
            if (await CheckIfFinished()) break;
            if (await CheckIfCancelled()) break;

            //Create List of transitions to new states from conditional movement
            GetNewProcesses();

            //Exit the current Processes
            await ExitActiveProcesses();

            //Reset Active Processes Here
            await InitilizeAllNewProcesses();
        }
    }
}

/// <summary>
/// Represents a state machine that processes inputs of type <typeparamref name="TInput"/> and produces outputs of
/// type <typeparamref name="TOutput"/>.
/// </summary>
/// <remarks>The <see cref="StateMachine{TInput, TOutput}"/> class allows for the execution of a series of
/// states, starting from a specified entry state and concluding at a result state.</remarks>
/// <typeparam name="TInput">The type of input that the state machine processes.</typeparam>
/// <typeparam name="TOutput">The type of output that the state machine produces.</typeparam>
public class StateMachine<TInput, TOutput> : StateMachine
{
    /// <summary>
    /// Provides a mechanism for comparing two <see cref="RunOutputCollection{TOutput}"/> objects based on their
    /// index values.
    /// </summary>
    /// <remarks>This comparer is used to sort or order <see cref="RunOutputCollection{TOutput}"/>
    /// instances by their index. If either collection has an index of zero, the collections are considered
    /// equal.</remarks>
    class IndexSorter : IComparer<RunOutputCollection<TOutput?>>
    {
        public int Compare(RunOutputCollection<TOutput?>? x, RunOutputCollection<TOutput?>? y)
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
    public List<TOutput>? Results => ResultState.BaseOutput.ConvertAll(item => (TOutput)item)!;

    /// <summary>
    /// Gets or sets the initial state of the system or process.
    /// </summary>
    public BaseState StartState { get; private set; }
    /// <summary>
    /// Gets or sets the result state of the operation.
    /// </summary>
    public BaseState ResultState { get; private set; }

    public StateMachine() { }

    /// <summary>
    /// Executes the state machine starting from the specified start state and processes the given input.
    /// </summary>
    /// <param name="input">The input data to be processed by the state machine.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results produced by the state machine.
    /// Each result corresponds to an output from the state machine, and may be null if no output is produced for a
    /// particular state transition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the start state or result state is not set before execution.</exception>
    public async Task<List<TOutput?>> Run(TInput input)
    {
        //Input validation before running the state machine
        if (StartState == null)
        {
            throw new InvalidOperationException("Need to Set a Start State for the Resulting StateMachine");
        }

        if (ResultState == null)
        {
            throw new InvalidOperationException("Need to Set a Result State for the Resulting StateMachine");
        }

        await base.Run(StartState, input);

        return Results;
    }

    /// <summary>
    /// Executes the state machine for each input and returns the results.
    /// </summary>
    /// <remarks>This method processes each input asynchronously and collects the results. The results
    /// are sorted by their processing order before being returned.</remarks>
    /// <param name="inputs">An array of inputs to process through the state machine.</param>
    /// <returns>A list of lists containing the output results for each input. Each inner list corresponds to the results of
    /// processing a single input.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="StartState"/> or <see cref="ResultState"/> is not set before calling this method.</exception>
    public async Task<List<List<TOutput?>>> Run(TInput[] inputs)
    {
        //Input validation before running the state machine
        if (StartState == null)
        {
            throw new InvalidOperationException("Need to Set a Start State for the Resulting StateMachine");
        }

        if (ResultState == null)
        {
            throw new InvalidOperationException("Need to Set a Result State for the Resulting StateMachine");
        }

        // Use a ConcurrentBag to collect results from multiple tasks
        ConcurrentBag<RunOutputCollection<TOutput?>> oResults = new ConcurrentBag<RunOutputCollection<TOutput?>>();

        for (int i = 0; i < inputs.Length; i++)
        {
            (int, List<object>) runResult = await Run(StartState, inputs[i], i + 1, ResultState);
            oResults.Add(new RunOutputCollection<TOutput?>(runResult.Item1, runResult.Item2.ConvertAll(item => (TOutput)item)!));
        }

        // Convert the ConcurrentBag to a List and sort it by index
        List<RunOutputCollection<TOutput?>> outResults = oResults!.ToList();

        outResults.Sort(new IndexSorter());

        return outResults.Select(item => item.Results).ToList();
    }

    /// <summary>
    /// Sets the initial state for the entry point of the state machine.
    /// </summary>
    /// <param name="startState">The initial state to be set. Must have an input type assignable to <typeparamref name="TInput"/>.</param>
    /// <exception cref="InvalidCastException">Thrown if the input type of <paramref name="startState"/> is not assignable to <typeparamref
    /// name="TInput"/>.</exception>
    public void SetEntryState(BaseState startState)
    {
        if (!startState.GetInputType().IsAssignableTo(typeof(TInput)))
        {
            throw new InvalidCastException($"Entry State {startState.ToString()} with Input type of {startState.GetInputType()} Requires Input Type of {typeof(TInput)}");
        }

        StartState = startState;
    }

    /// <summary>
    /// Sets the output state for the current state machine.
    /// </summary>
    /// <remarks>This method updates the current output state, ensuring type compatibility with the
    /// expected output type.</remarks>
    /// <param name="resultState">The state to be set as the output. Must have an output type assignable to <typeparamref name="TOutput"/>.</param>
    /// <exception cref="InvalidCastException">Thrown if the output type of <paramref name="resultState"/> is not assignable to <typeparamref
    /// name="TOutput"/>.</exception>
    public void SetOutputState(BaseState resultState)
    {
        if (!resultState.GetOutputType().IsAssignableTo(typeof(TOutput)))
        {
            throw new InvalidCastException($"Output State {resultState.ToString()} with Output type of {resultState.GetOutputType()} Requires Cast of Output Type to {typeof(TOutput)}");
        }

        ResultState = resultState;
    }

}

/// <summary>
/// Represents a collection of output results from a run, along with an index indicating the position of the input array.
/// </summary>
/// <remarks>This class is used to store and manage the results of a run operation, providing both the
/// results and the index of the run. It can be used to track multiple runs and their respective outputs.</remarks>
/// <typeparam name="TOutput">The type of the output results contained in the collection.</typeparam>
public class RunOutputCollection<TOutput>
{
    public int Index { get; set; } = 0;
    public List<TOutput> Results { get; set; }

    public RunOutputCollection() { }

    public RunOutputCollection(int index, List<TOutput> results)
    {
        Index = index;
        Results = results;
    }
}