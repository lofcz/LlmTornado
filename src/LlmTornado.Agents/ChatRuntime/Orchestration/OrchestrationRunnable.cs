using System.Collections.Concurrent;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;

public abstract class OrchestrationRunnable<TInput, TOutput> : OrchestrationRunnableBase
{
    private List<RunnerResult<TOutput>> _outputResults = new List<RunnerResult<TOutput>>();
    public override Type GetInputType() => typeof(TInput);
    public override Type GetOutputType() => typeof(TOutput);

    /// <summary>
    /// Output results from the invocation of the runnable.
    /// </summary>
    public List<TOutput> Output => OutputResults.Select(output => output.Result).ToList();

    /// <summary>
    /// Input values for the runnable processes.
    /// </summary>
    public List<TInput> Input => InputProcesses.Select(process => process.Input).ToList();

    private List<RunnableProcess<TInput>> _inputProcesses = new List<RunnableProcess<TInput>>();

    /// <summary>
    /// Input processes to be executed by the runnable.
    /// </summary>
    public List<RunnableProcess<TInput>> InputProcesses
    {
        get => _inputProcesses;
        set
        {
            _inputProcesses = value ?? new List<RunnableProcess<TInput>>();
            BaseInputProcesses = ConvertInputProcesses();
        }
    }

    private List<RunnableProcess> ConvertInputProcesses()
    {
        List<RunnableProcess> inputProcs = new List<RunnableProcess>();
        foreach (RunnableProcess<TInput> process in InputProcesses)
        {
            inputProcs.Add(new RunnableProcess(process.Runner, (object)process.Input!));
        }
        return inputProcs;
    }

    /// <summary>
    /// Output results from the invocation of the runnable.
    /// </summary>
    public List<RunnerResult<TOutput>> OutputResults
    {
        get => _outputResults;
        set
        {
            _outputResults = value ?? new List<RunnerResult<TOutput>>();
            BaseOutputResults = ConvertOutputResults();
        }
    }


    private List<RunnerResult> ConvertOutputResults()
    {
        return _outputResults.Select(x => new RunnerResult(x.ProcessId, x.ResultObject)).ToList();
    }

    private void AddInputProcess(RunnableProcess process)
    {
        InputProcesses.Add(new RunnableProcess<TInput>(process.Runner, (TInput)process.BaseInput!, process.Id));
    }

    /// <summary>
    /// List of advancements (transitions) from this runnable to the next runnables.
    /// </summary>
    public List<OrchestrationAdvancer<TOutput>> Advances { get; set; } = new List<OrchestrationAdvancer<TOutput>>();

    /// <summary>
    /// Latest advancements determined after invocation.
    /// </summary>
    public List<RunnableProcess> LatestAdvancements { get; set; } = new List<RunnableProcess>();

    internal override async ValueTask _InitializeRunnable(RunnableProcess? input)
    {
        AddInputProcess(input);
        await InitializeRunnable((TInput)input!.BaseInput!);
    }


    internal override async ValueTask _CleanupRunnable()
    {
        InputProcesses.Clear();
        await CleanupRunnable();
    }

    /// <summary>
    /// Setup any resources needed for the runnable to operate.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public virtual ValueTask InitializeRunnable(TInput? input) { return Threading.ValueTaskCompleted; }

    /// <summary>
    /// Cleanup any resources used by the runnable.
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask CleanupRunnable() { return Threading.ValueTaskCompleted; }

    private async ValueTask<List<RunnerResult<TOutput>>> InvokeCore()
    {
        if (InputProcesses.Count == 0)
            throw new InvalidOperationException($"Input Process is required on Runnable {GetType()}");

        //Setup Invoke Task
        List<Task> Tasks = new List<Task>();
        ConcurrentBag<RunnerResult<TOutput>> oResults = new ConcurrentBag<RunnerResult<TOutput>>();

        if (SingleInvokeForInput)
        {
            //Invoke Should handle the Input as a whole (Single Thread can handle processing all the inputs)
            Tasks.Add(Task.Run(async () => oResults.Add(await InternalInvoke(InputProcesses[0]))));
        }
        else
        {
            //Default option to process each input in as its own item
            //(This process is resource bound by the single state instance)
            InputProcesses.ForEach(process => Tasks.Add(Task.Run(async () => oResults.Add(await InternalInvoke(process)))));
        }

        // Wait for collection
        await Task.WhenAll(Tasks);
        Tasks.Clear();

        OutputResults = oResults.ToList();

        return OutputResults;
    }

    internal override async ValueTask Invoke()
    {
        await InvokeCore();
    }

    private async ValueTask<RunnerResult<TOutput>> InternalInvoke(RunnableProcess<TInput> input)
    {
        return new RunnerResult<TOutput>(input.Id, await Invoke(input.Input));
    }

    /// <summary>
    /// Processes the specified input and returns the corresponding output asynchronously.
    /// </summary>
    /// <param name="input">The input data to be processed. Cannot be null.</param>
    /// <returns>A <see cref="ValueTask{TOutput}"/> representing the asynchronous operation.  The result contains the processed
    /// output of type <typeparamref name="TOutput"/>.</returns>
    public abstract ValueTask<TOutput> Invoke(TInput input);

