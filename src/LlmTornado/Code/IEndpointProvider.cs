using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Code.Models;
using LlmTornado.Threads;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Code;

/// <summary>
/// Interface for all endpoint providers.
/// </summary>
public interface IEndpointProvider
{
    /// <summary>
    /// Invoked to construct outbound messages.
    /// </summary>
    public HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming);

    /// <summary>
    /// Invoked to parse inbound messages.
    /// </summary>
    public T? InboundMessage<T>(string jsonData, string? postData);
    
    /// <summary>
    /// Invoked to parse inbound messages.
    /// </summary>
    public object? InboundMessage(Type type, string jsonData, string? postData);

    /// <summary>
    /// Invoked to parse headers of inbound requests.
    /// </summary>
    public void ParseInboundHeaders<T>(T res, HttpResponseMessage response) where T : ApiResultBase;
    
    /// <summary>
    /// Invoked to parse headers of inbound requests.
    /// </summary>
    public void ParseInboundHeaders(object? res, HttpResponseMessage response);
    
    /// <summary>
    /// Invoked to process any inbound streams where type conditionally changes.
    /// </summary>
    public IAsyncEnumerable<object?> InboundStream(Type type, StreamReader streamReader);

    /// <summary>
    /// Raw SSE stream.
    /// </summary>
    public IAsyncEnumerable<ServerSentEvent> InboundStream(StreamReader reader);
    
    /// <summary>
    /// Invoked to process any inbound streams where type is known ahead of time.
    /// </summary>
    public IAsyncEnumerable<T?> InboundStream<T>(StreamReader streamReader) where T : class;
    
    /// <summary>
    /// Streaming for chat requests.
    /// </summary>
    IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request, ChatStreamEventHandler? eventHandler);
    
    /// <summary>
    /// API instance owning this provider. Note that this is not null only after the provider is enlisted in the API.
    /// </summary>
    public TornadoApi? Api { get; set; }
    
    /// <summary>
    /// Known/custom provider.
    /// </summary>
    public LLmProviders Provider { get; set; }
    
    /// <summary>
    /// API url resolver.
    /// </summary>
    public string ApiUrl(CapabilityEndpoints endpoint, string? url, IModel? model = null);

    /// <summary>
    /// Authentication.
    /// </summary>
    public ProviderAuthentication? Auth { get; set; }
    
    /// <summary>
    /// Invoked when resolving concrete API url.<br/>
    /// Arguments: endpoint, url (if any), context. This function can return deferred fragments <c>{0}</c>, <c>{1}</c>, <c>{2}</c> which are resolved to endpoint and url fragment, for example:<br/>
    /// <c>https://api.anthropic.com/v1/{0}{1}</c>.<br/>Alternatively, fully resolved url can be returned.<br/>
    /// Fragments substituted by the library:<br/>
    /// <c>{0}</c> = endpoint<br/>
    /// <c>{1}</c> = action<br/>
    /// <c>{2}</c> = model's name (if any)
    /// </summary>
    public Func<CapabilityEndpoints, string?, RequestUrlContext, string>? UrlResolver { get; set; }
    
    /// <summary>
    /// Invoked when outbound request is constructed. Can be used to customize headers/url/content.<br/>
    /// Arguments: the request, outbound data, whether the request is streaming or not.
    /// </summary>
    public Action<HttpRequestMessage, object?, bool>? RequestResolver { get; set; }
    
    /// <summary>
    /// Invoked before outbound request's body is serialized into string.<br/>
    /// Arguments: the request, context.<br/>
    /// Note: this callback is not invoked for every request. Currently only the following actions are supported:<br/>
    /// <see cref="RequestActionTypes.ChatCompletionCreate"/>, <see cref="RequestActionTypes.EmbeddingCreate"/>
    /// </summary>
    public Action<JObject, RequestSerializerContext>? RequestSerializer { get; set; }
}

/// <summary>
/// Context for request url.
/// </summary>
public class RequestUrlContext
{
    /// <summary>
    /// Endpoint fragment - {0}.
    /// </summary>
    public string Endpoint { get; set; }
    
    /// <summary>
    /// Action fragment - {1}.
    /// </summary>
    public string? Action { get; set; }
    
    /// <summary>
    /// Associated model (if any) - {2}.
    /// </summary>
    public IModel? Model { get; set; }

    internal RequestUrlContext(string endpoint, string? action, IModel? model)
    {
        Endpoint = endpoint;
        Action = action;
        Model = model;
    }
}

