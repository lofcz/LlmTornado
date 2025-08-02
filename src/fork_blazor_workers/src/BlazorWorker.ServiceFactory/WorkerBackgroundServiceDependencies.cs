using System.Linq;

namespace BlazorWorker.BackgroundServiceFactory
{
    internal class WorkerBackgroundServiceDependencies
    {
        public static string[] BlazorWorkerDependentAssemblyFilenames = new[] {
            $"{typeof(Core.IWorker).Assembly.GetName().Name}.dll",
            $"{typeof(WorkerCore.IWorkerMessageService).Assembly.GetName().Name}.dll",
            $"{typeof(WorkerBackgroundService.WorkerInstanceManager).Assembly.GetName().Name}.dll",
            $"{typeof(Newtonsoft.Json.JsonConvert).Assembly.GetName().Name}.dll",
            $"{typeof(System.Reflection.Assembly).Assembly.GetName().Name}.dll",
        };

#if NETSTANDARD21
        public static string[] DependentAssemblyFilenames =
            BlazorWorkerDependentAssemblyFilenames.Concat(
                new[] {
                    "System.Xml.dll",
                    "Serialize.Linq.dll",
                    "System.dll",
                    "System.Buffers.dll",
                    "System.Data.dll",
                    "System.Core.dll",
                    "System.Memory.dll",
                    "System.Numerics.dll",
                    "System.Numerics.Vectors.dll",
                    "System.Runtime.CompilerServices.Unsafe.dll",
                    "System.Runtime.Serialization.dll",
                    "Microsoft.Bcl.AsyncInterfaces.dll",
                    "System.Threading.Tasks.Extensions.dll",
                    "Mono.Security.dll",
                    "System.ServiceModel.Internals.dll"
                }).ToArray();
#endif

#if NET5_0_OR_GREATER
        public static string[] DependentAssemblyFilenames =
            BlazorWorkerDependentAssemblyFilenames.Concat(
                new[] {
                    "System.Xml.dll",
                    "Serialize.Linq.dll",
                    "System.dll",
                    "System.Buffers.dll",
                    "System.Data.dll",
                    "System.Core.dll",
                    "System.Memory.dll",
                    "System.Numerics.dll",
                    "System.Numerics.Vectors.dll",
                    "System.Runtime.CompilerServices.Unsafe.dll",
                    "System.Runtime.Serialization.dll",
                    "System.Threading.Tasks.Extensions.dll",
                    "System.Xml.ReaderWriter.dll",
                    "System.Text.RegularExpressions.dll",
                    "System.Collections.Concurrent.dll",
                    "System.ComponentModel.Primitives.dll",
                    "System.ComponentModel.TypeConverter.dll",
                    "System.ComponentModel.dll",
                    "System.Collections.Concurrent.dll",
                    "System.Collections.Immutable.dll",
                    "System.Collections.NonGeneric.dll",
                    "System.Collections.Specialized.dll",
                    "System.Data.Common.dll",
                    "System.Data.DataSetExtensions.dll",
                    "System.Data.dll",
                    "System.Reflection.Emit.ILGeneration.dll",
                    "System.Reflection.Emit.Lightweight.dll",
                    "System.Private.DataContractSerialization.dll",
                    "System.Runtime.Serialization.Formatters.dll",
                    "System.Runtime.Serialization.Json.dll",
                    "System.Runtime.Serialization.Primitives.dll",
                    "System.Runtime.Serialization.Xml.dll",
                    "System.Runtime.Serialization.dll",
                    "System.Text.Encoding.CodePages.dll",
                    "System.Text.Encoding.Extensions.dll",
                    "System.Text.Encoding.dll",
                }).ToArray();
#endif

    }
}
