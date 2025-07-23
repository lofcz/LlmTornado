using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using LlmTornado.Responses.Events;
using LlmTornado.Threads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using FunctionCall = LlmTornado.ChatFunctions.FunctionCall;

namespace LlmTornado.Agents
{
    public partial class TornadoClient 
    {
        /// <summary>
        /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation requests.
        /// </summary>
        public CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();
        /// <summary>
        /// Model being ran
        /// </summary>
        public string Model { get; set; }
        public TornadoApi Client { get; set; }
        public ChatModel CurrentModel { get; set; }

        public bool UseResponseAPI { get; set; }

        public bool AllowComputerUse { get; set; }
        //public VectorSearchOptions? VectorSearchOptions { get; set; }
        public bool EnableWebSearch { get; set; } = false;

        //Need to add in some Converters first
        //public bool EnableCodeInterpreter { get; set; } = false;
        //Need MPC
        //Need LocalShell

        public TornadoClient(
            ChatModel model, List<ProviderAuthentication> provider, bool useResponseAPI = false, 
            bool allowComputerUse = false, 
            //VectorSearchOptions? searchOptions = null,
            bool enableWebSearch = false)
        {
            Model = model.Name;
            CurrentModel = model;
            Client = new TornadoApi(provider);
            UseResponseAPI = useResponseAPI;
            AllowComputerUse = allowComputerUse;
            //VectorSearchOptions = searchOptions;
            EnableWebSearch = enableWebSearch;
        }

        public TornadoClient(
            ChatModel model, Uri provider, bool useResponseAPI = false, bool allowComputerUse = false)
        {
            Model = model.Name;
            CurrentModel = model;
            Client = new TornadoApi(provider);
            UseResponseAPI = useResponseAPI;
            AllowComputerUse = allowComputerUse;
        }

        public TornadoClient(
            ChatModel model, TornadoApi client, bool useResponseAPI = false, bool allowComputerUse = false)
        {
            Model = model;
            CurrentModel = model;
            Client = client;
            UseResponseAPI = useResponseAPI;
            AllowComputerUse = allowComputerUse;
        }




        public async Task<ModelResponse> CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            return UseResponseAPI ? await CreateFromResponseAPIAsync(messages, options) : await CreateFromChatAPIAsync(messages, options);
        }


        public async Task<ModelResponse> CreateFromResponseAPIAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            ResponseRequest request = SetupResponseClient(messages, options);
            request.CancellationToken = CancelTokenSource.Token;
            ResponseResult response = await Client.Responses.CreateResponse(request);

            List<ModelItem> ModelItems = ConvertOutputItems(response.Output);

            options.PreviousResponseId = response.Id;

            return new ModelResponse(ModelItems, outputFormat: options.OutputFormat ?? null, messages, id: response.Id);
        }

        public async Task<ModelResponse> CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            if (AllowComputerUse)
            {
                throw new Exception("Cannot Stream while using Computer Model, try verbose callbacks");
            }

            return UseResponseAPI ? await StreamingResponseAPIAsync(messages, options, streamingCallback) : await StreamingChatAPIAsync(messages, options, streamingCallback);
        }


        public async Task<ModelResponse> CreateFromChatAPIAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            Conversation chat = Client.Chat.CreateConversation(CurrentModel);

            chat = SetupClient(chat, messages, options);

            //Create Open response
            RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe(CancelTokenSource.Token);

            //Convert the response back to Model
            //A bit redundant I can cache the current Model items already converted and only process the new ones
            List<ModelItem> ModelItems = ConvertFromProviderItems(response.Data!, chat).ToList();

            //Return results.
            return new ModelResponse(options.Model, [ConvertLastFromProviderItems(chat),], outputFormat: options.OutputFormat ?? null, ModelItems);
        }

        public async Task<ModelResponse> StreamingChatAPIAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            Conversation chat = Client.Chat.CreateConversation(CurrentModel);

            chat = SetupClient(chat, messages, options);

            return await HandleStreaming(chat, messages, options, streamingCallback);
        }
    }
}
