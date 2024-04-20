using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAiNg.ChatFunctions;
using OpenAiNg.Code;
using OpenAiNg.Common;
using OpenAiNg.Models;
using OpenAiNg.Vendor.Anthropic;

namespace OpenAiNg.Chat;

/// <summary>
///     Represents on ongoing chat with back-and-forth interactions between the user and the chatbot.  This is the simplest
///     way to interact with the ChatGPT API, rather than manually using the ChatEnpoint methods.  You do lose some
///     flexibility though.
/// </summary>
public class Conversation
{
    /// <summary>
    ///     An internal reference to the API endpoint, needed for API requests
    /// </summary>
    private readonly ChatEndpoint _endpoint;

    /// <summary>
    ///     An internal handle to the messages currently enlisted in the conversation.
    /// </summary>
    private readonly List<ChatMessage> _messages;

    /// <summary>
    ///     Creates a new conversation with ChatGPT chat
    /// </summary>
    /// <param name="endpoint">
    ///     A reference to the API endpoint, needed for API requests.  Generally should be
    ///     <see cref="OpenAiApi.Chat" />.
    /// </param>
    /// <param name="model">
    ///     Optionally specify the model to use for ChatGPT requests.  If not specified, used
    ///     <paramref name="defaultChatRequestArgs" />.Model or falls back to <see cref="Models.Model.GPT35_Turbo" />
    /// </param>
    /// <param name="defaultChatRequestArgs">
    ///     Allows setting the parameters to use when calling the ChatGPT API.  Can be useful for setting temperature,
    ///     presence_penalty, and more.  See
    ///     <see href="https://platform.openai.com/docs/api-reference/chat/create">
    ///         OpenAI documentation for a list of possible
    ///         parameters to tweak.
    ///     </see>
    /// </param>
    public Conversation(ChatEndpoint endpoint, Model? model = null, ChatRequest? defaultChatRequestArgs = null)
    {
        RequestParameters = new ChatRequest(defaultChatRequestArgs);
        
        if (model is not null)
        {
            RequestParameters.Model = model;
        }
        
        RequestParameters.Model ??= Model.GPT35_Turbo;

        _messages = new List<ChatMessage>();
        _endpoint = endpoint;
        RequestParameters.NumChoicesPerMessage = 1;
        RequestParameters.Stream = false;
    }

    /// <summary>
    ///     Allows setting the parameters to use when calling the ChatGPT API.  Can be useful for setting temperature,
    ///     presence_penalty, and more.
    ///     <see href="https://platform.openai.com/docs/api-reference/chat/create">
    ///         Se  OpenAI documentation for a list of
    ///         possible parameters to tweak.
    ///     </see>
    /// </summary>
    public ChatRequest RequestParameters { get; }

    /// <summary>
    ///     Specifies the model to use for ChatGPT requests.  This is just a shorthand to access
    ///     <see cref="RequestParameters" />.Model
    /// </summary>
    public Model Model
    {
        get => RequestParameters.Model;
        set => RequestParameters.Model = value;
    }

    /// <summary>
    ///     Called after one or more tools are requested by the model and the corresponding results are resolved.
    /// </summary>
    public Func<ResolvedToolsCall, Task>? OnAfterToolsCall { get; set; }

    /// <summary>
    ///     After calling <see cref="GetResponseFromChatbotAsync" />, this contains the full response object which can contain
    ///     useful metadata like token usages, <see cref="ChatChoice.FinishReason" />, etc.  This is overwritten with every
    ///     call to <see cref="GetResponseFromChatbotAsync" /> and only contains the most recent result.
    /// </summary>
    public ChatResult MostRecentApiResult { get; private set; }

    /// <summary>
    ///     If not null, overrides the default OpenAI auth
    /// </summary>
    public ApiAuthentication? Auth { get; set; }

