using BlazorWorker.WorkerCore;
using System.Text.RegularExpressions;

namespace BlazorWorker.Demo.Shared
{
    /// <summary>
    /// This service runs in the worker.
    /// Uses hand-written seriaization of messages.
    /// </summary>
    public class CoreMathsService
    {
        public static readonly string EventsPi = $"Events.{nameof(MathsService.Pi)}";
        public static readonly string ResultMessage = $"Methods.{nameof(MathsService.EstimatePI)}.Result";

        private readonly MathsService mathsService;
        private readonly IWorkerMessageService messageService;

        public CoreMathsService(IWorkerMessageService messageService)
        {
            this.messageService = messageService;
            this.messageService.IncomingMessage += OnMessage;
            mathsService = new MathsService();
            mathsService.Pi += (s, progress) => messageService.PostMessageAsync($"{EventsPi}:{progress.Progress}");
        }

        private void OnMessage(object sender, string message)
        {
            if (message.StartsWith(nameof(mathsService.EstimatePI)))
            {
                var messageParams = message.Substring(nameof(mathsService.EstimatePI).Length).Trim();
                var rx = new Regex(@"\((?<arg>[^\)]+)\)");
                var arg0 = rx.Match(messageParams).Groups["arg"].Value.Trim();
                var iterations = int.Parse(arg0);
                mathsService.EstimatePI(iterations).ContinueWith(t =>
                    messageService.PostMessageAsync($"{ResultMessage}:{t.Result}"));
                return;
            }
        }
    }
}
