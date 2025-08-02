using System.Text.RegularExpressions;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Infra;
using Newtonsoft.Json;

namespace LlmTornado.Toolkit.Memory;

public class LlmResponseParsed
{
    public string? Message { get; set; }
    public FunctionResult? FunctionResult { get; set; }
    public bool Ok { get; set; }
    public LlmResponseErrors? Error { get; set; }
    public ChatRichResponseBlockTypes Kind { get; set; }
    public Exception? NativeException { get; set; }
    public object? Key { get; set; }
    [JsonIgnore]
    public HttpCallRequest? HttpRequest { get; set; }
    [JsonIgnore]
    public IHttpCallResult? HttpResult { get; set; }
}

public enum LlmResponseErrors
{
    Unknown,
    Generic,
    RateLimit,
    ContextOverflow,
    InvalidFunctionsScheme,
    ServiceUnreachable,
    NoResponse,
    RetriesExceeded,
    HandledWithProvidedException,
    NoActivity,
    ToolProcessing
}

public class ToolkitChatConfig
{
    public string SystemMessage { get; set; }
    public double Temp { get; set; }
    public ChatModel Model { get; set; }
    public int MaxLength { get; set; }
    public double FreqPenalty { get; set; }
    public double PresPenalty { get; set; }
    public int RefEntityId { get; set; }

    public ToolkitChatConfig()
    {
    }

    public ToolkitChatConfig(string systemMessage, double temp, ChatModel model, int maxLength, double freqPenalty, double presPenalty, int refEntityId, int ownerId)
    {
        SystemMessage = systemMessage;
        Temp = temp;
        Model = model;
        MaxLength = maxLength;
        FreqPenalty = freqPenalty;
        PresPenalty = presPenalty;
        RefEntityId = refEntityId;
    }

    private static readonly Regex DuplicatedWhitespacesRegex = new Regex("[ ]{2,}", RegexOptions.Compiled);
    
    private void ProcessSystemMessage()
    {
        SystemMessage = SystemMessage is null ? null : DuplicatedWhitespacesRegex.Replace(SystemMessage.Trim(), " ");
    }

    private void Init()
    {
        ProcessSystemMessage();
    }

    public ToolkitChatConfig(ChatModel model, int maxLength, double freqPenalty, double presPenalty, int refEntityId, string systemMessage, double temp)
    {
        Model = model;
        MaxLength = maxLength;
        FreqPenalty = freqPenalty;
        PresPenalty = presPenalty;
        RefEntityId = refEntityId;
        SystemMessage = systemMessage;
        Temp = temp;
        Init();
    }

    /// <summary>
    /// Ad-hoc minimal ctor
    /// </summary>
    /// <param name = "model"></param>
    /// <param name = "systemMessage"></param>
    /// <param name = "maxResponseTokens"></param>
    /// <param name = "temp"></param>
    public ToolkitChatConfig(ChatModel model, string? systemMessage, int? maxResponseTokens = 1024, double temp = 0.0d)
    {
        Model = model;
        SystemMessage = systemMessage ?? string.Empty;
        MaxLength = maxResponseTokens ?? 1024;
        Temp = temp;
        Init();
    }
}

public class LlmResponseParsedRich
{
    public bool Ok { get; set; }
    public LlmResponseErrors? Error { get; set; }
    public Exception? NativeException { get; set; }
    public object? Key { get; set; }
    public List<LlmResponseRichBlock> Blocks { get; set; } = [];
}

public class LlmResponseRichBlock
{
    public string? Message { get; set; }
    public FunctionResult? FunctionResult { get; set; }
    public ChatRichResponseBlockTypes Kind { get; set; }
}

public enum ChatSides
{
    User,
    Ml,
    MlSystem,
    MlSystemStale,
    MlFunctionResult,
    MlFunctionResultStale,
    MlFunctionResultContainer,
    MlFunctionResultContainerStale,
    Decoration
}

public class LlmResponseRaw
{
    public ChatChoice Response { get; set; }
    public bool Ok { get; set; }
    public LlmResponseErrors? Error { get; set; }
}

