using BlazorWorker.WorkerCore;
using System;

namespace BlazorWorker.Core
{
    public class InstanceHandle : IDisposable
    {
        public InstanceHandle(
            IWorkerMessageService messageService, 
            Type serviceType, 
            long identifier, 
            Action onDispose)
        {
            MessageService = messageService;
            ServiceType = serviceType;
            Identifier = identifier;
            OnDispose = onDispose;
        }

        public IWorkerMessageService MessageService { get; }

        public Type ServiceType { get; }
        public long Identifier { get; }
        public Action OnDispose { get; }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}
