using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace BlazorWorker.Demo.Shared
{
    public partial class JsDirectExample
    {
        private readonly IWorkerFactory workerFactory;
        private readonly IJSRuntime jsRuntime;
        private IWorkerBackgroundService<JsDirectExampleWorkerService> service;
        private long workerId;
        public event EventHandler<string> LogHandler;

        public JsDirectExample(IWorkerFactory workerFactory, IJSRuntime jsRuntime)
        {
            this.workerFactory = workerFactory;
            this.jsRuntime = jsRuntime; 
        }

        private void Log(string message)
        {
            LogHandler?.Invoke(this, message);
        }

        public async Task Execute()
        {
            if (this.service == null)
            {
                Log("Execute: Creating worker...");
                var worker = await this.workerFactory.CreateAsync();
                this.workerId = worker.Identifier;
                Log("Execute: Creating service...");
                this.service = await worker.CreateBackgroundServiceAsync<JsDirectExampleWorkerService>();

                Log("Execute: Setting up main js...");

                // Method setupJsDirectForWorker is defined in BlazorWorker.Demo.SharedPages/wwwroot/JsDirectExample.js
                await this.jsRuntime.InvokeVoidAsync("setupJsDirectForWorker", this.workerId);
            }

            Log("Execute: Calling ExecuteJsDirect on worker...");
            await service.RunAsync(s => s.ExecuteJsDirect());
            Log("Execute: Done");
        }

    }

    public class JsDirectExampleWorkerService
    {
        private readonly IWorkerMessageService messageService;
        public JsDirectExampleWorkerService(IWorkerMessageService messageService)
        {
            this.messageService = messageService;
        }
        public async Task ExecuteJsDirect()
        {
            await messageService.PostMessageJsDirectAsync("Hello main js thread.");
        }
    }
}