    /// <summary>
    ///     A list of messages exchanged so far.  Do not modify this list directly.  Instead, use
    ///     <see cref="AppendMessage(ChatMessage)" />, <see cref="AppendUserInput(string)" />,
    ///     <see cref="AppendSystemMessage(string)" />, or <see cref="AppendExampleChatbotOutput(string)" />.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => _messages.ToList();

    /// <summary>
    ///     Appends a <see cref="ChatMessage" /> to the chat history
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage" /> to append to the chat history</param>
    public void AppendMessage(ChatMessage message)
    {
        _messages.Add(message);
    }

    /// <summary>
    ///     Appends a <see cref="ChatMessage" /> to the chat hstory
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage" /> to append to the chat history</param>
    /// <param name="position">Zero-based index at which to insert the message</param>
    public void AppendMessage(ChatMessage message, int position)
    {
        _messages.Insert(position, message);
    }

    /// <summary>
    ///     Removes given message from the conversation. If the message is not found, nothing happens
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Whether message was removed</returns>
    public bool RemoveMessage(ChatMessage message)
    {
        ChatMessage? msg = _messages.FirstOrDefault(x => x.Id == message.Id);

        if (msg is not null)
        {
            _messages.Remove(msg);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Removes message with given id from the conversation. If the message is not found, nothing happens
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Whether message was removed</returns>
    public bool RemoveMessage(Guid id)
    {
        ChatMessage? msg = _messages.FirstOrDefault(x => x.Id == id);

        if (msg is not null)
        {
            _messages.Remove(msg);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Updates text of a given message
    /// </summary>
    /// <param name="message">Message to update</param>
    /// <param name="content">New text</param>
    public void EditMessageContent(ChatMessage message, string content)
    {
        message.Content = content;
        message.Parts = null;
    }

    /// <summary>
    ///     Updates parts of a given message
    /// </summary>
    /// <param name="message">Message to update</param>
    /// <param name="parts">New parts</param>
    public void EditMessageContent(ChatMessage message, IEnumerable<ChatMessagePart> parts)
    {
        message.Content = null;
        message.Parts = parts.ToList();
    }

    /// <summary>
    ///     Finds a message in the conversation by id. If found, updates text of this message
    /// </summary>
    /// <param name="id">Message to update</param>
    /// <param name="content">New text</param>
    /// <returns>Whether message was updated</returns>
    public bool EditMessageContent(Guid id, string content)
    {
        ChatMessage? msg = _messages.FirstOrDefault(x => x.Id == id);

        if (msg is not null)
        {
            msg.Content = content;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Updates role of a given message
    /// </summary>
    /// <param name="message">Message to update</param>
    /// <param name="role">New role</param>
    public void EditMessageRole(ChatMessage message, ChatMessageRole role)
    {
        message.Role = role;
    }

    /// <summary>
    ///     Finds a message in the conversation by id. If found, updates text of this message
    /// </summary>
    /// <param name="id">Message to update</param>
    /// <param name="role">New role</param>
    /// <returns>Whether message was updated</returns>
    public bool EditMessageRole(Guid id, ChatMessageRole role)
    {
        ChatMessage? msg = _messages.FirstOrDefault(x => x.Id == id);

        if (msg is not null)
        {
            msg.Role = role;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history
    /// </summary>
    /// <param name="role">
    ///     The <see cref="ChatMessageRole" /> for the message.  Typically, a conversation is formatted with a
    ///     system message first, followed by alternating user and assistant messages.  See
    ///     <see href="https://platform.openai.com/docs/guides/chat/introduction">the OpenAI docs</see> for more details about
    ///     usage.
    /// </param>
    /// <param name="content">The content of the message)</param>
    public void AppendMessage(ChatMessageRole role, string content)
    {
        AppendMessage(new ChatMessage(role, content));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat hstory
    /// </summary>
    /// <param name="role">
    ///     The <see cref="ChatMessageRole" /> for the message.  Typically, a conversation is formatted with a
    ///     system message first, followed by alternating user and assistant messages.  See
    ///     <see href="https://platform.openai.com/docs/guides/chat/introduction">the OpenAI docs</see> for more details about
    ///     usage.
    /// </param>
    /// <param name="content">The content of the message</param>
    /// <param name="id">Id of the message</param>
    public void AppendMessage(ChatMessageRole role, string content, Guid? id)
    {
        AppendMessage(new ChatMessage(role, content, id));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.User" />.  The user messages help instruct the assistant. They can be generated by the
    ///     end users of an application, or set by a developer as an instruction.
    /// </summary>
    /// <param name="content">
    ///     Text content generated by the end users of an application, or set by a developer as an
    ///     instruction
    /// </param>
    public void AppendUserInput(string content)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.User, content));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.User" />.  The user messages help instruct the assistant. They can be generated by the
    ///     end users of an application, or set by a developer as an instruction.
    /// </summary>
    /// <param name="content">
    ///     Text content generated by the end users of an application, or set by a developer as an
    ///     instruction
    /// </param>
    /// <param name="id">id of the message</param>
    public void AppendUserInput(string content, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.User, content, id));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.User" />.  The user messages help instruct the assistant. They can be generated by the
    ///     end users of an application, or set by a developer as an instruction.
    /// </summary>
    /// <param name="parts">
    ///     Parts of the message
    /// </param>
    /// <param name="id">id of the message</param>
    public void AppendUserInput(IEnumerable<ChatMessagePart> parts, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.User, parts, id));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.User" />.  The user messages help instruct the assistant. They can be generated by the
    ///     end users of an application, or set by a developer as an instruction.
    /// </summary>
    /// <param name="userName">The name of the user in a multi-user chat</param>
    /// <param name="content">
    ///     Text content generated by the end users of an application, or set by a developer as an
    ///     instruction
    /// </param>
    public void AppendUserInputWithName(string userName, string content)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.User, content) { Name = userName });
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.User" />.  The user messages help instruct the assistant. They can be generated by the
    ///     end users of an application, or set by a developer as an instruction.
    /// </summary>
    /// <param name="userName">The name of the user in a multi-user chat</param>
    /// <param name="content">
    ///     Text content generated by the end users of an application, or set by a developer as an
    ///     instruction
    /// </param>
    /// <param name="id">id of the message</param>
    public void AppendUserInputWithName(string userName, string content, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.User, content, id) { Name = userName });
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.User" />.  The user messages help instruct the assistant. They can be generated by the
    ///     end users of an application, or set by a developer as an instruction.
    /// </summary>
    /// <param name="userName">The name of the user in a multi-user chat</param>
    /// <param name="parts">
    ///     Parts of the message generated by the end users of an application, or set by a developer as an
    ///     instruction
    /// </param>
    /// <param name="id">id of the message</param>
    public void AppendUserInputWithName(string userName, IEnumerable<ChatMessagePart> parts, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.User, parts, id) { Name = userName });
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.System" />.  The system message helps set the behavior of the assistant.
    /// </summary>
    /// <param name="content">text content that helps set the behavior of the assistant</param>
    public void AppendSystemMessage(string content)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.System, content));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.System" />.  The system message helps set the behavior of the assistant.
    /// </summary>
    /// <param name="content">text content that helps set the behavior of the assistant</param>
    /// <param name="id">id of the message</param>
    public void AppendSystemMessage(string content, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.System, content, id));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.System" />.  The system message helps set the behavior of the assistant.
    /// </summary>
    /// <param name="parts">Parts of the message which helps set the behavior of the assistant</param>
    /// <param name="id">id of the message</param>
    public void AppendSystemMessage(IEnumerable<ChatMessagePart> parts, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.System, parts, id));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.System" />.  The system message helps set the behavior of the assistant.
    /// </summary>
    /// <param name="content">text content that helps set the behavior of the assistant</param>
    /// <param name="id">id of the message</param>
    public void PrependSystemMessage(string content, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.System, content, id), 0);
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.System" />.  The system message helps set the behavior of the assistant.
    /// </summary>
    /// <param name="parts">Parts of the message which helps set the behavior of the assistant</param>
    /// <param name="id">id of the message</param>
    public void PrependSystemMessage(IEnumerable<ChatMessagePart> parts, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.System, parts, id), 0);
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.Assistant" />.  Assistant messages can be written by a developer to help give examples
    ///     of desired behavior.
    /// </summary>
    /// <param name="content">Text content written by a developer to help give examples of desired behavior</param>
    public void AppendExampleChatbotOutput(string content)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.Assistant, content));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.Tool" />.  The function message is a response to a request from the system for
    ///     output from a predefined function.
    /// </summary>
    /// <param name="functionName">The name of the function for which the content has been generated as the result</param>
    /// <param name="content">The text content (usually JSON)</param>
    public void AppendFunctionMessage(string functionName, string content)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.Tool, content) { Name = functionName });
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.Assistant" />.  Assistant messages can be written by a developer to help give examples
    ///     of desired behavior.
    /// </summary>
    /// <param name="content">Text content written by a developer to help give examples of desired behavior</param>
    /// <param name="id">id of the message</param>
    public void AppendExampleChatbotOutput(string content, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.Assistant, content, id));
    }

    /// <summary>
    ///     Creates and appends a <see cref="ChatMessage" /> to the chat history with the Role of
    ///     <see cref="ChatMessageRole.Assistant" />.  Assistant messages can be written by a developer to help give examples
    ///     of desired behavior.
    /// </summary>
    /// <param name="parts">Parts of the message written by a developer to help give examples of desired behavior</param>
    /// <param name="id">id of the message</param>
    public void AppendExampleChatbotOutput(IEnumerable<ChatMessagePart> parts, Guid id)
    {
        AppendMessage(new ChatMessage(ChatMessageRole.Assistant, parts, id));
    }

    #region Non-streaming

    /// <summary>
    ///     Calls the API to get a response, which is appended to the current chat's <see cref="Messages" /> as an
    ///     <see cref="ChatMessageRole.Assistant" /> <see cref="ChatMessage" />.
    /// </summary>
    /// <returns>The string of the response from the chatbot API</returns>
    public async Task<string?> GetResponseFromChatbotAsync()
    {
        ChatRequest req = new(RequestParameters)
        {
            Messages = _messages.ToList()
        };

        ChatResult? res = await _endpoint.CreateChatCompletionAsync(req);

        if (res is null) return null;

        MostRecentApiResult = res;

        if (res.Choices is null) return null;

        if (res.Choices.Count > 0)
        {
            ChatMessage? newMsg = res.Choices[0].Message;

            if (newMsg is not null)
            {
                AppendMessage(newMsg);    
            }
            
            return newMsg?.Content;
        }

        return null;
    }

    /// <summary>
    ///     Calls the API to get a response, which is appended to the current chat's <see cref="Messages" /> as an
    ///     <see cref="ChatMessageRole.Assistant" /> <see cref="ChatMessage" />.
    /// </summary>
    /// <returns>The string of the response from the chatbot API</returns>
    public async Task<ChatResponse?> GetResponseFromChatbotAsyncWithFunctions(Func<List<FunctionCall>, Task<FunctionResult?>> functionCallHandler)
    {
        ChatRequest req = new(RequestParameters)
        {
            Messages = _messages.ToList()
        };

        ChatResult? res = await _endpoint.CreateChatCompletionAsync(req);

        if (res is null) return null;

        MostRecentApiResult = res;

        if (res.Choices is null) return null;

        if (res.Choices.Count > 0)
        {
            ChatMessage? newMsg = res.Choices[0].Message;

            if (newMsg is null) return null;

            AppendMessage(newMsg);

            if (newMsg.ToolCalls is { Count: > 0 } && newMsg.ToolCalls[0].FunctionCall.Name is not ("none" or "auto"))
            {
                FunctionResult? result = await functionCallHandler.Invoke([newMsg.ToolCalls[0].FunctionCall]);
                return new ChatResponse { Kind = ChatResponseKinds.Function, FunctionResult = result };
            }

            return new ChatResponse { Kind = ChatResponseKinds.Message, Message = newMsg.Content };
        }

        return null;
    }

    /// <summary>
    ///     Calls the API to get a response, which is appended to the current chat's <see cref="Messages" /> as an
    ///     <see cref="ChatMessageRole.Assistant" /> <see cref="ChatMessage" />.
    /// </summary>
    /// <returns>The string of the response from the chatbot API</returns>
    public async Task<ChatChoice?> GetResponseFromChatbotAsyncRaw()
    {
        ChatRequest req = new(RequestParameters)
        {
            Messages = _messages.ToList()
        };

        ChatResult? res = await _endpoint.CreateChatCompletionAsync(req);

        if (res is null) return null;

        MostRecentApiResult = res;

        if (res.Choices is null) return null;

        if (res.Choices.Count > 0)
        {
            ChatMessage? newMsg = res.Choices[0].Message;

            if (newMsg is not null)
            {
                AppendMessage(newMsg);    
            }
            
            return res.Choices[0];
        }

        return null;
    }

    #endregion

    #region Streaming

    /// <summary>
    ///     Calls the API to get a response, which is appended to the current chat's <see cref="Messages" /> as an
    ///     <see cref="ChatMessageRole.Assistant" /> <see cref="ChatMessage" />, and streams the results to the
    ///     <paramref name="resultHandler" /> as they come in. <br />
    ///     If you are on the latest C# supporting async enumerables, you may prefer the cleaner syntax of
    ///     <see cref="StreamResponseEnumerableFromChatbotAsync" /> instead.
    /// </summary>
    /// <param name="resultHandler">An action to be called as each new result arrives.</param>
    public async Task StreamResponseFromChatbotAsync(Action<string> resultHandler)
    {
        await foreach (string res in StreamResponseEnumerableFromChatbotAsync()) resultHandler(res);
    }

    /// <summary>
    ///     Calls the API to get a response, which is appended to the current chat's <see cref="Messages" /> as an
    ///     <see cref="ChatMessageRole.Assistant" /> <see cref="ChatMessage" />, and streams the results to the
    ///     <paramref name="resultHandler" /> as they come in. <br />
    ///     If you are on the latest C# supporting async enumerables, you may prefer the cleaner syntax of
    ///     <see cref="StreamResponseEnumerableFromChatbotAsync" /> instead.
    /// </summary>
    /// <param name="resultHandler">
    ///     An action to be called as each new result arrives, which includes the index of the result
    ///     in the overall result set.
    /// </param>
    public async Task StreamResponseFromChatbotAsync(Action<int, string> resultHandler)
    {
        int index = 0;
        await foreach (string res in StreamResponseEnumerableFromChatbotAsync()) resultHandler(index++, res);
    }

    /// <summary>
    ///     Calls the API to get a response, which is appended to the current chat's <see cref="Messages" /> as an
    ///     <see cref="ChatMessageRole.Assistant" /> <see cref="ChatMessage" />, and streams the results as they come in.
    ///     <br />
    ///     If you are not using C# 8 supporting async enumerables or if you are using the .NET Framework, you may need to use
    ///     <see cref="StreamResponseFromChatbotAsync(Action{string})" /> instead.
    /// </summary>
    /// <returns>
    ///     An async enumerable with each of the results as they come in.  See
    ///     <see href="https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-8#asynchronous-streams" /> for more
    ///     details on how to consume an async enumerable.
    /// </returns>
    public async IAsyncEnumerable<string> StreamResponseEnumerableFromChatbotAsync(Guid? messageId = null)
    {
        ChatRequest req = new(RequestParameters)
        {
            Messages = _messages.ToList()
        };

        StringBuilder responseStringBuilder = new();
        ChatMessageRole? responseRole = null;

        await foreach (ChatResult res in _endpoint.StreamChatEnumerableAsync(req))
        {
            if (res.Choices is null) yield break;

            if (res.Choices.Count <= 0) yield break;

            if (res.Choices[0].Delta is { } delta)
            {
                if (responseRole == null && delta.Role != null) responseRole = delta.Role;

                string? deltaContent = delta.Content;

                if (!string.IsNullOrEmpty(deltaContent))
                {
                    responseStringBuilder.Append(deltaContent);
                    yield return deltaContent;
                }
            }

            MostRecentApiResult = res;
        }

        if (responseRole != null) AppendMessage(responseRole, responseStringBuilder.ToString(), messageId);
    }

    /// <summary>
    ///     Stream LLM response. If the response is of type "function", entire response is buffered and then
    ///     <see cref="functionCallHandler" /> is invoked.
    ///     Otherwise the message is intended for the end user and <see cref="messageTokenHandler" /> is called for each
    ///     incoming token
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="messageTokenHandler"></param>
    /// <param name="functionCallHandler"></param>
    /// <param name="messageTypeResolvedHandler">
    ///     This is called typically after the first token arrives signaling type of the
    ///     incoming message
    /// </param>
    /// <param name="chatRequestHandler">
    ///     if false, functions won't be allowed to executed regardless of the conversation
    ///     settings
    /// </param>
    /// <param name="outboundRequest">set to an empty <see cref="Ref" /> to receive the outbound request as well</param>
    public async Task StreamResponseEnumerableFromChatbotAsyncWithFunctions(Guid? messageId, Func<string?, Task>? messageTokenHandler, Func<List<FunctionCall>, Task<List<FunctionResult>>>? functionCallHandler, Func<ChatMessageRole, Task>? messageTypeResolvedHandler, Func<ChatRequest, Task<ChatRequest>>? chatRequestHandler, Ref<string>? outboundRequest = null)
    {
        ChatRequest req = new(RequestParameters)
        {
            Messages = _messages.ToList(),
            OuboundFunctionsContent = outboundRequest
        };

        req = chatRequestHandler is not null ? await chatRequestHandler.Invoke(req) : req;

        StringBuilder responseStringBuilder = new();
        ChatMessageRole? responseRole = null;
        string currentFunction = string.Empty;
        Dictionary<string, StringBuilder> functionCalls = new();
        bool typeResolved = false;

        await foreach (ChatResult res in _endpoint.StreamChatEnumerableAsync(req))
        {
            if (res.Choices is null)
            {
                MostRecentApiResult = res;
                continue;
            }

            if (res.Choices.Count is 0)
            {
                MostRecentApiResult = res;
                continue;
            }

            ChatChoice choice = res.Choices[0];
            string? finishReason = choice.FinishReason;

            if (finishReason is not null && (res.Provider?.ToolFinishReasons.Contains(finishReason) ?? false))
            {
                responseRole = ChatMessageRole.Tool;
            }

            if (choice.Delta is not null)
            {
                ChatMessage delta = choice.Delta;
                string? deltaContent = delta.Content;
                bool empty = string.IsNullOrEmpty(deltaContent);
                
                if (choice.Delta.ToolCalls is not null)
                {
                    responseRole ??= ChatMessageRole.Tool;

                    if (!typeResolved)
                    {
                        typeResolved = true;

                        if (messageTypeResolvedHandler != null) await messageTypeResolvedHandler(ChatMessageRole.Tool);
                    }

                    if (choice.Delta.ToolCalls.Count > 0)
                    {
                        if (!choice.Delta.ToolCalls[0].FunctionCall.Name.IsNullOrWhiteSpace())
                        {
                            currentFunction = choice.Delta.ToolCalls[0].FunctionCall.Name;
                            functionCalls.TryAdd(currentFunction, new StringBuilder());
                        }
                        else
                        {
                            if (functionCalls.TryGetValue(currentFunction, out StringBuilder? sb))
                            {
                                sb.Append(choice.Delta.ToolCalls[0].FunctionCall.Arguments);
                            }
                        }
                    }
                }

                if (responseRole is null && delta.Role is not null)
                {
                    responseRole = delta.Role;

                    if (!typeResolved)
                    {
                        typeResolved = true;

                        if (messageTypeResolvedHandler != null) await messageTypeResolvedHandler(responseRole);
                    }

                    if (functionCallHandler is not null && responseRole == "function")
                    {
                        if (!empty)
                        {
                            responseStringBuilder.Append(deltaContent);
                        }

                        continue;
                    }
                }

                if (!empty)
                {
                    responseStringBuilder.Append(deltaContent);

                    if (messageTokenHandler is not null)
                    {
                        await messageTokenHandler.Invoke(deltaContent);
                    }
                }
            }
            else if (responseRole != null && responseRole.Equals(ChatMessageRole.Tool)) 
            {
                foreach (ChatChoice iChoice in res.Choices.Where(ch => ch.Message?.ToolCalls != null) )
                {
                    if (iChoice.Message is { ToolCalls: not null })
                    {
                        if (!iChoice.Message.ToolCalls[0].FunctionCall.Name.IsNullOrWhiteSpace())
                        {
                            StringBuilder sb = new StringBuilder();
                            currentFunction = iChoice.Message.ToolCalls[0].Id;
                            sb.Append(iChoice.Message.ToolCalls[0].FunctionCall.Arguments);
                            functionCalls.TryAdd(currentFunction, sb);
                        }
                    }

                }
            }
            else if (responseRole is null && res.Choices[0].Message?.Role is not null)
            {
                if (string.IsNullOrEmpty(res.Choices[0].Message?.Content))
                {
                    responseStringBuilder.Append(res.Choices[0].Message?.Content);

                    if (messageTokenHandler is not null)
                    {
                        await messageTokenHandler.Invoke(res.Choices[0].Message?.Content);
                    }
                }
            }
            MostRecentApiResult = res;
        }

        if (responseRole is not null && responseRole.Equals(ChatMessageRole.Tool))
        {
            if (functionCallHandler is not null)
            {
                ResolvedToolsCall result = new ResolvedToolsCall();
                
                List<FunctionCall> calls = functionCalls.Select(pair => new FunctionCall { Name = pair.Key, Arguments = pair.Value.ToString() }).ToList();
                List<FunctionResult> frs = await functionCallHandler.Invoke(calls);
                
                ChatMessage fnCallMsg = new(ChatMessageRole.Assistant, string.Empty, Guid.NewGuid())
                {
                    ToolCalls = calls.Select(x => new ToolCall
                    {
                        FunctionCall = x, 
                        Type = "function", 
                        Id = x.Name
                    }).ToList(),
                    Content = null
                };

                if (MostRecentApiResult.Choices?.Count > 0 && MostRecentApiResult.Choices[0].FinishReason == VendorAnthropicChatMessageTypes.ToolUse)
                {
                    fnCallMsg.Content = MostRecentApiResult.Object;
                }

                result.AssistantMessage = fnCallMsg;
                AppendMessage(fnCallMsg);
    
                for (int i = 0; i < Math.Min(calls.Count, frs.Count); i++)
                {
                    ChatMessage fnResultMsg = new(ChatMessageRole.Tool, frs[i].Content, Guid.NewGuid())
                    {
                        ToolCallId = calls[i].Name
                    };
                    
                    AppendMessage(fnResultMsg);

                    result.ToolResults.Add(new ResolvedToolCall
                    {
                        Call = calls[i],
                        Result = frs[i],
                        ToolMessage = fnResultMsg
                    });
                }

                if (OnAfterToolsCall is not null)
                {
                    await OnAfterToolsCall(result);
                }   

                return;
            }
        }

        if (responseRole is not null)
        {
            AppendMessage(responseRole, responseStringBuilder.ToString(), messageId);
        }
    }

    #endregion
}