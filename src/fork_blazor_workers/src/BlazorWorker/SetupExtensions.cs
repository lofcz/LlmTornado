using Microsoft.Extensions.DependencyInjection;

namespace BlazorWorker.Core
{
    public static class SetupExtensions
    {
        /// <summary>
        /// Adds <see cref="IWorkerFactory"/> as a singleton service
        /// to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services"></param>
        public static IServiceCollection AddWorkerFactory(this IServiceCollection services)
        {
            services.AddSingleton<IWorkerFactory, WorkerFactory>();
            return services;
        }
    }
}
