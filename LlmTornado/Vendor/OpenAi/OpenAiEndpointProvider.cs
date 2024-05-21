using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using Newtonsoft.Json;

namespace LlmTornado.Code.Vendor;

/// <summary>
/// 
/// </summary>
internal class OpenAiEndpointProvider : BaseEndpointProvider, IEndpointProvider, IEndpointProviderExtended
{
    private const string DataString = "data:";
    private const string DoneString = "[DONE]";
    private static readonly HashSet<string> toolFinishReasons = [ "function_call", "tool_calls" ];

    public static Version OutboundVersion { get; set; } = HttpVersion.Version20;
    public override HashSet<string> ToolFinishReasons => toolFinishReasons;
    
    public OpenAiEndpointProvider(TornadoApi api) : base(api)
    {
        Provider = LLmProviders.OpenAi;
        StoreApiAuth();
    }

    enum ChatStreamParsingStates
    {
        Text,
        Tools
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public override string ApiUrl(CapabilityEndpoints endpoint, string? url)
    {
        string eStr = endpoint switch
        {
            CapabilityEndpoints.Audio => "audio",
            CapabilityEndpoints.Chat => "chat/completions",
            CapabilityEndpoints.Completions => "completions",
            CapabilityEndpoints.Embeddings => "embeddings",
            CapabilityEndpoints.FineTuning => "fine_tuning",
            CapabilityEndpoints.Files => "files",
            CapabilityEndpoints.ImageGeneration => "images/generations",
            CapabilityEndpoints.ImageEdit => "images/edits",
            CapabilityEndpoints.Models => "models",
            CapabilityEndpoints.Moderation => "moderations",
            CapabilityEndpoints.Assistants => "assistants",
            CapabilityEndpoints.Threads => "threads",
            _ => throw new Exception($"OpenAI doesn't support endpoint {endpoint}")
        };

        return $"{string.Format(Api.ApiUrlFormat ?? "https://api.openai.com/{0}/{1}", Api.ApiVersion, eStr)}{url}";
    }
    
    public override HttpRequestMessage OutboundMessage(string url, HttpMethod verb, object? data, bool streaming)
    {
        HttpRequestMessage req = new(verb, url)
        {
            Version = OutboundVersion
        };
        req.Headers.Add("User-Agent", EndpointBase.GetUserAgent().Trim());
        req.Headers.Add("OpenAI-Beta", "assistants=v2");

        ProviderAuthentication? auth = Api.GetProvider(LLmProviders.OpenAi).Auth;
        
        if (auth is not null)
        {
            if (auth.ApiKey is not null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.ApiKey.Trim());
                req.Headers.Add("api-key", auth.ApiKey.Trim());
            }

            if (auth.Organization is not null)
            {
                req.Headers.Add("OpenAI-Organization", auth.Organization.Trim());
            }
        }
        
        return req;
    }

    public override void ParseInboundHeaders<T>(T res, HttpResponseMessage response)
    {
        res.Provider = this;

        if (response.Headers.TryGetValues("Openai-Organization", out IEnumerable<string>? orgH))
        {
            res.Organization = orgH.FirstOrDefault();
        }

        if (response.Headers.TryGetValues("X-Request-ID", out IEnumerable<string>? reqId))
        {
            res.RequestId = reqId.FirstOrDefault();
        }

        if (response.Headers.TryGetValues("Openai-Processing-Ms", out IEnumerable<string>? pms))
        {
            string? processing = pms.FirstOrDefault();
            
            if (processing is not null && int.TryParse(processing, out int n))
            {
                res.ProcessingTime = TimeSpan.FromMilliseconds(n);
            }
        }

        if (response.Headers.TryGetValues("Openai-Version", out IEnumerable<string>? oav))
        {
            res.RequestId = oav.FirstOrDefault();
        }
    }
    
    public override T? InboundMessage<T>(string jsonData, string? postData) where T : default
    {
        return JsonConvert.DeserializeObject<T>(jsonData);
    }

