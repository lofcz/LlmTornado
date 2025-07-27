using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Threads;
using FunctionCall = LlmTornado.ChatFunctions.FunctionCall;
using LlmTornado.Responses.Events;

namespace LlmTornado.Chat;

/// <summary>
///     Enables subscribing to various streaming events.
/// </summary>
public class ChatStreamEventHandler
{
    /// <summary>
    ///     Called when plaintext token/chunk arrives. This can be forwarded to the end-user immediately.
    /// </summary>
    public Func<string?, ValueTask>? MessageTokenHandler { get; set; }
    
    /// <summary>
    ///     Called when plaintext token/chunk arrives. Contains extended information about the incoming token.
    /// </summary>
    public Func<StreamedMessageToken, ValueTask>? MessageTokenExHandler { get; set; }
    
    /// <summary>
    ///     Called when reasoning token/chunk arrives. This can be forwarded to the end-user immediately.
    ///     Both content and signature might be empty. If content is empty and signature isn't, the block is redacted.
    ///     If content is not empty and signature is empty, the signature is yet to arrive.
    /// </summary>
    public Func<ChatMessageReasoningData, ValueTask>? ReasoningTokenHandler { get; set; }
    
    /// <summary>
    ///     Called when a message block is fully streamed.
    /// </summary>
    public Func<ChatMessage?, ValueTask>? BlockFinishedHandler { get; set; }
    
    /// <summary>
    ///     Called when audio token/chunk arrives. This can be forwarded to the end-user immediately.
    /// </summary>
    public Func<ChatMessageAudio, ValueTask>? AudioTokenHandler { get; set; }
    
    /// <summary>
    ///     Called when audio token/image arrives. This can be forwarded to the end-user immediately.
    /// </summary>
    public Func<ChatImage, ValueTask>? ImageTokenHandler { get; set; }
    
    /// <summary>
    ///     Called when a message part arrives. This message part can contain text, images, audio, or other modalities. This can be forwarded to the end-user immediately.
    ///     This is a fired before modality specific handlers, such as token, audio, reasoning, etc.
    /// </summary>
    public Func<ChatMessagePart, ValueTask>? MessagePartHandler { get; set; }
    
    /// <summary>
    ///     Called when one or more tools are to be executed. Execute the tools and return the responses in <see cref="ChatFunctions.FunctionCall.Result"/>.
    ///     If this field is empty once control is returned to the API, the tool call is considered to be failed with no data returned.
    /// </summary>
    public Func<List<FunctionCall>, ValueTask>? FunctionCallHandler { get; set; }
    
    /// <summary>
    ///     If this handler isn't null, results from tool calls with delegates attached are automatically added to the conversation.
    /// </summary>
    public ToolCallsHandler? ToolCallsHandler { get; set; }
    
    /// <summary>
    ///     Called after <see cref="FunctionCallHandler"/> and internal upkeep. Use this handler to implement tool request -> tool execution -> model response pattern.
    /// </summary>
    public Func<ResolvedToolsCall, ChatStreamEventHandler?, ValueTask>? AfterFunctionCallsResolvedHandler { get; set; } 
    
    /// <summary>
    ///     Called when the first event arrives from the Provider. This can be used to inform the end-user early in the process about the kind of response the model selected.
    /// </summary>
    public Func<ChatMessageRoles, ValueTask>? MessageTypeResolvedHandler { get; set; }
    
    /// <summary>
    ///     Called once, before the streaming request is established. Use this to mutate the request if necessary. 
    /// </summary>
    public Func<ChatRequest, ValueTask<ChatRequest>>? MutateChatRequestHandler { get; set; }
    
    /// <summary>
    ///     Called for events supported only by specific vendors with no shared equivalent.
    /// </summary>
    public Func<ChatResponseVendorExtensions, ValueTask>? VendorFeaturesHandler { get; set; }
    
    /// <summary>
    ///     Called whenever the bill arrives.
    /// </summary>
    public Func<ChatUsage, ValueTask>? OnUsageReceived { get; set; }
    
    /// <summary>
    ///     Called after the streaming completes, this can be used for debugging finish_reason and other metadata.
    /// </summary>
    public Func<ChatStreamFinishedData, ValueTask>? OnFinished { get; set; }
    
    /// <summary>
    ///     Called when raw server-sent event data arrives. This handler receives the raw SSE data before any parsing.
    /// </summary>
    public Func<ServerSentEvent, ValueTask>? OnSse { get; set; }
    
    /// <summary>
    ///     Called whenever a successful HTTP request is made. In case of streaming requests this is called before the stream is read.
    /// </summary>
    public Func<HttpCallRequest, ValueTask>? OutboundHttpRequestHandler { get; set; }
    
    /// <summary>
    ///     If this is set, HTTP level exceptions are caught and returned via this handler.
    /// </summary>
    public Func<HttpFailedRequest, ValueTask>? HttpExceptionHandler { get; set; }
    
    /// <summary>
    ///     Called when any response event arrives. This handler receives the event as IResponsesEvent interface.
    /// </summary>
    public Func<IResponseEvent, ValueTask>? OnResponseEvent { get; set; }
    
    /// <summary>
    ///     The ID of the message that will be appended to the conversation, if null a random GUID is used.
    /// </summary>
    public Guid? MessageId { get; set; }
}