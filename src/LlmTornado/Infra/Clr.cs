using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LlmTornado.Code;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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

    private static ArrayList DeserializeNonGenericEnumerable(JToken token)
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

    private static Array? DeserializeMdArray(JToken token, Type dataType)
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
                jObject.TryGetValue(toolParam.Name, StringComparison.OrdinalIgnoreCase, out JToken? token))
            {
                args.Add(Deserialize(token, toolParam.Type));
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

    private static object? Deserialize(JToken token, IToolParamType paramType)
    {
        if (token.Type == JTokenType.Null)
        {
            return null;
        }

        if (paramType is ToolParamNullable nullable)
        {
            return Deserialize(token, nullable.InnerType);
        }

        Type? dataType = paramType.DataType;
        
        if (dataType is null)
        {
            return null;
        }
        
        switch (paramType.Serializer)
        {
            case ToolParamSerializer.Dictionary:
                return DeserializeDictionary(token, dataType);
            case ToolParamSerializer.Set:
                return DeserializeSet(token, dataType);
            case ToolParamSerializer.MultidimensionalArray:
                return DeserializeMdArray(token, dataType);
            case ToolParamSerializer.NonGenericEnumerable:
                return DeserializeNonGenericEnumerable(token);
            case ToolParamSerializer.Array:
            case ToolParamSerializer.Object:
                return DeserializeObject(token, dataType);
            case ToolParamSerializer.Atomic:
                return DeserializePrimitive(token, dataType);
            case ToolParamSerializer.Any:
                if (dataType == typeof(ExpandoObject))
                {
                    return token.ToObject<ExpandoObject>();
                }
                
                return token.ToObject<object>();
            case ToolParamSerializer.AnyOf:
                if (paramType is ToolParamAnyOf anyOfParam)
                {
                    return DeserializeAnyOf(token, anyOfParam);
                }

                return null;
            case ToolParamSerializer.Undefined:
            default:
                return token.ToObject(dataType);
        }
    }

    private static object? DeserializeAnyOf(JToken token, ToolParamAnyOf anyOfParam)
    {
        if (token is not JObject jObject)
        {
            throw new JsonException("Expected a JSON object for 'anyOf' deserialization, but received a different type.");
        }

        string? typeName = null;
        
        foreach (string key in ToolDefaults.DiscriminatorKeys)
        {
            if (jObject.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken? discriminatorToken))
            {
                typeName = discriminatorToken.Value<string>();
                break;
            }
        }
        
        if (typeName is null)
        {
            throw new JsonException($"Required discriminator property not found on object for 'anyOf' deserialization. Searched for: {string.Join(", ", ToolDefaults.DiscriminatorKeys)}");
        }
        
        Type? targetType = anyOfParam.PossibleTypes.FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

        if (targetType is null)
        {
            throw new JsonException($"Received discriminator value '{typeName}' does not match any of the possible types for this 'anyOf' parameter.");
        }

        return token.ToObject(targetType);
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