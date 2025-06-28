using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Models;
using LlmTornado.Common;

namespace LlmTornado.Chat;

/// <summary>
///     ChatGPT API endpoint. Use this endpoint to send multiple messages and carry on a conversation.
/// </summary>
public class ChatEndpoint : EndpointBase
{
    /// <summary>
    ///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
    ///     <see cref="TornadoApi" /> as <see cref="TornadoApi.Completions" />.
    /// </summary>
    /// <param name="api"></param>
    internal ChatEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  For example, "completions".
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Chat;
    
    /// <summary>
    ///     This allows you to set default parameters for every request, for example to set a default temperature or max
    ///     tokens.  For every request, if you do not have a parameter set on the request but do have it set here as a default,
    ///     the request will automatically pick up the default value.
    /// </summary>
    public ChatRequest DefaultChatRequestArgs { get; set; } = new ChatRequest
    {
        Model = ChatModel.OpenAi.Gpt35.Turbo
    };

    /// <summary>
    ///     Creates an ongoing chat which can easily encapsulate the conversation.  This is the simplest way to use the Chat
    ///     endpoint.
    /// </summary>
    /// <param name="defaultChatRequestArgs">The request to send to the API.</param>
    /// <returns>A <see cref="Conversation" /> which encapsulates a back and forth chat between a user and an assistant.</returns>
    public Conversation CreateConversation(ChatRequest? defaultChatRequestArgs = null)
    {
        return new Conversation(this, defaultChatRequestArgs: defaultChatRequestArgs ?? DefaultChatRequestArgs);
    }
    
    /// <summary>
    ///     Creates an ongoing chat which can easily encapsulate the conversation.  This is the simplest way to use the Chat
    ///     endpoint.
    /// </summary>
    /// <param name="model">The model to use.</param>
    /// <param name="temperature">The temperature.</param>
    /// <param name="maxTokens">The maximum amount of tokens to be used in response.</param>
    /// <returns>A <see cref="Conversation" /> which encapsulates a back and forth chat between a user and an assistant.</returns>
    public Conversation CreateConversation(ChatModel model, double? temperature = null, int? maxTokens = null)
    {
        return new Conversation(this, model, new ChatRequest
        {
            Model = model,
            Temperature = temperature,
            MaxTokens = maxTokens
        });
    }
    
    /// <summary>
    ///     Creates an ongoing chat which can easily encapsulate the conversation.  This is the simplest way to use the Chat
    ///     endpoint.
    /// </summary>
    /// <param name="model">The model to use.</param>
    /// <param name="temperature">The temperature.</param>
    /// <param name="maxTokens">The maximum amount of tokens to be used in response.</param>
    /// <returns>A <see cref="Conversation" /> which encapsulates a back and forth chat between a user and an assistant.</returns>
    public Conversation CreateConversation(string model, double? temperature = null, int? maxTokens = null)
    {
        return new Conversation(this, model, new ChatRequest
        {
            Model = model,
            Temperature = temperature,
            MaxTokens = maxTokens
        });
    }

    #region Non-streaming

    /// <summary>
    ///     Ask the API to complete the request using the specified parameters. This is non-streaming, so it will wait until
    ///     the API returns the full result. Any non-specified parameters will fall back to default values specified in
    ///     <see cref="DefaultChatRequestArgs" /> if present.
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <returns>
    ///     Asynchronously returns the completion result. Look in its <see cref="ChatResult.Choices" /> property for the
    ///     results.
    /// </returns>
    public async Task<ChatResult?> CreateChatCompletion(ChatRequest request)
    {
        request.Stream = null;
        IEndpointProvider provider = Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        TornadoRequestContent requestBody = request.Serialize(provider);
        HttpCallResult<ChatResult> result = await HttpPost<ChatResult>(provider, Endpoint, requestBody.Url, requestBody.Body, request.Model, request.CancellationToken);
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }
        
        NormalizeChatResult(result);
        
