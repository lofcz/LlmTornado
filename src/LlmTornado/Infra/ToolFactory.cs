using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using LlmTornado.Code;
using LlmTornado.Common;

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

            Handle(par, function.Params);
        }

        if (metadata?.Params is not null)
        {
            foreach (ToolParamDefinition def in metadata.Params)
            {
                ToolParam? existing = function.Params.FirstOrDefault(x => x.Name == def.Name);

                if (existing is not null)
                {
                    // Potentially an override
                    if (existing.Type.GetType() == def.Param.GetType())
                    {
                        // Compatible override (e.g. string -> string)
                        IToolParamType newParam = def.Param;
                        newParam.DataType = existing.Type.DataType;
                        newParam.Serializer = existing.Type.Serializer;
                        existing.Type = newParam;
                    }
                    else if (existing.Type is ToolParamListEnum listEnum && def.Param is ToolParamEnum newEnumDefinition)
                    {
                        // Special case: User is overriding an array of enums with a new enum definition for the items.
                        listEnum.Items = newEnumDefinition.EnumValues;
                    }
                    else
                    {
                        // Incompatible override. Remove the original, and the new one will be added as a ToolArgument-only parameter.
                        function.Params.Remove(existing);
                        function.Params.Add(new ToolParam(def.Name, def.Param));
                    }
                }
                else
                {
                    // It's a new parameter
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

    private static IToolParamType GetParamFromType(Type type, ParameterInfo? par = null, PropertyInfo? prop = null)
    {
        Tuple<Type, bool> baseTypeInfo = GetNullableBaseType(type);
        Type baseType = baseTypeInfo.Item1;

        SchemaAnyOfAttribute? anyOf = par?.GetCustomAttribute<SchemaAnyOfAttribute>() ?? prop?.GetCustomAttribute<SchemaAnyOfAttribute>();

        if ((anyOf is not null || (baseType.IsInterface || baseType.IsAbstract)) && !baseType.IsArray && !baseType.IsIEnumerable())
        {
            return HandleAnyOf(type, anyOf);
        }

        if (baseType == typeof(ToolArguments))
        {
            return new ToolParamArguments();
        }

        if (baseType == typeof(object) || baseType == typeof(ExpandoObject))
        {
            return new ToolParamAny(null) { DataType = type, Serializer = ToolParamSerializer.Any };
        }

        if (IsKnownAtomicType(baseType, out _))
        {
            return new ToolParamString(null) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }
        
        if (baseType == typeof(DateTime))
        {
            return new ToolParamDateTime(null) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }
            
#if MODERN
        if (baseType == typeof(DateOnly))
        {
            return new ToolParamDate(null) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }
            
        if (baseType == typeof(TimeOnly))
        {
            return new ToolParamTime(null) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }
#endif
        
        if (baseType.IsEnum)
        {
            List<string> vals = Enum.GetValues(baseType).Cast<object>().Select(x => x.ToString()!).ToList();
            return new ToolParamEnum(null, vals) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }

        if (baseType.IsArray)
        {
            int rank = baseType.GetArrayRank();

            if (rank > 1)
            {
                Type elementType = baseType.GetElementType()!;
                Type flatArrayType = Array.CreateInstance(elementType, 0).GetType();
                IToolParamType valuesParamType = GetParamFromType(flatArrayType, par, prop);
                
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
                return new ToolParamListEnum(null, vals) { DataType = type, Serializer = ToolParamSerializer.Array };
            }

            return new ToolParamList(null, GetParamFromType(innerType, par, prop)) { DataType = type, Serializer = ToolParamSerializer.Array };
        }

        if (IsIEnumerable(baseType))
        {
            if (!baseType.IsGenericType)
            {
                return new ToolParamList(null, GetParamFromType(typeof(object), par, prop)) { DataType = baseType, Serializer = ToolParamSerializer.NonGenericEnumerable };
            }
            
            if (IsISet(baseType))
            {
                return new ToolParamList(null, GetParamFromType(baseType.GetGenericArguments()[0], par, prop)) { DataType = type, Serializer = ToolParamSerializer.Set };
            }

            if (IsIList(baseType))
            {
                return new ToolParamList(null, GetParamFromType(baseType.GetGenericArguments()[0], par, prop)) { DataType = type, Serializer = ToolParamSerializer.Array };
            }

            if (IsIDictionary(baseType))
            {
                Type[] genericArgs = baseType.GetGenericArguments();
                IToolParamType valueType;

                if (genericArgs.Length >= 2)
                {
                    valueType = GetParamFromType(genericArgs[1], par, prop);
                }
                else
                {
                    valueType = new ToolParamString(null) { DataType = typeof(string) };
                }

                return new ToolParamDictionary(null, valueType) { DataType = type, Serializer = ToolParamSerializer.Dictionary };
            }
        }

        ToolParamObject obj = new ToolParamObject(null, []) { DataType = type, Serializer = ToolParamSerializer.Object };
        UnpackType(baseType, obj, par, prop);
        return obj;
    }
    
#if MODERN
    private static readonly NullabilityInfoContext nullabilityContext = new NullabilityInfoContext();
#endif
    
    static void UnpackType(Type type, ToolParamObject parent, ParameterInfo? par = null, PropertyInfo? prop = null)
    {
        PropertyInfo[] props = type.GetProperties();
        
        foreach (PropertyInfo property in props)
        {
            IToolParamType propType = GetParamFromType(property.PropertyType, par, property);
            
#if MODERN
            NullabilityInfo nullabilityInfo = nullabilityContext.Create(property);
            propType.Required = nullabilityInfo.WriteState is NullabilityState.NotNull;
#else
            var (baseType, isNullableValueType) = GetNullableBaseType(property.PropertyType);
            propType.Required = baseType.IsValueType && !isNullableValueType;
#endif
            
            parent.Properties.Add(new ToolParam(property.Name, propType));
        }
    }

    private static readonly Dictionary<Type, ToolParamAtomicTypes> atomicTypes = new Dictionary<Type, ToolParamAtomicTypes>
    {
        { typeof(string), ToolParamAtomicTypes.String },
        { typeof(double), ToolParamAtomicTypes.Float },
        { typeof(float), ToolParamAtomicTypes.Float },
        { typeof(bool), ToolParamAtomicTypes.Bool },
        { typeof(int), ToolParamAtomicTypes.Int },
        { typeof(uint), ToolParamAtomicTypes.Int },
        { typeof(byte), ToolParamAtomicTypes.Int },
        { typeof(short), ToolParamAtomicTypes.Int },
        { typeof(long), ToolParamAtomicTypes.Int },
        { typeof(ulong), ToolParamAtomicTypes.Int },
        { typeof(decimal), ToolParamAtomicTypes.Float }
    };

    static bool IsKnownAtomicType(Type type, [NotNullWhen(true)] out ToolParamAtomicTypes? atomicType)
    {
        bool val = atomicTypes.TryGetValue(type, out ToolParamAtomicTypes cc);
        atomicType = cc;
        return val;
    }

    private static void Handle(ParameterInfo par, List<ToolParam> pars)
    {
        if (par.Name is null) return;

        IToolParamType baseParam = GetParamFromType(par.ParameterType, par);
        baseParam.Required = !par.IsOptional;
        
        SchemaNullableAttribute? nullableAttribute = par.GetCustomAttribute<SchemaNullableAttribute>();
        
#if MODERN
        NullabilityInfo nullabilityInfo = nullabilityContext.Create(par);
        bool isNullable = nullabilityInfo.WriteState is NullabilityState.Nullable;
#else
        var (_, isNullableValueType) = GetNullableBaseType(par.ParameterType);
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

    private static IToolParamType HandleAnyOf(Type parameterType, SchemaAnyOfAttribute? anyOf)
    {
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
                return GetParamFromType(possibleTypes[0]);
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
            IToolParamType paramType = GetParamFromType(type);
            
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