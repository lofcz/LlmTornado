using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;

public abstract class OrchestrationRunnable<TInput, TOutput> : OrchestrationRunnableBase
{
    #region Properties
    /// <summary>
    /// Output results from the invocation of the runnable.
    /// </summary>
    public List<TOutput> Output => Processes.Select(process => process.Result).ToList();

    /// <summary>
    /// Input values for the runnable processes.
    /// </summary>
    public List<TInput> Input => Processes.Select(process => process.Input).ToList();

    /// <summary>
    /// Input processes to be executed by the runnable.
    /// </summary>
    public List<RunnableProcess<TInput, TOutput>> Processes
    {
        get => GetBaseRunnableProcesses<TInput, TOutput>();
    }

    public void AddProcess(RunnableProcess<TInput, TOutput> process)
    {
        AddBaseRunnableProcess(process);
    }

    /// <summary>
    /// List of advancements (transitions) from this runnable to the next runnables.
    /// </summary>
    public OrchestrationAdvancer<TOutput>[] Advances => GetAdvances();

    public OrchestrationAdvancer<TOutput>[] GetAdvances()
    {
        return BaseAdvancers.Select(advancer => {
            AdvancementRequirement<TOutput> advancementRequirement = (TOutput input) => advancer.InvokeMethod(input);
            return new OrchestrationAdvancer<TOutput>(advancementRequirement, advancer.NextRunnable);
        }).ToArray();
    }

    public void AddAdvancer(OrchestrationAdvancer<TOutput> advancer)
    {
        AdvancementRequirement<object> advancementRequirement = (object input) => advancer.InvokeMethod((TOutput)input);
        BaseAdvancers.Add(new OrchestrationAdvancer<object>(advancementRequirement, advancer.NextRunnable));
    }

    /// <summary>
    /// Latest advancements determined after invocation.
    /// </summary>
    public List<RunnableProcess> LatestAdvancements { get; set; } = new List<RunnableProcess>();

    public override Type GetInputType() => typeof(TInput);
    public override Type GetOutputType() => typeof(TOutput);
    #endregion


    public OrchestrationRunnable(Orchestration orchestrator, string runnableName = "") 
    {
        RunnableName = string.IsNullOrWhiteSpace(runnableName) ? this.GetType().Name : runnableName;
        Orchestrator = orchestrator;
        Orchestrator?.Runnables.Add(RunnableName, this);
    }

    #region Abstract Class Overrides
    /// <summary>
    /// Starting the state
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    internal override async ValueTask _InitializeRunnable(RunnableProcess? process)
    {
        Processes.Clear();
        RunnableProcess<TInput, TOutput> newProcess = process.ReturnProcess<TInput, TOutput>();
        AddProcess(newProcess);
        await InitializeRunnable(newProcess);
    }

    /// <summary>
    /// Main Invoke to start the execution
    /// </summary>
    /// <returns></returns>
    internal override async ValueTask Invoke()
    {
        await InvokeCore();
    }

    internal override async ValueTask _CleanupRunnable()
    {
        await CleanupRunnable();
    }

    #endregion

    private async ValueTask InvokeCore()
    {
        if (Processes.Count == 0)
            throw new InvalidOperationException($"Process is required on Runnable {GetType()}");

        if (SingleInvokeForProcesses)
        {
            //Invoke Should handle the Input as a whole (Single Thread can handle processing all the inputs)
            await InternalInvoke(Processes[0]);
        }
        else
        {
            if (IsThreadSafe)
            {
                List<Task> Tasks = new List<Task>();
                Processes.ForEach(process => Tasks.Add(Task.Run(async () => await InternalInvoke(process))));
                await Task.WhenAll(Tasks);
                Tasks.Clear();
            }
            else
            {
                foreach (var process in Processes)
                {
                    await InternalInvoke(process);
                }
            }
        }
    }

    private async ValueTask<RunnableProcess<TInput, TOutput>> InternalInvoke(RunnableProcess<TInput, TOutput> input)
    {
        Orchestrator?.OnStartingRunnableProcess(input);
        input.SetupProcess();
        input.Result = await Invoke(input);
        input.FinalizeProcess();
        UpdateBaseRunnableProcess(input.Id, input);
        Orchestrator?.OnFinishedRunnableProcess(input);
        List<RunnableProcess>? advancements = CheckResultForAdvancementsRecords(input);
        List<AdvancementRecord> advancementRecords = new List<AdvancementRecord>();
        foreach (var advancement in advancements)
        {
            advancementRecords.Add(new AdvancementRecord(input.Runner.RunnableName, advancement.Runner.RunnableName, input.Result!));
        }
        Orchestrator?.AddRecordStep(new RunnerRecord(input.Id, RunnableName, input.TokenUsage, input.StartTime.Value, input.RunnableExecutionTime, input: input.Input, transitions: advancementRecords.ToArray()));
        return input;
    }

    #region Runnable User Configurables
    /// <summary>
    /// Setup any resources needed for the runnable to operate.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public virtual ValueTask InitializeRunnable(RunnableProcess<TInput, TOutput> process) { return Threading.ValueTaskCompleted; }

