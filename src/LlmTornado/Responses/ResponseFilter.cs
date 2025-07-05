using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Base class for file search filters
/// </summary>
[JsonConverter(typeof(ResponseFilterConverter))]
public abstract class ResponseFilter
{
}

/// <summary>
/// A filter used to compare a specified attribute key to a given value using a defined comparison operation
/// </summary>
public class ResponseComparisonFilter : ResponseFilter
{
    /// <summary>
    /// The key to compare against the value
    /// </summary>
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Specifies the comparison operator: eq, ne, gt, gte, lt, lte
    /// </summary>
    [JsonProperty("type")]
    public ResponseComparisonOperator Type { get; set; }

    /// <summary>
    /// The value to compare against the attribute key; supports string, number, or boolean types
    /// </summary>
    [JsonProperty("value")]
    public object Value { get; set; } = string.Empty;
}

/// <summary>
/// Combine multiple filters using 'and' or 'or'
/// </summary>
public class ResponseCompoundFilter : ResponseFilter
{
    /// <summary>
    /// Array of filters to combine. Items can be ResponseComparisonFilter or ResponseCompoundFilter
    /// </summary>
    [JsonProperty("filters")]
    public List<ResponseFilter> Filters { get; set; } = new List<ResponseFilter>();

    /// <summary>
    /// Type of operation: 'and' or 'or'
    /// </summary>
    [JsonProperty("type")]
    public ResponseCompoundOperator Type { get; set; }
}

/// <summary>
/// Comparison operators for filters
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseComparisonOperator
{
    /// <summary>
    /// equals
    /// </summary>
    [EnumMember(Value = "eq")]
    Equals,

    /// <summary>
    /// not equal
    /// </summary>
    [EnumMember(Value = "ne")]
    NotEqual,

    /// <summary>
    /// greater than
    /// </summary>
    [EnumMember(Value = "gt")]
    GreaterThan,

    /// <summary>
    /// greater than or equal
    /// </summary>
    [EnumMember(Value = "gte")]
    GreaterThanOrEqual,

    /// <summary>
    /// less than
    /// </summary>
    [EnumMember(Value = "lt")]
    LessThan,

    /// <summary>
    /// less than or equal
    /// </summary>
    [EnumMember(Value = "lte")]
    LessThanOrEqual
}

/// <summary>
/// Compound operators for filters
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseCompoundOperator
{
    /// <summary>
    /// All filters must match
    /// </summary>
    [EnumMember(Value = "and")]
    And,

    /// <summary>
    /// Any filter must match
    /// </summary>
    [EnumMember(Value = "or")]
    Or
}

/// <summary>
/// Custom converter for polymorphic deserialization of filters
/// </summary>
public class ResponseFilterConverter : JsonConverter<ResponseFilter>
{
    public override ResponseFilter? ReadJson(JsonReader reader, Type objectType, ResponseFilter? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JToken token = JToken.ReadFrom(reader);
        
        // Check if this is a compound filter (has "filters" property) or comparison filter (has "key" property)
        if (token["filters"] != null)
        {
            return token.ToObject<ResponseCompoundFilter>(serializer);
        }
        else if (token["key"] != null)
        {
            return token.ToObject<ResponseComparisonFilter>(serializer);
        }
        else
        {
            throw new JsonSerializationException("Unable to determine filter type from JSON structure");
        }
    }

    public override void WriteJson(JsonWriter writer, ResponseFilter? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        JToken token = JToken.FromObject(value, serializer);
        token.WriteTo(writer);
    }
}