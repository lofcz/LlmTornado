using System;
using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Plugins;

public class ChatPluginFunctionInputParams
{
    private readonly Dictionary<string, object?>? source;

    public ChatPluginFunctionInputParams(Dictionary<string, object?>? pars)
    {
        source = pars;
    }

    public bool Get<T>(string param, out T? data, out Exception? exception)
    {
        exception = null;
        
        if (source == null)
        {
            data = default;
            return false; 
        }
        
        if (!source.TryGetValue(param, out object? rawData))
        {
            data = default;
            return false;
        }

        if (rawData is T obj)
        {
            data = obj;
        }

        if (rawData is JArray jArr)
        {
            data = jArr.ToObject<T?>();
            return true;
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
}