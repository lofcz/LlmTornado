using System;
using System.Reflection;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    internal class Runtime
    {
#if NETSTANDARD21
        private const string assembly = "WebAssembly.Bindings";
        private static readonly string type = $"WebAssembly.{nameof(Runtime)}";
#endif

#if NET5_0_OR_GREATER
        private const string assembly = "System.Private.Runtime.InteropServices.JavaScript";
        private static readonly string type = $"System.Runtime.InteropServices.JavaScript.{nameof(Runtime)}";
#endif
        private delegate object GetGlobalObjectDelegate(string globalObjectName);

        private static Assembly SourceAssembly => Assembly.Load(assembly) 
            ?? throw new InvalidOperationException($"Unable to load assembly {assembly}");

        private static GetGlobalObjectDelegate _getGlobalObjectMethod =
                SourceAssembly
                .GetType(type)?
                .GetMethod(nameof(GetGlobalObject))?
                .CreateDelegate(typeof(GetGlobalObjectDelegate)) as GetGlobalObjectDelegate;

        public static object GetGlobalObject(string globalObjectName) => _getGlobalObjectMethod?.Invoke(globalObjectName) 
            ?? throw new InvalidOperationException($"Unable to load method {type}.{nameof(GetGlobalObject)} from assembly {assembly}");
    }
}