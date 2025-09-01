using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Utility;

namespace LlmTornado.Agents.ChatRuntime;

public class OrchestrationBuilder
{
    public OrchestrationRuntimeConfiguration Configuration { get; private set; }
    public OrchestrationBuilder()
    {
        Configuration = new OrchestrationRuntimeConfiguration();
    }
    public OrchestrationBuilder WithOnRuntimeEvent(Func<ChatRuntimeEvents, ValueTask> onRuntimeEvent)
    {
        Configuration.OnRuntimeEvent = onRuntimeEvent;
        return this;
    }

    public OrchestrationBuilder WithCancellationTokenSource(CancellationTokenSource cts)
    {
        Configuration.cts = cts;
        return this;
    }

    public OrchestrationBuilder SetEntryRunnable(OrchestrationRunnableBase entryRunnable)
    {
        Configuration.SetEntryRunnable(entryRunnable);
        return this;
    }

    public OrchestrationBuilder SetOutputRunnable(OrchestrationRunnableBase outputRunnable)
    {
        Configuration.SetRunnableWithResult(outputRunnable);
        return this;
    }
    
    public OrchestrationBuilder AddAdvancer<TOutput>(OrchestrationRunnableBase fromRunnable, OrchestrationRunnableBase toRunnable)
    {
        if(!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);

        fromRunnable.AddAdvancer(new OrchestrationAdvancer<TOutput>(_ => true, toRunnable));
        return this;
    }

    public OrchestrationBuilder AddDeadEndAdvancer<TOutput>(OrchestrationRunnableBase fromRunnable, OrchestrationRunnableBase toRunnable)
    {
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);

        fromRunnable.AddAdvancer(new OrchestrationAdvancer<TOutput>(_ => true, toRunnable));
        toRunnable.AllowDeadEnd = true;
        return this;
    }

    public OrchestrationBuilder AddDeadEndAdvancer<TValue, TOutput>(OrchestrationRunnableBase fromRunnable, AdvancementRequirement<TValue> condition, AdvancementResultConverter<TValue, TOutput> converter, OrchestrationRunnableBase toRunnable)
    {
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);

        fromRunnable.AddAdvancer(new OrchestrationAdvancer<TValue, TOutput>(condition, converter, toRunnable));
        toRunnable.AllowDeadEnd = true;
        return this;
    }

    public OrchestrationBuilder AddAdvancer<TOutput>(OrchestrationRunnableBase fromRunnable, AdvancementRequirement<TOutput> condition, OrchestrationRunnableBase toRunnable)
    {
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);

        fromRunnable.AddAdvancer(new OrchestrationAdvancer<TOutput>(condition, toRunnable));
        return this;
    }

    public OrchestrationBuilder AddAdvancer<TValue, TOutput>(OrchestrationRunnableBase fromRunnable, AdvancementRequirement<TValue> condition, AdvancementResultConverter<TValue, TOutput> converter, OrchestrationRunnableBase toRunnable)
    {
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);

        fromRunnable.AddAdvancer(new OrchestrationAdvancer<TValue, TOutput>(condition, converter, toRunnable));
        return this;
    }

    public OrchestrationBuilder AddAdvancer<TValue, TOutput>(OrchestrationRunnableBase fromRunnable, AdvancementResultConverter<TValue, TOutput> converter, OrchestrationRunnableBase toRunnable, AdvancementRequirement<TValue>? condition = null)
    {
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);

        condition ??= _ => true;
        fromRunnable.AddAdvancer(new OrchestrationAdvancer<TValue, TOutput>(condition, converter, toRunnable));
        return this;
    }

    public OrchestrationBuilder AddAdvancers(OrchestrationRunnableBase fromRunnable, params OrchestrationAdvancer[] advancers)
    {
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);

        foreach(var advancer in advancers)
        {
            if (!Configuration.Runnables.ContainsKey(advancer.NextRunnable.RunnableName))
                Configuration.Runnables.Add(advancer.NextRunnable.RunnableName, advancer.NextRunnable);
            fromRunnable.AddAdvancer(advancer);
        }
        
        return this;
    }

    public OrchestrationBuilder AddParallelAdvancement(OrchestrationRunnableBase fromRunnable, params OrchestrationAdvancer[] advancers)
    {
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);

        foreach (var advancer in advancers)
        {
            if (!Configuration.Runnables.ContainsKey(advancer.NextRunnable.RunnableName))
                Configuration.Runnables.Add(advancer.NextRunnable.RunnableName, advancer.NextRunnable);
            fromRunnable.AddAdvancer(advancer);
        }

        fromRunnable.AllowsParallelAdvances = true;
        return this;
    }

    public OrchestrationBuilder AddExitPath<TOutput>(OrchestrationRunnableBase fromRunnable, AdvancementRequirement<TOutput> condition)
    {
        ExitRunnable<TOutput> toRunnable = new ExitRunnable<TOutput>(Configuration, "Tornado_Exit");
        if (!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);

        fromRunnable.AddAdvancer(new OrchestrationAdvancer<TOutput>(condition, toRunnable));
        return this;
    }

    public void CreateDotGraphVisualization(string filePath, string graphName = "OrchestrationGraph")
    {
        OrchestrationVisualization.SaveDotGraphToFile(Configuration, filePath, graphName);
    }

    public void CreatePlantUmlVisualization(string filePath, string graphName = "OrchestrationGraph")
    {
        OrchestrationVisualization.SavePlantUMLToFile(Configuration, filePath, graphName);
    }

    public OrchestrationRuntimeConfiguration Build()
    {
        return Configuration;
    }
}
