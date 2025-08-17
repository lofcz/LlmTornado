using System.Collections.Concurrent;

namespace LlmTornado.Agents.Orchestration.Core;

public abstract class OrchestrationRunnable<TInput, TOutput> : OrchestrationRunnableBase
{
    private List<RunnableResult<TOutput>> _outputResults = new List<RunnableResult<TOutput>>();
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

    public List<RunnableResult<TOutput>> OutputResults
    {
        get => _outputResults;
        set
        {
            _outputResults = value ?? new List<RunnableResult<TOutput>>();
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


    public List<OrchestrationAdvancer<TOutput>> Advances { get; set; } = new List<OrchestrationAdvancer<TOutput>>();


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

    public virtual ValueTask InitializeRunnable(TInput? input) { return Threading.ValueTaskCompleted; }

    public virtual ValueTask CleanupRunnable() { return Threading.ValueTaskCompleted; }

    private async ValueTask<List<RunnableResult<TOutput>>> InvokeCore()
    {
        if (InputProcesses.Count == 0)
            throw new InvalidOperationException($"Input Process is required on Runnable {GetType()}");

        //Setup Invoke Task
        List<Task> Tasks = new List<Task>();
        ConcurrentBag<RunnableResult<TOutput>> oResults = new ConcurrentBag<RunnableResult<TOutput>>();

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

    internal override async ValueTask _Invoke()
    {
        await InvokeCore();
    }

    private async ValueTask<RunnableResult<TOutput>> InternalInvoke(RunnableProcess<TInput> input)
    {
        //OnRuntimeInvoked?.Invoke(input);
        return new RunnableResult<TOutput>(input.Id, await Invoke(input.Input));
    }

    public abstract ValueTask<TOutput> Invoke(TInput input);

    private OrchestrationAdvancer? GetFirstValidAdvancement(TOutput output)
    {
        return Advances?.DefaultIfEmpty(null)?.FirstOrDefault(transition => transition?.CanAdvance(output) ?? false) ?? null;
    }

    private List<RunnableProcess>? GetFirstValidAdvancementForEachResult()
    {
        LatestAdvancements.Clear();
        //Results Gathered from invoking
        OutputResults.ForEach(result =>
        {
            //Transitions are selected in order they are added
            OrchestrationAdvancer? advancement = GetFirstValidAdvancement(result.Result);

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
                if (!IsDeadEnd)
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

    public void AddAdvancer(OrchestrationRunnableBase nextRunable)
    {
        Advances.Add(new OrchestrationAdvancer<TOutput>(_ => true, nextRunable));
    }


    public void AddAdvancer(AdvancementRequirement<TOutput> methodToInvoke, OrchestrationRunnableBase nextRuntime)
    {
        Advances.Add(new OrchestrationAdvancer<TOutput>(methodToInvoke, nextRuntime));
    }


    public void AddAdvancer<T>(AdvancementRequirement<TOutput> methodToInvoke, AdvancementResultConverter<TOutput, T> conversionMethod, OrchestrationRunnableBase nextRuntime)
    {
        OrchestrationAdvancer<TOutput, T> transition = new OrchestrationAdvancer<TOutput, T>(methodToInvoke, conversionMethod, nextRuntime);
        Advances.Add(transition);
    }


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