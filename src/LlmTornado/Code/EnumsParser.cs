using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
#if MODERN
using System.ComponentModel.DataAnnotations;
#endif

internal enum EnumFormat
{
    DecimalValue = 0,
    HexadecimalValue = 1,
    UnderlyingValue = 2,
    Name = 3,
    Description = 4,
    EnumMemberValue = 5,
    DisplayName = 6
}

internal class EnumCache
{
    public struct EnumMemberInfo
    {
        public string Name;
        public object Value;
        public string? Description;
        public string? EnumMemberValue;
        public string? DisplayName;
    }

    public readonly Type EnumType;
    public readonly bool IsFlags;
    public readonly Type UnderlyingType;
    public readonly Dictionary<string, EnumMemberInfo> NameLookup;
    public readonly Dictionary<string, EnumMemberInfo> DescriptionLookup;
    public readonly Dictionary<string, EnumMemberInfo> EnumMemberValueLookup;
    public readonly Dictionary<string, EnumMemberInfo> DisplayNameLookup;
    public readonly Dictionary<object, EnumMemberInfo> ValueLookup;
    public readonly List<EnumMemberInfo> Members;

    public EnumCache(Type enumType)
    {
        EnumType = enumType;
        IsFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;
        UnderlyingType = Enum.GetUnderlyingType(enumType);
        NameLookup = new(StringComparer.Ordinal);
        DescriptionLookup = new(StringComparer.Ordinal);
        EnumMemberValueLookup = new(StringComparer.Ordinal);
        DisplayNameLookup = new(StringComparer.Ordinal);
        ValueLookup = new();
        Members = [];

        foreach (FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string name = field.Name;
            object value = field.GetValue(null)!;
            string? description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
            string? enumMemberValue = field.GetCustomAttribute<EnumMemberAttribute>()?.Value;
            string? displayName = null;
#if MODERN
            displayName = field.GetCustomAttribute<DisplayAttribute>()?.Name;
#endif
            EnumMemberInfo info = new EnumMemberInfo
            {
                Name = name,
                Value = value,
                Description = description,
                EnumMemberValue = enumMemberValue,
                DisplayName = displayName
            };
            NameLookup[name] = info;
            if (!string.IsNullOrEmpty(description))
                DescriptionLookup[description] = info;
            if (!string.IsNullOrEmpty(enumMemberValue))
                EnumMemberValueLookup[enumMemberValue] = info;
            if (!string.IsNullOrEmpty(displayName))
                DisplayNameLookup[displayName] = info;
            ValueLookup[value] = info;
            Members.Add(info);
        }
    }
}

internal static class EnumsParser
{
    private static readonly ConcurrentDictionary<Type, EnumCache> _cache = new();

    private static EnumCache GetCache(Type enumType)
    {
        if (!enumType.IsEnum) throw new ArgumentException("Type must be an enum.", nameof(enumType));
        return _cache.GetOrAdd(enumType, t => new EnumCache(t));
    }

    public static bool TryParse<TEnum>(string? value, out TEnum result, bool ignoreCase = false, params EnumFormat[]? formats) where TEnum : struct, Enum
    {
        EnumCache cache = GetCache(typeof(TEnum));
        if (TryParseInternal(cache, value, ignoreCase, out object? boxed, formats))
        {
            result = (TEnum)boxed!;
            return true;
        }
        result = default;
        return false;
    }

    public static bool TryParse(Type enumType, string? value, bool ignoreCase, out object? result, params EnumFormat[]? formats)
    {
        EnumCache cache = GetCache(enumType);
        return TryParseInternal(cache, value, ignoreCase, out result, formats);
    }

    private static bool TryParseInternal(EnumCache cache, string? value, bool ignoreCase, out object? result, EnumFormat[]? formats)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        value = value.Trim();
        EnumFormat[] formatList = (formats == null || formats.Length == 0)
            ? [EnumFormat.Name, EnumFormat.DecimalValue, EnumFormat.HexadecimalValue, EnumFormat.UnderlyingValue, EnumFormat.Description, EnumFormat.EnumMemberValue, EnumFormat.DisplayName]
            : formats;
        
        if (cache.IsFlags && value.Contains(","))
        {
            string[] parts = value.Split(',');
            long combined = 0;
            foreach (string part in parts)
            {
                if (!TryParseInternal(cache, part.Trim(), ignoreCase, out object? partResult, formats))
                    return false;
                combined |= Convert.ToInt64(partResult, CultureInfo.InvariantCulture);
            }
            result = Enum.ToObject(cache.EnumType, combined);
            return true;
        }

        foreach (EnumFormat format in formatList)
        {
            switch (format)
            {
                case EnumFormat.Name:
                    if (TryLookup(cache.NameLookup, value, ignoreCase, out result)) return true;
                    break;
                case EnumFormat.Description:
                    if (TryLookup(cache.DescriptionLookup, value, ignoreCase, out result)) return true;
                    break;
                case EnumFormat.EnumMemberValue:
                    if (TryLookup(cache.EnumMemberValueLookup, value, ignoreCase, out result)) return true;
                    break;
                case EnumFormat.DisplayName:
                    if (TryLookup(cache.DisplayNameLookup, value, ignoreCase, out result)) return true;
                    break;
                case EnumFormat.DecimalValue:
                case EnumFormat.UnderlyingValue:
                    if (TryParseNumber(value, cache.UnderlyingType, out object num))
                    {
                        if (cache.ValueLookup.TryGetValue(num, out EnumCache.EnumMemberInfo info))
                        {
                            result = info.Value;
                            return true;
                        }
                        
                        result = Enum.ToObject(cache.EnumType, num);
                        return true;
                    }
                    break;
                case EnumFormat.HexadecimalValue:
                    if (TryParseHex(value, cache.UnderlyingType, out object hex))
                    {
                        if (cache.ValueLookup.TryGetValue(hex, out EnumCache.EnumMemberInfo info))
                        {
                            result = info.Value;
                            return true;
                        }
                        result = Enum.ToObject(cache.EnumType, hex);
                        return true;
                    }
                    break;
            }
        }
        return false;
    }

    private static bool TryLookup(Dictionary<string, EnumCache.EnumMemberInfo> dict, string value, bool ignoreCase, out object? result)
    {
        if (ignoreCase)
        {
            foreach (KeyValuePair<string, EnumCache.EnumMemberInfo> kv in dict)
            {
                if (string.Equals(kv.Key, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = kv.Value.Value;
                    return true;
                }
            }
        }
        else
        {
            if (dict.TryGetValue(value, out EnumCache.EnumMemberInfo info))
            {
                result = info.Value;
                return true;
            }
        }
        result = null;
        return false;
    }

    private static bool TryParseNumber(string value, Type underlyingType, out object num)
    {
        try
        {
            if (underlyingType == typeof(byte)) num = byte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(sbyte)) num = sbyte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(short)) num = short.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(ushort)) num = ushort.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(int)) num = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(uint)) num = uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(long)) num = long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(ulong)) num = ulong.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            else throw new NotSupportedException();
            return true;
        }
        catch { num = null!; return false; }
    }

    private static bool TryParseHex(string value, Type underlyingType, out object num)
    {
        value = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value.Substring(2) : value;
        try
        {
            if (underlyingType == typeof(byte)) num = byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(sbyte)) num = sbyte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(short)) num = short.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(ushort)) num = ushort.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(int)) num = int.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(uint)) num = uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(long)) num = long.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else if (underlyingType == typeof(ulong)) num = ulong.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            else throw new NotSupportedException();
            return true;
        }
        catch { num = null!; return false; }
    }
} 