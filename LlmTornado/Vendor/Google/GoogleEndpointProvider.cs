using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Files;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public override string ApiUrl(CapabilityEndpoints endpoint, string? url)
    {
        const string baseUrl = "https://generativelanguage.googleapis.com/";
        const string baseUrlVersion = $"{baseUrl}v1beta/";
        
        switch (endpoint)
        {
            case CapabilityEndpoints.None:
            {
                return url ?? string.Empty;
            }
            case CapabilityEndpoints.BaseUrlStripped:
            {
                return $"{baseUrl}{url}";
            }
            case CapabilityEndpoints.BaseUrl:
            {
                return $"{baseUrlVersion}{url}";
            }
            default:
            {
                string eStr = endpoint switch
                {
                    CapabilityEndpoints.Chat => "models",
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

        await using JsonTextReader jsonReader = new JsonTextReader(reader);
        JsonSerializer serializer = new JsonSerializer();
            
        if (await jsonReader.ReadAsync() && jsonReader.TokenType is JsonToken.StartArray)
        {
            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType is JsonToken.StartObject)
                {
                    VendorGoogleChatResult? obj = serializer.Deserialize<VendorGoogleChatResult>(jsonReader);

                    if (obj is not null)
                    {
                        foreach (VendorGoogleChatResult.VendorGoogleChatResultMessage candidate in obj.Candidates)
                        {
                            foreach (VendorGoogleChatRequest.VendorGoogleChatRequestMessagePart part in candidate.Content.Parts)
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
    
    public override T? InboundMessage<T>(string jsonData, string? postData) where T : default
    {
        if (typeof(T) == typeof(ChatResult))
        {
            return (T?)(object?)ChatResult.Deserialize(LLmProviders.Google, jsonData, postData);
        }

        if (typeof(T) == typeof(TornadoFile))
        {
            return (T?)(object?)FileUploadRequest.Deserialize(LLmProviders.Google, jsonData, postData);
        }
        
        
        
        return JsonConvert.DeserializeObject<T>(jsonData);
    }

    public override object? InboundMessage(Type type, string jsonData, string? postData)
    {
        return JsonConvert.DeserializeObject(jsonData, type);
    }
}