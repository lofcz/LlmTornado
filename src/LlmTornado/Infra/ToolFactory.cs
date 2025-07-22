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

        if (baseType == typeof(string))
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
        else if (IsIEnumerable(baseType))
        {
            Type genericType = baseType.GetGenericTypeDefinition();
            Type[] genericArgs = baseType.GetGenericArguments();

            if (IsIList(baseType))
            {
                if (genericArgs.Length > 0)
                {
                    Type innerType = genericArgs[0];
                    Tuple<Type, bool> baseInnerTypeInfo = GetNullableBaseType(innerType);
                    Type baseInnerType = baseInnerTypeInfo.Item1;
                    bool innerTypeIsNullable = baseInnerTypeInfo.Item2;

                    if (IsKnownAtomicType(baseInnerType, out ToolParamAtomicTypes? atomicType))
                    {
                        ToolParamListAtomic list = new ToolParamListAtomic(null, !typeIsNullable, atomicType.Value) { DataType = type };
                        pars.Add(new ToolParam(name, list));
                        return;
                    }

                    ToolParamListObject listObj = new ToolParamListObject(null, !typeIsNullable, []) { DataType = type, Items = { DataType = innerType } };
                    UnpackType(baseInnerType, listObj.Items);
                    pars.Add(new ToolParam(name, listObj));
                }
            }
            else if (IsISet(baseType))
            {
                
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