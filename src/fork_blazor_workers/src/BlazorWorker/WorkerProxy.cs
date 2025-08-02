using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
namespace BlazorWorker.Core
{
    [DependencyHint(typeof(MessageService))]
    public class WorkerProxy : IWorker
    {
        private readonly IJSRuntime jsRuntime;
        private readonly ScriptLoader scriptLoader;
        private static long idSource;
        private bool isDisposed = false;
        private static readonly MethodIdentifier messageMethod;
        private readonly DotNetObjectReference<WorkerProxy> thisReference;


        public event EventHandler<string> IncomingMessage;
        public bool IsInitialized { get; set; }
        static WorkerProxy()
        {
            var messageServiceType = typeof(MessageService);
            messageMethod = MonoTypeHelper.GetStaticMethodId<MessageService>(nameof(MessageService.OnMessage));
            //$"[{messageServiceType.Assembly.GetName().Name}]{messageServiceType.FullName}:{nameof(MessageService.OnMessage)}";
        }

        public WorkerProxy(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            this.scriptLoader = new ScriptLoader(this.jsRuntime);
            this.Identifier = ++idSource;
            thisReference = DotNetObjectReference.Create(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!isDisposed)
            {
                await this.jsRuntime.InvokeVoidAsync("BlazorWorker.disposeWorker", this.Identifier);
                thisReference.Dispose();
                isDisposed = true;
            }
        }

        public async Task InitAsync(WorkerInitOptions initOptions)
        {
            await this.scriptLoader.InitScript();

            await this.jsRuntime.InvokeVoidAsync(
                "BlazorWorker.initWorker", 
                this.Identifier, 
                thisReference,
                new WorkerInitOptions {
                    CallbackMethod = nameof(OnMessage),
                    MessageEndPoint = messageMethod
               }.MergeWith(initOptions));
        }

        [JSInvokable]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task OnMessage(string message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            IncomingMessage?.Invoke(this, message);
        }

        public async Task PostMessageAsync(string message)
        {
            await jsRuntime.InvokeVoidAsync("BlazorWorker.postMessage", this.Identifier, message);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task PostMessageJsDirectAsync(string message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotSupportedException("JsDirect calls are only supported in the direction from worker to main js");
        }



        public long Identifier { get; }
    }
}
