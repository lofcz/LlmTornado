using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BlazorWorker.Core
{
    public static class MonoTypeHelper
    {
        public static MethodIdentifier GetStaticMethodId<T>(string method)
        {
            var owningType = typeof(T);
            if (!owningType.GetRuntimeMethods().Any(x => x.IsStatic && x.Name == method))
            {
                throw new ArgumentException($"Method '{method}' is not a static member of type {owningType.Name}", nameof(method));
            }
            return new MethodIdentifier
            {
                AssemblyName = owningType.Assembly.GetName().Name,
                FullMethodName = $"{owningType.FullName}.{method}"
            };
        }
    }
}
