using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;

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
    
    public static bool ContainsLineBreaks(this ReadOnlySpan<char> text) => text.IndexOfAny('\r', '\n') >= 0;
    
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
    
    private static readonly JsonSerializerSettings JsonSettingsIgnoreNulls = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
    
    public static string ToJson(this object? obj, bool prettify = false)
    {
        return obj is null ? "{}" : JsonConvert.SerializeObject(obj, prettify ? Formatting.Indented : Formatting.None, JsonSettingsIgnoreNulls);
    }
    
    public static T? JsonDecode<T>(this string? obj)
    {
        return obj is null ? default : JsonConvert.DeserializeObject<T>(obj);
    }
    
    public static void AddOrUpdate<TKey, TVal>(this Dictionary<TKey, TVal> dict, TKey key, TVal val)
    {
        dict[key] = val;
    }
}