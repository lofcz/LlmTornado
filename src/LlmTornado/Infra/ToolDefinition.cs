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
    public string? Description { get; set; }

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
    
    /// <summary>
    /// Defines a function that can be called by the model.
    /// </summary>
    /// <param name="name">A name for the function, which must be unique within a single request. Should be composed of letters, digits, and underscores, with a maximum length of 40 characters.</param>
    /// <param name="description">A detailed description of the function's purpose and its parameters. The model uses this information to decide when and how to call the function.</param>
    public ToolDefinition(string name, string? description)
    {
        Name = name;
        Description = description;
        Params = null;
    }
    
    /// <summary>
    /// Defines a function with parameters that can be called by the model.
    /// </summary>
    /// <param name="name">A name for the function, which must be unique within a single request. Should be composed of letters, digits, and underscores, with a maximum length of 40 characters.</param>
    /// <param name="description">A detailed description of the function's purpose and its parameters. The model uses this information to decide when and how to call the function.</param>
    /// <param name="pars">A list of parameters that the function accepts.</param>
    public ToolDefinition(string name, string? description, List<ToolParam>? pars)
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
    /// Creates a tool call result that is discarded and not sent to the model. Useful for functions that have side effects but no return value for the model.
    /// </summary>
    public ToolCallResult()
    {

    }
    
    /// <summary>
    /// Creates a failed tool call result with a generic error message. Use this for general failures that are not specific to a single parameter.
    /// </summary>
    /// <param name="errorMessage">A descriptive message explaining the reason for the failure. This is sent to the model.</param>
    /// <param name="reason">The generic category of the error.</param>
    public ToolCallResult(string errorMessage, ToolCallResultParameterErrors reason)
    {
        Error = errorMessage;
        SerializeError();
    }
    
    /// <summary>
    /// Creates a failed tool call result due to a parameter error. Use this to indicate that the function call failed because a parameter was missing or had an invalid type.
    /// </summary>
    /// <param name="paramErrorKind">The type of parameter error.</param>
    /// <param name="paramName">The name of the parameter that caused the error.</param>
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
    /// Creates a successful tool call result with data to be sent back to the model. The model will use this data to generate its next response.
    /// </summary>
    /// <param name="data">A dictionary, anonymous object, or JSON-serializable class containing the results of the function call.</param>
    /// <param name="allowSuccessiveFunctionCalls">If true, the model may choose to call another function before generating a user-facing response.</param>
    public ToolCallResult(object? data, bool allowSuccessiveFunctionCalls = true)
    {
        BaseCtor(data, allowSuccessiveFunctionCalls);
    }
    
    /// <summary>
    /// Creates a successful tool call result, providing separate data for model feedback and for post-render processing. This is useful for passing structured data to a UI after the model's response is fully streamed, without exposing that data to the model.
    /// </summary>
    /// <param name="llmFeedbackData">Data to be sent to the model for generating the next response.</param>
    /// <param name="passtroughData">Data that will be stored in the chat message and made available after the message is fully streamed. This is not sent to the model.</param>
    /// <param name="allowSuccessiveFunctionCalls">If true, the model may choose to call another function before generating a user-facing response.</param>
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

    /// <summary>
    /// Defines a single parameter for a tool, combining its name and its schema definition.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The schema definition for the parameter (e.g., string, number, object).</param>
    public ToolParam(string name, IToolParamType type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Defines a single parameter for a tool with a primitive type (string, boolean, integer, or float).
    /// This constructor is a convenient way to create simple parameters without manually instantiating the specific `ToolParam` type classes.
    /// </summary>
    /// <param name="name">The name of the parameter, which will be used to identify it in the tool call.</param>
    /// <param name="description">A clear and concise description of the parameter's purpose, which helps the model understand how to use it.</param>
    /// <param name="type">The fundamental data type of the parameter, such as string, boolean, integer, or float.</param>
    /// <param name="required">Specifies whether the parameter must be included in the tool call. Defaults to `true`.</param>
    public ToolParam(string name, string? description, ToolParamAtomicTypes type, bool required = true)
    {
        Name = name;
        Type = type switch
        {
            ToolParamAtomicTypes.String => new ToolParamString(description, required),
            ToolParamAtomicTypes.Bool => new ToolParamBool(description, required),
            ToolParamAtomicTypes.Int => new ToolParamInt(description, required),
            ToolParamAtomicTypes.Float => new ToolParamNumber(description, required),
            _ => Type
        } ?? new ToolParamError(description, required);
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
    
    /// <summary>
    /// Defines a parameter that can be one of several different object types, forming a discriminated union.
    /// </summary>
    /// <param name="types">An array of possible types for the parameter.</param>
    public SchemaAnyOfAttribute(params Type[] types)
    {
        Types = types;
    }
}

/// <summary>
/// Allows providing specific names for the elements of a tuple when generating the JSON schema.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class SchemaTupleAttribute : Attribute
{
    public string[] Names { get; }

    /// <summary>
    /// Specifies custom names for the elements of a tuple parameter. This allows providing meaningful names instead of the default 'item_1', 'item_2', etc., in the generated JSON schema.
    /// </summary>
    /// <param name="names">An array of names corresponding to the tuple elements.</param>
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

/// <summary>
/// Represents a special parameter type that provides access to all raw tool arguments.
/// This parameter is not part of the generated JSON schema but is populated at runtime with the arguments provided by the model.
/// </summary>
public class ToolParamArguments : IToolParamType
{
    /// <summary>
    /// Gets the JSON schema type, which is an empty string for this parameter as it's not represented in the schema.
    /// </summary>
    public string Type => string.Empty;

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets the CLR type of this parameter, which is <see cref="ToolArguments"/>.
    /// </summary>
    [JsonIgnore]
    public Type? DataType { get; set; } = typeof(ToolArguments);
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    [JsonIgnore]
    public ToolParamSerializer Serializer { get; set; } = ToolParamSerializer.Undefined;

    /// <summary>
    /// This method returns null as this parameter is not included in the generated JSON schema.
    /// </summary>
    public object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return null!;
    }
}

public abstract class ToolParamTypeBase : IToolParamType
{
    public abstract string Type { get; }
    public string? Description { get; set; }
    public bool Required { get; set; } = true;
    
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

/// <summary>
/// Represents a string parameter for a tool.
/// </summary>
public class ToolParamString : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "string";
    
    /// <summary>
    /// A parameter that accepts a string value.
    /// </summary>
    /// <param name="description">The description of the parameter.</param>
    /// <param name="required">Whether the parameter is required.</param>
    public ToolParamString(string? description = null, bool required = true)
    {
        Description = description;
        Required = required;
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

/// <summary>
/// Represents an error parameter type. This is a special type that cannot be compiled and indicates a problem during schema generation.
/// </summary>
public class ToolParamError : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "";
 
    /// <summary>
    /// An error during schema generation. This is a special type that is not sent to the model but indicates a problem with the tool's definition.
    /// </summary>
    /// <param name="description">The description of the error.</param>
    /// <param name="required">Indicates if the parameter was required. Defaults to true.</param>
    public ToolParamError(string? description, bool required = true)
    {
        Description = description;
        Required = required;
    }

    /// <summary>
    /// Throws an exception as this type cannot be compiled.
    /// </summary>
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        throw new Exception("Error typ can't be compiled!");
    }
}

/// <summary>
/// Represents an integer parameter for a tool.
/// </summary>
public class ToolParamInt : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "integer";
    
    /// <summary>
    /// A parameter that accepts an integer value.
    /// </summary>
    /// <param name="description">A description of what the integer represents.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamInt(string? description, bool required = true)
    {
        Description = description;
        Required = required;
    }
}

