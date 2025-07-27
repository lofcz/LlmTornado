using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Infra;

/// <summary>
/// Agentic tool.
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// A name of the function, no longer than 40 characters
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// A description passed to LLM. This need to explain the function as much as possible, as the LLM decided when to call the function based on this information
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Leave as null if arity = 0
    /// </summary>
    public List<ToolParam>? Params { get; set; }

    /// <summary>
    /// Whether strict JSON mode is enabled
    /// </summary>
    public bool Strict { get; set; } = true;
    
    internal ToolDefinition()
    {

    }
    
    public ToolDefinition(string name, string description)
    {
        Name = name;
        Description = description;
        Params = null;
    }
    
    public ToolDefinition(string name, string description, List<ToolParam>? pars)
    {
        Name = name;
        Description = description;
        Params = pars;
    }
}

internal static class ToolDefaults
{
    public static readonly List<string> DiscriminatorKeys = ["type", "_type", "discriminator_type", "discriminator"];
}

public enum ToolCallResultParameterErrors
{
    MissingRequiredParameter,
    RequiredParameterInvalidType,
    MalformedParam,
    Generic
}

public class ToolCallResult
{
    public bool Ok { get; private set; }
    public string? Error { get; private set; }
    public object? Result { get; private set; }
    public object? PostRenderData { get; private set; }
    public bool AllowSuccessiveFunctionCalls { get; private set;  }

    /// <summary>
    /// Use only when discarding the result
    /// </summary>
    public ToolCallResult()
    {

    }
    
    /// <summary>
    /// Function call cancelled or failed. This is the most general constructor, use specialized overloads if applicable. Include a descriptive reason why, LLM uses this
    /// </summary>
    /// <param name="errorMessage"></param>
    public ToolCallResult(string errorMessage, ToolCallResultParameterErrors reason)
    {
        Error = errorMessage;
        SerializeError();
    }
    
    /// <summary>
    /// Function call cancelled. Include a descriptive reason why, LLM uses this
    /// </summary>
    /// <param name="paramErrorKind"></param>
    /// <param name="paramName"></param>
    public ToolCallResult(ToolCallResultParameterErrors paramErrorKind, string paramName)
    {
        Error = paramErrorKind switch
        {
            ToolCallResultParameterErrors.MissingRequiredParameter => $"No function called. Missing required parameter '{paramName}'.",
            ToolCallResultParameterErrors.RequiredParameterInvalidType => $"No function called. Required parameter '{paramName}' has invalid type.",
            _ => Error
        };

        SerializeError();
    }

    /// <summary>
    /// Function call succeeded. LLM will use <see cref="data"/> to generate the next message<br/>
    /// Note this function automatically adds key "result": "ok" to the <see cref="data"/> if it's not found
    /// </summary>
    /// <param name="data">Dictionary / anonymous object / JSON serializable class</param>
    /// <param name="allowSuccessiveFunctionCalls">If true, LLM may decide to call this or any other available function again before passing control to the user</param>
    public ToolCallResult(object? data, bool allowSuccessiveFunctionCalls = true)
    {
        BaseCtor(data, allowSuccessiveFunctionCalls);
    }
    
    /// <summary>
    /// Function call succeeded. LLM will use <see cref="data"/> to generate the next message<br/>
    /// Note this function automatically adds key "result": "ok" to the <see cref="data"/> if it's not found
    /// </summary>
    /// <param name="llmFeedbackData">Dictionary / anonymous object / JSON serializable class</param>
    /// <param name="passtroughData">This data will be stored in the chat message and are available after the messages is fully streamed for rendering</param>
    /// <param name="allowSuccessiveFunctionCalls">If true, LLM may decide to call this or any other available function again before passing control to the user</param>
    public ToolCallResult(object? llmFeedbackData, object? passtroughData, bool allowSuccessiveFunctionCalls = true)
    {
        llmFeedbackData ??= new
        {
            result = "ok"
        };
        
        BaseCtor(llmFeedbackData, allowSuccessiveFunctionCalls);
        PostRenderData = passtroughData;
    }

