using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
}