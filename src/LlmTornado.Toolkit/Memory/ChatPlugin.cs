using LlmTornado.Code;
using LlmTornado.Infra;
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
    public List<ToolParam>? Params { get; set; }

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
    
    public ChatFunction(string name, string description, List<ToolParam>? pars, Func<ChatFunctionInputParams, ToolkitChat, Task<ChatFunctionCallResult?>> callHandler)
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
    public ChatFunction(List<ToolParam>? pars, Func<ChatFunctionInputParams, ToolkitChat, Task<ChatFunctionCallResult?>> callHandler)
    {
        Name = "task";
        Description = "function which handles the task";
        Params = pars;
        CallHandler = callHandler;
    }
    
    public ChatFunction(string name, string description, List<ToolParam>? pars, Func<ChatFunctionInputParams, ToolkitChat, ChatFunctionCallResult?> callHandler)
    {
        Name = name;
        Description = description;
        Params = pars;
        SyncCallHandler = callHandler;
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

public enum ChatFunctionCallResultParameterErrors
{
    MissingRequiredParameter,
    RequiredParameterInvalidType,
    MalformedParam,
    Generic
}