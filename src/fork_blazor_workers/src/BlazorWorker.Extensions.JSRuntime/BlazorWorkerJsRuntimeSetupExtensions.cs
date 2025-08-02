using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Text.Json;

namespace BlazorWorker.Extensions.JSRuntime
{
    public static class BlazorWorkerJsRuntimeSetupExtensions
    {
        public static IServiceCollection AddBlazorWorkerJsRuntime(this IServiceCollection source, Action<JsonSerializerOptions> optionsModifier = null)
        {
            source.AddSingleton(CreateBlazorWorkerJSRuntime(optionsModifier));
            return source;
        }

        private static Func<IServiceProvider, IJSRuntime> CreateBlazorWorkerJSRuntime(Action<JsonSerializerOptions> optionsModifier) {

            var instance = new BlazorWorkerJSRuntime();

            if (optionsModifier != null)
            {
                optionsModifier(instance.SerializerOptions);
            }

            return _ => instance;
        }
    }
}