    private void BaseCtor(object? data, bool allowSuccessiveFunctionCalls = true)
    {
        Result = data;
        Ok = true;
        AllowSuccessiveFunctionCalls = allowSuccessiveFunctionCalls;

        if (Result != null)
        {
            Dictionary<string, object?> dict = Result.ComponentToDictionary();
            dict.AddOrUpdate("result", "ok");
            Result = data;
        }
    }

    private void SerializeError()
    {
        Result = new
        {
            result = "error",
            message = Error
        };
    }
}

public class ToolInputParams
{
    private readonly Dictionary<string, object?>? source;

    public ToolInputParams(Dictionary<string, object?>? pars)
    {
        source = pars;
    }
    
    public bool ParamTryGet<T>(string paramName, out T? val)
    {
        if (!Get(paramName, out T? paramValue, out Exception? e))
        {
            val = default;
            return false;
        }

        val = paramValue;
        return true;
    }

    public bool Get<T>(string param, out T? data, out Exception? exception)
    {
        exception = null;
        
        if (source is null || !source.TryGetValue(param, out object? rawData))
        {
            data = default;
            return false; 
        }

        if (rawData is T obj)
        {
            data = obj;
            return true;
        }

        switch (rawData)
        {
            case JArray jArr:
            {
                data = jArr.ToObject<T?>();
                return true;
            }
            case JObject jObj:
            {
                data = jObj.ToObject<T?>();
                return true;
            }
            case string str:
            {
                if (typeof(T).IsClass || (typeof(T).IsValueType && !typeof(T).IsPrimitive && !typeof(T).IsEnum))
                {
                    if (str.SanitizeJsonTrailingComma().CaptureJsonDecode(out T? decoded, out Exception? parseException))
                    {
                        data = decoded;
                        return true;
                    }
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
            default:
            {
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
        }
    }
}

/// <summary>
/// Tool parameter.
/// </summary>
public class ToolParam
{
    /// <summary>
    /// A descriptive name of the param, LLM uses this
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Type of the parameter, will be converted into JSON schema object
    /// </summary>
    public IToolParamType Type { get; set; }

    public ToolParam(string name, IToolParamType type)
    {
        Name = name;
        Type = type;
    }
}

/// <summary>
/// JSON schema serializers.
/// </summary>
public enum ToolParamSerializer
{
    /// <summary>
    /// Used for custom parameters, no attached behaviour.
    /// </summary>
    Undefined,
    
    /// <summary>
    /// Special type for accessing arguments as a dictionary.
    /// </summary>
    Arguments,
    
    /// <summary>
    /// IDictionary.
    /// </summary>
    Dictionary,
    
    /// <summary>
    /// ISet.
    /// </summary>
    Set,
    
    /// <summary>
    /// Rank > 1 array.
    /// </summary>
    MultidimensionalArray,
    
    /// <summary>
    /// Rank 1 array.
    /// </summary>
    Array,
    
    /// <summary>
    /// IEnumerable, IList..
    /// </summary>
    NonGenericEnumerable,
    
    /// <summary>
    /// Object with any content.
    /// </summary>
    Object,
    
    /// <summary>
    /// Primitives like string, number, etc.
    /// </summary>
    Atomic,
    
    /// <summary>
    /// Object with any content.
    /// </summary>
    Any,
    
    /// <summary>
    /// Discriminated union.
    /// </summary>
    AnyOf,
    
    /// <summary>
    /// Nullable wrapper.
    /// </summary>
    Nullable,
    
    /// <summary>
    /// JSON types.
    /// </summary>
    Json,
    
    /// <summary>
    /// Tuple.
    /// </summary>
    Tuple,

    /// <summary>
    /// Awaitable.
    /// </summary>
    Awaitable
}

[AttributeUsage(AttributeTargets.Parameter)]
public class SchemaNullableAttribute : Attribute
{
    
}

/// <summary>
/// Allows controlling anyOf JSON schema generation.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class SchemaAnyOfAttribute : Attribute
{
    public Type[] Types { get; }
    
    public SchemaAnyOfAttribute(params Type[] types)
    {
        Types = types;
    }
}

/// <summary>
/// Allows controlling JSON schema generation of tuples.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class SchemaTupleAttribute : Attribute
{
    public string[] Names { get; }

    public SchemaTupleAttribute(params string[] names)
    {
        Names = names;
    }
}

public class ToolMeta
{
    public IEndpointProvider Provider { get; set; }
    internal int RecursionLevel { get; set; }
    internal const int MaxRecursionLevel = 64;
}

/// <summary>
/// Tool param.
/// </summary>
public interface IToolParamType
{
    /// <summary>
    /// Type of the param from JSON schema perspective.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; }
    
    /// <summary>
    /// Description forwarded to the LLM.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether the param is required.
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// The CLR type of this parameter, used for deserialization.
    /// </summary>
    [JsonIgnore]
    public Type? DataType { get; set; }
    
    /// <summary>
    /// The cached serializer for this parameter, used for fast deserialization.
    /// </summary>
    [JsonIgnore]
    public ToolParamSerializer Serializer { get; set; }

    /// <summary>
    /// Compiles the param into JSON schema.
    /// </summary>
    public object Compile(ToolDefinition sourceFn, ToolMeta meta);
}

public class ToolParamArguments : IToolParamType
{
    public string Type { get; } = string.Empty;
    public string? Description { get; set; }
    public bool Required { get; set; }

    [JsonIgnore]
    public Type? DataType { get; set; } = typeof(ToolArguments);
    
    [JsonIgnore]
    public ToolParamSerializer Serializer { get; set; } = ToolParamSerializer.Undefined;

    public object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return null!;
    }
}

public abstract class ToolParamTypeBase : IToolParamType
{
    public abstract string Type { get; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IToolParamType.DataType"/>
    /// </summary>
    [JsonIgnore]
    public Type? DataType { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IToolParamType.Serializer"/>
    /// </summary>
    [JsonIgnore]
    public ToolParamSerializer Serializer { get; set; }
    
    public virtual object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type,
            description = Description
        };
    }
}

public class ToolParamString : ToolParamTypeBase
{
    public override string Type => "string";
    
    public ToolParamString(string? description)
    {
        Description = description;
    }
    
    /// <summary>
    /// The format of the string.
    /// </summary>
    [JsonProperty("format")]
    public string? Format { get; set; }

    /// <summary>
    /// The minimum length of the string.
    /// </summary>
    [JsonProperty("minLength")]
    public int? MinLength { get; set; }

    /// <summary>
    /// The maximum length of the string.
    /// </summary>
    [JsonProperty("maxLength")]
    public int? MaxLength { get; set; }
}

public class ToolParamError : ToolParamTypeBase
{
    public override string Type => "";
 
    public ToolParamError(string? description)
    {
        Description = description;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        throw new Exception("Error typ can't be compiled!");
    }
}

public class ToolParamInt : ToolParamTypeBase
{
    public override string Type => "integer";
    
    public ToolParamInt(string? description)
    {
        Description = description;
    }
}

public class ToolParamNumber : ToolParamTypeBase
{
    public override string Type => "number";
    
    public ToolParamNumber(string? description)
    {
        Description = description;
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type,
            description = Description
        };
    }
}

public class ToolParamBool : ToolParamTypeBase
{
    public override string Type => "boolean";
    