/// <summary>
/// Represents a number parameter for a tool.
/// </summary>
public class ToolParamNumber : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "number";
    
    /// <summary>
    /// A parameter that accepts a numeric (floating-point) value.
    /// </summary>
    /// <param name="description">A description of what the number represents.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamNumber(string? description, bool required = true)
    {
        Description = description;
        Required = required;
    }
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        return new
        {
            type = Type,
            description = Description
        };
    }
}

/// <summary>
/// Represents a boolean parameter for a tool.
/// </summary>
public class ToolParamBool : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "boolean";
    
    /// <summary>
    /// A parameter that accepts a boolean (true/false) value.
    /// </summary>
    /// <param name="description">A description of what the boolean value represents.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamBool(string? description, bool required = true)
    {
        Description = description;
        Required = required;
    }
}

/// <summary>
/// Represents a parameter of any type, which will be serialized as a generic object.
/// </summary>
public class ToolParamAny : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "object"; // Placeholder, not used in Compile

    /// <summary>
    /// A parameter that can accept any JSON object. Use this when the structure of the parameter is dynamic or unknown.
    /// </summary>
    /// <param name="description">A description of what this parameter represents.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamAny(string? description, bool required = true)
    {
        Description = description;
        Required = required;
        Serializer = ToolParamSerializer.Any;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (Description is null) return new { };
        return new { description = Description };
    }
}

