namespace BlazorWorker.Core
{
    public class WorkerProxyDependencies
    {
#if NETSTANDARD21
        public static readonly string[] DependentAssemblyFilenames = new[]
        {
            "BlazorWorker.WorkerCore.dll", 
            "netstandard.dll",
            "mscorlib.dll",
            "WebAssembly.Bindings.dll",
            "System.dll",
            "System.Core.dll"
        };
#endif

#if NET5_0_OR_GREATER
        public static readonly string[] DependentAssemblyFilenames = new[]
        {
            "BlazorWorker.WorkerCore.dll",
            "netstandard.dll",
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
            "System.Buffers.dll",
            "System.Collections.dll",
            "System.Configuration.dll",
            "System.Console.dll",
            "System.Core.dll",
            "System.Diagnostics.Debug.dll",
            "System.Diagnostics.DiagnosticSource.dll",
            "System.Diagnostics.StackTrace.dll",
            "System.Diagnostics.TraceSource.dll",
            "System.Dynamic.Runtime.dll",
            "System.Globalization.Calendars.dll",
            "System.Globalization.Extensions.dll",
            "System.Globalization.dll",
            "System.Linq.Expressions.dll",
            "System.Linq.Queryable.dll",
            "System.Linq.dll",
            "System.Memory.dll",
            "System.Numerics.Vectors.dll",
            "System.Numerics.dll",
            "System.ObjectModel.dll",
            "System.Private.CoreLib.dll",
            "System.Private.Runtime.InteropServices.JavaScript.dll",
            "System.Private.Uri.dll",
            "System.Private.Xml.Linq.dll",
            "System.Private.Xml.dll",
            "System.Reflection.DispatchProxy.dll",
            "System.Reflection.Extensions.dll",
            "System.Reflection.Metadata.dll",
            "System.Reflection.Primitives.dll",
            "System.Reflection.TypeExtensions.dll",
            "System.Reflection.dll",
            "System.Runtime.Extensions.dll",
            "System.Runtime.Handles.dll",
            "System.Runtime.InteropServices.RuntimeInformation.dll",
            "System.Runtime.InteropServices.dll",
            "System.Runtime.Intrinsics.dll",
            "System.Runtime.Loader.dll",
            "System.Runtime.Numerics.dll",
            "System.Runtime.dll",
            "System.Threading.Tasks.dll",
            "System.Threading.Thread.dll",
            "System.Threading.dll",
        };
#endif


    }
}
