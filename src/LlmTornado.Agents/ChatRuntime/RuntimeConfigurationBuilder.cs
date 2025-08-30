using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

    public OrchestrationBuilder AddAdvancer<TInput, TOutput>(OrchestrationRunnableBase fromRunnable, OrchestrationRunnableBase toRunnable)
    {
        if(!Configuration.Runnables.ContainsKey(fromRunnable.RunnableName))
            Configuration.Runnables.Add(fromRunnable.RunnableName, fromRunnable);
        if (!Configuration.Runnables.ContainsKey(toRunnable.RunnableName))
            Configuration.Runnables.Add(toRunnable.RunnableName, toRunnable);
        return this;
    }

    public OrchestrationRuntimeConfiguration Build()
    {
        return Configuration;
    }
}
