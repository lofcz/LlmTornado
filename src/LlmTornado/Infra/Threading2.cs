using System;
using System.Threading.Tasks;

namespace LlmTornado.Infra;

internal partial class Threading
{
    public static async ValueTask WhenAll(params ValueTask?[] tasks)
    {
        if (tasks.Length == 0)
            return;

        // this looks like sequential awaiting, but since the tasks already started (and run concurrently), we only wait as long as the longest running task
        foreach (ValueTask? task in tasks)
        {
#if MODERN
            if (task is null || task == ValueTask.CompletedTask)
#else
            if (task is null)
#endif
            {
                continue;
            }
            
            await task.Value.ConfigureAwait(false);
        }
    }
}