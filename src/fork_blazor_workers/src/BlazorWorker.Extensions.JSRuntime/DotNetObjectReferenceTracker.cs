using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BlazorWorker.Extensions.JSRuntime
{
    public class DotNetObjectReferenceTracker
    {
        public static JsonEncodedText DotNetObjectRefKey = JsonEncodedText.Encode("__dotNetObject");

        private static readonly TrackerJsRuntime objectTracker = new TrackerJsRuntime();
        private static readonly ConditionalWeakTable<object, BlazorWorkerJSRuntime> jsRuntimeReferences =
            new ConditionalWeakTable<object, BlazorWorkerJSRuntime>();

        internal static DotNetObjectReference<T> GetObjectReference<T>(long dotNetObjectId) where T : class
        {
            return (DotNetObjectReference<T>)GetObjectReference(dotNetObjectId);
        }

        internal static object GetObjectReference(long dotNetObjectId)
        {
            return objectTracker.InternalGetObjectReference(dotNetObjectId);
        }

        internal static long TrackObjectReference<T>(DotNetObjectReference<T> value) where T : class
        {
            return objectTracker.InternalTrackObjectReference<T>(value);
        }

        public static void SetCallbackJSRuntime<T>(DotNetObjectReference<T> source, BlazorWorkerJSRuntime jsruntime) where T : class
        {
            jsRuntimeReferences.Add(source, jsruntime);
        }
        public static BlazorWorkerJSRuntime GetCallbackJSRuntime(object source)
        {
            return jsRuntimeReferences.GetValue(source, _ => (BlazorWorkerJSRuntime)null);
        }

        internal class TrackerJsRuntime : Microsoft.JSInterop.JSRuntime
        {
            private static readonly Type selfType = typeof(TrackerJsRuntime);

            public delegate long TrackObjectReferenceDelegate<T>(DotNetObjectReference<T> value) where T : class;
            public delegate object GetObjectReferenceDelegate(long dotNetObjectId);

            private static readonly MethodInfo GetObjectReferenceMethod;
            private static readonly GenericNonPublicDelegateCache TrackObjectReferenceDelegates =
                new GenericNonPublicDelegateCache(selfType);

            static TrackerJsRuntime()
            {
                var targetType = typeof(Microsoft.JSInterop.JSRuntime);
                var name = nameof(GetObjectReference);
                var firstArgType = typeof(long);
                var methodInfo = targetType.GetRuntimeMethods().FirstOrDefault(methodInfo =>
                    !methodInfo.IsPublic &&
                    methodInfo.Name == name &&
                    methodInfo.GetParameters().FirstOrDefault()?.ParameterType == firstArgType);

                if (methodInfo == null)
                {
                    throw new ArgumentException($"Unable to find non-public method {targetType}.{name}({firstArgType})");
                }

                GetObjectReferenceMethod = methodInfo;
            }

            // internal IDotNetObjectReference GetObjectReference(long dotNetObjectId)
            public object InternalGetObjectReference(long dotNetObjectId)
            {
                return GetObjectReferenceMethod.Invoke(this, new object[] { dotNetObjectId });
            }

            public long InternalTrackObjectReference<T>(DotNetObjectReference<T> value) where T : class
            {
                var method = TrackObjectReferenceDelegates
                    .GetDelegate<TrackObjectReferenceDelegate<T>, T, DotNetObjectReference<T>>
                        (this, nameof(DotNetObjectReferenceTracker.TrackObjectReference));

                return method.Invoke(value);
            }

            #region Unsupported methods

            protected override void BeginInvokeJS(long taskId, string identifier, string argsJson)
            {
                throw new NotSupportedException();
            }

            protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            {
                throw new NotSupportedException();
            }

#if NET5_0_OR_GREATER
            protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
            {
                throw new NotSupportedException();
            }
#endif

            #endregion
        }
    }
}
