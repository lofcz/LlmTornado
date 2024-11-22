using System;
using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Plugins;

/// <summary>
///     Input params of a function call.
/// </summary>
public class ChatPluginFunctionInputParams
{
    /// <summary>
    ///     Source dictionary.
    /// </summary>
    public Dictionary<string, object?>? Source { get; set; }
    
    /// <summary>
    ///     Creates an input params from a dictionary.
    /// </summary>
    /// <param name="pars"></param>
    public ChatPluginFunctionInputParams(Dictionary<string, object?>? pars)
    {
        Source = pars;
    }
}