        if (Api.ChatRequestInterceptor is not null)
        {
            await Api.ChatRequestInterceptor.Invoke(request, result.Data);
        }

        if (result.Data is not null)
        {
            result.Data.Request = result.Request.HttpRequest;
        }
        
        return result.Data;
    }

    private static void NormalizeChatResult(HttpCallResult<ChatResult> result)
    {
        if (result.Response is not null && result.Data is not null)
        {
            result.Data.RawResponse = result.Response;
        }
        
        if (result.Data?.Choices is not null)
        {
            foreach (ChatChoice choice in result.Data.Choices)
            {
                if (choice.Message is not null)
                {
                    choice.Message.Role = ChatMessageRoles.Assistant;
                    
                    if (choice.Message.Parts?.Count > 0 && choice.Message.Content is null)
                    {
                        choice.Message.Content = choice.Message.Parts.FirstOrDefault(x => x.Type is ChatMessageTypes.Text)?.Text;
                    }
                }
            }
        }
    }
    
    /// <summary>
    ///     Ask the API to complete the request using the specified parameters. This is non-streaming, so it will wait until
    ///     the API returns the full result. Any non-specified parameters will fall back to default values specified in
    ///     <see cref="DefaultChatRequestArgs" /> if present. This method doesn't throw exceptions (even if the network layer fails).
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <returns>
    ///     Asynchronously returns the completion result. Look in its <see cref="ChatResult.Choices" /> property for the
    ///     results.
    /// </returns>
    public async Task<HttpCallResult<ChatResult>> CreateChatCompletionSafe(ChatRequest request)
    {
        request.Stream = null;
        IEndpointProvider provider = Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        TornadoRequestContent requestBody = request.Serialize(provider);
        HttpCallResult<ChatResult> result = await HttpPost<ChatResult>(provider, Endpoint, requestBody.Url, requestBody.Body, request.Model, request.CancellationToken);
        NormalizeChatResult(result);
        
        if (Api.ChatRequestInterceptor is not null && result.Ok)
        {
            await Api.ChatRequestInterceptor.Invoke(request, result.Data);
        }

        return result;
    }

    /// <summary>
    ///     Ask the API to complete the request using the specified parameters. This is non-streaming, so it will wait until
    ///     the API returns the full result. Any non-specified parameters will fall back to default values specified in
    ///     <see cref="DefaultChatRequestArgs" /> if present.
    /// </summary>
    /// <param name="messages">The array of messages to send to the API</param>
    /// <param name="model">
    ///     The model to use. See the ChatGPT models available from
    ///     <see cref="ModelsEndpoint.GetModels" />
    /// </param>
    /// <param name="temperature">
    ///     What sampling temperature to use. Higher values means the model will take more risks. Try 0.9
    ///     for more creative applications, and 0 (argmax sampling) for ones with a well-defined answer. It is generally
    ///     recommend to use this or <paramref name="topP" /> but not both.
    /// </param>
    /// <param name="topP">
    ///     An alternative to sampling with temperature, called nucleus sampling, where the model considers the
    ///     results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability
    ///     mass are considered. It is generally recommend to use this or <paramref name="temperature" /> but not both.
    /// </param>
    /// <param name="numOutputs">How many different choices to request for each prompt.</param>
    /// <param name="maxTokens">How many tokens to complete to. Can return fewer if a stop sequence is hit.</param>
    /// <param name="frequencyPenalty">
    ///     The scale of the penalty for how often a token is used.  Should generally be between 0
    ///     and 1, although negative numbers are allowed to encourage token reuse.
    /// </param>
    /// <param name="presencePenalty">
    ///     The scale of the penalty applied if a token is already present at all.  Should generally
    ///     be between 0 and 1, although negative numbers are allowed to encourage token reuse.
    /// </param>
    /// <param name="logitBias">
    ///     Maps tokens (specified by their token ID in the tokenizer) to an associated bias value from
    ///     -100 to 100. Mathematically, the bias is added to the logits generated by the model prior to sampling. The exact
    ///     effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of selection; values
    ///     like -100 or 100 should result in a ban or exclusive selection of the relevant token.
    /// </param>
    /// <param name="stopSequences">
    ///     One or more sequences where the API will stop generating further tokens. The returned text
    ///     will not contain the stop sequence.
    /// </param>
    /// <returns>
    ///     Asynchronously returns the completion result. Look in its <see cref="ChatResult.Choices" /> property for the
    ///     results.
    /// </returns>
    public Task<ChatResult?> CreateChatCompletion(List<ChatMessage> messages,
        ChatModel? model = null,
        double? temperature = null,
        double? topP = null,
        int? numOutputs = null,
        int? maxTokens = null,
        double? frequencyPenalty = null,
        double? presencePenalty = null,
        IReadOnlyDictionary<string, float>? logitBias = null,
        params string[]? stopSequences)
    {
        ChatRequest request = new ChatRequest(DefaultChatRequestArgs)
        {
            Messages = messages,
            Model = model ?? DefaultChatRequestArgs.Model,
            Temperature = temperature ?? DefaultChatRequestArgs.Temperature,
            TopP = topP ?? DefaultChatRequestArgs.TopP,
            NumChoicesPerMessage = numOutputs ?? DefaultChatRequestArgs.NumChoicesPerMessage,
            MultipleStopSequences = stopSequences ?? DefaultChatRequestArgs.MultipleStopSequences,
            MaxTokens = maxTokens ?? DefaultChatRequestArgs.MaxTokens,
            FrequencyPenalty = frequencyPenalty ?? DefaultChatRequestArgs.FrequencyPenalty,
            PresencePenalty = presencePenalty ?? DefaultChatRequestArgs.PresencePenalty,
            LogitBias = logitBias ?? DefaultChatRequestArgs.LogitBias,
            Stream = null
        };
        
        return CreateChatCompletion(request);
    }

    /// <summary>
    ///     Ask the API to complete the request using the specified message(s).  Any parameters will fall back to default
    ///     values specified in <see cref="DefaultChatRequestArgs" /> if present.
    /// </summary>
    /// <param name="messages">The messages to use in the generation.</param>
    /// <returns>The <see cref="ChatResult" /> with the API response.</returns>
    public Task<ChatResult?> CreateChatCompletion(params ChatMessage[] messages)
    {
        ChatRequest request = new ChatRequest(DefaultChatRequestArgs)
        {
            Messages = messages.ToList(),
            Stream = null
        };
        return CreateChatCompletion(request);
    }

    /// <summary>
    ///     Ask the API to complete the request using the specified message(s).  Any parameters will fall back to default
    ///     values specified in <see cref="DefaultChatRequestArgs" /> if present.
    /// </summary>
    /// <param name="userMessages">
    ///     The user message or messages to use in the generation.  All strings are assumed to be of
    ///     Role <see cref="ChatMessageRoles.User" />
    /// </param>
    /// <returns>The <see cref="ChatResult" /> with the API response.</returns>
    public Task<ChatResult?> CreateChatCompletion(params string[] userMessages)
    {
        return CreateChatCompletion(userMessages.Select(m => new ChatMessage(ChatMessageRoles.User, m)).ToArray());
    }

    #endregion

    #region Streaming

    /// <summary>
    ///     Ask the API to complete the message(s) using the specified request, and stream the results to the
    ///     <paramref name="resultHandler" /> as they come in.
    ///     If you are on the latest C# supporting async enumerables, you may prefer the cleaner syntax of
    ///     <see cref="StreamChatEnumerable" /> instead.
    /// </summary>
    /// <param name="request">
    ///     The request to send to the API. This does not fall back to default values specified in
    ///     <see cref="DefaultChatRequestArgs" />.
    /// </param>
    /// <param name="resultHandler">
    ///     An action to be called as each new result arrives, which includes the index of the result
    ///     in the overall result set.
    /// </param>
    public async Task StreamCompletion(ChatRequest request, Action<int, ChatResult> resultHandler)
    {
        int index = 0;

        await foreach (ChatResult res in StreamChatEnumerable(request)) resultHandler(index++, res);
    }

    /// <summary>
    ///     Ask the API to complete the message(s) using the specified request, and stream the results to the
    ///     <paramref name="resultHandler" /> as they come in.
    ///     If you are on the latest C# supporting async enumerables, you may prefer the cleaner syntax of
    ///     <see cref="StreamChatEnumerable" /> instead.
    /// </summary>
    /// <param name="request">
    ///     The request to send to the API.  This does not fall back to default values specified in
    ///     <see cref="DefaultChatRequestArgs" />.
    /// </param>
    /// <param name="resultHandler">An action to be called as each new result arrives.</param>
    public async Task StreamChatAsync(ChatRequest request, Action<ChatResult> resultHandler)
    {
        await foreach (ChatResult res in StreamChatEnumerable(request)) resultHandler(res);
    }

    /// <summary>
    ///     Ask the API to complete the message(s) using the specified request, and stream the results as they come in.
    ///     If you are not using C# 8 supporting async enumerables or if you are using the .NET Framework, you may need to use
    ///     <see cref="StreamChatAsync(ChatRequest, Action{ChatResult})" /> instead.
    /// </summary>
    /// <param name="request">
    ///     The request to send to the API.  This does not fall back to default values specified in
    ///     <see cref="DefaultChatRequestArgs" />.
    /// </param>
    /// <returns>
    ///     An async enumerable with each of the results as they come in.  See
    ///     <see href="https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-8#asynchronous-streams" /> for more
    ///     details on how to consume an async enumerable.
    /// </returns>
    public IAsyncEnumerable<ChatResult> StreamChatEnumerable(ChatRequest request)
    {
        return StreamChatEnumerable(request, null);
    }
    
    internal IAsyncEnumerable<ChatResult> StreamChatEnumerable(ChatRequest request, ChatStreamEventHandler? handler)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        return StreamChatReal(provider, request, handler);
    }

    private async IAsyncEnumerable<ChatResult> StreamChatReal(IEndpointProvider provider, ChatRequest request, ChatStreamEventHandler? handler)
    {
        request.Stream = true;
        ChatStreamOptions? requestStreamOpts = request.StreamOptions;

        if (request.StreamOptions is null)
        {
            request.StreamOptions = ChatStreamOptions.KnownOptionsIncludeUsage;
            requestStreamOpts = null;
        }
        
        if (!request.StreamOptions.IncludeUsage)
        {
            request.StreamOptions = null;
        }
        
        TornadoRequestContent requestBody = request.Serialize(provider);
        await using TornadoStreamRequest tornadoStreamRequest = await HttpStreamingRequestData(Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo), Endpoint, requestBody.Url, queryParams: null, HttpVerbs.Post, requestBody.Body, request.Model, request.CancellationToken);

        if (tornadoStreamRequest.Exception is not null)
        {
            if (handler?.HttpExceptionHandler is null)
            {
                throw tornadoStreamRequest.Exception;
            }

            await handler.HttpExceptionHandler(new HttpFailedRequest
            {
                Exception = tornadoStreamRequest.Exception,
                Result = tornadoStreamRequest.CallResponse,
                Request = tornadoStreamRequest.CallRequest,
                RawMessage = tornadoStreamRequest.Response ?? new HttpResponseMessage(),
                Body = requestBody
            });
            
            yield break;
        }

        if (handler?.OutboundHttpRequestHandler is not null && tornadoStreamRequest.CallRequest is not null)
        {
            await handler.OutboundHttpRequestHandler(tornadoStreamRequest.CallRequest);
        }

        if (tornadoStreamRequest.StreamReader is not null)
        {
            await foreach (ChatResult? x in provider.InboundStream(tornadoStreamRequest.StreamReader, request))
            {
                if (x is null)
                {
                    continue;
                }

                yield return x;
            }
        }
        
        request.StreamOptions = requestStreamOpts;
    }

    /// <summary>
    ///     Ask the API to complete the message(s) using the specified request, and stream the results as they come in.
    ///     If you are not using C# 8 supporting async enumerables or if you are using the .NET Framework, you may need to use
    ///     <see cref="StreamChatAsync(ChatRequest, Action{ChatResult})" /> instead.
    /// </summary>
    /// <param name="messages">The array of messages to send to the API</param>
    /// <param name="model">
    ///     The model to use. See the ChatGPT models available from
    ///     <see cref="ModelsEndpoint.GetModels" />
    /// </param>
    /// <param name="temperature">
    ///     What sampling temperature to use. Higher values means the model will take more risks. Try 0.9
    ///     for more creative applications, and 0 (argmax sampling) for ones with a well-defined answer. It is generally
    ///     recommend to use this or <paramref name="top_p" /> but not both.
    /// </param>
    /// <param name="top_p">
    ///     An alternative to sampling with temperature, called nucleus sampling, where the model considers the
    ///     results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability
    ///     mass are considered. It is generally recommend to use this or <paramref name="temperature" /> but not both.
    /// </param>
    /// <param name="numOutputs">How many different choices to request for each prompt.</param>
    /// <param name="max_tokens">How many tokens to complete to. Can return fewer if a stop sequence is hit.</param>
    /// <param name="frequencyPenalty">
    ///     The scale of the penalty for how often a token is used.  Should generally be between 0
    ///     and 1, although negative numbers are allowed to encourage token reuse.
    /// </param>
    /// <param name="presencePenalty">
    ///     The scale of the penalty applied if a token is already present at all.  Should generally
    ///     be between 0 and 1, although negative numbers are allowed to encourage token reuse.
    /// </param>
    /// <param name="logitBias">
    ///     Maps tokens (specified by their token ID in the tokenizer) to an associated bias value from
    ///     -100 to 100. Mathematically, the bias is added to the logits generated by the model prior to sampling. The exact
    ///     effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of selection; values
    ///     like -100 or 100 should result in a ban or exclusive selection of the relevant token.
    /// </param>
    /// <param name="stopSequences">
    ///     One or more sequences where the API will stop generating further tokens. The returned text
    ///     will not contain the stop sequence.
    /// </param>
    /// <returns>
    ///     An async enumerable with each of the results as they come in. See
    ///     <see href="https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-8#asynchronous-streams">the C# docs</see>
    ///     for more details on how to consume an async enumerable.
    /// </returns>
    public IAsyncEnumerable<ChatResult> StreamChatEnumerable(List<ChatMessage> messages,
        ChatModel? model = null,
        double? temperature = null,
        double? top_p = null,
        int? numOutputs = null,
        int? max_tokens = null,
        double? frequencyPenalty = null,
        double? presencePenalty = null,
        IReadOnlyDictionary<string, float>? logitBias = null,
        params string[]? stopSequences)
    {
        ChatRequest request = new ChatRequest(DefaultChatRequestArgs)
        {
            Messages = messages,
            Model = model ?? DefaultChatRequestArgs.Model,
            Temperature = temperature ?? DefaultChatRequestArgs.Temperature,
            TopP = top_p ?? DefaultChatRequestArgs.TopP,
            NumChoicesPerMessage = numOutputs ?? DefaultChatRequestArgs.NumChoicesPerMessage,
            MultipleStopSequences = stopSequences ?? DefaultChatRequestArgs.MultipleStopSequences,
            MaxTokens = max_tokens ?? DefaultChatRequestArgs.MaxTokens,
            FrequencyPenalty = frequencyPenalty ?? DefaultChatRequestArgs.FrequencyPenalty,
            PresencePenalty = presencePenalty ?? DefaultChatRequestArgs.PresencePenalty,
            LogitBias = logitBias ?? DefaultChatRequestArgs.LogitBias,
        };
        return StreamChatEnumerable(request);
    }

    #endregion
}