    private List<RunnableProcess>? GetFirstValidAdvancementForEachResult()
    {
        LatestAdvancements.Clear();
        //Results Gathered from invoking
        OutputResults.ForEach(result =>
        {
            //Transitions are selected in order they are added
            OrchestrationAdvancer? advancement = Advances?.FirstOrDefault(transition => transition?.CanAdvance(result.Result) ?? false) ?? null;

            //If not transition is found, we can reattempt the process
            if (advancement != null)
            {
                //Check if transition is conversion type or use the output.Result directly
                object? ilResult = advancement.type == "in_out" ? advancement.ConverterMethodResult : result.Result;

                LatestAdvancements.Add(new RunnableProcess(advancement.NextRunnable, ilResult!));
            }
            else
            {
                //If the state is a dead end, we do not reattempt the process
                if (!AllowDeadEnd)
                {
                    //ReRun the process that failed
                    RunnableProcess<TInput> failedProcess = InputProcesses.First(process => process.Id == result.ProcessId);
                    //Cap the amount of times a Runtime can reattempt (Fixed at 3 right now)
                    if (failedProcess.CanReAttempt())
                    {
                        LatestAdvancements.Add(failedProcess);
                    }
                }
            }
        });

        return LatestAdvancements;
    }

    private List<RunnableProcess>? GetAllValidAdvancements()
    {
        LatestAdvancements.Clear();
        //Results Gathered from invoking
        OutputResults.ForEach((result) =>
        {
            LatestAdvancements.AddRange(CheckResultForAdvancements(result));
        });

        return LatestAdvancements;
    }


    private List<RunnableProcess> CheckResultForAdvancements(RunnerResult stateResult) {         //Check if the state result has a valid transition
        List<RunnableProcess> stateProcessesFromOutput = new List<RunnableProcess>();                                                                                                                                         //If the transition evaluates to true for the output, add it to the new state processes
        Advances.ForEach(advancer =>
        {
            TOutput output = (TOutput)stateResult.ResultObject;
            if (advancer.CanAdvance(output))
            {
                //Check if transition is conversion type or use the output.Result directly
                object? result = advancer.type == "in_out" ? advancer.ConverterMethodResult : output;

                stateProcessesFromOutput.Add(new RunnableProcess(advancer.NextRunnable, result!));
            }
        });

        //If process produces no transitions and not at a dead end rerun the process
        if (stateProcessesFromOutput.Count == 0 && !AllowDeadEnd)
        {
            RunnableProcess failedProcess = InputProcesses.First(process => process.Id == stateResult.ProcessId);
            //rerun the process up to the max attempts
            if (failedProcess.CanReAttempt()) stateProcessesFromOutput.Add(failedProcess);
        }

        return stateProcessesFromOutput;
    }

    internal override List<RunnableProcess>? CanAdvance()
    {
        return AllowsParallelAdvances ? GetAllValidAdvancements() : GetFirstValidAdvancementForEachResult();
    }

    /// <summary>
    /// Adds a simple advancer that always advances to the specified next runnable.
    /// </summary>
    /// <param name="nextRunnable"></param>
    public void AddAdvancer(OrchestrationRunnableBase nextRunnable)
    {
        Advances.Add(new OrchestrationAdvancer<TOutput>(_ => true, nextRunnable));
    }

    /// <summary>
    /// Adds an advancer with a specified method to invoke for determining advancement to the next runnable.
    /// </summary>
    /// <param name="methodToInvoke">Condition required to make advancement</param>
    /// <param name="nextRunnable">Next Runnable to advance too</param>
    public void AddAdvancer(AdvancementRequirement<TOutput> methodToInvoke, OrchestrationRunnableBase nextRunnable)
    {
        Advances.Add(new OrchestrationAdvancer<TOutput>(methodToInvoke, nextRunnable));
    }

    /// <summary>
    /// Adds an advancer with a specified method to invoke for determining advancement to the next runnable, along with a conversion method to convert the output type to the input type of the next runnable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="methodToInvoke">Condition required to make advancement</param>
    /// <param name="conversionMethod">Method to convert the output type to the input type of the next runnable</param>
    /// <param name="nextRunnable">Next Runnable to advance too</param>
    public void AddAdvancer<T>(AdvancementRequirement<TOutput> methodToInvoke, AdvancementResultConverter<TOutput, T> conversionMethod, OrchestrationRunnableBase nextRunnable)
    {
        OrchestrationAdvancer<TOutput, T> transition = new OrchestrationAdvancer<TOutput, T>(methodToInvoke, conversionMethod, nextRunnable);
        Advances.Add(transition);
    }

    /// <summary>
    /// Add an advancer that always advances to the specified next runnable, along with a conversion method to convert the output type to the input type of the next runnable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conversionMethod">Conversion method for the output result to the next runnable</param>
    /// <param name="nextRunnable"></param>
    /// <param name="methodToInvoke"> Optional Method to check for invocation</param>
    public void AddAdvancer<T>(AdvancementResultConverter<TOutput, T> conversionMethod, OrchestrationRunnableBase nextRunnable, AdvancementRequirement<TOutput>? methodToInvoke = null)
    {
        if (methodToInvoke != null)
        {
            OrchestrationAdvancer<TOutput, T> transition = new OrchestrationAdvancer<TOutput, T>(methodToInvoke, conversionMethod, nextRunnable);
            Advances.Add(transition);
        }
        else
        {
            OrchestrationAdvancer<TOutput, T> transition = new OrchestrationAdvancer<TOutput, T>(_ => true, conversionMethod, nextRunnable);
            Advances.Add(transition);
        }
    }

}