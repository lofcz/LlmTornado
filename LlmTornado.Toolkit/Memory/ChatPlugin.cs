using LlmTornado.Code;
using LlmTornado.Toolkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ChatFunction
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
    public List<ChatFunctionParam>? Params { get; set; }

    /// <summary>
    /// The function dispatcher, invoked when the function is called by LLM
    /// </summary>
    public Func<ChatFunctionInputParams, ToolkitChat, Task<ChatFunctionCallResult?>>? CallHandler { get; set; }
    
    /// <summary>
    /// The function dispatcher, invoked when the function is called by LLM
    /// </summary>
    public Func<ChatFunctionInputParams, ToolkitChat, ChatFunctionCallResult?>? SyncCallHandler { get; set; }

    /// <summary>
    /// Whether strict JSON mode is enabled
    /// </summary>
    public bool Strict { get; set; } = true;
    
    public ChatFunction(string name, string description)
    {
        Name = name;
        Description = description;
        Params = null;
        CallHandler = null;
    }
    
    public ChatFunction(string name, string description, List<ChatFunctionParam>? pars, Func<ChatFunctionInputParams, ToolkitChat, Task<ChatFunctionCallResult?>> callHandler)
    {
        Name = name;
        Description = description;
        Params = pars;
        CallHandler = callHandler;
    }
    
    /// <summary>
    /// Use this ctor only for anonymous functions, only one anonymous function is supported at time
    /// </summary>
    /// <param name="pars"></param>
    /// <param name="callHandler"></param>
    public ChatFunction(List<ChatFunctionParam>? pars, Func<ChatFunctionInputParams, ToolkitChat, Task<ChatFunctionCallResult?>> callHandler)
    {
        Name = "ukol";
        Description = "funkce, která splní úkol";
        Params = pars;
        CallHandler = callHandler;
    }
    
    public ChatFunction(string name, string description, List<ChatFunctionParam>? pars, Func<ChatFunctionInputParams, ToolkitChat, ChatFunctionCallResult?> callHandler)
    {
        Name = name;
        Description = description;
        Params = pars;
        SyncCallHandler = callHandler;
    }
}

public enum ChatFunctionCallResultParameterErrors
{
    MissingRequiredParameter,
    RequiredParameterInvalidType,
    MalformedParam,
    Generic
}

public class ChatFunctionCallResult
{
    public bool Ok { get; private set; }
    public string? Error { get; private set; }
    public object? Result { get; private set; }
    public object? PostRenderData { get; private set; }
    public bool AllowSuccessiveFunctionCalls { get; private set;  }

    /// <summary>
    /// Use only when discarding the result
    /// </summary>
    public ChatFunctionCallResult()
    {

    }
    
    /// <summary>
    /// Function call cancelled or failed. This is the most general constructor, use specialized overloads if applicable. Include a descriptive reason why, LLM uses this
    /// </summary>
    /// <param name="errorMessage"></param>
    public ChatFunctionCallResult(string errorMessage, ChatFunctionCallResultParameterErrors reason)
    {
        Error = errorMessage;
        SerializeError();
    }
    
