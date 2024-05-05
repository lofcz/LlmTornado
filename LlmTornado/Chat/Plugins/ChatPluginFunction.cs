using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunction
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
    public Func<ChatPluginFunctionInputParams, Task<ChatFunctionCallResult?>>? CallHandler { get; set; }
    
    /// <summary>
    /// The function dispatcher, invoked when the function is called by LLM
    /// </summary>
    public Func<ChatPluginFunctionInputParams, ChatFunctionCallResult?>? SyncCallHandler { get; set; }
    
    public ChatPluginFunction(string name, string description, List<ChatFunctionParam>? pars)
    {
        Name = name;
        Description = description;
        Params = pars;
    }
    
    public ChatPluginFunction(string name, string description, List<ChatFunctionParam>? pars, Func<ChatPluginFunctionInputParams, Task<ChatFunctionCallResult?>> callHandler)
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
    public ChatPluginFunction(List<ChatFunctionParam>? pars, Func<ChatPluginFunctionInputParams, Task<ChatFunctionCallResult?>> callHandler)
    {
        Name = "ukol";
        Description = "funkce, která splní úkol";
        Params = pars;
        CallHandler = callHandler;
    }
    
    public ChatPluginFunction(string name, string description, List<ChatFunctionParam>? pars, Func<ChatPluginFunctionInputParams, ChatFunctionCallResult?> callHandler)
    {
        Name = name;
        Description = description;
        Params = pars;
        SyncCallHandler = callHandler;
    }
}
