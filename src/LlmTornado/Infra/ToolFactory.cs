using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Infra;

internal static class ToolFactory
{
    public static Tuple<Type, bool> GetNullableBaseType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return new Tuple<Type, bool>(Nullable.GetUnderlyingType(type)!, true);
        }

        return new Tuple<Type, bool>(type, false);
    }
    
    public static bool IsIEnumerable(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x == typeof(IEnumerable) || x == typeof(IEnumerable<>));
    }
    
    public static bool IsIList(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
    }
    
    public static bool IsISet(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISet<>));
    }
    
    static bool IsIDictionary(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
    }
    
    public static bool IsICollection(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
    }
    
    public static DelegateMetadata CreateFromMethod(Delegate del, ToolMetadata? metadata, IEndpointProvider provider)
    {
        ParameterInfo[] pars = del.Method.GetParameters();
        ToolDefinition function = new ToolDefinition
        {
            Name = "output",
            Params = []
        };

        HashSet<string> ignored = metadata?.Ignore?.ToHashSet() ?? [];

        foreach (ParameterInfo par in pars)
        {
            if (par.Name is null || ignored.Contains(par.Name))
            {
                continue;
            }

            Handle(del, par, function.Params, 0);
        }

        if (metadata?.Params is not null)
        {
            foreach (ToolParamDefinition def in metadata.Params)
            {
                ToolParam? existing = function.Params.FirstOrDefault(x => x.Name == def.Name);

                if (existing is not null)
                {
                    // override vs new parameter
                    if (existing.Type.GetType() == def.Param.GetType())
                    {
                        IToolParamType newParam = def.Param;
                        newParam.DataType = existing.Type.DataType;
                        newParam.Serializer = existing.Type.Serializer;
                        existing.Type = newParam;
                    }
                    else if (existing.Type is ToolParamListEnum listEnum && def.Param is ToolParamEnum newEnumDefinition)
                    {
                        listEnum.Values = newEnumDefinition.Values;
                    }
                    else
                    {
                        function.Params.Remove(existing);
                        function.Params.Add(new ToolParam(def.Name, def.Param));
                    }
                }
                else
                {
                    function.Params.Add(new ToolParam(def.Name, def.Param));
                }
            }
        }

        ToolFunction compiled = ChatPluginCompiler.Compile(function, new ToolMeta
        {
            Provider = provider
        });
        
        return new DelegateMetadata(compiled, function);
    }

    private static IToolParamType GetParamFromType(Delegate? del, Type type, string? description, ParameterInfo? par, PropertyInfo? prop, int recursionLevel, Type topLevelType)
    {
        if (recursionLevel > ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel} for type '{topLevelType.Name}'. This may be caused by a self-referencing type.");
        }

        Tuple<Type, bool> baseTypeInfo = GetNullableBaseType(type);
        Type baseType = baseTypeInfo.Item1;

        SchemaAnyOfAttribute? anyOf = par?.GetCustomAttribute<SchemaAnyOfAttribute>() ?? prop?.GetCustomAttribute<SchemaAnyOfAttribute>();

        if ((anyOf is not null || (baseType.IsInterface || baseType.IsAbstract)) && !baseType.IsArray && !baseType.IsIEnumerable())
        {
            return HandleAnyOf(del, type, description, anyOf, recursionLevel + 1, topLevelType);
        }
        
        // atomic types
        if (TypeLookup.TryGetValue(baseType, out Func<Delegate?, Type, string?, IToolParamType>? factory))
        {
            return factory(del, type, description);
        }

        // complex types
        if (baseType.IsEnum)
        {
            List<string> vals = Enum.GetValues(baseType).Cast<object>().Select(x => x.ToString()!).ToList();
            return new ToolParamEnum(description, vals) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }

        if (baseType == typeof(JsonElement) || baseType.IsSubclassOf(typeof(JToken)) || baseType == typeof(JToken))
        {
            if (baseType == typeof(JValue) || baseType == typeof(JsonElement))
            {
                return new ToolParamAny(description) { DataType = type, Serializer = ToolParamSerializer.Json };
            }

            if (baseType == typeof(JObject))
            {
                return new ToolParamObject(description, []) { DataType = type, Serializer = ToolParamSerializer.Json, AllowAdditionalProperties = true };
            }

            if (baseType == typeof(JArray))
            {
                return new ToolParamList(description, new ToolParamAny(null)) { DataType = type, Serializer = ToolParamSerializer.Json };
            }

            return new ToolParamAny(description) { DataType = type, Serializer = ToolParamSerializer.Json };
        }
        
        if (baseType.IsArray)
        {
            if (baseType.GetArrayRank() > 1)
            {
                Type elementType = baseType.GetElementType()!;
                Type flatArrayType = Array.CreateInstance(elementType, 0).GetType();
                IToolParamType valuesParamType = GetParamFromType(del, flatArrayType, null, par, prop, recursionLevel + 1, topLevelType);

                return new ToolParamObject("A multi-dimensional array.",
                [
                    new ToolParam("lengths", new ToolParamListAtomic("Dimensions of the array", ToolParamAtomicTypes.Int) { DataType = typeof(int[]) }),
                    new ToolParam("values", valuesParamType)
                ]) { DataType = type, Serializer = ToolParamSerializer.MultidimensionalArray };
            }

            Type innerType = baseType.GetElementType()!;

            if (GetNullableBaseType(innerType).Item1.IsEnum)
            {
                List<string> vals = Enum.GetValues(GetNullableBaseType(innerType).Item1).Cast<object>().Select(x => x.ToString()!).ToList();
                return new ToolParamListEnum(description, vals) { DataType = type, Serializer = ToolParamSerializer.Array };
            }

            return new ToolParamList(description, GetParamFromType(del, innerType, null, par, prop, recursionLevel + 1, topLevelType)) { DataType = type, Serializer = ToolParamSerializer.Array };
        }
        
        if (baseType != typeof(string) && baseType.IsIEnumerable())
        {
            if (!baseType.IsGenericType)
            {
                return new ToolParamList(description, GetParamFromType(del, typeof(object), null, par, prop, recursionLevel + 1, topLevelType)) { DataType = baseType, Serializer = ToolParamSerializer.NonGenericEnumerable };
            }
            
            if (baseType.IsISet() || baseType.IsIList())
            {
                Type innerType = baseType.GetGenericArguments()[0];
                IToolParamType innerParam = GetParamFromType(del, innerType, null, par, prop, recursionLevel + 1, topLevelType);
                innerParam = WrapIfNullable(innerParam, 0, par, prop);

                return new ToolParamList(description, innerParam) { DataType = type, Serializer = baseType.IsISet() ? ToolParamSerializer.Set : ToolParamSerializer.Array };
            }

            if (baseType.IsIDictionary())
            {
                Type[] genericArgs = baseType.GetGenericArguments();
                IToolParamType valueType;

                if (genericArgs.Length >= 2)
                {
                    valueType = GetParamFromType(del, genericArgs[1], null, par, prop, recursionLevel + 1, topLevelType);
                    valueType = WrapIfNullable(valueType, 1, par, prop);
                }
                else
                {
                    valueType = new ToolParamString(null) { DataType = typeof(string) };
                }

                return new ToolParamDictionary(description, valueType) { DataType = type, Serializer = ToolParamSerializer.Dictionary };
            }
        }
        
        if (typeof(Task).IsAssignableFrom(baseType))
        {
            Type innerType = baseType.IsGenericType ? baseType.GetGenericArguments()[0] : typeof(object);
            IToolParamType innerParam = GetParamFromType(del, innerType, description, par, prop, recursionLevel + 1, topLevelType);
            return new ToolParamAwaitable(innerParam) { DataType = type };
        }
        
