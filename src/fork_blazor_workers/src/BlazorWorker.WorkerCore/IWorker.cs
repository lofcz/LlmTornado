using BlazorWorker.WorkerCore;
using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{

    public interface IWorker : IWorkerMessageService, IAsyncDisposable
    {
        bool IsInitialized { get; }
        
        long Identifier { get; }

        Task InitAsync(WorkerInitOptions initOptions);
    }
}
