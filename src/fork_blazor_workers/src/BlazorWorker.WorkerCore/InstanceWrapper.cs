using System;

namespace BlazorWorker.WorkerCore
{
    public class InstanceWrapper : IDisposable
    {
        public object Instance { get; set; }
        public IDisposable Services { get; set; }

        public void Dispose()
        {
            if (Instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Services?.Dispose();
        }
    }
}
