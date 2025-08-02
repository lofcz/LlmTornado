using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using System;

namespace BlazorWorker.BackgroundServiceFactory
{
    public static class WorkerInitExtension
    {

        /// <summary>
        /// Sets a custom ExpressionSerializer type. Must implement <see cref="IExpressionSerializer"/>..
        /// </summary>
        /// <param name="source"></param>
        /// <param name="expressionSerializerType">A type that implements <see cref="IExpressionSerializer"/></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static WorkerInitOptions UseCustomExpressionSerializer(this WorkerInitOptions source, Type expressionSerializerType)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            source.SetEnv(WebWorkerOptions.ExpressionSerializerTypeEnvKey, expressionSerializerType.AssemblyQualifiedName);
            return source;
        }
    }
}
