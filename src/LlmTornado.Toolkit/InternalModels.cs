using System.Collections.Concurrent;
using System.Reflection;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Toolkit;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
internal class StringValueAttribute : Attribute
{
    public StringValueAttribute(string? value, string key = "")
    {
        Value = value;
        Key = key;
    }

    public StringValueAttribute(object? value, string key = "")
    {
        Value = value?.ToString();
        Key = key;
    }

    public string? Value { get; }
    public string? Key { get; }
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
internal class TypeValueAttribute : Attribute
{
    public TypeValueAttribute(object value, string key)
    {
        Value = value;
        Key = key;
    }

    public TypeValueAttribute(object value)
    {
        Value = value;
    }

    public object Value { get; }
    public string? Key { get; }
}

internal class SerializedObject
{
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("properties")]
    public Dictionary<string, object>? Properties { get; set; }
    [JsonProperty("required")]
    public List<string>? Required { get; set; }
    [JsonProperty("description")]
    public string? Description { get; set; }
}

internal class StringEnum
{
    private static readonly ConcurrentDictionary<Enum, string?> StringValues = [];
    private static readonly ConcurrentDictionary<Enum, ConcurrentDictionary<string, string?>> StringValuesWithKeys = [];
    private static readonly ConcurrentDictionary<Enum, object?> TypeValues = [];
    private static readonly ConcurrentDictionary<Enum, ConcurrentDictionary<string, object?>> TypeValuesWithKeys = [];
    
    public static T? GetTypeValue<T>(Enum? value, string? key = null)
    {
        if (value is null)
        {
            return default;
        }

        T? output = default;
        Type type = value.GetType();
        MemberInfo? fi;

        if (key.IsNullOrWhiteSpace())
        {
            if (TypeValues.TryGetValue(value, out object? value2))
            {
                if (value2 is T tVal)
                {
                    return tVal;
                }

                return default;
            }

            fi = type.GetField(value.ToString());
            fi ??= type.GetProperty(value.ToString());
            
            if (fi?.GetCustomAttributes(typeof(TypeValueAttribute), false) is not TypeValueAttribute[] { Length: > 0 } attrs)
            {
                return default;
            }

            if (TypeValues.ContainsKey(value))
            {
                return default;
            }
            
            object? s = attrs[0].Value;

            if (s is not null)
            {
                TypeValues.TryAdd(value, s);
                object obj = attrs[0].Value;

                if (obj is T tVal)
                {
                    return tVal;
                }
            }
            
            return output;
        }

        if (TypeValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, object?>? withKey))
        {
            if (withKey.TryGetValue(key, out object? value2))
            {
                if (value2 is T tVal)
                {
                    return tVal;
                }

                return default;
            }
        }

        fi = type.GetField(value.ToString());
        
        if (fi?.GetCustomAttributes(typeof(TypeValueAttribute), false) is not TypeValueAttribute[] { Length: > 0 } attrs2)
        {
            return default;
        }
  
        TypeValueAttribute? attr = attrs2.FirstOrDefault(x => x.Key == key);
        
        if (attr is null)
        {
            return default;
        }

        try
        {
            if (key is not null)
            {
                if (TypeValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, object?>? value2))
                {
                    value2.TryAdd(key, attr.Value);
                }
                else
                {
                    ConcurrentDictionary<string, object?> cd = [];
                    cd.TryAdd(key, attr.Value);
                    TypeValuesWithKeys.TryAdd(value, cd);
                }
            }

            if (attr.Value is T tVal2)
            {
                return tVal2;
            }

            return output;
        }
        catch (Exception e)
        {
            return default;
        }
    }

    public static string? GetStringValue(Enum? value, string? key = null)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string? output = null;
        Type type = value.GetType();
        MemberInfo? fi;

        if (key.IsNullOrWhiteSpace())
        {
            if (StringValues.TryGetValue(value, out string? value2))
            {
                return value2;
            }

            fi = type.GetField(value.ToString());
            fi ??= type.GetProperty(value.ToString());
            
            try
            {
                if (fi?.GetCustomAttributes(typeof(StringValueAttribute), false) is not StringValueAttribute[] { Length: > 0 } attrs || StringValues.ContainsKey(value))
                {
                    StringValues.TryAdd(value, null);
                    return null;
                }

                string? s = attrs[0].Value;

                if (s is not null)
                {
                    StringValues.TryAdd(value, s);
                    output = attrs[0].Value;
                }

                return output;
            }
            catch (Exception e)
            {
                
            }
        }

        if (StringValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, string?>? withKey))
        {
            if (withKey.TryGetValue(key ?? string.Empty, out string? value2))
            {
                return value2;
            }
        }

        fi = type.GetField(value.ToString());
        if (fi?.GetCustomAttributes(typeof(StringValueAttribute), false) is not StringValueAttribute[] { Length: > 0 } attrs2)
        {
            return null;
        }
        
        StringValueAttribute? attr = attrs2.FirstOrDefault(x => x.Key == key);
        
        if (attr is null)
        {
            return null;
        }

        try
        {
            if (key is not null)
            {
                if (StringValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, string?>? value2))
                {
                    value2.TryAdd(key, attr.Value);
                }
                else
                {
                    ConcurrentDictionary<string, string?> cd = [];
                    cd.TryAdd(key, attr.Value);
                    StringValuesWithKeys.TryAdd(value, cd);
                }
            }

            output = attr.Value;
            return output;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}