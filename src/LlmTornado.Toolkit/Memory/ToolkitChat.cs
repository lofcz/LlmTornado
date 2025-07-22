using System.Security.Claims;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Toolkit;
using LlmTornado.Toolkit.Memory;
using McfCs;

#if FALSE
public class ToolkitChat
{
    public ToolkitChatConfig Cfg => cfg;
    public int? ConversationId { get; set; }
    
    /// <summary>
    /// Number of functions called since the last user message
    /// </summary>
    public int FunctionsCalledInSuccession => functionsCalledInSuccession;
    
    public bool SingleUse { get; set; }
    
    /// <summary>
    /// True if a user's request is being processed
    /// </summary>
    public bool Busy { get; private set; }
    
    /// <summary>
    /// Underlying conversation.
    /// </summary>
    public Conversation Memory => chat;
    
    public Func<string, Task>? TokenHandler { get; set; }
    public Func<ToolkitChat, Task<bool>>? OutboundMessageVerifier { get; set; }
    private TornadoApi api;
    private Conversation? chat;
    private int contextTokens;
    private int conversationUserId;
    private int conversationBotId;
    private bool useStream = true;
    private int userTokens = -1;
    private int maxContextTokens = 14000;
    private double temp = 1.0d;
    private int? responseMaxTokens = 8196;
    private string? systemMsg;
    private bool andromeda;
    private int userId;
    private bool preprocessing;
    private bool usePublicnet;
    private bool useEmbeddings = false;
    private decimal priceMultiplier = 0.0m;
    private ClaimsPrincipal user;
    private int tenantId;
    private readonly ChatPluginCompiler pluginCompiler;
    private string? forceFunctionName;
    private IList<IChatPlugin>? plugins;
    private ChatFunction? function;
    private bool enableMultipleFunctionCalls;
    private int functionsCalledInSuccession;
    private List<FunctionResult>? lastFunctionResult;
    private bool streaming;
    private IEnumerable<ChatMessagePart>? initParts;
    private bool conversationNeedsFullRender;
    private bool strictJson;
    private int streamRetryAttempts = 0;
    private ChatRequestVendorExtensions? vendorExtensions;
    private string? prefill;
    private bool prefillSolved;
    private ToolkitChatConfig? cfg;

    private ToolkitChat()
    {
        
    }
    
    private ToolkitChat(TornadoApi api, bool singleUse = false, bool useStream = true)
    {
        this.api = api;
        this.useStream = useStream;
        SingleUse = singleUse;
        pluginCompiler = new ChatPluginCompiler(this);
    }
    