#if MODERN
        if (typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(baseType))
        {
            List<IToolParamType> itemTypes = [];
            Type[] genericArgs = baseType.GetGenericArguments();
            
            foreach (Type genericArgument in genericArgs)
            {
                itemTypes.Add(GetParamFromType(del, genericArgument, null, par, prop, recursionLevel + 1, topLevelType));
            }

            ToolParamTuple tupleParam = new ToolParamTuple(description, itemTypes) { DataType = type };
            
            SchemaTupleAttribute? tupleAttribute = par?.GetCustomAttribute<SchemaTupleAttribute>() ?? prop?.GetCustomAttribute<SchemaTupleAttribute>();
            if (tupleAttribute?.Names.Length == genericArgs.Length)
            {
                tupleParam.Names = tupleAttribute.Names.ToList();
            }
            
            return tupleParam;
        }
#endif
        
        ToolParamObject obj = new ToolParamObject(description, []) { DataType = type, Serializer = ToolParamSerializer.Object };
        UnpackType(del, baseType, obj, par, prop, recursionLevel + 1, topLevelType);
        return obj;
    }
    
    private static IToolParamType WrapIfNullable(IToolParamType param, int genericArgumentIndex, ParameterInfo? par, PropertyInfo? prop)
    {
        #if MODERN
        NullabilityInfo? nullabilityInfo = null;
        if (par is not null) nullabilityInfo = nullabilityContext.Create(par);
        else if (prop is not null) nullabilityInfo = nullabilityContext.Create(prop);

        if (nullabilityInfo?.GenericTypeArguments.Length > genericArgumentIndex && 
            nullabilityInfo.GenericTypeArguments[genericArgumentIndex].ReadState == NullabilityState.Nullable)
        {
            return new ToolParamNullable(param);
        }
        #else
        Type? innerType = null;
        if (par is not null) innerType = par.ParameterType.GetGenericArguments().ElementAtOrDefault(genericArgumentIndex);
        else if (prop is not null) innerType = prop.PropertyType.GetGenericArguments().ElementAtOrDefault(genericArgumentIndex);
        
        if (innerType is not null && Nullable.GetUnderlyingType(innerType) is not null)
        {
            return new ToolParamNullable(param);
        }
        #endif

        return param;
    }

