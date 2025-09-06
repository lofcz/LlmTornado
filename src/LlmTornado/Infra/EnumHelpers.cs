using System;
using System.Diagnostics.CodeAnalysis;

namespace LlmTornado.Infra;

internal static class EnumHelpers
{
    public static bool TryParse(Type enumType, string? value, [NotNullWhen(true)] out object? result) =>
        TryParse(enumType, value, ignoreCase: false, out result);

#if !MODERN
    public static bool TryParse(Type enumType, string? value, bool ignoreCase, [NotNullWhen(true)] out object? result)
    {
        result = null;
        
        if (value is null)
        {
            return false;
        }

        try
        {
            result = Enum.Parse(enumType, value, ignoreCase);
            return Enum.IsDefined(enumType, result);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
#else
    public static bool TryParse(Type enumType, string? value, bool ignoreCase, [NotNullWhen(true)] out object? result)
    {
        return Enum.TryParse(enumType, value, ignoreCase, out result);
    }
#endif
    
    /// <summary>
    /// Helper to try to parse string into enum.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static bool TryParseEnum<T>(this string? input, [NotNullWhen(true)] out T? output) where T : struct
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type");
        }

        if (input is not null && TryParse(typeof(T), input, true, out object? result))
        {
            output = (T)result;
            return true;
        }
        
        output = null;
        return false;
    }

    public static T ParseEnum<T>(this string? input) where T : struct
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type");
        }

        if (input is not null && TryParse(typeof(T), input, true, out object? result))
        {
            return (T)result;
        }

        throw new ArgumentException($"Failed to parse '{input}' into enum of type {typeof(T).Name}");
    }
}