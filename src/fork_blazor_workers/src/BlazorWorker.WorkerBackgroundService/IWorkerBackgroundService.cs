using BlazorWorker.WorkerCore;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerBackgroundService
{
    public interface IWorkerBackgroundService<T> : IAsyncDisposable where T : class
    {
        /// <summary>
        /// Registers an event listener to the specified event.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="eventName"></param>
        /// <param name="myHandler"></param>
        /// <returns></returns>
        Task<EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler);
 
        /// <summary>
        /// Queues the specified work to run on the underlying worker and returns a <see cref="Task"/> object that represents that work.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        Task<TResult> RunAsync<TResult>(Expression<Func<T, TResult>> function);

        /// <summary>
        /// Queues the specified work to run on the underlying worker and returns a <see cref="Task"/> object that represents that work.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        Task RunAsync(Expression<Action<T>> action);

        /// <summary>
        /// Queues the specified awaitable work to run on the underlying worker, awaits the result, and returns a <see cref="Task"/> object that represents that work.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        Task<TResult> RunAsync<TResult>(Expression<Func<T, Task<TResult>>> function);

        /// <summary>
        /// Queues the specified awaitable work to run on the underlying worker, awaits the execution, and returns a <see cref="Task"/> object that represents that work.
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        Task RunAsync(Expression<Func<T, Task>> function);

        /// <summary>
        /// Returns the message service used by the underlying worker.
        /// </summary>
        /// <returns></returns>
        IWorkerMessageService GetWorkerMessageService();

        /// <summary>
        /// Unregisters the event corresponding to the specified <paramref name="handle"/>
        /// </summary>
        Task UnRegisterEventListenerAsync(EventHandle handle);
    }
}