/// <summary>
/// Represents a date-time parameter, serialized as a string in 'date-time' format.
/// </summary>
public class ToolParamDateTime : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "string";

    /// <summary>
    /// Specifies the format of the string ('date-time').
    /// </summary>
    [JsonProperty("format")]
    public string Format => "date-time";

    /// <summary>
    /// A parameter that accepts a date and time, serialized as a string in the 'date-time' format according to RFC 3339.
    /// </summary>
    /// <param name="description">A description of the date-time value.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamDateTime(string? description, bool required = true)
    {
        Description = description;
        Required = required;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
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
/// <summary>
/// Represents a date parameter, serialized as a string in 'date' format.
/// </summary>
public class ToolParamDate : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "string";

    /// <summary>
    /// Specifies the format of the string ('date').
    /// </summary>
    [JsonProperty("format")]
    public string Format => "date";

    /// <summary>
    /// A parameter that accepts a date, serialized as a string in the 'date' format according to RFC 3339.
    /// </summary>
    /// <param name="description">A description of the date value.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamDate(string? description, bool required = true)
    {
        Description = description;
        Required = required;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents a time parameter, serialized as a string in 'time' format.
/// </summary>
public class ToolParamTime : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "string";

    /// <summary>
    /// Specifies the format of the string ('time').
    /// </summary>
    [JsonProperty("format")]
    public string Format => "time";

    /// <summary>
    /// A parameter that accepts a time, serialized as a string in the 'time' format according to RFC 3339.
    /// </summary>
    /// <param name="description">A description of the time value.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamTime(string? description, bool required = true)
    {
        Description = description;
        Required = required;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents a parameter that can be one of several types (discriminated union).
/// </summary>
public class ToolParamAnyOf : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "object";

    /// <summary>
    /// A list of possible types for this parameter.
    /// </summary>
    [JsonProperty("anyOf")]
    public List<IToolParamType> AnyOf { get; set; } = [];
    
    /// <summary>
    /// The list of possible CLR types, used for deserialization.
    /// </summary>
    [JsonIgnore]
    public List<Type> PossibleTypes { get; set; } = [];

    /// <summary>
    /// A parameter that can be one of several different object types, forming a discriminated union. This constructor initializes an empty container, which can be populated later.
    /// </summary>
    public ToolParamAnyOf()
    {
        
    }

    /// <summary>
    /// A parameter that can be one of several different object types, forming a discriminated union.
    /// </summary>
    /// <param name="anyOf">A list of <see cref="IToolParamType"/> representing the possible schemas for this parameter.</param>
    /// <param name="description">A description of what this parameter represents.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamAnyOf(List<IToolParamType> anyOf, string? description = null, bool required = true)
    {
        AnyOf = anyOf;
        Description = description;
        Required = required;
    }

    /// <summary>
    /// A parameter that can be one of several different object types, forming a discriminated union.
    /// </summary>
    /// <param name="anyOf">An array of <see cref="IToolParamType"/> representing the possible schemas for this parameter.</param>
    public ToolParamAnyOf(params IToolParamType[] anyOf)
    {
        AnyOf = anyOf.ToList();
    }
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents an enum parameter, serialized as a string with a predefined set of values.
/// </summary>
public class ToolParamEnum : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "string";
    
    /// <summary>
    /// The list of possible enum values.
    /// </summary>
    [JsonProperty("enum")]
    public List<string> Values { get; set; }

    /// <summary>
    /// A parameter that must be one of a predefined set of string values.
    /// </summary>
    /// <param name="description">A description of what this enum represents.</param>
    /// <param name="values">The list of allowed string values.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamEnum(string? description, List<string> values, bool required = true)
    {
        Description = description;
        Values = values;
        Required = required;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents a list parameter where each item is a string from an enumeration.
/// </summary>
public class ToolParamListEnum : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "array";

    /// <summary>
    /// The enumerable collection of possible string values for the items in the list.
    /// </summary>
    public IEnumerable<string> Values { get; set; }

    /// <summary>
    /// A list parameter where each item must be one of a predefined set of string values.
    /// </summary>
    /// <param name="description">A description of what the list represents.</param>
    /// <param name="values">The enumerable collection of allowed string values for each item.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamListEnum(string? description, IEnumerable<string> values, bool required = true)
    {
        Description = description;
        Values = values;
        Required = required;
    }
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents a list parameter containing atomic types (string, integer, number, boolean).
/// </summary>
public class ToolParamListAtomic : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "array";

    /// <summary>
    /// The atomic type of the items in the list.
    /// </summary>
    public ToolParamAtomicTypes ItemsType { get; set; }

    /// <summary>
    /// A list parameter containing simple, or "atomic," types (e.g., string, integer, boolean).
    /// </summary>
    /// <param name="description">A description of what the list represents.</param>
    /// <param name="type">The atomic type for all items in the list.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamListAtomic(string? description, ToolParamAtomicTypes type, bool required = true)
    {
        Description = description;
        ItemsType = type;
        Required = required;
    }
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override object Compile(ToolDefinition sourceFn, ToolMeta meta)
    {
        if (meta.RecursionLevel >= ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel}. This may be caused by a self-referencing type.");
        }
        
        meta.RecursionLevel++;

        try
        {
            string itemType = ItemsType switch
            {
                ToolParamAtomicTypes.Int => "integer",
                ToolParamAtomicTypes.Float => "number",
                ToolParamAtomicTypes.Bool => "boolean",
                ToolParamAtomicTypes.String => "string",
                _ => throw new Exception($"Please implement the type of the atomic {ItemsType} in ToolParamListAtomic.Compile")
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

/// <summary>
/// Represents a generic list parameter with items of a specified complex type.
/// </summary>
public class ToolParamList : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "array";

    /// <summary>
    /// The schema for the items in the list.
    /// </summary>
    public IToolParamType Items { get; }

    /// <summary>
    /// A list parameter where each item conforms to a specified schema.
    /// </summary>
    /// <param name="description">A description of what the list represents.</param>
    /// <param name="items">The schema that each item in the list must conform to.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamList(string? description, IToolParamType items, bool required = true)
    {
        Description = description;
        Items = items;
        Required = required;
    }
    
    public ToolParamList(string? description, List<ToolParam> objectProperties, bool required = true, string? objectDescription = null)
    {
        Description = description;
        Items = new ToolParamObject(objectDescription, objectProperties, required);
        Required = required;
    }
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents an object parameter with a defined set of properties.
/// </summary>
public class ToolParamObject : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "object";
    
    /// <summary>
    /// The list of properties for this object.
    /// </summary>
    [JsonProperty("properties")]
    public List<ToolParam> Properties { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether additional properties are allowed.
    /// </summary>
    public bool AllowAdditionalProperties { get; set; }
    
    /// <summary>
    /// Gets or sets any extra properties to be included in the schema.
    /// </summary>
    internal Dictionary<string, object>? ExtraProperties { get; set; }
    
    /// <summary>
    /// A parameter that is a structured object with a predefined set of properties.
    /// </summary>
    /// <param name="description">A description of the object.</param>
    /// <param name="properties">The list of properties that define the object's structure.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamObject(string? description, List<ToolParam> properties, bool required = true)
    {
        Description = description;
        Properties = properties;
        Required = required;
    }
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents a dictionary parameter, which is serialized as an array of key-value pairs.
/// </summary>
public class ToolParamDictionary : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "object";
    
    /// <summary>
    /// The schema for the values in the dictionary.
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

    /// <summary>
    /// A parameter that is a dictionary (or map), where keys are strings and values conform to a specified schema. Note: This is serialized as an array of key-value pair objects to be compatible with JSON schema.
    /// </summary>
    /// <param name="description">A description of the dictionary.</param>
    /// <param name="valueType">The schema that all values in the dictionary must conform to.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamDictionary(string? description, IToolParamType valueType, bool required = true)
    {
        Description = description;
        ValueType = valueType;
        Required = required;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
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

/// <summary>
/// Represents a tuple parameter, which is serialized as an object with named or indexed properties.
/// </summary>
public class ToolParamTuple : ToolParamTypeBase
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public override string Type => "object";
    
    /// <summary>
    /// The list of types for the items in the tuple.
    /// </summary>
    public List<IToolParamType> Items { get; }
    
    /// <summary>
    /// The optional list of names for the items in the tuple.
    /// </summary>
    public List<string>? Names { get; set; }

    /// <summary>
    /// A parameter that is a tuple—an ordered, fixed-size collection of elements that can have different types.
    /// </summary>
    /// <param name="description">A description of the tuple.</param>
    /// <param name="items">A list of schemas, one for each element in the tuple, defining its type.</param>
    /// <param name="required">Whether the parameter must be provided.</param>
    public ToolParamTuple(string? description, List<IToolParamType> items, bool required = true)
    {
        Description = description;
        Items = items;
        Required = required;
        Serializer = ToolParamSerializer.Tuple;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
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
