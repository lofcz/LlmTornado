using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using EnumsNET;
using Argon;

namespace LlmTornado.Code;

internal static class Extensions
{
    private static readonly ConcurrentDictionary<string, string?> DescriptionAttrCache = [];
    
    internal static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
    
    public static void AddOrUpdate<TK, TV>(this ConcurrentDictionary<TK, TV> dictionary, TK key, TV value) where TK : notnull
    {
        dictionary.AddOrUpdate(key, value, (k, v) => value);
    }
    
    public static string? GetDescription<T>(this T source)
    {
        if (source is null)
        {
            return null;
        }

        if (DescriptionAttrCache.TryGetValue($"{source.GetType()}_{source.ToString()}", out string? val))
        {
            return val;
        }
        
        FieldInfo? fi = source.GetType().GetField(source.ToString() ?? string.Empty);

        if (fi is null)
        {
            DescriptionAttrCache.TryAdd($"{source.GetType()}_{source.ToString()}", null);
            return null;
        }
        
        DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
        string? value = attributes is { Length: > 0 } ? attributes[0].Description : source.ToString();
        DescriptionAttrCache.TryAdd($"{source.GetType()}_{source.ToString()}", value);
        
        return value;
    }
    
    public static Dictionary<string, object?>? ToDictionary(this object obj)
    {       
        string json = JsonConvert.SerializeObject(obj);
        Dictionary<string, object?>? dictionary = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json);   
        return dictionary;
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
            if (nullableUnderlyingType.IsEnum && value != null)
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

        if (t == typeof(int) && value?.ToString() == "")
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

        if (value != null && t is { IsGenericType: true } && value.GetType().IsGenericType)
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

        return t != null ? System.Convert.ChangeType(value, t) : null;
    }
    
    private static readonly JsonSerializerSettings JsonSettingsIgnoreNulls = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
    
    public static string ToJson(this object? obj, bool prettify = false)
    {
        return obj is null ? "{}" : JsonConvert.SerializeObject(obj, prettify ? Formatting.Indented : Formatting.None, JsonSettingsIgnoreNulls);
    }
    
    public static void AddOrUpdate<TKey, TVal>(this Dictionary<TKey, TVal> dict, TKey key, TVal val)
    {
        dict[key] = val;
    }
}