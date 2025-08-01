using System;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    public interface IWorkerMessageService
    {
        /// <summary>
        /// Events for incoming messages to the current context
        /// </summary>
        event EventHandler<string> IncomingMessage;

        /// <summary>
        /// Post a message to the context this message service belongs to
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PostMessageAsync(string message);

        /// <summary>
        /// Post a message that can be read directly on the main js thread using the "blazorworker:jsdirect" event on the window object
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PostMessageJsDirectAsync(string message);
    }
}
