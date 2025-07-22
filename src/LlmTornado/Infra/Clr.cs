using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Code;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Infra;

internal static class Clr
{
    public static async ValueTask<object?> Invoke(Delegate? function, DelegateMetadata? metadata, string? data)
    {
        if (function is not null && metadata is not null)
        {
            object? result = null;

            List<object?> args = [];
            JObject jObject = JObject.Parse(data ?? "{}");
            
            if (metadata.Tool.Params is not null)
            {
                foreach (ToolParam param in metadata.Tool.Params)
                {
                    if (jObject.TryGetValue(param.Name, StringComparison.OrdinalIgnoreCase, out JToken? token) && param.Type.DataType is not null)
                    {
                        Type dataType = param.Type.DataType;
                        bool isDictionary = dataType.GetInterfaces().Append(dataType).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                        if (isDictionary && token is JArray array)
                        {
                            IDictionary dict = (IDictionary)Activator.CreateInstance(dataType)!;
                            Type[] genericArgs = dataType.GetGenericArguments();
                            Type keyType = genericArgs[0];
                            Type valueType = genericArgs[1];

                            foreach(JObject item in array.Children<JObject>())
                            {
                                if (item.TryGetValue("key", StringComparison.OrdinalIgnoreCase, out JToken? keyToken) && item.TryGetValue("value", StringComparison.OrdinalIgnoreCase, out JToken? valueToken))
                                {
                                    object? key = keyToken.ToObject(keyType);
                                    object? val = valueToken.ToObject(valueType);
                                    
                                    if (key is not null)
                                    {
                                        dict.Add(key, val);
                                    }
                                }
                            }
                            
                            args.Add(dict);
                        }
                        else
                        {
                            args.Add(token.ToObject(dataType));
                        }
                    }
                    else
                    {
                        args.Add(null);
                    }
                }
            }
            
            try
            {
                object? invocationResult = function.DynamicInvoke(args.ToArray());
                
                if (invocationResult is Task task)
                {
                    await task.ConfigureAwait(false);
                    
                    if (task.GetType().IsGenericType)
                    {
                        result = (task as dynamic).Result; 
                    }
                }
                else if (invocationResult is not null)
                {
                    Type type = invocationResult.GetType();
                    
                    if (type == typeof(ValueTask) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
                    {
                        result = await (dynamic)invocationResult;
                    }
                    else
                    {
                        result = invocationResult;
                    }
                }
            }
            catch (Exception ex)
            {
                // todo: handle
                throw; 
            }

            return result;
        }

        return null;
    }
}