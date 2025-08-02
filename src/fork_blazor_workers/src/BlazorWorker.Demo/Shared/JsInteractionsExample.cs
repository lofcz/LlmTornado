using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.Core;
using BlazorWorker.Extensions.JSRuntime;
using BlazorWorker.WorkerBackgroundService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace BlazorWorker.Demo.Shared
{
    public partial class JsInteractionsExample
    {
        private readonly IWorkerFactory workerFactory;
        private IWorkerBackgroundService<JsInteractionsExampleWorkerService> service;
        public event EventHandler<string> LogHandler;
        public event EventHandler<string> WorkerLogHandler;

        public JsInteractionsExample(IWorkerFactory workerFactory)
        {
            this.workerFactory = workerFactory;
        }

        private void Log(string message)
        {
            LogHandler?.Invoke(this, message);
        }

        private void WorkerLog(string message)
        {
            WorkerLogHandler?.Invoke(this, message);
        }

        public async Task Execute()
        {
            if (this.service == null)
            {
                Log("Execute: Creating worker...");
                var worker = await this.workerFactory.CreateAsync();
                Log("Execute: Creating service...");
                this.service = await worker
                    .CreateBackgroundServiceUsingFactoryAsync<JsInteractionsExampleStartup, JsInteractionsExampleWorkerService>(x => 
                        x.Resolve<JsInteractionsExampleWorkerService>());
                Log("Execute: Registering log event...");
                await this.service.RegisterEventListenerAsync<string>("Log", (s, log) => WorkerLog(log));
                Log("Execute: Service Created.");
            }

            Log($"Execute: Calling ExecuteJsInteractionWithCallback on worker...");
            await service.RunAsync(s => s.ExecuteJsInteractionWithCallback());
            Log("Execute: Done");
        }

    }

    #region Runs on web worker
    public class JsInteractionsExampleStartup
    {
        private readonly IServiceProvider serviceProvider;

        public JsInteractionsExampleStartup()
        {
            var serviceCollection = new ServiceCollection();
            Configure(serviceCollection);
            this.serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public T Resolve<T>() => serviceProvider.GetService<T>();

        public void Configure(IServiceCollection services)
        {
            services.AddBlazorWorkerJsRuntime()
                    .AddTransient<JsInteractionsExampleWorkerService>();
        }

    }

    public partial class JsInteractionsExampleWorkerService : IDisposable, IAsyncDisposable
    {
        private readonly IJSRuntime blazorJsRuntime;

        public event EventHandler<string> Log;

        private DotNetObjectReference<JsInteractionsExampleWorkerService> selfRef;

        public JsInteractionsExampleWorkerService(IJSRuntime blazorJsRuntime)
        {
            this.blazorJsRuntime = blazorJsRuntime;
        }
        public async Task ExecuteJsInteractionWithCallback()
        {
            // Method setupJsDirectForWorker is defined in BlazorWorker.Demo.SharedPages/wwwroot/JsDirectExample.js
            await this.blazorJsRuntime.InvokeVoidAsync("importLocalScripts", "_content/BlazorWorker.Demo.SharedPages/JsInteractionsExample.js");

            await this.blazorJsRuntime.InvokeVoidAsync("jsInteractionsExample", selfRef ??= DotNetObjectReference.Create(this));
        }

        /// <summary>
        /// This instance method will be called from JsInteractionsExample.js.
        /// </summary>
        /// <param name="arg"></param>
        [JSInvokable]
        public void CallbackFromJavascript(string arg)
        {
            Console.WriteLine($"Worker Console: {nameof(CallbackFromJavascript)}('{arg}')");
            Log?.Invoke(this, $"{nameof(CallbackFromJavascript)}('{arg}')");
        }

        [JSExport]
        public static int StaticCallbackFromJs(string arg)
        {
            Console.WriteLine($"Worker Console: {nameof(StaticCallbackFromJs)}('{arg}')");
            return 5;
        }

        public void Dispose()
        {
            selfRef?.Dispose();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async ValueTask DisposeAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.Dispose();
        }
    }

    #endregion
}
