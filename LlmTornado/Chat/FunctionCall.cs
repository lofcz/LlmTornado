using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LlmTornado.Code;
using LlmTornado.Common;
using Argon;

namespace LlmTornado.ChatFunctions;

/// <summary>
///     An optional class to be used with models that support returning function calls.
/// </summary>
public class FunctionCall
{
    [JsonIgnore] 
    private Lazy<ChatFunctionParamsGetter> argGetter;

    [JsonIgnore] 
    private Lazy<Dictionary<string, object?>?> decodedArguments;

    public FunctionCall()
    {
        argGetter = new Lazy<ChatFunctionParamsGetter>(() => new ChatFunctionParamsGetter(decodedArguments?.Value));
        decodedArguments = new Lazy<Dictionary<string, object?>?>(() => Arguments.IsNullOrWhiteSpace() ? [] : JsonConvert.DeserializeObject<Dictionary<string, object?>>(Arguments));
    }
    
    [JsonIgnore] 
    private string? JsonEncoded { get; set; }

    /// <summary>
    ///     The name of the function.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    ///     Any arguments that need to be passed to the function. This needs to be in JSON format.
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = default!;
    
    /// <summary>
    ///     The result of the function. This is resolved by the API consumer.
    /// </summary>
    [JsonIgnore]
    public FunctionResult? Result { get; set; }
    
    /// <summary>
    ///     The full tool call object.
    /// </summary>
    [JsonIgnore]
    public ToolCall? ToolCall { get; set; }

    /// <summary>
    ///     Attempts to get value of a given argument. If the argument is of incompatible type, <see cref="exception"/> is returned.
    /// </summary>
    /// <param name="param"></param>
    /// <param name="data"></param>
    /// <param name="exception"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetArgument<T>(string param, [NotNullWhen(returnValue: true)] out T? data, out Exception? exception)
    {
        return argGetter.Value.Get(param, out data, out exception);
    }
    
    /// <summary>
    ///     Attempts to get value of a given argument. If the argument is not found or is of incompatible type, null is returned.
    /// </summary>
    /// <param name="param"></param>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetArgument<T>(string param, [NotNullWhen(returnValue: true)] out T? data)
    {
        return argGetter.Value.Get(param, out data, out _);
    }
    
    /// <summary>
    ///     Gets the json encoded function call, this is cached to avoid serializing the function over and over.
    /// </summary>
    /// <returns></returns>
    public string GetJson()
    {
        return JsonEncoded ??= JsonConvert.SerializeObject(this, EndpointBase.NullSettings);
    }
}