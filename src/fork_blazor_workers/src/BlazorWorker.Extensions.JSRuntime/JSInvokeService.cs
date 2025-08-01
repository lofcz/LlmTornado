using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace BlazorWorker.Extensions.JSRuntime
{
    public partial class JSInvokeService
    {
        /// <summary>
        /// Imports locally hosted module scripts
        /// </summary>
        /// <param name="relativeUrls"></param>
        /// <returns></returns>
        public static Task ImportLocalScripts(params string[] relativeUrls)
            => PrivateImportLocalScripts(relativeUrls);

        /// <summary>
        /// Invokes a method defined on the worker globalThis / self object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="args">JSON serialized parameters</param>
        /// <returns></returns>
        public static string WorkerInvoke<T>(string method, string args)
            => (string) PrivateWorkerInvoke(method, args);

        /// <summary>
        /// Asynchronically Invokes a method defined on the worker globalThis / self object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="args">JSON serialized parameters</param>
        /// <returns></returns>
        public static async Task<string> WorkerInvokeAsync<T>(string method, string args)
            => (await PrivateWorkerInvokeAsync(method, args));

        #region Generated methods

        /// <summary>
        /// Checks if the specified object path is defined using the self / globalThis object as root.
        /// </summary>
        /// <param name="objectPath"></param>
        /// <returns></returns>
        [JSImport("IsObjectDefined", "BlazorWorker.js")]
        public static partial bool IsObjectDefined(string objectPath);

        /// <summary>
        /// Prepending the specified <paramref name="relativeUrls"/> with the base path of the application, invokes the importScripts() method of the WorkerGlobalScope interface, which synchronously imports one or more scripts into the worker's scope.
        /// </summary>
        /// <param name="relativeUrls"></param>
        [JSImport("ImportLocalScripts", "BlazorWorker.js")]
        private static partial Task PrivateImportLocalScripts(string[] relativeUrls);

        [JSImport("WorkerInvokeAsync", "BlazorWorkerJSRuntime.js")]
        private static partial Task<string> PrivateWorkerInvokeAsync(string method, string args);

        [JSImport("WorkerInvoke", "BlazorWorkerJSRuntime.js")]
        private static partial string PrivateWorkerInvoke(string method, string args);

        #endregion
    }
}
