using BlazorWorker.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWorker.Demo.Shared
{
    public static class SharedDemoExtensions
    {
        public static IServiceCollection AddDemoDependencies(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddWorkerFactory();
            serviceCollection.AddTransient<JsDirectExample>();
            serviceCollection.AddTransient<JsInteractionsExample>();
            return serviceCollection;
        }
    }
}
