using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core.CoreInstanceService
{
    internal class CoreInstanceHandle : IInstanceHandle
    {
        private Func<Task> onDispose;

        public CoreInstanceHandle(Func<Task> onDispose)
        {
            this.onDispose = onDispose;
        }

        public async ValueTask DisposeAsync()
        {
            await this.onDispose();
        }
    }
}
