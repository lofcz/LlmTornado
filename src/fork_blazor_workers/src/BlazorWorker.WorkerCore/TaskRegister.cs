using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    public class TaskRegister<TMessage> : ConcurrentDictionary<long, TaskCompletionSource<TMessage>>
    {
        public (long, TaskCompletionSource<TMessage>) CreateAndAdd()
        {
            var tcs = new TaskCompletionSource<TMessage>();
            var id = Interlocked.Increment(ref TaskRegister.idSource);
            var retries = 100;
            string errorMessage = string.Empty;
            while (!this.TryAdd(id, tcs))
            {
                if (retries < 0)
                {
                    throw new InvalidOperationException(errorMessage);
                }

                errorMessage = $"{nameof(TaskRegister)}: Unable to add task id {id} as it already exists. This may happen if a task has not been properly disposed, awaited, or is running in an infinite loop.";
                id = Interlocked.Increment(ref TaskRegister.idSource);
                retries--;
            }

            if (retries < 100) {
                Console.Error.WriteLine(errorMessage);
            }

            return (id, tcs);
        }
    }

    public class TaskRegister : TaskRegister<object> {
        public static long idSource;
    }
}