public class ChatPluginCompiler
{
    private List<IChatPlugin>? plugins;
    private readonly Dictionary<string, ChatFunction> callMap = new Dictionary<string, ChatFunction>();
    private readonly ToolkitChat ToolkitChat;
    private List<Tool>? functions;
    
    public ChatPluginCompiler(ToolkitChat chat)
    {
        ToolkitChat = chat;
    }

    private Tool SerializeFunction(ChatFunction function, IEndpointProvider provider)
    {
        function.Params ??= [];

        object obj = ToolFactory.Compile(new ToolDefinition(function.Name, function.Description, function.Params), new ToolMeta
        {
            Provider = provider
        });
        
        return ToolFactory.ModelsSupportingStrictFunctions.Contains(ToolkitChat.Cfg.Model) ? new Tool(new ToolFunction(function.Name, function.Description, obj), function.Strict) : new Tool(new ToolFunction(function.Name, function.Description, obj));
    }

    private Tool Serialize(IChatPlugin plugin, ChatFunction function, IEndpointProvider provider)
    {
        Tool f = SerializeFunction(function, provider);

        if (f.Function is not null)
        {
            f.Function.Name = $"{plugin.Namespace}-{function.Name}";    
        }
        
        return f;
    }

    public async Task SetPlugins(IEnumerable<IChatPlugin> plgs, IEndpointProvider provider)
    {
        plugins = plgs.ToList();
        functions ??= [];
        functions.Clear();
        
        foreach (IChatPlugin plugin in plugins)
        {
            foreach (ChatFunction x in (await plugin.Export()).Functions)
            {
                callMap.AddOrUpdate($"{plugin.Namespace}-{x.Name}", x);
                functions.Add(Serialize(plugin, x, provider));
            }
        }
    }
    
    /// <summary>
    /// Use only for standalone functions not incorporated into plugins
    /// </summary>
    /// <param name="function"></param>
    public void SetFunction(ChatFunction function, IEndpointProvider provider)
    {
        functions ??= [];
        functions.Clear();
        
        callMap.AddOrUpdate(function.Name, function);
        functions.Add(SerializeFunction(function, provider));
    }
    
    /// <summary>
    /// Use only for standalone functions not incorporated into plugins
    /// </summary>
    /// <param name="fns"></param>
    public void SetFunctions(IEnumerable<ChatFunction> fns, IEndpointProvider provider)
    {
        functions ??= [];
        functions.Clear();

        foreach (ChatFunction fn in fns)
        {
            callMap.AddOrUpdate(fn.Name, fn);
            functions.Add(SerializeFunction(fn, provider));
        }
    }

    public List<Tool>? GetFunctions()
    {
        if (functions == null || functions.Count == 0)
        {
            return null;
        }
        
        return functions;
    }


    public async Task<FunctionResult> Call(FunctionCall fi, Dictionary<string, object?>? args)
    {
        if (fi.Name.IsNullOrWhiteSpace() || !callMap.TryGetValue(fi.Name, out ChatFunction? cf) || cf.CallHandler is null && cf.SyncCallHandler is null)
        {
            return new FunctionResult("none", new
            {
                text = "no function executed"
            });
        }

        ChatFunctionCallResult? callResult = cf.CallHandler is not null ? await cf.CallHandler.Invoke(new ChatFunctionInputParams(args), ToolkitChat) : cf.SyncCallHandler?.Invoke(new ChatFunctionInputParams(args), ToolkitChat);

        if (callResult is null)
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
        
        return new FunctionResult(fi.Name, callResult.Result, callResult.PostRenderData ?? callResult.Result);
    }
}

public class ChatPluginExportResult
{
    public List<ChatFunction> Functions { get; set; }

    public ChatPluginExportResult(List<ChatFunction> functions)
    {
        Functions = functions;
    }
}

public interface IChatPlugin
{
    /// <summary>
    /// A unique vendor namespace to avoid collisions between function symbols cross plugins. Max 20 characters
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// A list o
    /// </summary>
    /// <returns></returns>
    public Task<ChatPluginExportResult> Export();
    
    ChatFunctionCallResult MissingParam(string name)
    {
        return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MissingRequiredParameter, name);
    }
    
    ChatFunctionCallResult MalformedParam(string name)
    {
        return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MalformedParam, name);
    }
}