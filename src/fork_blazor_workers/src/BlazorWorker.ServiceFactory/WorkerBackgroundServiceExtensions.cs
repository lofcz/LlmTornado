using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public static class WorkerBackgroundServiceExtensions
    {
        public static async Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(this IWorker webWorkerProxy, Action<WorkerInitOptions> workerInitOptionsModifier) where T : class
        {
            if (webWorkerProxy is null)
            {
                throw new ArgumentNullException(nameof(webWorkerProxy));
            }

            var options = new WorkerInitOptions();
            workerInitOptionsModifier(options);
            return await webWorkerProxy.CreateBackgroundServiceAsync<T>(options);
        }

        public static async Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(this IWorker webWorkerProxy, WorkerInitOptions workerInitOptions = null) where T : class
        {
            if (webWorkerProxy is null)
            {
                throw new ArgumentNullException(nameof(webWorkerProxy));
            }
            if (workerInitOptions == null)
            {
                workerInitOptions = new WorkerInitOptions();
            }

            var webWorkerOptions = new WebWorkerOptions();
            webWorkerOptions.SetExpressionSerializerFromInitOptions(workerInitOptions);

            var proxy = new WorkerBackgroundServiceProxy<T>(webWorkerProxy, webWorkerOptions);

            await proxy.InitAsync(workerInitOptions);
            return proxy;
        }

        /// <summary>
        /// Creates a background service using the specified <paramref name="factoryExpression"/>.
        /// </summary>
        /// <typeparam name="TFactory"></typeparam>
        /// <typeparam name="TService"></typeparam>
        /// <param name="webWorkerProxy"></param>
        /// <param name="factoryExpression"></param>
        /// <param name="workerInitOptionsModifier"></param>
        /// <returns></returns>
        public static async Task<IWorkerBackgroundService<TService>> CreateBackgroundServiceUsingFactoryAsync<TFactory, TService>(
            this IWorker webWorkerProxy,
            Expression<Func<TFactory, TService>> factoryExpression,
            Action<WorkerInitOptions> workerInitOptionsModifier = null) 
            where TFactory : class 
            where TService : class
        {
            if (webWorkerProxy is null)
            {
                throw new ArgumentNullException(nameof(webWorkerProxy));
            }

            if (factoryExpression is null)
            {
                throw new ArgumentNullException(nameof(factoryExpression));
            }

            var workerInitOptions = new WorkerInitOptions();
            if (workerInitOptionsModifier != null)
            {
                workerInitOptionsModifier(workerInitOptions);
            }

            if (workerInitOptions == null)
            {
                workerInitOptions = new WorkerInitOptions();
            }

            var webWorkerOptions = new WebWorkerOptions();
            webWorkerOptions.SetExpressionSerializerFromInitOptions(workerInitOptions);
            

            var factoryProxy = new WorkerBackgroundServiceProxy<TFactory>(webWorkerProxy, webWorkerOptions);
            await factoryProxy.InitAsync(workerInitOptions);

            var newProxy = await factoryProxy.InitFromFactoryAsync(factoryExpression);
            newProxy.Disposables.Add(factoryProxy);
            return newProxy;
        }

        private static void SetExpressionSerializerFromInitOptions(this WebWorkerOptions target, WorkerInitOptions workerInitOptions)
        {
            if (workerInitOptions.EnvMap?.TryGetValue(WebWorkerOptions.ExpressionSerializerTypeEnvKey, out var serializerType) == true)
            {
                target.ExpressionSerializerType = Type.GetType(serializerType);
            }
        }

        /// <summary>
        /// Creates a new background service using the specified <paramref name="factoryExpression"/>
        /// </summary>
        /// <typeparam name="TFactory"></typeparam>
        /// <typeparam name="TService"></typeparam>
        /// <param name="webWorkerService"></param>
        /// <param name="factoryExpression"></param>
        /// <returns></returns>
        public static async Task<IWorkerBackgroundService<TService>> CreateBackgroundServiceAsync<TFactory, TService>(
            this IWorkerBackgroundService<TFactory> webWorkerService,
            Expression<Func<TFactory, TService>> factoryExpression)
            where TFactory : class
            where TService : class
        {
            if (webWorkerService is null)
            {
                throw new ArgumentNullException(nameof(webWorkerService));
            }

            if (factoryExpression is null)
            {
                throw new ArgumentNullException(nameof(factoryExpression));
            }

            if (!(webWorkerService is WorkerBackgroundServiceProxy<TFactory> webWorkerProxy))
            {
                throw new ArgumentException($"{nameof(webWorkerService)} must be of type {nameof(WorkerBackgroundServiceProxy<TFactory>)}", nameof(webWorkerProxy));
            }

            return await webWorkerProxy.InitFromFactoryAsync(factoryExpression);
        }
    }
}