/// <summary>
/// Context for request serializer.
/// </summary>
public class RequestSerializerContext
{
    /// <summary>
    /// Source of the request. This object was serialized into JObject. 
    /// </summary>
    public object SourceObject { get; set; }
    
    /// <summary>
    /// Provider associated with this request.
    /// </summary>
    public IEndpointProvider Provider { get; set; }

    /// <summary>
    /// Action type.
    /// </summary>
    public RequestActionTypes Type { get; set; }
    
    internal RequestSerializerContext(object sourceObject, IEndpointProvider provider, RequestActionTypes type)
    {
        SourceObject = sourceObject;
        Provider = provider;
        Type = type;
    } 
}

/// <summary>
/// Known action types.
/// </summary>
public enum RequestActionTypes
{
    /// <summary>
    /// Misc / custom actions.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Creates a chat completion.
    /// </summary>
    ChatCompletionCreate,
    
    /// <summary>
    /// Gets a chat completion.
    /// </summary>
    ChatCompletionGet,
    
    /// <summary>
    /// Get the messages in a stored chat completion.
    /// </summary>
    ChatMessageGet,
    
    /// <summary>
    /// List stored Chat Completions.
    /// </summary>
    ChatCompletionList,
    
    /// <summary>
    /// Modify a stored chat completion.
    /// </summary>
    ChatCompletionUpdate,
    
    /// <summary>
    /// Delete a stored chat completion.
    /// </summary>
    ChatCompletionDelete,
    
    /// <summary>
    /// Generates audio from the input text.
    /// </summary>
    AudioSpeechCreate,
    
    /// <summary>
    /// Transcribes audio into the input language.
    /// </summary>
    AudioTranscriptionCreate,
    
    /// <summary>
    /// Translates audio.
    /// </summary>
    AudioTranslationCreate,
    
    /// <summary>
    /// Creates an image given a prompt.
    /// </summary>
    ImageCreate,
    
    /// <summary>
    /// Creates an edited or extended image given one or more source images and a prompt.
    /// </summary>
    ImageEditCreate,
    
    /// <summary>
    /// Creates a variation of a given image.
    /// </summary>
    ImageVariationCreate,
    
    /// <summary>
    /// Creates an embedding vector representing the input text.
    /// </summary>
    EmbeddingCreate,
    
    /// <summary>
    /// Upload a file that can be used across various endpoints. 
    /// </summary>
    FileUpload,
    
    /// <summary>
    /// Returns a list of files.
    /// </summary>
    FileList,
    
    /// <summary>
    /// Returns information about a specific file.
    /// </summary>
    FileGet,
    
    /// <summary>
    /// Delete a file.
    /// </summary>
    FileDelete,
    
    /// <summary>
    /// Creates an intermediate Upload object that you can add Parts to. 
    /// </summary>
    UploadCreate,
    
    /// <summary>
    /// Adds a Part to an Upload object.
    /// </summary>
    UploadAddPart,
    
    /// <summary>
    /// Completes the Upload.
    /// </summary>
    UploadComplete,
    
    /// <summary>
    /// Cancels the Upload. No Parts may be added after an Upload is cancelled.
    /// </summary>
    UploadCancel,
    
    /// <summary>
    /// Lists the currently available models, and provides basic information about each one such as the owner and availability.
    /// </summary>
    ModelList,
    
    /// <summary>
    /// Retrieves a model instance, providing basic information about the model such as the owner and permissioning.
    /// </summary>
    ModelGet,
    
    /// <summary>
    /// Delete a fine-tuned model. You must have the Owner role in your organization to delete a model.
    /// </summary>
    ModelDelete,
    
    /// <summary>
    /// Classifies if text and/or image inputs are potentially harmful. Learn more in the moderation guide.
    /// </summary>
    ModerationCreate,
    
    /// <summary>
    /// Create a vector store.
    /// </summary>
    VectorStoreCreate,
    
    /// <summary>
    /// Returns a list of vector stores.
    /// </summary>
    VectorStoreList,
    
    /// <summary>
    /// Retrieves a vector store.
    /// </summary>
    VectorStoreGet,
    
    /// <summary>
    /// Modifies a vector store.
    /// </summary>
    VectorStoreUpdate,
    
    /// <summary>
    /// Delete a vector store.
    /// </summary>
    VectorStoreDelete,
    
    /// <summary>
    /// Search a vector store for relevant chunks based on a query and file attributes filter.
    /// </summary>
    VectorStoreSearch
}

/// <summary>
/// Extended capabilities providers.
/// </summary>
public interface IEndpointProviderExtended
{
    /// <summary>
    /// Gets version of the protocol.
    /// </summary>
    public abstract Version OutboundVersion { get; set; }
}