    public ToolParamBool(string? description)
    {
        Description = description;
    }
}

public class ToolParamAny : ToolParamTypeBase
{
    public override string Type => "object"; // Placeholder, not used in Compile

    public ToolParamAny(string? description)
    {
        Description = description;
        Serializer = ToolParamSerializer.Any;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (Description is null) return new { };
        return new { description = Description };
    }
}

public class ToolParamDateTime : ToolParamTypeBase
{
    public override string Type => "string";

    [JsonProperty("format")]
    public string Format => "date-time";

    public ToolParamDateTime(string? description)
    {
        Description = description;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type,
            description = Description,
            format = Format
        };
    }
}

#if MODERN
public class ToolParamDate : ToolParamTypeBase
{
    public override string Type => "string";

    [JsonProperty("format")]
    public string Format => "date";

    public ToolParamDate(string? description)
    {
        Description = description;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type,
            description = Description,
            format = Format
        };
    }
}

public class ToolParamTime : ToolParamTypeBase
{
    public override string Type => "string";

    [JsonProperty("format")]
    public string Format => "time";

    public ToolParamTime(string? description)
    {
        Description = description;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type,
            description = Description,
            format = Format
        };
    }
}
#endif

public class ToolParamAnyOf : ToolParamTypeBase
{
    public override string Type => "object";

