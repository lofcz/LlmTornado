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
            string normalizedData = data ?? "{}";
            JObject jObject = JObject.Parse(normalizedData);
            
            if (metadata.Tool.Params is not null)
            {
                foreach (ToolParam param in metadata.Tool.Params)
                {
                    if (param.Type is ToolParamArguments toolArgs)
                    {
                        args.Add(new ToolArguments
                        {
                            Data = normalizedData
                        });
                        
                        continue;
                    }
                    
                    if (jObject.TryGetValue(param.Name, StringComparison.OrdinalIgnoreCase, out JToken? token) && param.Type.DataType is not null)
                    {
                        Type dataType = param.Type.DataType;
                        bool isDictionary = dataType.GetInterfaces().Append(dataType).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                        bool isMdArray = dataType.IsArray && dataType.GetArrayRank() > 1;
                        bool isNonGenericEnumerable = !dataType.IsArray && !dataType.IsGenericType && dataType.GetInterfaces().Append(dataType).Any(x => x == typeof(IEnumerable));
                        
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
                        else if (isMdArray && token is JObject mdArrayObject)
                        {
                            if (mdArrayObject.TryGetValue("lengths", StringComparison.OrdinalIgnoreCase, out JToken? lengthsToken) && mdArrayObject.TryGetValue("values", StringComparison.OrdinalIgnoreCase, out JToken? valuesToken))
                            {
                                int[]? lengths = lengthsToken.ToObject<int[]>();
                                Type? elementType = dataType.GetElementType();

                                if (lengths is not null && elementType is not null)
                                {
                                    Array mdArray = Array.CreateInstance(elementType, lengths);
                                    JArray valuesArray = (JArray)valuesToken;
                                    int[] indices = new int[lengths.Length];
                                    
                                    for(int i = 0; i < valuesArray.Count; i++)
                                    {
                                        int linearIndex = i;
                                        for (int dim = lengths.Length - 1; dim >= 0; dim--)
                                        {
                                            indices[dim] = linearIndex % lengths[dim];
                                            linearIndex /= lengths[dim];
                                        }
                                        mdArray.SetValue(valuesArray[i].ToObject(elementType), indices);
                                    }
                                    
                                    args.Add(mdArray);
                                }
                                else
                                {
                                    args.Add(null);
                                }
                            }
                            else
                            {
                                args.Add(null);
                            }
                        }
                        else if (isNonGenericEnumerable && token is JArray jArray)
                        {
                            ArrayList list = new ArrayList();
                            foreach (JToken item in jArray)
                            {
                                list.Add(item.ToObject<object>());
                            }
                            args.Add(list);
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
    
    /// <summary>
    /// Gets the specified argument.
    /// </summary>
    public static bool Get<T>(string param, Dictionary<string, object?> arguments, out T? data, out Exception? exception)
    {
        exception = null;

        if (!arguments.TryGetValue(param, out object? rawData))
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