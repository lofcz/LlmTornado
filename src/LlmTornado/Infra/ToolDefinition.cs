using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public ToolParam(string name, string description, ToolParamAtomicTypes type)
    {
        Name = name;
        Type = type switch
        {
            ToolParamAtomicTypes.Bool => new ToolParamBool(description),
            ToolParamAtomicTypes.Float => new ToolParamNumber(description),
            ToolParamAtomicTypes.Int => new ToolParamInt(description),
            ToolParamAtomicTypes.String => new ToolParamString(description),
            _ => new ToolParamError(name)
        };
    }
}

public enum ToolParamSerializer
{
    Undefined,
    Arguments,
    Dictionary,
    Set,
    MultidimensionalArray,
    Array,
    NonGenericEnumerable,
    Object,
    Atomic,
    Any,
    AnyOf,
    Nullable
}

[AttributeUsage(AttributeTargets.Parameter)]
public class SchemaNullableAttribute : Attribute
{
    
}

[AttributeUsage(AttributeTargets.Parameter)]
public class SchemaAnyOfAttribute : Attribute
{
    public Type[] Types { get; }
    
    public SchemaAnyOfAttribute(params Type[] types)
    {
        Types = types;
    }
}

public class ToolMeta
{
    public IEndpointProvider Provider { get; set; }
}

public interface IToolParamType
{
    [JsonProperty("type")]
    public string Type { get; }
    public string? Description { get; set; }
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

    public object Compile(ToolDefinition sourceFn, ToolMeta meta);
}

public class ToolParamArguments : IToolParamType
{
    public string Type { get; }
    public string? Description { get; set; }
    public bool Required { get; set; }

    [JsonIgnore]
    public Type? DataType { get; set; } = typeof(ToolArguments);
    
    [JsonIgnore]
    public ToolParamSerializer Serializer { get; set; } = ToolParamSerializer.Arguments;

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
        throw new Exception("Error typ není možné zkompilovat!");
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
    public override string Type => "object";

    public ToolParamAny(string? description)
    {
        Description = description;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            description = Description
        };
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

public class ToolParamAnyOf : ToolParamTypeBase
{
    public override string Type => "object";

    [JsonProperty("anyOf")]
    public List<IToolParamType> AnyOf { get; set; } = [];
    
    [JsonIgnore]
    public List<Type> PossibleTypes { get; set; } = [];

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            anyOf = AnyOf.Select(x => x.Compile(sourceFn, meta)).ToList()
        };
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
        return new
        {
            anyOf = new object[]
            {
                new { type = "null" },
                InnerType.Compile(sourceFn, meta)
            }
        };
    }
}

public class ToolParamEnum : ToolParamTypeBase
{
    public override string Type => "string";
    
    [JsonProperty("enum")]
    public List<string> EnumValues { get; set; }

    public ToolParamEnum(string? description, IEnumerable<string> enumVales)
    {
        Description = description;
        EnumValues = enumVales.ToList();
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type, 
            description = Description,
            @enum = EnumValues
        };
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
    public IEnumerable<string> Items { get; set; }

    public ToolParamListEnum(string? description, IEnumerable<string> values)
    {
        Description = description;
        Items = values;
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type, 
            description = Description, 
            items = new
            {
                type = "string",
                @enum = Items
            }
        };
    }
}

public class ToolParamListAtomic : ToolParamTypeBase
{
    public override string Type => "array";
    public ToolParamListItems Items { get; set; }

    public ToolParamListAtomic(string? description, ToolParamAtomicTypes listType)
    {
        Description = description;
        Items = new ToolParamListItems
        {
            Type = listType
        };
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new 
        { 
            type = Type, 
            description = Description, 
            items = new
            {
                type = Items.Type.GetStringValue()
            } 
        };
    }
}

public class ToolParamList : ToolParamTypeBase
{
    public override string Type => "array";
    public IToolParamType Items { get; set; }

    public ToolParamList(string? description, IToolParamType list)
    {
        Description = description;
        Items = list;
    }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type,
            description = Description,
            items = Items.Compile(sourceFn, meta)
        };
    }
}

public class ToolParamObject : ToolParamTypeBase
{
    public override string Type => "object";
    public List<ToolParam> Properties { get; set; }
    
    internal Dictionary<string, object>? ExtraProperties { get; set; }
    
    public ToolParamObject(List<ToolParam> properties)
    {
        Properties = properties;
    }
    
    public ToolParamObject(string description, List<ToolParam> properties)
    {
        Description = description;
        Properties = properties;
    }

    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        JsonSchemaSerializedObject so = new JsonSchemaSerializedObject
        {
            Type = "object",
            Description = Description,
            Properties = []
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
            foreach (var extra in ExtraProperties)
            {
                so.Properties.AddOrUpdate(extra.Key, extra.Value);
                so.Required?.Add(extra.Key);
            }
        }

        if (sourceFn.Strict)
        {
            if (meta.Provider.Provider is LLmProviders.Google)
            {
                goto ret;
            }
            
            so.AdditionalProperties = false;
        }

        ret:
        return so;
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
        if (sourceFn.Strict)
        {
            ToolParamObject keyValueObject = new ToolParamObject(null, [
                new ToolParam("key", new ToolParamString(null)),
                new ToolParam("value", ValueType)
            ]);
            
            ToolParamList listType = new ToolParamList(Description, keyValueObject);
            return listType.Compile(sourceFn, meta);
        }
        
        JsonSchemaSerializedObject result = new JsonSchemaSerializedObject
        {
            Type = Type,
            Description = Description,
            AdditionalProperties = ValueType.Compile(sourceFn, meta)
        };

        if (MaxProperties is not null)
        {
            Dictionary<string, object> dict = result.ToDictionary();
            dict["maxProperties"] = MaxProperties.Value;
            return dict;
        }
        
        if (MinProperties is not null)
        {
            Dictionary<string, object> dict = result.ToDictionary();
            dict["minProperties"] = MinProperties.Value;
            return dict;
        }

        return result;
    }
}

public class ToolParamStringExt : ToolParamTypeBase
{
    public override string Type => "string";
    [JsonProperty("maxLength")]
    public int MaxLength { get; set; }
    [JsonProperty("minLength")]
    public int MinLength { get; set; }
    
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        // strict mode doesn't support min/max lenght yet
        if (sourceFn.Strict)
        {
            return new
            {
                type = Type
            };
        }
        
        return new
        {
            type = Type,
            minLength = MinLength,
            maxLength = MaxLength
        };
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
    [JsonProperty("type")]
    public string? Type { get; set; }
    
    [JsonProperty("properties")]
    public Dictionary<string, object>? Properties { get; set; }
    
    [JsonProperty("required")]
    public List<string>? Required { get; set; }
    
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    [JsonProperty("additionalProperties")]
    public object? AdditionalProperties { get; set; }
    
    public Dictionary<string, object> ToDictionary()
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        
        if (Type != null)
            dict["type"] = Type;
            
        if (Properties != null)
            dict["properties"] = Properties;
            
        if (Required != null)
            dict["required"] = Required;
            
        if (!string.IsNullOrEmpty(Description))
            dict["description"] = Description;
            
        if (AdditionalProperties != null)
            dict["additionalProperties"] = AdditionalProperties;
            
        return dict;
    }
}