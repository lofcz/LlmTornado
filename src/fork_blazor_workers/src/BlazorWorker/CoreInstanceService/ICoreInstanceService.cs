using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core.CoreInstanceService
{
    public interface ICoreInstanceService
    {
        Task<IInstanceHandle> CreateInstance(Type type);
        Task<IInstanceHandle> CreateInstance<T>();

        Task<IInstanceHandle> CreateInstance<T>(Action<WorkerInitOptions> options);

        Task<IInstanceHandle> CreateInstance<T>(WorkerInitOptions options);
    }

    public interface IInstanceHandle : IAsyncDisposable {
    }
}
