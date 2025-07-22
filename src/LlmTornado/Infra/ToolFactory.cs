using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Infra;

internal static class ToolFactory
{
    static Tuple<Type, bool> GetNullableBaseType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return new Tuple<Type, bool>(Nullable.GetUnderlyingType(type)!, true);
        }

        return new Tuple<Type, bool>(type, false);
    }
    
    static bool IsIEnumerable(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x == typeof(IEnumerable) || x == typeof(IEnumerable<>));
    }
    
    static bool IsIList(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
    }
    
    static bool IsISet(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISet<>));
    }
    
    static bool IsIDictionary(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
    }
    
    static bool IsICollection(this Type type)
    {
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
    }
    
    public static DelegateMetadata CreateFromMethod(Delegate del, IEndpointProvider provider)
    {
        ParameterInfo[] pars = del.Method.GetParameters();
        ToolDefinition function = new ToolDefinition
        {
            Name = "output",
            Params = []
        };

        foreach (ParameterInfo par in pars)
        {
            if (par.Name is null)
            {
                continue;
            }

            Handle(par.Name, par.ParameterType, function.Params);
        }

        ToolFunction compiled = ChatPluginCompiler.Compile(function, new ToolMeta
        {
            Provider = provider
        });
        
        return new DelegateMetadata(compiled, function);
    }

    private static IToolParamType GetParamFromType(Type type)
    {
        Tuple<Type, bool> baseTypeInfo = GetNullableBaseType(type);
        Type baseType = baseTypeInfo.Item1;
        bool typeIsNullable = baseTypeInfo.Item2;

        if (baseType == typeof(ToolArguments))
        {
            return new ToolParamArguments();
        }

        if (IsKnownAtomicType(baseType, out _))
        {
            return new ToolParamString(null, !typeIsNullable) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }
        
        if (baseType.IsEnum)
        {
            List<string> vals = Enum.GetValues(baseType).Cast<object>().Select(x => x.ToString()!).ToList();
            return new ToolParamEnum(null, !typeIsNullable, vals) { DataType = type, Serializer = ToolParamSerializer.Atomic };
        }

        if (baseType.IsArray)
        {
            int rank = baseType.GetArrayRank();

            if (rank > 1)
            {
                Type elementType = baseType.GetElementType()!;
                Type flatArrayType = Array.CreateInstance(elementType, 0).GetType();
                IToolParamType valuesParamType = GetParamFromType(flatArrayType);
                
                return new ToolParamObject("A multi-dimensional array.",
                [
                    new ToolParam("lengths", new ToolParamListAtomic("Dimensions of the array", true, ToolParamAtomicTypes.Int) { DataType = typeof(int[]) }),
                    new ToolParam("values", valuesParamType)
                ]) { DataType = type, Serializer = ToolParamSerializer.MultidimensionalArray };
            }

            return new ToolParamList(null, !typeIsNullable, GetParamFromType(baseType.GetElementType()!)) { DataType = type, Serializer = ToolParamSerializer.Array };
        }

        if (IsIEnumerable(baseType))
        {
            if (!baseType.IsGenericType)
            {
                return new ToolParamList(null, !typeIsNullable, GetParamFromType(typeof(string))) { DataType = baseType, Serializer = ToolParamSerializer.NonGenericEnumerable };
            }
            
            if (IsISet(baseType))
            {
                return new ToolParamList(null, !typeIsNullable, GetParamFromType(baseType.GetGenericArguments()[0])) { DataType = type, Serializer = ToolParamSerializer.Set };
            }

            if (IsIList(baseType))
            {
                return new ToolParamList(null, !typeIsNullable, GetParamFromType(baseType.GetGenericArguments()[0])) { DataType = type, Serializer = ToolParamSerializer.Array };
            }

            if (IsIDictionary(baseType))
            {
                Type[] genericArgs = baseType.GetGenericArguments();
                IToolParamType valueType;

                if (genericArgs.Length >= 2)
                {
                    valueType = GetParamFromType(genericArgs[1]);
                }
                else
                {
                    valueType = new ToolParamString(null, true) { DataType = typeof(string) };
                }

                return new ToolParamDictionary(null, !typeIsNullable, valueType) { DataType = type, Serializer = ToolParamSerializer.Dictionary };
            }
        }

        ToolParamObject obj = new ToolParamObject(null, []) { DataType = type, Serializer = ToolParamSerializer.Object };
        UnpackType(baseType, obj);
        return obj;
    }
    
    static void UnpackType(Type type, ToolParamObject parent)
    {
        PropertyInfo[] props = type.GetProperties();
        
        foreach (PropertyInfo property in props)
        {
            parent.Properties.Add(new ToolParam(property.Name, GetParamFromType(property.PropertyType)));
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

    static void Handle(string name, Type type, List<ToolParam> pars)
    {
        pars.Add(new ToolParam(name, GetParamFromType(type)));
    }
}