#if MODERN
    private static readonly NullabilityInfoContext nullabilityContext = new NullabilityInfoContext();
#endif
    
    static void UnpackType(Delegate? del, Type type, ToolParamObject parent, ParameterInfo? par, PropertyInfo? prop, int recursionLevel, Type topLevelType)
    {
        if (recursionLevel > ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel} for type '{topLevelType.Name}'. This may be caused by a self-referencing type.");
        }
        
        PropertyInfo[] props = type.GetProperties();
        
        foreach (PropertyInfo property in props)
        {
            IToolParamType propType = GetParamFromType(del, property.PropertyType, GetDescription(property), par, property, recursionLevel + 1, topLevelType);
            
#if MODERN
            NullabilityInfo nullabilityInfo = nullabilityContext.Create(property);
            propType.Required = nullabilityInfo.WriteState is NullabilityState.NotNull;
#else
            (Type? baseType, bool isNullableValueType) = GetNullableBaseType(property.PropertyType);
            propType.Required = baseType.IsValueType && !isNullableValueType;
#endif
            
            parent.Properties.Add(new ToolParam(property.Name, propType));
        }
    }

    private static readonly Dictionary<Type, Func<Delegate?, Type, string?, IToolParamType>> TypeLookup = new Dictionary<Type, Func<Delegate?, Type, string?, IToolParamType>>
    {
        { typeof(string), (del, t, d) => new ToolParamString(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(char), (del, t, d) => new ToolParamString(d) { DataType = t, Serializer = ToolParamSerializer.Atomic, MinLength = 1, MaxLength = 1 } },
#if MODERN
        { typeof(Rune), (del, t, d) => new ToolParamString(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
#endif
        { typeof(double), (del, t, d) => new ToolParamNumber(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(float), (del, t, d) => new ToolParamNumber(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(bool), (del, t, d) => new ToolParamBool(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(int), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(uint), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(nuint), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(byte), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(sbyte), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(short), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(ushort), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(long), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(ulong), (del, t, d) => new ToolParamInt(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(decimal), (del, t, d) => new ToolParamNumber(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(object), (del, t, d) => new ToolParamAny(d) { DataType = t, Serializer = ToolParamSerializer.Any } },
        { typeof(Guid), (del, t, d) => new ToolParamString(d) { DataType = t, Serializer = ToolParamSerializer.Atomic, Format = "uuid" } },
        { typeof(TimeSpan), (del, t, d) => new ToolParamString(d) { DataType = t, Serializer = ToolParamSerializer.Atomic, Format = "duration" } },
        { typeof(Uri), (del, t, d) => new ToolParamString(d) { DataType = t, Serializer = ToolParamSerializer.Atomic, Format = "uri" } },
        { typeof(Regex), (del, t, d) => new ToolParamString(d) { DataType = t, Serializer = ToolParamSerializer.Atomic, Format = "regex" } },
        { typeof(DateTime), (del, t, d) => new ToolParamDateTime(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(ExpandoObject), (del, t, d) => new ToolParamAny(d) { DataType = t, Serializer = ToolParamSerializer.Any } },
#if MODERN
        { typeof(DateOnly), (del, t, d) => new ToolParamDate(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
        { typeof(TimeOnly), (del, t, d) => new ToolParamTime(d) { DataType = t, Serializer = ToolParamSerializer.Atomic } },
#endif
        
        // special
        { typeof(ToolArguments), (del, t, d) => new ToolParamArguments() }
    };

    private static string? GetDescription(ICustomAttributeProvider element)
    {
        return (element.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description;
    }

    private static void Handle(Delegate? del, ParameterInfo par, List<ToolParam> pars, int recursionLevel)
    {
        if (par.Name is null) return;

        IToolParamType baseParam = GetParamFromType(del, par.ParameterType, GetDescription(par), par, null, recursionLevel + 1, par.ParameterType);
        baseParam.Required = !par.IsOptional;
        
        SchemaNullableAttribute? nullableAttribute = par.GetCustomAttribute<SchemaNullableAttribute>();
        
#if MODERN
        NullabilityInfo nullabilityInfo = nullabilityContext.Create(par);
        bool isNullable = nullabilityInfo.WriteState is NullabilityState.Nullable;
#else
        (_, bool isNullableValueType) = GetNullableBaseType(par.ParameterType);
        bool isNullable = isNullableValueType;
#endif

        if (nullableAttribute is not null || isNullable)
        {
            pars.Add(new ToolParam(par.Name, new ToolParamNullable(baseParam)
            {
                Serializer = ToolParamSerializer.Nullable
            }));
        }
        else
        {
            pars.Add(new ToolParam(par.Name, baseParam));
        }
    }

    private static IToolParamType HandleAnyOf(Delegate? del, Type parameterType, string? description, SchemaAnyOfAttribute? anyOf, int recursionLevel, Type topLevelType)
    {
        if (recursionLevel > ToolMeta.MaxRecursionLevel)
        {
            throw new InvalidOperationException($"Tool schema generation exceeded max recursion depth of {ToolMeta.MaxRecursionLevel} for type '{topLevelType.Name}'. This may be caused by a self-referencing type.");
        }
        
        List<Type> possibleTypes = [];

        if (anyOf?.Types.Length > 0)
        {
            possibleTypes.AddRange(anyOf.Types);
        }
        else
        {
            if (parameterType.IsInterface || parameterType.IsAbstract)
            {
                IEnumerable<Type> discoveredTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => parameterType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false });
                
                possibleTypes.AddRange(discoveredTypes);
            }
            else
            {
                possibleTypes.Add(parameterType);
            }
        }

        switch (possibleTypes.Count)
        {
            case 0:
            {
                string typeDescription = parameterType.IsInterface ? "interface" : "abstract class";
                throw new NotSupportedException($"Parameter of type '{parameterType.Name}' is an {typeDescription} with no concrete implementations found in the current AppDomain. Please ensure at least one implementing class is available or specify concrete types using [SchemaAnyOf(typeof(MyImpl))].");
            }
            case 1:
            {
                return GetParamFromType(del, possibleTypes[0], description, null, null, recursionLevel + 1, topLevelType);
            }
        }

        ToolParamAnyOf anyOfParam = new ToolParamAnyOf
        {
            PossibleTypes = possibleTypes,
            Serializer = ToolParamSerializer.AnyOf,
            DataType = parameterType
        };

        foreach (Type type in possibleTypes)
        {
            IToolParamType paramType = GetParamFromType(del, type, GetDescription(type), null, null, recursionLevel + 1, topLevelType);
            
            if (paramType is ToolParamObject obj)
            {
                HashSet<string> existingPropNames = new HashSet<string>(obj.Properties.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                string? safeDiscriminatorKey = ToolDefaults.DiscriminatorKeys.FirstOrDefault(key => !existingPropNames.Contains(key));

                if (safeDiscriminatorKey is null)
                {
                    throw new NotSupportedException($"The type '{type.Name}' cannot be used in [SchemaAnyOf] because all possible discriminator keys ({string.Join(", ", ToolDefaults.DiscriminatorKeys)}) are already defined as properties on the class. Please rename one of the conflicting properties.");
                }
                
                obj.ExtraProperties = new Dictionary<string, object>
                {
                    { safeDiscriminatorKey, new { @const = type.Name } }
                };
                
                anyOfParam.AnyOf.Add(obj);
            }
            else
            {
                throw new NotSupportedException($"The type '{type.Name}' cannot be used in [SchemaAnyOf] because it is not a complex object. Only classes or structs that serialize to objects are supported in anyOf lists to ensure safe deserialization with a type discriminator.");
            }
        }

        return anyOfParam;
    }
}