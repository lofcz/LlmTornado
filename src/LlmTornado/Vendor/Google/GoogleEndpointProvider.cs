using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using LlmTornado.Caching;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.ChatFunctions;
using LlmTornado.Code.Models;
using LlmTornado.Embedding;
using LlmTornado.Files;
using LlmTornado.Images;
using LlmTornado.Models.Vendors;
using LlmTornado.Threads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FunctionCall = LlmTornado.ChatFunctions.FunctionCall;
using ToolCall = LlmTornado.ChatFunctions.ToolCall;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// Built-in Google provider.
/// </summary>
public class GoogleEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
    private static readonly HashSet<string> toolFinishReasons = [ "tool_use" ];
    
    public static Version OutboundVersion { get; set; } = OutboundDefaultVersion;
    public Func<CapabilityEndpoints, string?, RequestUrlContext, string>? UrlResolver { get; set; }
    public Action<HttpRequestMessage, object?, bool>? RequestResolver { get; set; }
    public Action<JObject, RequestSerializerContext>? RequestSerializer { get; set; }
    
    public GoogleEndpointProvider()
    {
        Provider = LLmProviders.Google;
    }

    public const string BaseUrl = "https://generativelanguage.googleapis.com/";

    /// <summary>
    /// Gets endpoint url for a given capability.
    /// </summary>
    public static string GetEndpointUrlFragment(CapabilityEndpoints endpoint)
    {
        return endpoint switch
        {
            CapabilityEndpoints.Embeddings => "models",
            CapabilityEndpoints.Chat => "models",
            CapabilityEndpoints.ImageGeneration => "models",
            CapabilityEndpoints.Videos => "models",
            CapabilityEndpoints.Files => "files",
            CapabilityEndpoints.Caching => "cachedContents",
            CapabilityEndpoints.Models => "models",
            _ => throw new Exception($"Google doesn't support endpoint {endpoint}")
        };
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public override string ApiUrl(CapabilityEndpoints endpoint, string? url, IModel? model = null)
    {
        const string baseUrlVersion = $"{BaseUrl}v1beta/";
        
        switch (endpoint)
        {
            case CapabilityEndpoints.None:
            {
                return url ?? string.Empty;
            }
            case CapabilityEndpoints.BaseUrlStripped:
            {
                return $"{BaseUrl}{url}";
            }
            case CapabilityEndpoints.BaseUrl:
            {
                return $"{baseUrlVersion}{url}";
            }
            default:
            {
                string eStr = GetEndpointUrlFragment(endpoint);
                return UrlResolver is not null ? string.Format(UrlResolver.Invoke(endpoint, url, new RequestUrlContext(eStr, url, model)), eStr, url, model?.Name) : $"{baseUrlVersion}{eStr}{url}";
            }
        }
    }

    private static readonly HashSet<string> BadFinishReasons =
    [
        "FINISH_REASON_UNSPECIFIED",
        "STOP",
        "MAX_TOKENS",
        "SAFETY",
        "RECITATION",
        "OTHER",
        "BLOCKLIST",
        "PROHIBITED_CONTENT",
        "SPII",
        "MALFORMED_FUNCTION_CALL",
        "MODEL_ARMOR",
        "IMAGE_SAFETY",
        "IMAGE_PROHIBITED_CONTENT",
        "IMAGE_RECITATION",
        "IMAGE_OTHER",
        "UNEXPECTED_TOOL_CALL"
    ];

    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request, ChatStreamEventHandler? eventHandler)
    {
        ChatUsage? usage = null;
        ChatMessageFinishReasons finishReason = ChatMessageFinishReasons.Unknown;

        StringBuilder? plaintextAccu = null, reasoningAccu = null;
        
#if MODERN
        await using JsonTextReader jsonReader = new JsonTextReader(reader);
#else
        using JsonTextReader jsonReader = new JsonTextReader(reader);
#endif
        // whenever using json schema, the response is received as a series of plaintext events
        bool isBufferingTool = request.VendorExtensions?.Google?.ResponseSchema is not null;
        
        if (await jsonReader.ReadAsync(request.CancellationToken) && jsonReader.TokenType is JsonToken.StartArray)
        {
            while (await jsonReader.ReadAsync(request.CancellationToken))
            {
                if (jsonReader.TokenType is JsonToken.StartObject)
                {
                    JObject jsonObject = await JObject.LoadAsync(jsonReader, request.CancellationToken);
                    VendorGoogleChatResult? obj = jsonObject.ToObject<VendorGoogleChatResult>();

                    if (eventHandler?.OnSse is not null)
                    {
                        await eventHandler.OnSse.Invoke(new ServerSentEvent
                        {
                            Data = jsonObject.ToString(Formatting.Indented)
                        });
                    }
                    
                    if (obj is not null)
                    {
                        foreach (VendorGoogleChatResult.VendorGoogleChatResultMessage candidate in obj.Candidates)
                        {
                            if (candidate.Content.Parts is not null)
                            {
                                foreach (VendorGoogleChatRequestMessagePart part in candidate.Content.Parts)
                                {
                                    bool thought = part.Thought ?? false;
           
                                    if (part.Text is not null)
                                    {
                                        if (!thought)
                                        {
                                            plaintextAccu ??= new StringBuilder();
                                            plaintextAccu.Append(part.Text);
                                        }
                                        else
                                        {
                                            reasoningAccu ??= new StringBuilder();
                                            reasoningAccu.Append(part.Text);
                                        }
                                    }
                                }   
                            }
                        }
                        
                        ChatResult chatResult = obj.ToChatResult(null, request);
                        usage = chatResult.Usage;
                        
                        string? strFinishReason = obj.Candidates.FirstOrDefault()?.FinishReason;

                        if (strFinishReason is not null)
                        {
                            finishReason = ChatMessageFinishReasonsConverter.Map.GetValueOrDefault(strFinishReason, ChatMessageFinishReasons.Unknown);
                        }
                        
                        if (isBufferingTool)
                        {
                            continue;
                        }
                        
                        yield return chatResult;
                    }
                }
                else if (jsonReader.TokenType is JsonToken.EndArray)
                {
                    break;
                }
            }
        }

        if (plaintextAccu is not null || reasoningAccu is not null)
        {
            if (plaintextAccu is not null && isBufferingTool)
            {
                yield return new ChatResult
                {
                    Choices =
                    [
                        new ChatChoice
                        {
                            Delta = new ChatMessage
                            {
                                Role = ChatMessageRoles.Tool,
                                ToolCalls = [
                                    new ToolCall
                                    {
                                        Type = "function",
                                        FunctionCall = new FunctionCall(plaintextAccu.ToString())
                                        {
                                            Name = request.Tools?.FirstOrDefault(x => x.Strict ?? false)?.Function?.Name ?? request.VendorExtensions?.Google?.ResponseSchema?.Function?.Name ?? string.Empty
                                        }
                                    }
                                ]
                            }
                        }
                    ],
                    Usage = usage
                };
                
                yield break;
            }

            List<ChatMessagePart> parts = [];
            string? plaintext = plaintextAccu?.ToString();
            string? reasoning = reasoningAccu?.ToString();

            if (reasoning?.Length > 0)
            {
                parts.Add(new ChatMessagePart(ChatMessageTypes.Reasoning)
                {
                    Reasoning = new ChatMessageReasoningData
                    {
                        Content = reasoning
                    }
                });
            }

            if (plaintext?.Length > 0)
            {
                parts.Add(new ChatMessagePart(ChatMessageTypes.Text)
                {
                    Text = plaintext
                });
            }
            
            yield return new ChatResult
            {
                Choices =
                [
                    new ChatChoice
                    {
                        Delta = new ChatMessage
                        {
                            Parts = parts
                        }
                    }
                ],
                StreamInternalKind = ChatResultStreamInternalKinds.AppendAssistantMessage,
                Usage = usage
            };
        }
        
        yield return new ChatResult
        {
            Choices =
            [
                new ChatChoice
                {
                    FinishReason = finishReason
                }
            ],
            StreamInternalKind = ChatResultStreamInternalKinds.FinishData,
            Usage = usage
        };
    }
    
    public override async IAsyncEnumerable<object?> InboundStream(Type type, StreamReader reader)
    {
        yield break;
    }

    public override async IAsyncEnumerable<T?> InboundStream<T>(StreamReader reader) where T : class
    {
        yield break;
    }

    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming, object? sourceObject)
    {
        ProviderAuthentication? auth = Api?.GetProvider(LLmProviders.Google).Auth;
        UriBuilder uriBuilder = new UriBuilder(url);
        NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
        
        if (auth?.ApiKey is not null)
        {
            query["key"] = auth.ApiKey.Trim();
        }
        
        uriBuilder.Query = query.ToString();

        HttpRequestMessage req = new HttpRequestMessage(verb, uriBuilder.Uri) 
        {
            Version = OutboundVersion
        };

        req.Headers.Add("User-Agent", EndpointBase.GetUserAgent());

        RequestResolver?.Invoke(req, data, streaming);

        return req;
    }

    public override void ParseInboundHeaders<T>(T res, HttpResponseMessage response)
    {
        res.Provider = this;
    }
    
    public override void ParseInboundHeaders(object? res, HttpResponseMessage response)
    {
        
    }

    private static readonly Dictionary<Type, Func<string, string?, object?, object?>> InboundMap = new Dictionary<Type, Func<string, string?, object?, object?>>
    {
        { typeof(ChatResult), (s, s1, req) => ChatResult.Deserialize(LLmProviders.Google, s, s1, req) },
        { typeof(TornadoInputFile), (s, s1, req) => FileUploadRequest.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(CachedContentInformation), (s, s1, req) => CachedContentInformation.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(CachedContentList), (s, s1, req) => CachedContentList.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(ImageGenerationResult), (s, s1, req) => ImageGenerationResult.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(EmbeddingResult), (s, s1, req) => EmbeddingResult.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(RetrievedModelsResult), (s, s1, req) => RetrievedModelsResult.Deserialize(LLmProviders.Google, s, s1) }
    };
    
    public override T? InboundMessage<T>(string jsonData, string? postData, object? request) where T : default
    {
        if (InboundMap.TryGetValue(typeof(T), out Func<string, string?, object?, object?>? fn))
        {
            return (T?)fn.Invoke(jsonData, postData, request);
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(jsonData);
        }
        catch (Exception e)
        {
            return default;
        }
    }

    public override object? InboundMessage(Type type, string jsonData, string? postData, object? request)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}