    [JsonProperty("anyOf")]
    public List<IToolParamType> AnyOf { get; set; } = [];
    
    [JsonIgnore]
    public List<Type> PossibleTypes { get; set; } = [];

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }

        meta.RecursionLevel++;

        try
        {
            return new
            {
                anyOf = AnyOf.Select(x => x.Compile(sourceFn, meta)).ToList()
            };
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class ToolParamNullable : IToolParamType
{
    public IToolParamType InnerType { get; }

    public ToolParamNullable(IToolParamType innerType)
    {
        InnerType = innerType;
    }

    public string Type => InnerType.Type;
    public string? Description { get => InnerType.Description; set => InnerType.Description = value; }
    public bool Required { get => InnerType.Required; set => InnerType.Required = value; }
    public Type? DataType { get => InnerType.DataType; set => InnerType.DataType = value; }
    public ToolParamSerializer Serializer { get; set; }

    public object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }

        meta.RecursionLevel++;

        try
        {
            object compiledInner = InnerType.Compile(sourceFn, meta);
        
            string json = JsonConvert.SerializeObject(compiledInner, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Dictionary<string, object>? schemaDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (schemaDict is not null && schemaDict.TryGetValue("type", out object? typeValue) && typeValue is string typeString)
            {
                schemaDict["type"] = new[] { typeString, "null" };
                return schemaDict;
            }
        
            return new
            {
                anyOf = new object[]
                {
                    new { type = "null" },
                    compiledInner
                }
            };
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class ToolParamAwaitable : IToolParamType
{
    public IToolParamType InnerParam { get; }

    public ToolParamAwaitable(IToolParamType innerParam)
    {
        InnerParam = innerParam;
        Serializer = ToolParamSerializer.Awaitable;
    }
    
    public string Type => InnerParam.Type;
    public string? Description { get => InnerParam.Description; set => InnerParam.Description = value; }
    public bool Required { get => InnerParam.Required; set => InnerParam.Required = value; }
    
    [JsonIgnore]
    public Type? DataType { get; set; }

    [JsonIgnore]
    public ToolParamSerializer Serializer { get; set; }

    public object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }
        
        meta.RecursionLevel++;

        try
        {
            return InnerParam.Compile(sourceFn, meta);
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class ToolParamEnum : ToolParamTypeBase
{
    public override string Type => "string";
    
    [JsonProperty("enum")]
    public List<string> Values { get; }

    public ToolParamEnum(string? description, List<string> values)
    {
        Description = description;
        Values = values;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }
        
        meta.RecursionLevel++;

        try
        {
            return new
            {
                type = Type, 
                description = Description,
                @enum = Values
            };
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public enum ToolParamAtomicTypes
{
    /// <summary>
    /// String.
    /// </summary>
    [StringValue("string")]
    String,
    
    /// <summary>
    /// Integer.
    /// </summary>
    [StringValue("integer")]
    Int,
    
    /// <summary>
    /// Float.
    /// </summary>
    [StringValue("float")]
    Float,
    
    /// <summary>
    /// Boolean.
    /// </summary>
    [StringValue("boolean")]
    Bool
}

public class ToolParamListItems
{
    public ToolParamAtomicTypes Type { get; set; }
}

public class ToolParamListEnum : ToolParamTypeBase
{
    public override string Type => "array";
    public IEnumerable<string> Values { get; set; }

    public ToolParamListEnum(string? description, IEnumerable<string> values)
    {
        Description = description;
        Values = values;
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }
        
        meta.RecursionLevel++;

        try
        {
            return new
            {
                type = Type, 
                description = Description, 
                items = new
                {
                    type = "string",
                    @enum = Values
                }
            };
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class ToolParamListAtomic : ToolParamTypeBase
{
    public override string Type => "array";
    public ToolParamAtomicTypes Items { get; set; }

    public ToolParamListAtomic(string? description, ToolParamAtomicTypes items)
    {
        Description = description;
        Items = items;
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }
        
        meta.RecursionLevel++;

        try
        {
            string itemType = Items switch
            {
                ToolParamAtomicTypes.Int => "integer",
                ToolParamAtomicTypes.Float => "number",
                ToolParamAtomicTypes.Bool => "boolean",
                ToolParamAtomicTypes.String => "string",
                _ => throw new Exception($"Please implement the type of the atomic {Items} in ToolParamListAtomic.Compile")
            };
        
            return new
            {
                type = Type,
                description = Description,
                items = new
                {
                    type = itemType
                }
            };
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class ToolParamList : ToolParamTypeBase
{
    public override string Type => "array";
    public IToolParamType Items { get; }

    public ToolParamList(string? description, IToolParamType items)
    {
        Description = description;
        Items = items;
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }
        
        meta.RecursionLevel++;

        try
        {
            return new
            {
                type = Type,
                description = Description,
                items = Items.Compile(sourceFn, meta)
            };
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class ToolParamObject : ToolParamTypeBase
{
    public override string Type => "object";
    
    [JsonProperty("properties")]
    public List<ToolParam> Properties { get; set; }
    
    public bool AllowAdditionalProperties { get; set; }
    
    internal Dictionary<string, object>? ExtraProperties { get; set; }
    
    public ToolParamObject(string? description, List<ToolParam> properties)
    {
        Description = description;
        Properties = properties;
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }

        meta.RecursionLevel++;

        try
        {
            JsonSchemaSerializedObject so = new JsonSchemaSerializedObject
            {
                Type = "object",
                Description = Description,
                Properties = new Dictionary<string, object>()
            };

            foreach (ToolParam prop in Properties)
            {
                if (prop.Type is ToolParamArguments)
                {
                    continue;
                }
            
                if (prop.Type.Required || sourceFn.Strict) // in strict mode all props all required
                {
                    so.Required ??= [];
                    so.Required.Add(prop.Name);
                }
            
                so.Properties.AddOrUpdate(prop.Name, prop.Type.Compile(sourceFn, meta));
            }
        
            if (ExtraProperties is not null)
            {
                foreach (KeyValuePair<string, object> extra in ExtraProperties)
                {
                    so.Properties.AddOrUpdate(extra.Key, extra.Value);
                    so.Required?.Add(extra.Key);
                }

            }

            if (sourceFn.Strict && !AllowAdditionalProperties)
            {
                if (meta.Provider.Provider is LLmProviders.Google)
                {
                    return so;
                }
            
                so.AdditionalProperties = false;
            }

            return so;
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class ToolParamDictionary : ToolParamTypeBase
{
    public override string Type => "object";
    
    /// <summary>
    /// Values.
    /// </summary>
    public IToolParamType ValueType { get; set; }
    
    /// <summary>
    /// Minimum number of key-value pairs required in the dictionary.
    /// </summary>
    public int? MinProperties { get; set; }
    
    /// <summary>
    /// Maximum number of key-value pairs allowed in the dictionary.
    /// </summary>
    public int? MaxProperties { get; set; }

    public ToolParamDictionary(string? description, IToolParamType valueType)
    {
        Description = description;
        ValueType = valueType;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }
        
        meta.RecursionLevel++;

        try
        {
            ToolParamString keyParam = new ToolParamString("The key of the dictionary item.");

            ToolParamObject itemsSchema = new ToolParamObject("A key-value pair for the dictionary.", [
                new ToolParam("key", keyParam) { Type = { Required = true } },
                new ToolParam("value", ValueType) { Type = { Required = true } }
            ]);
        
            ToolParamList arraySchema = new ToolParamList(Description, itemsSchema)
            {
                DataType = DataType
            };
        
            return arraySchema.Compile(sourceFn, meta);
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}

public class TornadoPluginExportResult
{
    public List<ToolDefinition> Functions { get; set; }

    public TornadoPluginExportResult(List<ToolDefinition> functions)
    {
        Functions = functions;
    }
}

public interface ITornadoPlugin
{
    /// <summary>
    /// A unique vendor namespace to avoid collisions between function symbols cross plugins. Max 20 characters
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// A list o
    /// </summary>
    /// <returns></returns>
    public Task<TornadoPluginExportResult> Export();
    
#if MODERN
    ToolCallResult MissingParam(string name)
    {
        return new ToolCallResult(ToolCallResultParameterErrors.MissingRequiredParameter, name);
    }
    
    ToolCallResult MalformedParam(string name)
    {
        return new ToolCallResult(ToolCallResultParameterErrors.MalformedParam, name);
    }
#endif
}

internal class JsonSchemaSerializedObject
{
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, object>? Properties { get; set; }

    [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Required { get; set; }

    [JsonProperty("additionalProperties", NullValueHandling = NullValueHandling.Ignore)]
    public object? AdditionalProperties { get; set; }
    
    [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
    public object? Items { get; set; }
    
    [JsonProperty("prefixItems", NullValueHandling = NullValueHandling.Ignore)]
    public object? PrefixItems { get; set; }
    
    [JsonProperty("minItems", NullValueHandling = NullValueHandling.Ignore)]
    public int? MinItems { get; set; }
    
    [JsonProperty("maxItems", NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxItems { get; set; }
    
    [JsonProperty("enum", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Enum { get; set; }
    
    [JsonProperty("anyOf", NullValueHandling = NullValueHandling.Ignore)]
    public object? AnyOf { get; set; }
    
    [JsonProperty("maxProperties", NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxProperties { get; set; }
    
    [JsonProperty("minProperties", NullValueHandling = NullValueHandling.Ignore)]
    public int? MinProperties { get; set; }

    public Dictionary<string, object> ToDictionary()
    {
        string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json)!;
    }
}

public class ToolParamTuple : ToolParamTypeBase
{
    public override string Type => "object";
    public List<IToolParamType> Items { get; }
    public List<string>? Names { get; set; }

    public ToolParamTuple(string? description, List<IToolParamType> items)
    {
        Description = description;
        Items = items;
        Serializer = ToolParamSerializer.Tuple;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }

        meta.RecursionLevel++;

        try
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            List<string> required = [];

            for (int i = 0; i < Items.Count; i++)
            {
                string key = Names?.Count == Items.Count ? Names[i] : $"item_{i + 1}";
                properties.Add(key, Items[i].Compile(sourceFn, meta));
                required.Add(key);
            }
        
            return new
            {
                type = Type,
                description = Description,
                properties,
                required
            };
        }
        finally
        {
            meta.RecursionLevel--;
        }
    }
}