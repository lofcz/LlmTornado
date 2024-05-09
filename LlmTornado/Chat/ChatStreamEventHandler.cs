using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;

namespace LlmTornado.Chat;

/// <summary>
///     Enables subscribing to various streaming events.
/// </summary>
public class ChatStreamEventHandler
{
    /// <summary>
    ///     Called when plaintext token/chunk arrives. This can be forwarded to the end-user immediately.
    /// </summary>
    public Func<string?, Task>? MessageTokenHandler { get; set; }
    
    /// <summary>
    ///     Called when one or more tools are to be executed. Execute the tools and return the responses in <see cref="FunctionCall.Result"/>.
    ///     If this field is empty once control is returned to the API, the tool call is considered to be failed with no data returned.
    /// </summary>
    public Func<List<FunctionCall>, Task>? FunctionCallHandler { get; set; }
    
    /// <summary>
    ///     Called after <see cref="FunctionCallHandler"/> and internal upkeep. Use this handler to implement tool request -> tool execution -> model response pattern.
    /// </summary>
    public Func<ResolvedToolsCall, ChatStreamEventHandler?, Task>? AfterFunctionCallsResolvedHandler { get; set; } 
    
    /// <summary>
    ///     Called when the first event arrives from the Provider. This can be used to inform the end-user early in the process about the kind of response the model selected.
    /// </summary>
    public Func<ChatMessageRoles, Task>? MessageTypeResolvedHandler { get; set; }
    
    /// <summary>
    ///     Called once, before the streaming request is established. Use this to mutate the request if necessary. 
    /// </summary>
    public Func<ChatRequest, Task<ChatRequest>>? OutboundRequestHandler { get; set; }
    
    /// <summary>
    ///     Called for events supported only by specific vendors with no shared equivalent.
    /// </summary>
    public Func<ChatResponseVendorExtensions, Task>? VendorFeaturesHandler { get; set; }
    
    /// <summary>
    ///     Called whenever the bill arrives.
    /// </summary>
    public Func<ChatUsage, Task>? OnUsageReceived { get; set; }
    
    /// <summary>
    ///     The ID of the message that will be appended to the conversation, if null a random GUID is used.
    /// </summary>
    public Guid? MessageId { get; set; }
}