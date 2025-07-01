using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace LlmTornado.Toolkit;

internal static class Extensions
{
    internal static readonly ConcurrentDictionary<string, object?> Cache = [];
    
    public static bool CacheTryGetValue<TItem>(string key, out TItem? value)
    {
        if (Cache.TryGetValue(key, out object? result))
        {
            switch (result)
            {
                case null:
                {
                    value = default;
                    return true;
                }
                case TItem item:
                {
                    value = item;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
    
    public static TItem CacheSet<TItem>(string key, TItem value)
    {
        Cache[key] = value;
        return value;
    }
    
    public static List<Type> GetBaseTypes(this Type t)
    {
        List<Type> baseTypes = [];
            
        Type? baseType = t.BaseType;   
        while (baseType != null && baseType != typeof(object))
        {
            baseTypes.Add(baseType);    
            baseType = baseType.BaseType;   
        }

        return baseTypes;
    }
    
    public static Dictionary<string, object?> ComponentToDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
    {
        List<PropertyInfo> props = [];
            
        if (CacheTryGetValue($"reflectionComponentPi_{source.GetType()}", out List<PropertyInfo>? cachedProps))
        {
            props = cachedProps ?? [];
        }
        else
        {
            /*props = source.GetType().GetProperties(bindingAttr).Where(x => Attribute.IsDefined(x, typeof(ParameterAttribute))).ToList();

            List<Type> baseTypes = source.GetType().GetBaseTypes();

            foreach (Type baseType in baseTypes)
            {
                List<PropertyInfo> baseProps = baseType.GetProperties(bindingAttr).Where(x => Attribute.IsDefined(x, typeof(ParameterAttribute))).ToList();

                if (baseProps.Any())
                {
                    props.AddRange(baseProps);
                }
            }*/
            
            CacheSet($"reflectionComponentPi_{source.GetType()}", props);   
        }
            
        return props.ToDictionary
        (
            propInfo => propInfo.Name,
            propInfo => propInfo.GetValue(source, null)
        );
    }
    
    public static string? GetStringValue(this Enum? e)
    {
        return StringEnum.GetStringValue(e);
    }
    
    public static string? GetStringValue(this Enum e, string key)
    {
        return StringEnum.GetStringValue(e, key);
    }
    
    public static T? GetTypeValue<T>(this Enum e, string key)
    {
        return StringEnum.GetTypeValue<T>(e, key);
    }
        
    public static T? GetTypeValue<T>(this Enum e)
    {
        return StringEnum.GetTypeValue<T>(e);
    }
}