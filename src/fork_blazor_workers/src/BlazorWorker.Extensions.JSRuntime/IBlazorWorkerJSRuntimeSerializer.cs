using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.Extensions.JSRuntime
{
    public interface IBlazorWorkerJSRuntimeSerializer
    {
        public string Serialize(object obj);

        T Deserialize<T>(string serializedObject);
    }
}
