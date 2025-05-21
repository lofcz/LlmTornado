using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using LlmTornado.Caching;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Code.Sse;
using LlmTornado.Embedding;
using LlmTornado.Files;
using LlmTornado.Images;
using LlmTornado.Models.Vendors;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// 
/// </summary>
internal class GoogleEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
    private static readonly HashSet<string> toolFinishReasons = [ "tool_use" ];
    
    public static Version OutboundVersion { get; set; } = HttpVersion.Version20;
    public override HashSet<string> ToolFinishReasons => toolFinishReasons;
    public Func<CapabilityEndpoints, string?, string>? UrlResolver { get; set; } 
    
    public GoogleEndpointProvider(TornadoApi api) : base(api)
    {
        Provider = LLmProviders.Google;
        StoreApiAuth();
    }
    
    public const string BaseUrl = "https://generativelanguage.googleapis.com/";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public override string ApiUrl(CapabilityEndpoints endpoint, string? url)
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
                string eStr = endpoint switch
                {
                    CapabilityEndpoints.Embeddings => "models",
                    CapabilityEndpoints.Chat => "models",
                    CapabilityEndpoints.ImageGeneration => "models",
                    CapabilityEndpoints.Files => "files",
                    CapabilityEndpoints.Caching => "cachedContents",
                    CapabilityEndpoints.Models => "models",
                    _ => throw new Exception($"Google doesn't support endpoint {endpoint}")
                };
        
                return UrlResolver is not null ? UrlResolver.Invoke(endpoint, url) : $"{baseUrlVersion}{eStr}{url}";
            }
        }
    }

    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request)
    {
        ChatMessage? plaintextAccu = null;
        ChatUsage? usage = null;
        ChatMessageFinishReasons finishReason = ChatMessageFinishReasons.Unknown;
        
        await using JsonTextReader jsonReader = new JsonTextReader(reader);
        JsonSerializer serializer = new JsonSerializer();

        // use for debugging to inspect the raw data:
        // string data = await reader.ReadToEndAsync();
        
        // whenever using json schema, the response is received as a series of plaintext events
        bool isBufferingTool = request.VendorExtensions?.Google?.ResponseSchema is not null || (request.Tools?.Any(x => x.Strict ?? false) ?? false);
        
        if (await jsonReader.ReadAsync(request.CancellationToken) && jsonReader.TokenType is JsonToken.StartArray)
        {
            while (await jsonReader.ReadAsync(request.CancellationToken))
            {
                if (jsonReader.TokenType is JsonToken.StartObject)
                {
                    VendorGoogleChatResult? obj = serializer.Deserialize<VendorGoogleChatResult>(jsonReader);

                    if (obj is not null)
                    {
                        foreach (VendorGoogleChatResult.VendorGoogleChatResultMessage candidate in obj.Candidates)
                        {
                            foreach (VendorGoogleChatRequestMessagePart part in candidate.Content.Parts)
                            {
                                if (part.Text is not null)
                                {
                                    plaintextAccu ??= new ChatMessage();
                                    plaintextAccu.ContentBuilder ??= new StringBuilder();
                                    plaintextAccu.ContentBuilder.Append(part.Text);
                                }
                            }
                        }
                        
                        ChatResult chatResult = obj.ToChatResult(null);
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

        if (plaintextAccu is not null)
        {
            if (isBufferingTool)
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
                                        FunctionCall = new FunctionCall
                                        {
                                            Name = request.Tools?.FirstOrDefault(x => x.Strict ?? false)?.Function?.Name ?? request.VendorExtensions?.Google?.ResponseSchema?.Function?.Name ?? string.Empty,
                                            Arguments = plaintextAccu.ContentBuilder?.ToString() ?? string.Empty
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
            
            yield return new ChatResult
            {
                Choices =
                [
                    new ChatChoice
                    {
                        Delta = new ChatMessage
                        {
                            Content = plaintextAccu.ContentBuilder?.ToString()
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

    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming)
    {
        ProviderAuthentication? auth = Api.GetProvider(LLmProviders.Google).Auth;
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
        return req;
    }

    public override void ParseInboundHeaders<T>(T res, HttpResponseMessage response)
    {
        res.Provider = this;
    }
    
    public override void ParseInboundHeaders(object? res, HttpResponseMessage response)
    {
        
    }

    private static readonly Dictionary<Type, Func<string, string?, object?>> InboundMap = new Dictionary<Type, Func<string, string?, object?>>
    {
        { typeof(ChatResult), (s, s1) => ChatResult.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(TornadoInputFile), (s, s1) => FileUploadRequest.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(CachedContentInformation), (s, s1) => CachedContentInformation.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(CachedContentList), (s, s1) => CachedContentList.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(ImageGenerationResult), (s, s1) => ImageGenerationResult.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(EmbeddingResult), (s, s1) => EmbeddingResult.Deserialize(LLmProviders.Google, s, s1) },
        { typeof(RetrievedModelsResult), (s, s1) => RetrievedModelsResult.Deserialize(LLmProviders.Google, s, s1) }
    };
    
    public override T? InboundMessage<T>(string jsonData, string? postData) where T : default
    {
        if (InboundMap.TryGetValue(typeof(T), out Func<string, string?, object?>? fn))
        {
            return (T?)fn.Invoke(jsonData, postData);
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

    public override object? InboundMessage(Type type, string jsonData, string? postData)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}