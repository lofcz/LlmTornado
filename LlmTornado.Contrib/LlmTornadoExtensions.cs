using System.Collections;
using System.Diagnostics.CodeAnalysis;
using EnumsNET;
using LlmTornado.ChatFunctions;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Contrib;

public static class LlmTornadoExtensions
{
    /// <summary>
    ///     Attempts to get value of a given argument. If the argument is of incompatible type, <see cref="exception"/> is returned.
    /// </summary>
    /// <param name="call"></param>
    /// <param name="arg"></param>
    /// <param name="data"></param>
    /// <param name="exception"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryGetArgument<T>(this FunctionCall call, string arg, [NotNullWhen(returnValue: true)] out T? data, out Exception? exception)
    {
        return Get(call.ArgGetter.Value.Source, arg, out data, out exception);
    }
    
    /// <summary>
    ///     Attempts to get value of a given argument. If the argument is not found or is of incompatible type, null is returned.
    /// </summary>
    /// <param name="call"></param>
    /// <param name="arg"></param>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryGetArgument<T>(this FunctionCall call, string arg, [NotNullWhen(returnValue: true)] out T? data)
    {
        return Get(call.ArgGetter.Value.Source, arg, out data, out _);
    }
    
    internal static bool Get<T>(this Dictionary<string,object?>? args, string param, out T? data, out Exception? exception)
    {
        exception = null;
        
        if (args is null)
        {
            data = default;
            return false; 
        }
        
        if (!args.TryGetValue(param, out object? rawData))
        {
            data = default;
            return false;
        }

        if (rawData is T obj)
        {
            data = obj;
        }

        if (rawData is JArray jArr)
        {
            data = jArr.ToObject<T?>();
            return true;
        }

        try
        {
            data = (T?)rawData.ChangeType(typeof(T));
            return true;
        }
        catch (Exception e)
        {
            data = default;
            exception = e;
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
}