    public override async IAsyncEnumerable<ChatResult?> InboundStream(StreamReader reader, ChatRequest request)
    {
        ChatStreamParsingStates state = ChatStreamParsingStates.Text;
        bool parseTools = request.Tools?.Count > 0;
        ChatResult? toolsAccumulator = null;
        ChatMessage? toolsMessage = null;
        StringBuilder? plaintextBuilder = null;
        ChatUsage? usage = null;
        
        List<string> data = [];
        
        while (await reader.ReadLineAsync() is { } line)
        {
            data.Add(line);
            
            if (line.IsNullOrWhiteSpace())
            {
                continue;
            }
            
            if (line.StartsWith(':'))
            {
                continue;
            }
            
            if (line.Length > 4 && line[4] == ':') // line.StartsWith(DataString)
            {
                line = line[DataString.Length..];
            }

            line = line.TrimStart();
            
            if (string.Equals(line, DoneString, StringComparison.InvariantCulture))
            {
                goto afterStreamEnds;
            }

            ChatResult? res = JsonConvert.DeserializeObject<ChatResult>(line);

            if (res is null)
            {
                continue;
            }
            
            if (request.StreamOptions?.IncludeUsage ?? false)
            {
                if (usage is null && res.Choices is null || res.Choices?.Count is 0)
                {
                    usage = res.Usage;
                    continue;
                }
            }
            
            if (parseTools)
            {
                switch (state)
                {
                    case ChatStreamParsingStates.Text when res is ChatResult { Choices.Count: > 0 } && res.Choices[0].Delta?.ToolCalls?.Count > 0:
                    {
                        toolsAccumulator = res;
                        toolsMessage = res.Choices[0].Delta;
                        state = ChatStreamParsingStates.Tools;

                        if (toolsMessage is not null)
                        {
                            toolsMessage.ToolCallsDict = [];

                            if (toolsMessage.ToolCalls is not null)
                            {
                                foreach (ToolCall toolCall in toolsMessage.ToolCalls)
                                {
                                    toolsMessage.ToolCallsDict.TryAdd(toolCall.Index?.ToString() ?? toolCall.Id ?? string.Empty, new ToolCallInboundAccumulator
                                    {
                                        ToolCall = toolCall
                                    });
                                }   
                            }
                        }
                        
                        continue;
                    }
                    case ChatStreamParsingStates.Text:
                    {
                        if (res.Choices is null || res.Choices.Count is 0 || res.Choices[0].Delta?.Content is null || res.Choices[0].Delta?.Content?.Length is 0)
                        {
                            continue;
                        }

                        plaintextBuilder ??= new StringBuilder();
                        plaintextBuilder.Append(res.Choices[0].Delta!.Content);
                        res.Choices[0].Delta!.Role = ChatMessageRoles.Assistant;
                        yield return res;
                        continue;
                    }
                    case ChatStreamParsingStates.Tools:
                    {
                        if (toolsMessage?.ToolCalls is not null && toolsMessage.ToolCallsDict is not null && res.Choices?.Count > 0)
                        {
                            ChatChoice choice = res.Choices[0];

                            if (choice.Delta?.ToolCalls?.Count > 0)
                            {
                                foreach (ToolCall toolCall in choice.Delta.ToolCalls)
                                {
                                    string key = toolCall.Index?.ToString() ?? toolCall.Id ?? string.Empty;
                                    
                                    // we can either encounter a new function or we get a new arguments token
                                    if (toolsMessage.ToolCallsDict.TryGetValue(key, out ToolCallInboundAccumulator? accu))
                                    {
                                        accu.ArgumentsBuilder.Append(toolCall.FunctionCall.Arguments);
                                    }
                                    else
                                    {
                                        toolsMessage.ToolCalls.Add(toolCall);
                                        toolsMessage.ToolCallsDict.Add(key, new ToolCallInboundAccumulator
                                        {
                                            ToolCall = toolCall
                                        });
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                
                continue;
            }
            
            if (res.Choices is not null)
            {
                foreach (ChatChoice x in res.Choices)
                {
                    plaintextBuilder ??= new StringBuilder();
                    plaintextBuilder.Append(x.Delta!.Content);
                    x.Delta!.Role = ChatMessageRoles.Assistant;
                }
            }

            yield return res;
        }

        afterStreamEnds:
        
        if (parseTools && toolsAccumulator is not null && toolsMessage?.ToolCalls is not null && toolsMessage.ToolCallsDict is not null)
        {
            foreach (KeyValuePair<string, ToolCallInboundAccumulator> tool in toolsMessage.ToolCallsDict)
            {
                tool.Value.ToolCall.FunctionCall.Arguments = tool.Value.ArgumentsBuilder.ToString();
            }

            if (toolsAccumulator.Choices is not null)
            {
                toolsAccumulator.Choices[0].FinishReason = "function_call";
            }

            toolsAccumulator.Usage = usage;
            toolsMessage.Role = ChatMessageRoles.Tool;
            yield return toolsAccumulator;
            yield break;
        }

        string? accuPlaintext = plaintextBuilder?.ToString();

        if (accuPlaintext is not null)
        {
            yield return new ChatResult
            {
                Usage = usage,
                Choices =
                [
                    new ChatChoice
                    {
                        Delta = new ChatMessage
                        {
                            Content = accuPlaintext
                        }
                    }
                ],
                StreamInternalKind = ChatResultStreamInternalKinds.AppendAssistantMessage
            };
        }
    }

    public override async IAsyncEnumerable<T?> InboundStream<T>(StreamReader reader) where T : class
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith(DataString))
            {
                line = line[DataString.Length..];
            }

            line = line.TrimStart();

            if (line is DoneString)
            {
                yield break;
            }

            if (line.StartsWith(':') || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            T? res = JsonConvert.DeserializeObject<T>(line);

            if (res is null)
            {
                continue;
            }

            yield return res;
        }
    }
}