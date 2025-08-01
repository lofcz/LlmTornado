using System;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore.SimpleInstanceService
{
    public delegate bool IsInfrastructureMessage(string message);
    public class InjectableMessageService : IWorkerMessageService, IDisposable
    {
        private readonly IsInfrastructureMessage isInfrastructureMessage;

        public InjectableMessageService(IsInfrastructureMessage isInfrastructureMessage)
        {
            MessageService.Message += OnIncomingMessage;
            this.isInfrastructureMessage = isInfrastructureMessage;
        }

        private void OnIncomingMessage(object sender, string rawMessage)
        {
            if (isInfrastructureMessage(rawMessage))
            {
                // Prevents Infrastructure messages from propagating downwards
                return;
            }

            IncomingMessage?.Invoke(sender, rawMessage);
        }

        public event EventHandler<string> IncomingMessage;

        public void Dispose()
        {
            MessageService.Message -= OnIncomingMessage;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task PostMessageAsync(string message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if DEBUG
            Console.WriteLine($"{nameof(InjectableMessageService)}.{nameof(PostMessageAsync)}('{message}')");
#endif
            MessageService.PostMessage(message);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task PostMessageJsDirectAsync(string message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            MessageService.PostMessageJsDirect(message);
        }
    }
}
