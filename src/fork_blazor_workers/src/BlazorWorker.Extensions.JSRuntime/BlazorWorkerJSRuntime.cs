using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.Extensions.JSRuntime
{
    /// <summary>
    /// IJSRuntime implementation for use in a worker process
    /// </summary>
    public partial class BlazorWorkerJSRuntime : IJSRuntime, IJSInProcessRuntime
    {
        private static bool isJsInitialized;

        /// <summary>
        /// Serializer that will be used
        /// </summary>
        public IBlazorWorkerJSRuntimeSerializer Serializer { get; set; }

        /// <summary>
        /// The serializer options to be used for the underlying serializer
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; }
        
        /// <summary>
        /// Creates a new JSRuntime
        /// </summary>
        public BlazorWorkerJSRuntime()
        {

            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    { new DotNetObjectReferenceJsonConverterFactory(this) }
}
            };

            this.Serializer = new DefaultBlazorWorkerJSRuntimeSerializer(SerializerOptions);
        }

        /// <summary>
        /// Invokes a method defined on the worker globalThis (self) object
        /// </summary>
        /// <typeparam name="T">expected return type</typeparam>
        /// <param name="identifier">js method name</param>
        /// <param name="args">JSON serializable arguments to send to the js method</param>
        /// <returns></returns>
        public T Invoke<T>(string identifier, params object[] args)
        {
            var resultString = JSInvokeService.WorkerInvoke<T>(identifier, Serialize(args));

            return this.Serializer.Deserialize<T>(resultString);
        }

        /// <summary>
        /// Invokes a method defined on the worker globalThis (self) object asynchronically
        /// </summary>
        /// <typeparam name="TValue">expected return type</typeparam>
        /// <param name="identifier">js method name</param>
        /// <param name="args">JSON serializable arguments to send to the js method</param>
        /// <returns></returns>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? new object[] { });
        }

        private string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        private T Deserialize<T>(string serializedObject)
        {
            if (serializedObject is null)
            {
                return default;
            }

            return Serializer.Deserialize<T>(serializedObject);
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, 
            CancellationToken cancellationToken, 
            object[] args)
        {
            var serializedArgs = Serialize(args);
            string resultObj;

            await EnsureInitialized();
                
            resultObj = await JSInvokeService.WorkerInvokeAsync<TValue>(identifier, serializedArgs);
            
            cancellationToken.ThrowIfCancellationRequested();

            var result = Deserialize<TValue>(resultObj);
            return result;
        }

        private static async Task EnsureInitialized()
        {
            if (!isJsInitialized && !JSInvokeService.IsObjectDefined("BlazorWorkerJSRuntimeSerializer"))
            {
                await JSInvokeService.ImportLocalScripts("_content/Tewr.BlazorWorker.Extensions.JSRuntime/BlazorWorkerJSRuntime.js");
                isJsInitialized = true;
            }
        }

        [JSExport]
        public static string InvokeMethod(string objectInstanceId, string argsString)
        {
#if DEBUG
            Console.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeMethod)}({objectInstanceId}, {argsString})");
#endif
            try
            {
                var obj = DotNetObjectReferenceTracker.GetObjectReference(long.Parse(objectInstanceId));
                var serializer = DotNetObjectReferenceTracker.GetCallbackJSRuntime(obj).Serializer;
                var callBackArgs = serializer.Deserialize<CallBackArgs>(argsString);
#if DEBUG
                var callBackArgsStr = serializer.Serialize(callBackArgs);
                Console.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeMethod)}: ({nameof(CallBackArgs)}) {callBackArgsStr}");
#endif

                var underlyingObject = obj.GetType().GetProperty("Value").GetValue(obj);
                var underlyingObjectType = underlyingObject.GetType();
                var methodCandidates = underlyingObjectType.GetMethods().Where(m => m.Name == callBackArgs.MethodName);
                var methodArgsList = callBackArgs.MethodArgs.Cast<JsonElement>().ToList();
                var typedMethodsArgsList = new List<object>();
                System.Reflection.MethodInfo method = null;
                var exceptions = new List<Exception>();
                foreach (var methodCandidate in methodCandidates)
                {
                    try
                    {
                        var candidateParams = methodCandidate.GetParameters().ToList();
                        if (methodArgsList.Count > candidateParams.Count)
                        {
                            // Too many arguments is not allowed
                            continue;
                        }

                        var candidateTypedMethodsArgsList =
                            candidateParams.Select((p, i) => methodArgsList[i].Deserialize(p.ParameterType)).ToList();

                        method = methodCandidate;
                        typedMethodsArgsList = candidateTypedMethodsArgsList;
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        // Swallow exceptions. method will remain null
                    }
                }

                if (method == null)
                {
                    var availableMethods = string.Join(", ", methodCandidates.Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType))})"));
                    throw new MissingMethodException($"Unable to find a method on {underlyingObjectType.FullName} " +
                        $"corresponding to {callBackArgs.MethodName}({argsString}). " +
                        $"Available methods with matching name: {availableMethods}", new AggregateException(exceptions));
                }
                var resultObj = method.Invoke(underlyingObject, typedMethodsArgsList.ToArray());
                if (resultObj is null)
                {
                    return null;
                }

                return serializer.Serialize(resultObj);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeMethod)}({objectInstanceId}, {argsString}) error: {e}");
                throw;
            }
        }

        public class CallBackArgs { 
            public string MethodName { get; set; }
            public object[] MethodArgs { get; set; }
        }
    }
}
