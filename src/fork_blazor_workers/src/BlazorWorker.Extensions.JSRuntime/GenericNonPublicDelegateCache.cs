using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace BlazorWorker.Extensions.JSRuntime
{

    public class GenericNonPublicDelegateCache : ConcurrentDictionary<Type, Delegate>
    {
        public GenericNonPublicDelegateCache(Type forType)
        {
            TargetType = forType;
        }

        public Type TargetType { get; }

        public TDelegate GetDelegate<TDelegate, T, TFirstArg>(object definingTypeInstance, string name) where TDelegate : Delegate
        {
            var method = this.GetOrAdd(typeof(T), firstGenericTypeArg => {

                var firstArgGenericDef = typeof(TFirstArg).GetGenericTypeDefinition();
                var methodInfo = TargetType.GetRuntimeMethods().FirstOrDefault(methodInfo =>
                    !methodInfo.IsPublic &&
                    methodInfo.Name == name &&
                    methodInfo.ContainsGenericParameters &&
                    AreGenericTypeEquals(methodInfo.GetParameters().FirstOrDefault()?.ParameterType, firstArgGenericDef));
                if (methodInfo == null) 
                {
                    throw new ArgumentException($"Unable to find non-public method {definingTypeInstance}.{name}<T>({firstArgGenericDef})");
                }

                var genericMethodInfo = methodInfo.MakeGenericMethod(typeof(T));

                return Delegate.CreateDelegate(typeof(TDelegate), definingTypeInstance, genericMethodInfo);
            });

            return (TDelegate)method;
        }


        private bool AreGenericTypeEquals(Type genericType1, Type genericType2)
        {
            return genericType1.Assembly == genericType2.Assembly && 
                genericType1.Namespace == genericType2.Namespace && 
                genericType1.Name == genericType2.Name;
        }
    }

   
}