    /// <summary>
    /// Function call cancelled. Include a descriptive reason why, LLM uses this
    /// </summary>
    /// <param name="paramErrorKind"></param>
    /// <param name="paramName"></param>
    public ChatFunctionCallResult(ChatFunctionCallResultParameterErrors paramErrorKind, string paramName)
    {
        Error = paramErrorKind switch
        {
            ChatFunctionCallResultParameterErrors.MissingRequiredParameter => $"No function called. Missing required parameter '{paramName}'.",
            ChatFunctionCallResultParameterErrors.RequiredParameterInvalidType => $"No function called. Required parameter '{paramName}' has invalid type.",
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
    public ChatFunctionCallResult(object? data, bool allowSuccessiveFunctionCalls = true)
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
    public ChatFunctionCallResult(object? llmFeedbackData, object? passtroughData, bool allowSuccessiveFunctionCalls = true)
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

public class ChatFunctionInputParams
{
    private readonly Dictionary<string, object?>? source;

    public ChatFunctionInputParams(Dictionary<string, object?>? pars)
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



public class ChatFunctionParam
{
    /// <summary>
    /// A descriptive name of the param, LLM uses this
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Type of the parameter, will be converted into JSON schema object
    /// </summary>
    public IChatFunctionType Type { get; set; }

    public ChatFunctionParam(string name, IChatFunctionType type)
    {
        Name = name;
        Type = type;
    }

    public ChatFunctionParam(string name, string description, bool required, ChatFunctionAtomicParamTypes type)
    {
        Name = name;
        Type = type switch
        {
            ChatFunctionAtomicParamTypes.Bool => new ChatFunctionTypeBool(description, required),
            ChatFunctionAtomicParamTypes.Float => new ChatFunctionTypeNumber(description, required),
            ChatFunctionAtomicParamTypes.Int => new ChatFunctionTypeInt(description, required),
            ChatFunctionAtomicParamTypes.String => new ChatFunctionTypeString(description, required),
            _ => new ChatFunctionTypeError(name, required)
        };
    }
}

public interface IChatFunctionType
{
    [JsonProperty("type")]
    public string Type { get; }
    public string? Description { get; set; }
    public bool Required { get; set; }

    public object Compile(ChatFunction sourceFn, ToolkitChat? refChat);
}

public abstract class ChatFunctionTypeBase : IChatFunctionType
{
    public abstract string Type { get; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public virtual object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
    {
        return new
        {
            type = Type,
            description = Description
        };
    }
}

public class ChatFunctionTypeString : ChatFunctionTypeBase
{
    public override string Type => "string";
 
    public ChatFunctionTypeString(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}

public class ChatFunctionTypeError : ChatFunctionTypeBase
{
    public override string Type => "";
 
    public ChatFunctionTypeError(string description, bool required)
    {
        Description = description;
        Required = required;
    }

    public override object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
    {
        throw new Exception("Error typ není možné zkompilovat!");
    }
}

public class ChatFunctionTypeInt : ChatFunctionTypeBase
{
    public override string Type => "integer";
    
    public ChatFunctionTypeInt(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}

public class ChatFunctionTypeNumber : ChatFunctionTypeBase
{
    public override string Type => "number";
    
    public ChatFunctionTypeNumber(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}

public class ChatFunctionTypeBool : ChatFunctionTypeBase
{
    public override string Type => "boolean";
    
    public ChatFunctionTypeBool(string description, bool required)
    {
        Description = description;
        Required = required;
    }
}

public class ChatFunctionTypeEnum : ChatFunctionTypeBase
{
    public override string Type => "string";
    
    [JsonProperty("enum")]
    public List<string> EnumValues { get; set; }

    public ChatFunctionTypeEnum(string description, bool required, IEnumerable<string> enumVales)
    {
        Description = description;
        Required = required;
        EnumValues = enumVales.ToList();
    }

    public override object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
    {
        return new { type = Type, description = Description, @enum = EnumValues };
    }
}

public enum ChatFunctionAtomicParamTypes
{
    [StringValue("string")]
    String,
    [StringValue("integer")]
    Int,
    [StringValue("float")]
    Float,
    [StringValue("boolean")]
    Bool
}

public class ChatFunctionTypeListItems
{
    public ChatFunctionAtomicParamTypes Type { get; set; }
}

public class ChatFunctionTypeListTypedEnum : ChatFunctionTypeBase
{
    public override string Type => "array";
    public IEnumerable<string> Items { get; set; }

    public ChatFunctionTypeListTypedEnum(string description, bool required, IEnumerable<string> values)
    {
        Description = description;
        Required = required;
        Items = values;
    }
    
    public override object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
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

public class ChatFunctionTypeListTypedAtomic : ChatFunctionTypeBase
{
    public override string Type => "array";
    public ChatFunctionTypeListItems Items { get; set; }

    public ChatFunctionTypeListTypedAtomic(string description, bool required, ChatFunctionAtomicParamTypes listType)
    {
        Description = description;
        Required = required;
        Items = new ChatFunctionTypeListItems { Type = listType };
    }
    
    public override object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
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

public class ChatFunctionTypeListTypedObject : ChatFunctionTypeBase
{
    public override string Type => "array";
    public ChatFunctionTypeObject Items { get; set; }

    public ChatFunctionTypeListTypedObject(string description, bool required, ChatFunctionTypeObject listType)
    {
        Description = description;
        Required = required;
        Items = listType;
    }
    
    public ChatFunctionTypeListTypedObject(string description, bool required, List<ChatFunctionParam> properties)
    {
        Description = description;
        Required = required;
        Items = new ChatFunctionTypeObject(properties);
    }
    
    public override object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
    {
        return new
        {
            type = Type,
            description = Description,
            items = Items.Compile(sourceFn, refChat)
        };
    }
}

public class ChatFunctionTypeObject : ChatFunctionTypeBase
{
    public override string Type => "object";
    public List<ChatFunctionParam> Properties { get; set; }
    
    public ChatFunctionTypeObject(List<ChatFunctionParam> properties)
    {
        Properties = properties;
    }
    
    public ChatFunctionTypeObject(string description, List<ChatFunctionParam> properties)
    {
        Description = description;
        Properties = properties;
    }

    public override object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
    {
        SerializedObject so = new SerializedObject
        {
            Type = "object",
            Description = Description,
            Properties = []
        };

        foreach (ChatFunctionParam prop in Properties)
        {
            if (prop.Type.Required || sourceFn.Strict) // in strict mode all props all required
            {
                so.Required ??= [];
                so.Required.Add(prop.Name);
            }
            
            so.Properties.AddOrUpdate(prop.Name, prop.Type.Compile(sourceFn, refChat));
        }

        if (sourceFn.Strict)
        {
            if (refChat?.Cfg.Model.Provider is LLmProviders.Google)
            {
                goto ret;
            }
            
            so.Properties.AddOrUpdate("additionalProperties", false);
        }

        ret:
        return so;
    }
}

public class ChatFunctionTypeStringExt : ChatFunctionTypeBase
{
    public override string Type => "string";
    [JsonProperty("maxLength")]
    public int MaxLength { get; set; }
    [JsonProperty("minLength")]
    public int MinLength { get; set; }
    
    public override object Compile(ChatFunction sourceFn, ToolkitChat? refChat)
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


public class ChatPluginExportResult
{
    public List<ChatFunction> Functions { get; set; }

    public ChatPluginExportResult(List<ChatFunction> functions)
    {
        Functions = functions;
    }
}

public interface IChatPlugin
{
    /// <summary>
    /// A unique vendor namespace to avoid collisions between function symbols cross plugins. Max 20 characters
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// A list o
    /// </summary>
    /// <returns></returns>
    public Task<ChatPluginExportResult> Export();
    
    ChatFunctionCallResult MissingParam(string name)
    {
        return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MissingRequiredParameter, name);
    }
    
    ChatFunctionCallResult MalformedParam(string name)
    {
        return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MalformedParam, name);
    }
}