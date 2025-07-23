using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LlmTornado.Code;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Infra;

internal static class Clr
{
    private static object? DeserializePrimitive(JToken token, Type dataType)
    {
        return token.ToObject(dataType);
    }

    private static object? DeserializeObject(JToken token, Type dataType)
    {
        return token.ToObject(dataType);
    }

    private static object DeserializeNonGenericEnumerable(JToken token)
    {
        ArrayList list = new ArrayList();
        
        if (token is JArray jArray)
        {
            foreach (JToken item in jArray)
            {
                list.Add(item.ToObject<object>()!);
            }
        }

        return list;
    }

    private static object? DeserializeMdArray(JToken token, Type dataType)
    {
        if (token is JObject mdArrayObject &&
            mdArrayObject.TryGetValue("lengths", StringComparison.OrdinalIgnoreCase, out JToken? lengthsToken) &&
            mdArrayObject.TryGetValue("values", StringComparison.OrdinalIgnoreCase, out JToken? valuesToken))
        {
            int[]? lengths = lengthsToken.ToObject<int[]>();
            Type? elementType = dataType.GetElementType();

            if (lengths is not null && elementType is not null)
            {
                Array mdArray = Array.CreateInstance(elementType, lengths);
                JArray valuesArray = (JArray)valuesToken;
                int[] indices = new int[lengths.Length];

                for (int i = 0; i < valuesArray.Count; i++)
                {
                    int linearIndex = i;
                    for (int dim = lengths.Length - 1; dim >= 0; dim--)
                    {
                        indices[dim] = linearIndex % lengths[dim];
                        linearIndex /= lengths[dim];
                    }

                    mdArray.SetValue(valuesArray[i].ToObject(elementType), indices);
                }

                return mdArray;
            }
        }

        return null;
    }
    
    private static object DeserializeSet(JToken token, Type dataType)
    {
        Type[] genericArgs = dataType.GetGenericArguments();
        Type elementType = genericArgs[0];

        Type listType = typeof(List<>).MakeGenericType(elementType);
        IList list = (IList)Activator.CreateInstance(listType)!;

        if (token is JArray jSet)
        {
            foreach (JToken item in jSet)
            {
                list.Add(item.ToObject(elementType)!);
            }
        }
        
        Type concreteType = (dataType.IsInterface || dataType.IsAbstract) ? typeof(HashSet<>).MakeGenericType(elementType) : dataType;
        object set;

        try
        {
            set = Activator.CreateInstance(concreteType, list)!;
        }
        catch (MissingMethodException)
        {
            set = Activator.CreateInstance(concreteType)!;
            MethodInfo? addMethod = concreteType.GetMethod("Add", [elementType]);

            if (addMethod is not null)
            {
                foreach (object? listItem in list)
                {
                    addMethod.Invoke(set, [listItem]);
                }
            }
            else
            {
                set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType), list)!;
            }
        }

        return set;
    }

    private static object DeserializeDictionary(JToken token, Type dataType)
    {
        IDictionary dict = (IDictionary)Activator.CreateInstance(dataType)!;
        Type[] genericArgs = dataType.GetGenericArguments();
        Type keyType = genericArgs[0];
        Type valueType = genericArgs[1];

        if (token is JArray array)
        {
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
        }
        
        return dict;
    }
    
    public static async ValueTask<object?> Invoke(Delegate? function, DelegateMetadata? metadata, string? data)
    {
        if (function is null || metadata?.Tool?.Params is null)
        {
            return null;
        }

        object? result = null;
        List<object?> args = [];
        string normalizedData = data ?? "{}";
        JObject jObject = JObject.Parse(normalizedData);
        
        ParameterInfo[] delegateParams = function.Method.GetParameters();
        Dictionary<string, ToolParam> toolParamsMap = metadata.Tool.Params.ToDictionary(p => p.Name);

        foreach (ParameterInfo delegateParam in delegateParams)
        {
            if (delegateParam.Name is null)
            {
                args.Add(null);
                continue;
            }
            
            if (delegateParam.ParameterType == typeof(ToolArguments))
            {
                args.Add(new ToolArguments { Data = normalizedData });
                continue;
            }

            if (toolParamsMap.TryGetValue(delegateParam.Name, out ToolParam? toolParam) && 
                jObject.TryGetValue(toolParam.Name, StringComparison.OrdinalIgnoreCase, out JToken? token) && 
                toolParam.Type.DataType is not null)
            {
                Type dataType = toolParam.Type.DataType;
                        
                switch (toolParam.Type.Serializer)
                {
                    case ToolParamSerializer.Dictionary:
                        args.Add(DeserializeDictionary(token, dataType));
                        break;
                    case ToolParamSerializer.Set:
                        args.Add(DeserializeSet(token, dataType));
                        break;
                    case ToolParamSerializer.MultidimensionalArray:
                        args.Add(DeserializeMdArray(token, dataType));
                        break;
                    case ToolParamSerializer.NonGenericEnumerable:
                        args.Add(DeserializeNonGenericEnumerable(token));
                        break;
                    case ToolParamSerializer.Array:
                    case ToolParamSerializer.Object:
                        args.Add(DeserializeObject(token, dataType));
                        break;
                    case ToolParamSerializer.Atomic:
                        args.Add(DeserializePrimitive(token, dataType));
                        break;
                    case ToolParamSerializer.Any:
                        args.Add(token.ToObject<object>());
                        break;
                    case ToolParamSerializer.Undefined:
                    default:
                        args.Add(token.ToObject(dataType));
                        break;
                }
            }
            else
            {
                args.Add(delegateParam.IsOptional ? Type.Missing : null);
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