using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;
public class ExitRunnable<TValue> : OrchestrationRunnable<TValue, TValue>
{
    public ExitRunnable(Orchestration orchestrator, string runnableName = "") : base(orchestrator, runnableName)
    {
        AllowDeadEnd = true;
    }

    public override ValueTask<TValue> Invoke(RunnableProcess<TValue, TValue> value)
    {
        Orchestrator?.HasCompletedSuccessfully();
        return new ValueTask<TValue>(value.Input);
    }
}