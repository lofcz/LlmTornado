using System;
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
        return type.GetInterfaces().Append(type).Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
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

    static void UnpackType(Type type, ToolParamObject parent)
    {
        PropertyInfo[] props = type.GetProperties();
        
        foreach (PropertyInfo property in props)
        {
            Handle(property.Name, property.PropertyType, parent.Properties);
        }
    }

    private static IToolParamType CreateListParameterType(Type type, bool isNullable, string? description = null)
    {
        Type elementType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
        
        Tuple<Type, bool> baseInnerTypeInfo = GetNullableBaseType(elementType);
        Type baseInnerType = baseInnerTypeInfo.Item1;
        
        IToolParamType listType;

        if (IsKnownAtomicType(baseInnerType, out ToolParamAtomicTypes? atomicType))
        {
            listType = new ToolParamListAtomic(description, !isNullable, atomicType.Value) { DataType = type };
        }
        else if (baseInnerType.IsEnum)
        {
            List<string> vals = Enum.GetValues(baseInnerType).Cast<object>().Select(x => x.ToString()!).ToList();
            listType = new ToolParamListEnum(description, !isNullable, vals) { DataType = type };
        }
        else
        {
            ToolParamObject itemsObject = new ToolParamObject(null, []) { DataType = elementType };
            UnpackType(baseInnerType, itemsObject);
            listType = new ToolParamListObject(description, !isNullable, itemsObject) { DataType = type };
        }
        
        return listType;
    }

    private static readonly Dictionary<Type, ToolParamAtomicTypes> atomicTypes = new Dictionary<Type, ToolParamAtomicTypes>
    {
        { typeof(string), ToolParamAtomicTypes.String },
        { typeof(double), ToolParamAtomicTypes.Float },
        { typeof(float), ToolParamAtomicTypes.Float },
        { typeof(bool), ToolParamAtomicTypes.Bool },
        { typeof(int), ToolParamAtomicTypes.Int },
        { typeof(byte), ToolParamAtomicTypes.Int },
        { typeof(short), ToolParamAtomicTypes.Int },
        { typeof(long), ToolParamAtomicTypes.Int },
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
        Tuple<Type, bool> baseTypeInfo = GetNullableBaseType(type);
        Type baseType = baseTypeInfo.Item1;
        bool typeIsNullable = baseTypeInfo.Item2;

        if (baseType == typeof(ToolArguments))
        {
            pars.Add(new ToolParam(name, new ToolParamArguments()));
        }
        else if (baseType == typeof(string))
        {
            pars.Add(new ToolParam(name, new ToolParamString(null, !typeIsNullable) { DataType = type }));   
        }
        else if (baseType == typeof(int))
        {
            pars.Add(new ToolParam(name, new ToolParamInt(null, !typeIsNullable) { DataType = type }));   
        }
        else if (baseType == typeof(float) || baseType == typeof(double))
        {
            pars.Add(new ToolParam(name, new ToolParamNumber(null, !typeIsNullable) { DataType = type }));   
        }
        else if (baseType == typeof(bool))
        {
            pars.Add(new ToolParam(name, new ToolParamBool(null, !typeIsNullable) { DataType = type }));   
        }
        else if (baseType.IsEnum)
        {
            List<string> vals = [];

            foreach (object x in Enum.GetValues(baseType))
            {
                vals.Add(x.ToString());
            }
                
            pars.Add(new ToolParam(name, new ToolParamEnum(null, !typeIsNullable, vals) { DataType = type }));   
        }
        else if (baseType.IsArray)
        {
            int rank = baseType.GetArrayRank();

            if (rank > 1)
            {
                Type elementType = baseType.GetElementType()!;
                Type flatArrayType = Array.CreateInstance(elementType, 0).GetType();
                IToolParamType valuesParamType = CreateListParameterType(flatArrayType, false, "Flattened values of the array.");
                
                ToolParamObject mdArrayObject = new ToolParamObject("A multi-dimensional array.",
                [
                    new ToolParam("lengths", new ToolParamListAtomic("Dimensions of the array", true, ToolParamAtomicTypes.Int) { DataType = typeof(int[]) }),
                    new ToolParam("values", valuesParamType)
                ]) { DataType = type };
                
                pars.Add(new ToolParam(name, mdArrayObject));
            }
            else
            {
                pars.Add(new ToolParam(name, CreateListParameterType(baseType, typeIsNullable)));
            }
        }
        else if (IsIEnumerable(baseType))
        {
            if (!baseType.IsGenericType)
            {
                pars.Add(new ToolParam(name, new ToolParamListAtomic(null, !typeIsNullable, ToolParamAtomicTypes.String) { DataType = baseType }));
                return;
            }
            
            Type[] genericArgs = baseType.GetGenericArguments();

            if (IsIList(baseType))
            {
                pars.Add(new ToolParam(name, CreateListParameterType(baseType, typeIsNullable)));
                return;
            }
            else if (IsISet(baseType))
            {
                // todo: implement sets handling
            }
            else if (IsIDictionary(baseType))
            {
                IToolParamType valueType;
                
                if (genericArgs.Length >= 2)
                {
                    Type valueTypeArg = genericArgs[1];
                    Tuple<Type, bool> baseValueTypeInfo = GetNullableBaseType(valueTypeArg);
                    Type baseValueType = baseValueTypeInfo.Item1;
                    
                    if (IsKnownAtomicType(baseValueType, out ToolParamAtomicTypes? atomicType))
                    {
                        valueType = atomicType.Value switch
                        {
                            ToolParamAtomicTypes.String => new ToolParamString(null, true) { DataType = valueTypeArg },
                            ToolParamAtomicTypes.Int => new ToolParamInt(null, true) { DataType = valueTypeArg },
                            ToolParamAtomicTypes.Float => new ToolParamNumber(null, true) { DataType = valueTypeArg },
                            ToolParamAtomicTypes.Bool => new ToolParamBool(null, true) { DataType = valueTypeArg },
                            _ => new ToolParamString(null, true) { DataType = valueTypeArg }
                        };
                    }
                    else
                    {
                        ToolParamObject obj = new ToolParamObject(null, []) { DataType = valueTypeArg };
                        UnpackType(baseValueType, obj);
                        valueType = obj;
                    }
                }
                else
                {
                    valueType = new ToolParamString(null, true) { DataType = typeof(string) };
                }
                
                ToolParamDictionary dict = new ToolParamDictionary(null, !typeIsNullable, valueType) { DataType = type };
                pars.Add(new ToolParam(name, dict));
            }
            else
            {
                // Potentially other IEnumerable types can be handled here
            }
        }
        else
        {
            ToolParamObject obj = new ToolParamObject(null, []) { DataType = type };
            UnpackType(baseType, obj);
            pars.Add(new ToolParam(name, obj));
        }
    }
}