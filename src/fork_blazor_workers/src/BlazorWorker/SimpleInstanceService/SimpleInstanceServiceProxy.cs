using BlazorWorker.WorkerCore;
using BlazorWorker.WorkerCore.SimpleInstanceService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWorker.Core.SimpleInstanceService
{
    public class SimpleInstanceServiceProxy : ISimpleInstanceService
    {
        private readonly IWorker worker;
        private readonly TaskRegister<DisposeResult> disposeResultRegister = new();
        private readonly TaskRegister<InitInstanceResult> initInstanceRegister = new();
        private TaskCompletionSource<InitServiceResult> initWorker;

        public bool IsInitialized { get; internal set; }

        public SimpleInstanceServiceProxy(IWorker worker)
        {
            this.worker = worker;
            this.worker.IncomingMessage += OnIncomingMessage;
        }

        public async Task InitializeAsync(WorkerInitOptions options = null)
        {   
            if (!IsInitialized)
            {
                if (!this.worker.IsInitialized)
                {
                    initWorker = new TaskCompletionSource<InitServiceResult>();
                    await this.worker.InitAsync(options);
                    if (this.worker is WorkerProxy proxy)
                    {
                        proxy.IsInitialized = true;
                    }
                    await this.initWorker.Task;
                }

                IsInitialized = true;
            }
        }

        private void OnIncomingMessage(object sender, string message)
        {
#if DEBUG
            Console.WriteLine($"{nameof(SimpleInstanceServiceProxy)}:{message}");
#endif
            if (DisposeResult.CanDeserialize(message)) {
                var result = DisposeResult.Deserialize(message);
                if (disposeResultRegister.TryRemove(result.CallId, out var taskCompletionSource))
                {
                    taskCompletionSource.SetResult(result);
                }
                return;
            }

            if (InitServiceResult.CanDeserialize(message))
            {
                initWorker.SetResult(InitServiceResult.Deserialize(message));
                return;
            }

            if (InitInstanceResult.CanDeserialize(message))
            {
                var result = InitInstanceResult.Deserialize(message);
                if (initInstanceRegister.TryRemove(result.CallId, out var taskCompletionSource))
                {
                    taskCompletionSource.SetResult(result);
                }
                return;
            }
        }

        public async Task<DisposeResult> DisposeInstance(DisposeInstanceRequest request)
        {
            var (callIdSource, tcs) = disposeResultRegister.CreateAndAdd();
            request.CallId = callIdSource;
            await this.worker.PostMessageAsync(request.Serialize());
            return await tcs.Task;
        }

        public async Task<InitInstanceResult> InitInstance(InitInstanceRequest request)
        {
            var (callIdSource, tcs) = initInstanceRegister.CreateAndAdd();
            request.CallId = callIdSource;
            await this.worker.PostMessageAsync(request.Serialize());
            return await tcs.Task;
        }
    }
}