    /// <summary>
    /// Instantiates a new stateful chat configured to execute a single streaming response with no functions
    /// </summary>
    public static async Task<DataOrException<bool>> GetSingleStreamingResponse(TornadoApi api, ToolkitChatConfig? config, string userInput, Action<int, string> onChunkArrived)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(config, (ChatFunction?)null, userInput, true);
        return await sc.GetLlmStreamingResponse(onChunkArrived);
    }
    
    /// <summary>
    /// Instantiates a new stateful chat configured to execute a single request, executes it and returns the result
    /// </summary>
    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ToolkitChatConfig? config, string userInput, object? key = null)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(config, (ChatFunction?)null, userInput, false);
        
        LlmResponseParsed response = await sc.GetLlmResponse();
        response.Key = key;
        return response;
    }
    
    /// <summary>
    ///  Instantiates a new stateful chat configured to execute a single request, executes it and returns all blocks of the response
    /// </summary>
    public static async Task<LlmResponseParsedRich> GetSingleResponseRich(TornadoApi api, ToolkitChatConfig? config, string userInput, List<ChatFunction>? functions = null, object? key = null)
    {
        ToolkitChat sc = await Create(api, config, userInput, functions, key);
        
        LlmResponseParsedRich response = await sc.GetLlmResponseRich();
        response.Key = key;
        
        return response;
    }

    /// <summary>
    /// Creates a new instance of toolkit chat and instantiates it. 
    /// </summary>
    public static async Task<ToolkitChat> Create(TornadoApi api, ToolkitChatConfig? config, string userInput, List<ChatFunction>? functions = null, object? key = null)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(config, functions, userInput);
        return sc;
    }

    public static async Task<LlmResponseParsedRich> GetSingleResponseRich(TornadoApi api, ChatModel model, ChatModel? backupModel, string sysPrompt, string userInput, double temp = 0d, int maxTokens = 8196, object? key = null)
    {
        ToolkitChat sc = await Create(api, model, backupModel, sysPrompt, userInput, temp, maxTokens, key);
        LlmResponseParsedRich response = await sc.GetLlmResponseRich();
        response.Key = key;

        if (!response.Ok)
        {
            sc.Cfg.Model = backupModel ?? model;
            response = await sc.GetLlmResponseRich();
        }
        
        return response;
    }
    
    public static async Task<ToolkitChat> Create(TornadoApi api, ChatModel model, ChatModel? backupModel, string sysPrompt, string userInput, double temp = 0d, int maxTokens = 8196, object? key = null)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(new ToolkitChatConfig(model, sysPrompt, maxTokens, temp), [], userInput);
        return sc;
    }

    /// <summary>
    /// Instantiates a new stateful chat configured to execute a single function, executes the function and returns the result
    /// </summary>
    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ToolkitChatConfig? config, ChatFunction function, string userInput)
    {
        ToolkitChat sc = await Create(api, config, function, userInput);
        LlmResponseParsed response = await sc.GetLlmResponse();
        return response;
    }
    
    public static async Task<ToolkitChat> Create(TornadoApi api, ToolkitChatConfig? config, ChatFunction function, string userInput)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(config, function, userInput, false, true);
        return sc;
    }

    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ChatModel model, ChatModel? backupModel, string sysPrompt, string userInput, double temp = 0d, int maxTokens = 8196, string? prefill = null)
    {
        ToolkitChat sc = await Create(api, model, backupModel, sysPrompt, userInput, temp, maxTokens, prefill);
        LlmResponseParsed response = await sc.GetLlmResponse();

        if (!response.Ok)
        {
            sc.Cfg.Model = backupModel ?? model;
            response = await sc.GetLlmResponse();
        }
        
        return response;
    }
    
    public static async Task<ToolkitChat> Create(TornadoApi api, ChatModel model, ChatModel? backupModel, string sysPrompt, string userInput, double temp = 0d, int maxTokens = 8196, string? prefill = null)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(new ToolkitChatConfig(model, sysPrompt, maxTokens, temp), (ChatFunction?)null, userInput, false, true, prefill);
        return sc;
    }

    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ChatModel model, ChatModel? backupModel, string sysPrompt, ChatFunction function, string userInput, double temp = 0d, int maxTokens = 8196, bool strict = true)
    {
        ToolkitChat sc = await Create(api, model, backupModel, sysPrompt, function, userInput, temp, maxTokens, strict);
        LlmResponseParsed response = await sc.GetLlmResponse();

        // retry unless we don't think it makes sense (for errors induced by us)
        if (response is { Ok: false, Error: not (LlmResponseErrors.ToolProcessing) })
        {
            sc.Cfg.Model = backupModel ?? model;
            response = await sc.GetLlmResponse();
        }
        
        return response;
    }
    
    public static async Task<ToolkitChat> Create(TornadoApi api, ChatModel model, ChatModel? backupModel, string sysPrompt, ChatFunction function, string userInput, double temp = 0d, int maxTokens = 8196, bool strict = true)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        function.Strict = strict;
        await sc.Init(new ToolkitChatConfig(model, sysPrompt, maxTokens, temp), function, userInput, false, true);
        return sc;
    }
    
    /*public static Task<LlmResponseParsed> GetSingleResponse(ChatModel model, ChatModel? backupModel, string sysPrompt, AttachedFile file, string? userInput = null, ChatFunction? chatFunction = null, double temp = 0d, int maxTokens = 8196, bool strict = true, string? prefill = null)
    {
        return GetSingleResponse(model, backupModel, sysPrompt, [file], userInput, chatFunction, temp, maxTokens, strict, prefill);
    }*/
    
    /*public static async Task<LlmResponseParsed> GetSingleResponse(ChatModel model, ChatModel? backupModel, string sysPrompt, IEnumerable<AttachedFile> files, string? userInput = null, ChatFunction? chatFunction = null, double temp = 0d, int maxTokens = 8196, bool strict = true, string? prefill = null)
    {
        ToolkitChat sc = new ToolkitChat(true);
        List<ChatMessagePart> msgParts = [];
        
        if (model.Provider is LLmProviders.Google)
        {
            List<Task<DataOrException<string>>> uploadTasks = files.Select(x => LlmJobServiceFiles.UploadFileToProvider(x)).ToList();
            await Task.WhenAll(uploadTasks);
            
            foreach (Task<DataOrException<string>> uploadFileResult in uploadTasks)
            {
                if (uploadFileResult.Result.Data is not null)
                {
                    msgParts.Add(new ChatMessagePart(new ChatMessagePartFileLinkData(uploadFileResult.Result.Data)));   
                }
            }
        }
        
        await sc.Init(new ToolkitChatConfig(model, sysPrompt, maxTokens, temp), chatFunction, userInput, msgParts, strictJson: strict);
        
        LlmResponseParsed response = await sc.GetLlmResponse();
        
        // todo: for google, we want to retry their api for certain requests signalling temporary overload of their infra
        
        if (!response.Ok)
        {
            //TODO: upload file to the backup model
            sc.Cfg.Model = backupModel ?? model;
            response = await sc.GetLlmResponse();
        }
        
        return response;
    }*/
    
    /// <summary>
    /// Instantiates a new stateful chat configured to execute a single function, executes the function and returns the result
    /// </summary>
    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ToolkitChatConfig? config, List<ChatMessagePart> messageParts)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(config, null, null, messageParts);
        
        LlmResponseParsed response = await sc.GetLlmResponse();
        return response;
    }
    
    /// <summary>
    /// Instantiates a new stateful chat configured to execute a single function, executes the function and returns the result
    /// </summary>
    /// <param name="config"></param>
    /// <param name="function"></param>
    /// <param name="userInput"></param>
    /// <param name="strict"></param>
    /// <returns></returns>
    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ToolkitChatConfig? config, ChatFunction function, string userInput, bool strict)
    {
        ToolkitChat sc = await Create(api, config, function, userInput, strict);
        LlmResponseParsed response = await sc.GetLlmResponse();
        return response;
    }
    
    public static async Task<ToolkitChat> Create(TornadoApi api, ToolkitChatConfig? config, ChatFunction function, string userInput, bool strict)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        function.Strict = strict;
        await sc.Init(config, function, userInput, false, true);
        return sc;
    }
    
    /// <summary>
    /// Instantiates a new stateful chat configured to execute a single function, executes the function and returns the result
    /// </summary>
    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ToolkitChatConfig? config, List<ChatFunction> functions, string userInput)
    {
        ToolkitChat sc = await Create(api, config, functions, userInput);
        LlmResponseParsed response = await sc.GetLlmResponse();
        return response;
    }
    
    public static async Task<ToolkitChat> Create(TornadoApi api, ToolkitChatConfig? config, List<ChatFunction> functions, string userInput)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(config, functions, userInput, false, true);
        return sc;
    }
    
    /// <summary>
    /// Instantiates a new stateful chat configured to execute a single function, executes the function and returns the result
    /// </summary>
    public static async Task<LlmResponseParsed> GetSingleResponse(TornadoApi api, ToolkitChatConfig? config, ChatFunction function, List<ChatMessagePart> userInput)
    {
        ToolkitChat sc = new ToolkitChat(api, true);
        await sc.Init(config, function, userInput);
        
        LlmResponseParsed response = await sc.GetLlmResponse();
        return response;
    }
    
    /// <summary>
    /// Resets the chat state.
    /// </summary>
    /// <returns></returns>
    public void NewConversation()
    {
        ConversationId = null;
        chat = null;
        Reset();
    }

    /// <summary>
    /// Has side effects! See <see cref="PostInit"/> for the list of automatically triggered post-init actions.
    /// </summary>
    public async Task Init(IList<IChatPlugin>? plugins = null, ToolkitChatConfig? config = null, string? forceFunctionName = null, ChatFunction? function = null, bool streaming = false, bool strictJson = true, string? prefill = null)
    {
        await SharedInit(config, forceFunctionName, plugins, function, null, null, null, streaming, strictJson, prefill);
    }
    
    public async Task Init(ToolkitChatConfig? config = null, ChatFunction? fn = null, string? userInput = "", List<ChatMessagePart>? messageParts = null, IDictionary<string, object?>? functionSchema = null, bool strictJson = true, string? prefill = null)
    {
        await SharedInit(config, null, null, fn, null, messageParts, functionSchema, false, strictJson, prefill);

        if (userInput is not null)
        {
            AddUserMessage(userInput);   
        }

        if (messageParts is not null)
        {
            AddUserMessage(messageParts);
        }
    }

    public async Task Init(ToolkitChatConfig? config, ChatFunction? fn, string userInput = "", bool streaming = false, bool strictJson = true, string? prefill = null)
    {
        await SharedInit(config,  fn?.Name, null, fn, null, null, null, streaming, strictJson, prefill);
        
        if (!userInput.IsNullOrWhiteSpace())
        {
            AddUserMessage(userInput);
        }
    }
    
    public async Task Init(ToolkitChatConfig? config, List<ChatFunction>? fns, string userInput = "", bool streaming = false, bool strictJson = true)
    {
        await SharedInit(config, null, null, null, fns, null, null, streaming, strictJson, null);
        
        if (!userInput.IsNullOrWhiteSpace())
        {
            AddUserMessage(userInput);
        }
    }
    
    public async Task Init(ToolkitChatConfig? config, ChatFunction? fn, List<ChatMessagePart> userInput, bool streaming = false, bool strictJson = true)
    {
        await SharedInit(config, null, null, fn, null, userInput, null, streaming, strictJson, null);
        
        if (userInput.Count > 0)
        {
            AddUserMessage(userInput);
        }
    }

    async Task SharedInit(ToolkitChatConfig? config, string? forceFunctionName, IList<IChatPlugin>? plugins, ChatFunction? function, List<ChatFunction>? functions, IEnumerable<ChatMessagePart>? messageParts, IDictionary<string, object?>? functionSchema, bool streaming, bool strictJson, string? prefill)
    {
        this.forceFunctionName = forceFunctionName;
        this.plugins = plugins;
        this.function = function;
        this.strictJson = strictJson;
        this.prefill = prefill;

        cfg = config ?? new ToolkitChatConfig();
        
        if (SingleUse)
        {
            useStream = false;
        }
        
        responseMaxTokens = cfg.MaxLength;
        systemMsg = cfg.SystemMessage.Trim(); // Dnešní datum: {DateTime.Now.ToStringDdMmYyyyHhMm()}.
        temp = cfg.Temp;
        this.streaming = streaming;
        
        if (plugins is not null)
        {
            await pluginCompiler.SetPlugins(plugins);
        }
        else if (function is not null)
        {
            this.forceFunctionName = function.Name;
            pluginCompiler.SetFunction(function);
        }
        else if (functions?.Count > 0)
        {
            pluginCompiler.SetFunctions(functions);
        }

        if (function is not null)
        {
            cfg.MaxLength = 4096;
        }

        if (prefill is null && (function is not null || functions?.Count > 0) && cfg.Model.Provider is LLmProviders.Anthropic)
        {
            // anthropic doesn't support structured json (yet) so this is the best we can do for now
            this.prefill = "{";
        }
        
        userTokens = 99999999;
    }
    
    private void Reset()
    {
        contextTokens = 0;
    }
    
    private Task PostInit()
    {
        return Task.CompletedTask;
    }
    
    public async Task StreamNextMessage(int retryAttempt = 0)
    {
        if (chat is null)
        {
            return;
        }
        
        try
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenHandler = async (res) =>
                {
                    if (TokenHandler is not null)
                    {
                        await TokenHandler.Invoke(res ?? string.Empty);
                    }
                },
                FunctionCallHandler = async (functionInfo) =>
                {
                    if (functionsCalledInSuccession > 0 && lastFunctionResult is not null)
                    {
                        // return lastFunctionResult;
                    }

                    functionsCalledInSuccession++;
                    lastFunctionResult ??= [];
                    lastFunctionResult.Clear();

                    foreach (FunctionCall fc in functionInfo)
                    {
                        Dictionary<string, object?>? args = fc.Arguments.JsonDecode<Dictionary<string, object?>>();
                        fc.Result = await pluginCompiler.Call(fc, args);
                    }
                },
                MessageTypeResolvedHandler = async type =>
                {
                    
                },
                HttpExceptionHandler = async (ctx) =>
                {
                    if (streamRetryAttempts < 3)
                    {
                        await Task.Delay(100);
                        await StreamNextMessage(retryAttempt + 1);
                    }
                },
                OnUsageReceived = async (usage) =>
                {
                    
                }
            });
        }
        catch (Exception e) // timeout
        {
           
        }
    }
    
    private void AddSharedMessage(string message, ChatSides side, bool buffered)
    {
        InitChatIfNull();
    }
    
    /// <summary>
    /// Adds a decoration message that is only rendered but ignored model-wise.
    /// </summary>
    /// <param name="message"></param>
    public void AddDecorationMessage(string message)
    {
        AddSharedMessage(message, ChatSides.Decoration, false);
    }
    
    /// <summary>
    /// Adds a decoration message that is only rendered but ignored model-wise.
    /// </summary>
    /// <param name="message"></param>
    public void AddMlMessageBuffered(string message)
    {
        AddSharedMessage(message, ChatSides.Ml, true);
    }

    private void InitChatIfNull(bool appendSystemMessage = true)
    {
        if (cfg is not null && chat is null)
        {
            // api = TornadoService.Connect(cfg.Model);
            
            if (api is null)
            {
                return;
            }

            ChatRequest request = new ChatRequest
            {
                Model = cfg.Model,
                Temperature = temp,
                MaxTokens = responseMaxTokens <= 0 ? 4000 : responseMaxTokens,
                Tools = pluginCompiler.GetFunctions(),
                ToolChoice = forceFunctionName.IsNullOrWhiteSpace() ? null : new OutboundToolChoice
                {
                    Mode = OutboundToolChoiceModes.ToolFunction,
                    Function = new OutboundToolCallFunction
                    {
                        Name = forceFunctionName
                    }
                },
                StreamOptions = useStream ? new ChatStreamOptions
                {
                    IncludeUsage = true
                } : null,
                // ReasoningBudget = 0
            };
            
            if (vendorExtensions is not null)
            {
                request.VendorExtensions = vendorExtensions;
            }
            
            chat = api.Chat.CreateConversation(request);

            if (!SingleUse && function is null)
            {
                chat.OnAfterToolsCall = async (result) =>
                {
                    foreach (ResolvedToolCall toolResult in result.ToolResults)
                    {
                        string text = $"{toolResult.ToolMessage.Name} {toolResult.ToolMessage.Content}"; // todo: mby count name, context separately? shouldn't make much difference
                    }

                    await StreamNextMessage();   
                };   
            }
            
            if (appendSystemMessage && systemMsg is not null)
            {
                ChatMessage? currentSystemMsg = chat.Messages.FirstOrDefault(x => x.Role is ChatMessageRoles.System);

                if (currentSystemMsg is null)
                {
                    chat.AppendSystemMessage(systemMsg);
                }
                else
                {
                    currentSystemMsg.Content = systemMsg;
                }
            }

            if (initParts is not null)
            {
                chat.AppendMessage(new ChatMessage(ChatMessageRoles.User, initParts));
            }
        }
    }

    private void CheckPrefill()
    {
        if (chat is not null && prefill?.Length > 0 && !prefillSolved && cfg?.Model.Provider is LLmProviders.Anthropic)
        {
            prefillSolved = true;
            chat.AppendMessage(ChatMessageRoles.Assistant, prefill.Trim()); // note: anthropic requires that the prefill doesn't end with trailing whitespace
        }
    }
    
    /// <summary>
    /// Use only when using chat as a service
    /// </summary>
    /// <param name="message"></param>
    public void AddUserMessage(List<ChatMessagePart> message)
    {
        // gemini 2.0 requires user input contains text part
        /*if (cfg?.Model is ChatModel.Gemini25Flash)
        {
            if (message.All(x => x.Type is ChatMessageTypes.Image))
            {
                message.Insert(0, new ChatMessagePart(string.Empty));
            }
        }*/
        
        if (chat is null)
        {
            InitChatIfNull();
        }
        
        Guid msgId = Guid.NewGuid();

        chat?.AppendUserInput(message, msgId);
        functionsCalledInSuccession = 0;
    }

    /// <summary>
    /// Use only when using chat as a service
    /// </summary>
    /// <param name="message"></param>
    public void AddUserMessage(string message)
    {
        if (chat is null)
        {
            InitChatIfNull();
        }
        
        message = message.Trim();

        chat?.AppendUserInput(message);
        CheckPrefill();
        
        functionsCalledInSuccession = 0;
    }

    public async Task<LlmResponseRaw> GetLlmResponseRaw()
    {
        if (chat == null)
        {
            InitChatIfNull();
        }

        if (chat == null)
        {
            return new LlmResponseRaw {Error = LlmResponseErrors.Generic};
        }

        TrimContext();

        try
        {
            RestDataOrException<ChatChoice> response = await chat.GetResponseSafe();
            return new LlmResponseRaw { Response = response.Data, Ok = true };
        }
        catch (Exception e)
        {
            return new LlmResponseRaw { Error = LlmResponseErrors.Generic };
        }
    }

    public async Task<DataOrException<bool>> GetLlmStreamingResponse(Action<int, string> onChunkArrives)
    {
        if (chat is null)
        {
            InitChatIfNull();
        }
        
        if (chat is null)
        {
            return new DataOrException<bool>(new Exception("Chat instance not setup"));
        }

        TrimContext();

        int index = 0;
        
        try
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenHandler = (token) =>
                {
                    if (token is null)
                    {
                        return ValueTask.CompletedTask;
                    }
                    
                    onChunkArrives.Invoke(index, token);
                    index++;
                    return ValueTask.CompletedTask;
                }
            });
            
            return new DataOrException<bool>(true);
        }
        catch (Exception e)
        {
            return new DataOrException<bool>(e);
        }
    }

    public async Task<LlmResponseParsedRich> GetLlmResponseRich(bool mergeBlockMsgs = false)
    {
        if (chat is null)
        {
            InitChatIfNull();
        }

        if (chat is null)
        {
            return new LlmResponseParsedRich { Error = LlmResponseErrors.Generic };
        }

        TrimContext();

        using CancellationTokenSource cts = new CancellationTokenSource(180_000);
        CancellationToken ct = cts.Token; // never wait more than three minutes, consider such requests bricked and bail
        
        try
        {
            ChatRichResponse? response = null;
            
            if (chat.RequestParameters.Tools?.Count > 0)
            {
                response = await chat.GetResponseRich(async functionInfo =>
                {
                    if (functionInfo.Count is 0)
                    {
                        return;
                    }
                
                    foreach (FunctionCall fi in functionInfo)
                    {
                        Dictionary<string, object?>? args = fi.Arguments.JsonDecode<Dictionary<string, object?>>();
                        
                        // fix for Claude 3 models which sometimes return the required schema encapsulated in "properties" property, prolly due to the
                        // training data containing the JSON schema capsule both in input and output examples.
                        if (args?.TryGetValue("properties", out object? innerVal) ?? false)
                        {
                            args = innerVal?.ToString().JsonDecode<Dictionary<string, object?>>();
                        }
                        
                        fi.Result = await pluginCompiler.Call(fi, args);
                    }
                }, ct);                
            }
            else
            {
                RestDataOrException<ChatRichResponse> ilResult2 = await chat.GetResponseRichSafe(ct);
                
                if (ilResult2.Data is not null)
                {
                    response = ilResult2.Data;
                }
            }
            
            ChatResult last = chat.MostRecentApiResult;

            if (response is null)
            {
                return new LlmResponseParsedRich { Error = LlmResponseErrors.NoResponse };
            }
            
            List<LlmResponseRichBlock> parsedBlocks = [];

            if (response.Blocks is not null)
            {
                string cumulativeMsg = string.Empty;

                if (mergeBlockMsgs)
                {
                    foreach (ChatRichResponseBlock block in response.Blocks)
                    {
                        if (block.Type is ChatRichResponseBlockTypes.Message && block.Message.IsNullOrWhiteSpace())
                        {
                            continue;
                        }

                        if (block.Type is ChatRichResponseBlockTypes.Message)
                        {
                            cumulativeMsg += block.Message;   
                        }
                    }
                }
                
                foreach (ChatRichResponseBlock block in response.Blocks)
                {
                    if (block.Type is ChatRichResponseBlockTypes.Message && block.Message.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    switch (block.Type)
                    {
                        case ChatRichResponseBlockTypes.Message:
                        {
                            parsedBlocks.Add(new LlmResponseRichBlock { Message = block.Message, Kind = ChatRichResponseBlockTypes.Message });
                            break;
                        }
                        case ChatRichResponseBlockTypes.Function:
                        {
                            parsedBlocks.Add(new LlmResponseRichBlock { Kind = ChatRichResponseBlockTypes.Function, FunctionResult = block.FunctionCall?.Result });
                            break;
                        }
                        case ChatRichResponseBlockTypes.Image:
                        {
                            parsedBlocks.Add(new LlmResponseRichBlock { Kind = ChatRichResponseBlockTypes.Image }); // todo: store block result
                            break;
                        }
                    }
                }
            }

            return new LlmResponseParsedRich
            {
                Ok = true,
                Blocks = parsedBlocks
            };
        }
        catch (Exception e)
        {
            return new LlmResponseParsedRich { Error = LlmResponseErrors.Generic, NativeException = e };
        }
    }

    public TornadoRequestContent Serialize(ChatRequestSerializeOptions? options = null)
    {
        if (chat is null)
        {
            InitChatIfNull();
        }
        
        return chat?.Serialize(options) ?? TornadoRequestContent.Dummy;
    }

    public async Task<LlmResponseParsed> GetLlmResponse(bool mergeBlockMsgs = false)
    {
        if (chat is null)
        {
            InitChatIfNull();
        }

        if (chat is null)
        {
            return new LlmResponseParsed {Error = LlmResponseErrors.Generic};
        }

        TrimContext();

        using CancellationTokenSource cts = new CancellationTokenSource(180_000);
        CancellationToken ct = cts.Token; // never wait more than three minutes, consider such requests bricked and bail
        LlmResponseErrors? errorType = null;
        
        try
        {
            RestDataOrException<ChatRichResponse>? response = null;
            
            if (chat.RequestParameters.Tools?.Count > 0)
            {
                response = await chat.GetResponseRichSafe(async functionInfo =>
                {
                    if (functionInfo.Count is 0)
                    {
                        return;
                    }
                
                    foreach (FunctionCall fi in functionInfo)
                    {
                        Dictionary<string, object?>? args = fi.Arguments.JsonDecode<Dictionary<string, object?>>();
                        
                        // fix for Claude 3 models which sometimes return the required schema encapsulated in "properties" property, prolly due to the
                        // training data containing the JSON schema capsule both in input and output examples.
                        if (args?.TryGetValue("properties", out object? innerVal) ?? false)
                        {
                            args = innerVal?.ToString().JsonDecode<Dictionary<string, object?>>();
                        }

                        // handle errors on our side in a closure to avoid useless retrying of the llm calls
                        try
                        {
                            fi.Result = await pluginCompiler.Call(fi, args);
                        }
                        catch (Exception e)
                        {
                            if (response is not null)
                            {
                                response.Exception = e;
                                errorType = LlmResponseErrors.ToolProcessing;
                            }
                            
                            return;
                        }
                    }
                }, ct);                
            }
            else
            {
                RestDataOrException<ChatRichResponse> ilResult = await chat.GetResponseRichSafe(ct);

                if (ilResult.Data is not null)
                {
                    response = new RestDataOrException<ChatRichResponse>(new ChatRichResponse(null, null)
                    {
                        Blocks = [
                            new ChatRichResponseBlock
                            {
                                Message = ilResult.Data.Text,
                                Type = ChatRichResponseBlockTypes.Message
                            }
                        ]
                    }, ilResult.HttpResult);
                }
            }
            
            ChatResult? last = chat.MostRecentApiResult;

            if (response is null)
            {
                return new LlmResponseParsed
                {
                    Error = LlmResponseErrors.NoResponse
                };
            }

            if (response.Data is null || response.Exception is not null)
            {
                return new LlmResponseParsed
                {
                    Error = errorType ?? LlmResponseErrors.Generic,
                    NativeException = response.Exception,
                    HttpRequest = response.HttpRequest,
                    HttpResult = response.HttpResult
                };
            }

            if (response.Data.Blocks is not null)
            {
                string cumulativeMsg = string.Empty;

                if (mergeBlockMsgs)
                {
                    foreach (ChatRichResponseBlock block in response.Data.Blocks)
                    {
                        if (block.Type is ChatRichResponseBlockTypes.Message && block.Message.IsNullOrWhiteSpace())
                        {
                            continue;
                        }
                        
                        cumulativeMsg += block.Message;   
                    }
                }
                
                foreach (ChatRichResponseBlock block in response.Data.Blocks)
                {
                    if (block.Type is ChatRichResponseBlockTypes.Message && block.Message.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    
                    if (block.Type is ChatRichResponseBlockTypes.Function)
                    {
                        return new LlmResponseParsed
                        {
                            Ok = true, 
                            Message = cumulativeMsg.IsNullOrWhiteSpace() ? block.Message : cumulativeMsg, 
                            FunctionResult = block.FunctionCall?.Result, 
                            Kind = ChatRichResponseBlockTypes.Function,
                            HttpRequest = response.HttpRequest,
                            HttpResult = response.HttpResult
                        };
                    }
                }
            }

            return new LlmResponseParsed
            {
                Ok = true, 
                Message = response.Data.Blocks?.FirstOrDefault()?.Message, 
                FunctionResult = response.Data.Blocks?.FirstOrDefault()?.FunctionCall?.Result, 
                Kind = response.Data.Blocks?.FirstOrDefault()?.Type ?? ChatRichResponseBlockTypes.Message,
                HttpRequest = response.HttpRequest,
                HttpResult = response.HttpResult
            };
        }
        catch (Exception e)
        {
            return new LlmResponseParsed
            {
                Error = LlmResponseErrors.Generic, 
                NativeException = e
            };
        }
    }

    private void TrimContext()
    {
        
    }
    
    private void AddContextTokens(int n)
    {
        contextTokens += n;
    }
}
#endif