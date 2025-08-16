using System.Collections.Concurrent;

namespace LlmTornado.Agents.Runtime;

internal abstract class Runner<TInput, TOutput> : BaseRunner
{
    private List<RuntimeResult<TOutput>> _outputResults = new List<RuntimeResult<TOutput>>();
    public override Type GetInputType() => typeof(TInput);
    public override Type GetOutputType() => typeof(TOutput);

    public List<TOutput> Output => OutputResults.Select(output => output.Result).ToList();

    public List<TInput> Input => InputProcesses.Select(process => process.Input).ToList();

    private List<RunnableProcess<TInput>> _inputProcesses = new List<RunnableProcess<TInput>>();

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

    public List<RuntimeResult<TOutput>> OutputResults
    {
        get => _outputResults;
        set
        {
            _outputResults = value ?? new List<RuntimeResult<TOutput>>();
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


    public List<RuntimeAdvancer<TOutput>> Advances { get; set; } = new List<RuntimeAdvancer<TOutput>>();


    public List<RunnableProcess> LastAdvances { get; set; } = new List<RunnableProcess>();

    internal override async Task _InitializeRunnable(RunnableProcess? input)
    {
        AddInputProcess(input);
        await InitializeRunnable((TInput)input!.BaseInput!);
    }


    internal override async Task _CleanupRunnable()
    {
        InputProcesses.Clear();
        await CleanupRunnable();
    }

    public virtual async Task InitializeRunnable(TInput? input) { }


    public virtual async Task CleanupRunnable() { }

    private async Task<List<RuntimeResult<TOutput>>> InvokeCore()
    {
        if (InputProcesses.Count == 0)
            throw new InvalidOperationException($"Input Process is required on Runnable {GetType()}");

        //Setup Invoke Task
        List<Task> Tasks = new List<Task>();
        ConcurrentBag<RuntimeResult<TOutput>> oResults = new ConcurrentBag<RuntimeResult<TOutput>>();

        if (CombineInput)
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

    internal override async Task _Invoke()
    {
        await InvokeCore();
    }


    private async Task<RuntimeResult<TOutput>> InternalInvoke(RunnableProcess<TInput> input)
    {
        //OnRuntimeInvoked?.Invoke(input);
        return new RuntimeResult<TOutput>(input.Id, await Invoke(input.Input));
    }


    public abstract Task<TOutput> Invoke(TInput input);

    private RuntimeAdvancer? GetFirstValidAdvancement(TOutput output)
    {
        return Advances?.DefaultIfEmpty(null)?.FirstOrDefault(transition => transition?.CanAdvance(output) ?? false) ?? null;
    }

    private List<RunnableProcess>? GetFirstValidAdvancementForEachResult()
    {
        LastAdvances.Clear();
        //Results Gathered from invoking
        OutputResults.ForEach(result =>
        {
            //Transitions are selected in order they are added
            RuntimeAdvancer? route = GetFirstValidAdvancement(result.Result);

            //If not transition is found, we can reattempt the process
            if (route != null)
            {
                //Check if transition is conversion type or use the output.Result directly
                object? ilResult = route.type == "in_out" ? route.ConverterMethodResult : result.Result;

                LastAdvances.Add(new RunnableProcess(route.ProceedingRunner, ilResult!));
            }
            else
            {
                //If the state is a dead end, we do not reattempt the process
                if (!IsDeadEnd)
                {
                    //ReRun the process that failed
                    RunnableProcess<TInput> failedProcess = InputProcesses.First(process => process.Id == result.ProcessId);
                    //Cap the amount of times a Runtime can reattempt (Fixed at 3 right now)
                    if (failedProcess.CanReAttempt())
                    {
                        LastAdvances.Add(failedProcess);
                    }
                }
            }
        });

        return LastAdvances;
    }

    private List<RunnableProcess>? GetAllValidAdvancements()
    {
        LastAdvances.Clear();
        //Results Gathered from invoking
        OutputResults.ForEach((output) =>
        {
            LastAdvances = ProcessRunnerRoutes(output, LastAdvances);
        });

        return LastAdvances;
    }


    private List<RunnableProcess> ProcessRunnerRoutes(RunnerResult stateResult, List<RunnableProcess> stateProcessesFromOutput) {         //Check if the state result has a valid transition
                                                                                                                                                  //If the transition evaluates to true for the output, add it to the new state processes
        Advances.ForEach(advancer =>
        {
            TOutput output = (TOutput)stateResult.ResultObject;
            if (advancer.CanAdvance(output))
            {
                //Check if transition is conversion type or use the output.Result directly
                object? result = advancer.type == "in_out" ? advancer.ConverterMethodResult : output;

                stateProcessesFromOutput.Add(new RunnableProcess(advancer.ProceedingRunner, result!));
            }
        });

        //If process produces no transitions and not at a dead end rerun the process
        if (stateProcessesFromOutput.Count == 0 && !IsDeadEnd)
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

    public void AddAdvancer(BaseRunner nextRunable)
    {
        Advances.Add(new RuntimeAdvancer<TOutput>(_ => true, nextRunable));
    }


    public void AddAdvancer(ProceedRequirement<TOutput> methodToInvoke, BaseRunner nextRuntime)
    {
        Advances.Add(new RuntimeAdvancer<TOutput>(methodToInvoke, nextRuntime));
    }


    public void AddAdvancer<T>(ProceedRequirement<TOutput> methodToInvoke, ProceedConversion<TOutput, T> conversionMethod, BaseRunner nextRuntime)
    {
        RuntimeAdvancer<TOutput, T> transition = new RuntimeAdvancer<TOutput, T>(methodToInvoke, conversionMethod, nextRuntime);
        Advances.Add(transition);
    }


    public void AddAdvancer<T>(ProceedConversion<TOutput, T> conversionMethod, BaseRunner nextRunnable, ProceedRequirement<TOutput>? methodToInvoke = null)
    {
        if (methodToInvoke != null)
        {
            RuntimeAdvancer<TOutput, T> transition = new RuntimeAdvancer<TOutput, T>(methodToInvoke, conversionMethod, nextRunnable);
            Advances.Add(transition);
        }
        else
        {
            RuntimeAdvancer<TOutput, T> transition = new RuntimeAdvancer<TOutput, T>(_ => true, conversionMethod, nextRunnable);
            Advances.Add(transition);
        }
    }

}