    /// <summary>
    /// Processes the specified input and returns the corresponding output asynchronously.
    /// </summary>
    /// <param name="input">The input data to be processed. Cannot be null.</param>
    /// <returns>A <see cref="ValueTask{TOutput}"/> representing the asynchronous operation.  The result contains the processed
    /// output of type <typeparamref name="TOutput"/>.</returns>
    public abstract ValueTask<TOutput> Invoke(RunnableProcess<TInput, TOutput> input);

    /// <summary>
    /// Cleanup any resources used by the runnable.
    /// </summary>
    /// <returns></returns>
    public virtual ValueTask CleanupRunnable() { return Threading.ValueTaskCompleted; }

    #endregion

    #region Advancement Logic
    private List<RunnableProcess>? GetFirstValidAdvancementForEachResult()
    {
        LatestAdvancements.Clear();
        //Results Gathered from invoking
        Processes.ForEach(process =>
        {
            //Transitions are selected in order they are added
            OrchestrationAdvancer? advancement = Advances?.FirstOrDefault(transition => transition?.CanAdvance(process.Result) ?? false) ?? null;

            //If not transition is found, we can reattempt the process
            if (advancement != null)
            {
                //Check if transition is conversion type or use the output.Result directly
                object? nextResult = advancement.type == "in_out" ? advancement.ConverterMethodResult : process.Result;

                LatestAdvancements.Add(new RunnableProcess(advancement.NextRunnable, nextResult!));
            }
            else
            {
                //If the state is a dead end, we do not reattempt the process
                if (!AllowDeadEnd)
                {
                    //ReRun the process that failed
                    //Cap the amount of times a Runtime can reattempt (Fixed at 3 right now)
                    if (process.CanReAttempt())
                    {
                        LatestAdvancements.Add(process);
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
        Processes.ForEach((process) =>
        {
            LatestAdvancements.AddRange(CheckResultForAdvancements(process));
        });

        return LatestAdvancements;
    }

    private List<RunnableProcess> CheckResultForAdvancementsRecords(RunnableProcess<TInput, TOutput> process)
    {         //Check if the state result has a valid transition
        List<RunnableProcess> stateProcessesFromOutput = new List<RunnableProcess>();                                                                                                                                         //If the transition evaluates to true for the output, add it to the new state processes
        Advances.ToList().ForEach(advancer =>
        {
            if (advancer.CanAdvance(process.Result))
            {
                //Check if transition is conversion type or use the output.Result directly
                object? nextResult = advancer.type == "in_out" ? advancer.ConverterMethodResult : process;

                stateProcessesFromOutput.Add(new RunnableProcess(advancer.NextRunnable, nextResult!));
            }
        });

        //If process produces no transitions and not at a dead end rerun the process
        if (stateProcessesFromOutput.Count == 0 && !AllowDeadEnd)
        {
            //rerun the process up to the max attempts
            if (process.CanReAttempt()) stateProcessesFromOutput.Add(process);
        }

        return stateProcessesFromOutput;
    }

    private List<RunnableProcess> CheckResultForAdvancements(RunnableProcess<TInput,TOutput> process) {         //Check if the state result has a valid transition
        List<RunnableProcess> stateProcessesFromOutput = new List<RunnableProcess>();                                                                                                                                         //If the transition evaluates to true for the output, add it to the new state processes
        Advances.ToList().ForEach(advancer =>
        {
            if (advancer.CanAdvance(process.Result))
            {
                //Check if transition is conversion type or use the output.Result directly
                object? nextResult = advancer.type == "in_out" ? advancer.ConverterMethodResult : process;

                stateProcessesFromOutput.Add(new RunnableProcess(advancer.NextRunnable, nextResult!));
            }
        });

        //If process produces no transitions and not at a dead end rerun the process
        if (stateProcessesFromOutput.Count == 0 && !AllowDeadEnd)
        {
            //rerun the process up to the max attempts
            if (process.CanReAttempt()) stateProcessesFromOutput.Add(process);
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
        AddAdvancer(new OrchestrationAdvancer<TOutput>(_ => true, nextRunnable));
    }

    /// <summary>
    /// Adds an advancer with a specified method to invoke for determining advancement to the next runnable.
    /// </summary>
    /// <param name="methodToInvoke">Condition required to make advancement</param>
    /// <param name="nextRunnable">Next Runnable to advance too</param>
    public void AddAdvancer(AdvancementRequirement<TOutput> methodToInvoke, OrchestrationRunnableBase nextRunnable)
    {
        AddAdvancer(new OrchestrationAdvancer<TOutput>(methodToInvoke, nextRunnable));
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
        AddAdvancer(transition);
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
            AddAdvancer(transition);
        }
        else
        {
            OrchestrationAdvancer<TOutput, T> transition = new OrchestrationAdvancer<TOutput, T>(_ => true, conversionMethod, nextRunnable);
            AddAdvancer(transition);
        }
    }
    #endregion
}