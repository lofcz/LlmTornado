using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Chat.Plugins;

public class ChatPluginCompiler
{
    public IDictionary<string, object?>? JsonSchema { get; set; }
    
    private List<IChatPlugin>? plugins;
    private Dictionary<string, ChatPluginFunction> callMap = new Dictionary<string, ChatPluginFunction>();
    private List<Tool>? functions;

    private static Tool SerializeFunction(ChatPluginFunction pluginFunction, ChatPluginCompileBackends schema = ChatPluginCompileBackends.JsonSchema)
    {
        if (pluginFunction.Params is null)
        {
            return new Tool(new ToolFunction(pluginFunction.Name, pluginFunction.Description, new {}));
        }
        
        ChatPluginFunctionTypeObject root = new ChatPluginFunctionTypeObject(null, true, pluginFunction.Params);
        object obj = root.Compile(schema);
        return new Tool(new ToolFunction(pluginFunction.Name, pluginFunction.Description, obj)
        {
            RawParameters = root
        });
    }

    private static Tool Serialize(IChatPlugin plugin, ChatPluginFunction pluginFunction)
    {
        Tool f = SerializeFunction(pluginFunction);
        f.Function.Name = $"{plugin.Namespace}-{pluginFunction.Name}";
        return f;
    }

    public async Task SetPlugins(IEnumerable<IChatPlugin> chatPlugins)
    {
        this.plugins = chatPlugins.ToList();
        functions ??= [];
        functions.Clear();
        
        foreach (IChatPlugin plugin in this.plugins)
        {
            foreach (ChatPluginFunction x in (await plugin.Export()).Functions)
            {
                callMap.AddOrUpdate($"{plugin.Namespace}-{x.Name}", x);
                functions.Add(Serialize(plugin, x));
            }
        }
    }
    
    /// <summary>
    /// Use only for standalone functions not incorporated into plugins
    /// </summary>
    /// <param name="pluginFunction"></param>
    public void SetFunctions(ChatPluginFunction pluginFunction)
    {
        functions ??= [];
        functions.Clear();
        
        callMap.AddOrUpdate(pluginFunction.Name, pluginFunction);
        functions.Add(SerializeFunction(pluginFunction));
    }
    
    /// <summary>
    /// Use only for standalone functions not incorporated into plugins
    /// </summary>
    /// <param name="pluginFunctions"></param>
    public void SetFunctions(IEnumerable<ChatPluginFunction> pluginFunctions)
    {
        functions ??= [];
        functions.Clear();

        foreach (ChatPluginFunction pluginFunction in pluginFunctions)
        {
            callMap.AddOrUpdate(pluginFunction.Name, pluginFunction);
            functions.Add(SerializeFunction(pluginFunction));   
        }
    }

    public string Compile()
    {
        return GetFunctions().ToJson();
    }

    public List<Tool>? GetFunctions()
    {
        if (JsonSchema is not null)
        {
            if (JsonSchema.TryGetValue("description", out object? fnDesc) && fnDesc is string fnDescStr && JsonSchema.TryGetValue("parameters", out object? fnPars) && fnPars is IDictionary<string, object?> fnParsDict)
            {
                string name = "solve";

                if (JsonSchema.TryGetValue("name", out object? fnName) && fnName is string fnNameStr)
                {
                    name = fnNameStr;
                }
                
                return [new Tool(new ToolFunction(name, fnDescStr, fnParsDict))];
            }
        }

        if (functions == null || functions.Count == 0)
        {
            return null;
        }
        
        return functions;
    }

    public async Task<FunctionResult> Call(FunctionCall fi, Dictionary<string, object?>? args)
    {
        if (JsonSchema is not null)
        {
            return new FunctionResult(fi.Name, args)
            {
                PassthroughData = args
            };
        }
        
        if (fi.Name is null)
        {
            return new FunctionResult("none", new
            {
                text = "no function executed"
            });
        }
        
        if (!callMap.TryGetValue(fi.Name, out ChatPluginFunction? cf))
        {
            return new FunctionResult("none", new
            {
                text = "no function executed"
            });
        }

        if (cf.CallHandler == null && cf.SyncCallHandler == null)
        {
            return new FunctionResult("none", new
            {
                text = "no function executed"
            });
        }

        ChatFunctionCallResult? callResult = cf.CallHandler is not null ? await cf.CallHandler.Invoke(new ChatPluginFunctionInputParams(args)) : cf.SyncCallHandler?.Invoke(new ChatPluginFunctionInputParams(args));
        
        if (callResult == null)
        {
            return new FunctionResult("none", new
            {
                text = "no function executed"
            });
        }

        if (!callResult.Ok)
        {
            return new FunctionResult("none", new
            {
                error = callResult.Error ?? "unknown error"
            });
        }

        return new FunctionResult(fi.Name, callResult.Result, callResult.PostRenderData);
    }
}