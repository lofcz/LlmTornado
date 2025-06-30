using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EnumsNET;
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
    
    public static string SanitizeJsonTrailingComma(this string json)
    {
        string originalJson = json;
        string trimmedStart = json.TrimStart();
            
        if (trimmedStart.StartsWith('{') || trimmedStart.StartsWith('['))
        {
            json = trimmedStart;
        }

        json = json.TrimEnd();
            
        if (json.EndsWith(','))
        {
            json = json.TrimEnd(',');
        }
            
        return json != originalJson ? json : originalJson;
    }
    
    public static bool CaptureJsonDecode<T>(this string? s, out T? data, out Exception? parseException)
    {
        if (string.IsNullOrEmpty(s))
        {
            data = default;
            parseException = null;
            return true;
        }

        try
        {
            data = JsonConvert.DeserializeObject<T>(s);
            parseException = null;
            return true;
        }
        catch (Exception e)
        {
            data = default;
            parseException = e;
            return false;
        }
    }
    
    public static object? ChangeType(this object? value, Type conversion)
    {
        Type? t = conversion;

        if (t.IsEnum && value != null)
        {
            if (Enums.TryParse(t, value.ToString(), true, out object? x))
            {
                return x;
            }
        }
        
        Type? nullableUnderlyingType = Nullable.GetUnderlyingType(t);
        
        if (nullableUnderlyingType is not null && value is not null)
        {
            if (nullableUnderlyingType.IsEnum)
            {
                if (Enums.TryParse(nullableUnderlyingType, value.ToString(), true, out object? x))
                {
                    return x;
                }
            }
        }
        
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        if (t == typeof(int) && value?.ToString() == string.Empty)
        {
            return 0;
        }

        if (t == typeof(int) && ((value?.ToString()?.Contains('.') ?? false) || (value?.ToString()?.Contains(',') ?? false)))
        {
            if (double.TryParse(value.ToString()?.Replace(",", "."), out double x))
            {
                return (int)x;
            }
        }

        if (value is not null && t is { IsGenericType: true } && value.GetType().IsGenericType)
        {
            Type destT = t.GetGenericArguments()[0];
            Type sourceT = value.GetType().GetGenericArguments()[0];

            if (destT.IsEnum && sourceT == typeof(int))
            {
                IList? instance = (IList?)Activator.CreateInstance(t);

                foreach (object? x in (IList)value)
                {
                    instance?.Add(x);
                }

                return instance;
            }
        }

        return t is not null ? Convert.ChangeType(value, t) : null;
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