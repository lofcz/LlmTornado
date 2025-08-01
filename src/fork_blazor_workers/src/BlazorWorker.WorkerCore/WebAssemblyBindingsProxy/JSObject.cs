using System;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    internal class JSObject : IDisposable
    {
        public delegate object InvokeDelegate(string method, params object[] parameters);
        public delegate void DisposeDelegate();
        private readonly InvokeDelegate _invokeMethodDelegate;
        private readonly DisposeDelegate _disposeMethodDelegate;

        public JSObject(object target)
        {
            var type = target.GetType();
            var invokeMethod = type.GetMethod(nameof(Invoke));
            var disposeMethod = type.GetMethod(nameof(Dispose));
            _invokeMethodDelegate = Delegate.CreateDelegate(typeof(InvokeDelegate), target, invokeMethod) as InvokeDelegate;
            _disposeMethodDelegate = Delegate.CreateDelegate(typeof(DisposeDelegate), target, disposeMethod) as DisposeDelegate;
        }   

        public object Invoke(string method, params object[] parameters) => _invokeMethodDelegate(method, parameters);

        public void Dispose() => _disposeMethodDelegate();
    }
}