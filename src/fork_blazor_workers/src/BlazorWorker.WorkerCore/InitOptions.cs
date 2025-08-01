using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWorker.Core
{
    /// <summary>
    /// Options for initializing the worker.
    /// </summary>
    public class WorkerInitOptions
    {
        /// <summary>
        /// Creates a new instance of WorkerInitOptions
        /// </summary>
        public WorkerInitOptions()
        {
#if NETSTANDARD21
            DeployPrefix = "_framework/_bin";
            WasmRoot = "_framework/wasm";
#endif
#if NET5_0_OR_GREATER
            DeployPrefix = "_framework";
            WasmRoot = "_framework";
#endif
#if DEBUG
            Debug = true;
#endif
            RuntimePreprocessorSymbols = new();
#if NET7_0
            RuntimePreprocessorSymbols.Add("NET7_0", true);
#endif
#if NET7_0_OR_GREATER
            RuntimePreprocessorSymbols.Add("NET7_0_OR_GREATER", true);
#endif
#if NET8_0
            RuntimePreprocessorSymbols.Add("NET8_0", true);
#endif
#if NET8_0_OR_GREATER
            RuntimePreprocessorSymbols.Add("NET8_0_OR_GREATER", true);
#endif
#if NET9_0
            RuntimePreprocessorSymbols.Add("NET9_0", true);
#endif
#if NET9_0_OR_GREATER
            RuntimePreprocessorSymbols.Add("NET9_0_OR_GREATER", true);
#endif
#if NET10_0
            RuntimePreprocessorSymbols.Add("NET10_0", true);
#endif
#if NET10_0_OR_GREATER
            RuntimePreprocessorSymbols.Add("NET10_0_OR_GREATER", true);
#endif
        }

        /// <summary>
        /// Specifies the location of binaries
        /// </summary>
        public string DeployPrefix { get; }

        /// <summary>
        /// Specifies the location of the wasm binary
        /// </summary>
        public string WasmRoot { get; }

        /// <summary>
        /// Outputs additional debug information to the console
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for sending messages to the worker. Experts only.
        /// </summary>
        public MethodIdentifier MessageEndPoint { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for instantiating the worker. Experts only.
        /// </summary>
        public MethodIdentifier InitEndPoint { get; set; }

        /// <summary>
        /// Unique blazor identifier for handling callbacks. As referenced by JSInvokableAttribute. Experts only.
        /// </summary>
        public string CallbackMethod { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for doing callbacks on self invocations from the worker. Experts only.
        /// </summary>
        public string EndInvokeCallBackEndpoint { get; set; }

        /// <summary>
        /// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives
        /// </summary>
        public Dictionary<string, bool> RuntimePreprocessorSymbols { get; set; }

        /// <summary>
        /// Sets environment variables in the target worker. 
        /// </summary>
        /// <remarks>
        /// Defaults to a single entry: DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = '0'.
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public Dictionary<string, string> EnvMap { get; set; } 
            = new Dictionary<string, string>() { 
                { "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "0" },
            };

#if NET10_0_OR_GREATER
        /// <summary>
        /// Provides a map of assembly names to their content-hashed filenames for .NET 10+.
        /// </summary>
        public Dictionary<string, string> AssetMap { get; set; }
#endif

        public WorkerInitOptions MergeWith(WorkerInitOptions initOptions)
        {
            var newEnvMap = new Dictionary<string , string>(this.EnvMap);
            if (initOptions.EnvMap != null)
            {
                foreach (var entry in initOptions.EnvMap)
                {
                    newEnvMap[entry.Key] = entry.Value;
                }
            }
            return new WorkerInitOptions
            {
                CallbackMethod = initOptions.CallbackMethod ?? this.CallbackMethod,
                MessageEndPoint = initOptions.MessageEndPoint ?? this.MessageEndPoint,
                InitEndPoint = initOptions.InitEndPoint ?? this.InitEndPoint,
                EndInvokeCallBackEndpoint = initOptions.EndInvokeCallBackEndpoint ?? this.EndInvokeCallBackEndpoint,
                EnvMap = newEnvMap,
#if NET10_0_OR_GREATER
                AssetMap = initOptions.AssetMap ?? this.AssetMap
#endif
            };
        }
    }

    /// <summary>
    /// Contains convenience extensions for <see cref="WorkerInitOptions"/>
    /// </summary>
    public static class WorkerInitOptionsExtensions
    {
        /// <summary>
        /// Set the specified <paramref name="environmentVariableName"/> to the specified <paramref name="value"/> when the worker runtime has been initialized
        /// </summary>
        /// <param name="source"></param>
        /// <param name="environmentVariableName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public static WorkerInitOptions SetEnv(this WorkerInitOptions source, string environmentVariableName, string value)
        {
            source.EnvMap[environmentVariableName] = value;
            return source;
        }
    }

    public class MethodIdentifier
    {
        public string AssemblyName { get; set; }
        public string FullMethodName { get; set; }
    }
}
