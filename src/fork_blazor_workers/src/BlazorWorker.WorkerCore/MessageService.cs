using System;
using System.Linq;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#endif
using System.Threading.Tasks;

#if NET7_0_OR_GREATER
namespace BlazorWorker.WorkerCore
{
    /// <summary>
    /// Simple static message service that runs in the worker thread.
    /// </summary>
    public partial class MessageService
    {
        public static event EventHandler<string> Message;

        [JSExport]
        public static void OnMessage(string message)
        {
            Message?.Invoke(null, message);
#if DEBUG
            Console.WriteLine($"{nameof(MessageService)}.{nameof(OnMessage)}: {message}");
#endif
        }

        [JSImport("PostMessage", "BlazorWorker.js")]
        public static partial void PostMessage(string message);

        [JSImport("PostMessageJsDirect", "BlazorWorker.js")]
        public static partial void PostMessageJsDirect(string message);
    }
}
#else

namespace BlazorWorker.WorkerCore
{
    /// <summary>
    /// Simple static message service that runs in the worker thread.
    /// </summary>
    public class MessageService
    {
        private static readonly DOMObject self = DOMObject.Self;

        public static event EventHandler<string> Message;

        static MessageService()
        {   
        }

        public static void OnMessage(string message)
        {
            Message?.Invoke(null, message);
#if DEBUG
            Console.WriteLine($"{nameof(MessageService)}.{nameof(OnMessage)}: {message}");
#endif
        }
        public static void PostMessage(string message)
        {
            self.Invoke("postMessage", message);
        }

    }
}
#endif