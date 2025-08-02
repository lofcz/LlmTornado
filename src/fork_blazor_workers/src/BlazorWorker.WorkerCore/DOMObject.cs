using System;
using BlazorWorker.WorkerCore.WebAssemblyBindingsProxy;


namespace BlazorWorker.WorkerCore
{

    // Serves as a wrapper around a JSObject.
    class DOMObject : IDisposable
    {
        public static DOMObject Self { get; } = new DOMObject("self");

        public JSObject ManagedJSObject { get; private set; }

        public DOMObject(JSObject jsobject)
        {
            ManagedJSObject = jsobject ?? throw new ArgumentNullException(nameof(jsobject));
        }

        public DOMObject(string globalName) : this(new JSObject(Runtime.GetGlobalObject(globalName)))
        { }

        public object Invoke(string method, params object[] args)
        {
            return ManagedJSObject.Invoke(method, args);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {

            if (disposing)
            {

                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            ManagedJSObject?.Dispose();
            ManagedJSObject = null;
        }

    }
}
