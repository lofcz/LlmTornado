using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.ChatFunctions;

/// <summary>
///     An optional class to be used with models that support returning function calls.
/// </summary>
public class FunctionCall
{
    /// <summary>
    ///     Arguments.
    /// </summary>
    [JsonIgnore] 
    internal readonly Lazy<ChatFunctionParamsGetter> ArgGetter;

    /// <summary>
    ///     Decoded arguments.
    /// </summary>
    [JsonIgnore] 
    internal readonly Lazy<Dictionary<string, object?>?> DecodedArguments;

    /// <summary>
    ///     Creates an empty function call.
    /// </summary>
    public FunctionCall()
    {
        ArgGetter = new Lazy<ChatFunctionParamsGetter>(() => new ChatFunctionParamsGetter(DecodedArguments?.Value));
        DecodedArguments = new Lazy<Dictionary<string, object?>?>(() => Arguments.IsNullOrWhiteSpace() ? [] : JsonConvert.DeserializeObject<Dictionary<string, object?>>(Arguments));
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
    public string Arguments { get; set; } = null!;
    
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
    ///     Gets all arguments passed to the function call as a dictionary.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, object?> GetArguments()
    {
        return ArgGetter.Value.Source ?? [];
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