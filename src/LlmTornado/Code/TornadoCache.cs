using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace LlmTornado.Code;

internal static class TornadoCache
{
    public static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, bool>> ClrTypeAssignable = [];

    public static bool IsClrTypeAssignable(Type type, Type from)
    {
        if (ClrTypeAssignable.TryGetValue(type, out ConcurrentDictionary<Type, bool>? fromAssignable))
        {
            if (fromAssignable.TryGetValue(from, out bool b))
            {
                return b;
            }
        }
        
        bool assignable = type.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo());

        if (ClrTypeAssignable.TryGetValue(type, out ConcurrentDictionary<Type, bool>? dict))
        {
            dict.AddOrUpdate(from, assignable);
            return assignable;
        }

        ConcurrentDictionary<Type, bool> newDict = [];
        newDict.TryAdd(from, assignable);
        ClrTypeAssignable.TryAdd(from, newDict);

        return assignable;
    }
}