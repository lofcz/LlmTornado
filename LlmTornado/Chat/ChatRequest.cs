using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Completions;
using Newtonsoft.Json;
using LlmTornado.Code.Models;
using LlmTornado;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Mistral;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat;

/// <summary>
///     A request to the Chat API. This is similar, but not exactly the same as the
///     <see cref="Completions.CompletionRequest" />
///     Based on the <see href="https://platform.openai.com/docs/api-reference/chat">OpenAI API docs</see>
/// </summary>
public class ChatRequest
{
	/// <summary>
	///     Creates a new, empty <see cref="ChatRequest" />
	/// </summary>
	public ChatRequest()
    {
	   
    }

	/// <summary>
	///     Create a new chat request enlisted in a conversation, using the data from the input chat request.
	/// </summary>
	/// <param name="conversation"></param>
	/// <param name="basedOn"></param>
	internal ChatRequest(Conversation conversation, ChatRequest? basedOn)
	{
		OwnerConversation = conversation;

		if (basedOn is not null)
		{
			CopyData(basedOn);
		}
	}

	/// <summary>
	///     Create a new chat request using the data from the input chat request.
	/// </summary>
	/// <param name="basedOn"></param>
	public ChatRequest(ChatRequest? basedOn)
    {
	    if (basedOn is null)
	    {
		    return;
	    }

	    CopyData(basedOn);
    }

	private void CopyData(ChatRequest basedOn)
	{
		Model = basedOn.Model;
		Messages = basedOn.Messages;
		Temperature = basedOn.Temperature;
		TopP = basedOn.TopP;
		NumChoicesPerMessage = basedOn.NumChoicesPerMessage;
		StopSequence = basedOn.StopSequence;
		MultipleStopSequences = basedOn.MultipleStopSequences;
		MaxTokens = basedOn.MaxTokens;
		FrequencyPenalty = basedOn.FrequencyPenalty;
		PresencePenalty = basedOn.PresencePenalty;
		LogitBias = basedOn.LogitBias;
		Tools = basedOn.Tools;
		ToolChoice = basedOn.ToolChoice;
		OutboundFunctionsContent = basedOn.OutboundFunctionsContent;
		Adapter = basedOn.Adapter;
		VendorExtensions = basedOn.VendorExtensions;
		StreamOptions = basedOn.StreamOptions;
		TrimResponseStart = basedOn.TrimResponseStart;
		ParallelToolCalls = basedOn.ParallelToolCalls;
		Seed = basedOn.Seed;
		User = basedOn.User;
		ResponseFormat = basedOn.ResponseFormat;
		Audio = basedOn.Audio;
		Modalities = basedOn.Modalities;
		Metadata = basedOn.Metadata;
		Store = basedOn.Store;
		ReasoningEffort = basedOn.ReasoningEffort;
		Prediction = basedOn.Prediction;
		ServiceTier = basedOn.ServiceTier;
		Stream = basedOn.Stream;
		ReasoningBudget = basedOn.ReasoningBudget;
		Logprobs = basedOn.Logprobs;
		TopLogprobs = basedOn.TopLogprobs;
		WebSearchOptions = basedOn.WebSearchOptions;
	}

	/// <summary>
	///     The model to use for this request
	/// </summary>
	[JsonProperty("model")]
	[JsonConverter(typeof(ChatModelJsonConverter))]
	public ChatModel? Model { get; set; } = ChatModel.OpenAi.Gpt35.Turbo;
	
	/// <summary>
	///		Modalities of the model. Can be omitted for text only conversations.
	///		For audio, OpenAI requires both: <see cref="ChatModelModalities.Text"/> and <see cref="ChatModelModalities.Audio"/>, using only <see cref="ChatModelModalities.Audio"/> is invalid.
	/// </summary>
	[JsonProperty("modalities")]
	[JsonConverter(typeof(ModalitiesJsonConverter))]
	public List<ChatModelModalities>? Modalities { get; set; }
	
	/// <summary>
	///		Parameters for audio output. Required when audio output is requested with <see cref="Modalities"/>: ["<see cref="ChatModelModalities.Audio"/>"].
	///		Currently only works with OpenAI models.
	/// </summary>
	[JsonProperty("audio")]
	public ChatRequestAudio? Audio { get; set; }
	
	/// <summary>
	///		Developer-defined tags and values used for filtering completions in the dashboard.
	///		Currently only works with OpenAI models. Can be any object, e.g. new { test = 1 }
	/// </summary>
	[JsonProperty("metadata")]
	public object? Metadata { get; set; }
	
	/// <summary>
	///		Whether to store the output of this chat completion request for use in our model distillation or evals products.
	///		Currently only works with OpenAI models.
	/// </summary>
	[JsonProperty("store")]
	public bool? Store { get; set; }

	/// <summary>
	///     The messages to send with this Chat Request
	/// </summary>
	[JsonProperty("messages")]
    [JsonConverter(typeof(ChatMessageRequestMessagesJsonConverter))]
    public List<ChatMessage>? Messages { get; set; }

	/// <summary>
	///     What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative
	///     applications, and 0 (argmax sampling) for ones with a well-defined answer. It is generally recommend to use this or
	///     <see cref="TopP" /> but not both.
	/// </summary>
	[JsonProperty("temperature")]
    public double? Temperature { get; set; }

	/// <summary>
	///     An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the
	///     tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are
	///     considered. It is generally recommend to use this or <see cref="Temperature" /> but not both.
	/// </summary>
	[JsonProperty("top_p")]
    public double? TopP { get; set; }

	/// <summary>
	///     How many different choices to request for each message. Defaults to 1.
	/// </summary>
	[JsonProperty("n")]
    public int? NumChoicesPerMessage { get; set; }
	
	/// <summary>
	///     Balance option between response time & cost/latency. Currently supported only by O1, O1 Mini & Grok 3 series.
	/// </summary>
	[JsonProperty("reasoning_effort")]
	[JsonConverter(typeof(StringEnumConverter), true)]
	public ChatReasoningEfforts? ReasoningEffort { get; set; }
	
	/// <summary>
	///		Sets a token limit on reasoning. 0 disables reasoning. Currently supported by Google (natively "thinkingBudget") and Anthropic (natively "budget_tokens").<br/>
	///		Note: Some providers (Google) don't guarantee this limit is honored without under/over-flowing.<br/>
	///		Google: clamps to 0,1024-24576.<br/>
	///		Anthropic: 0,1024+
	/// </summary>
	[JsonIgnore]
	public int? ReasoningBudget { get; set; }
	
	/// <summary>
	/// Configuration for a Predicted Output, which can greatly improve response times when large parts of the model response are known ahead of time. This is most common when you are regenerating a file with only minor changes to most of the content.
	/// </summary>
	[JsonProperty("prediction")]
	public ChatRequestPrediction? Prediction { get; set; }
	
	/// <summary>
	///     The seed to use for deterministic requests.
	/// </summary>
	[JsonProperty("seed")]
    public int? Seed { get; set; }
	
	/// <summary>
	///     Specifies the latency tier to use for processing the request. This parameter is relevant for customers subscribed to the OpenAI scale tier service.
	/// </summary>
	[JsonProperty("service_tier")]
	public ChatRequestServiceTiers? ServiceTier { get; set; }

	/// <summary>
	///     The response format to use. If <see cref="ChatRequestResponseFormats.Json" />, either system or user message in the
	///     conversation must contain "JSON".
	/// </summary>
	[JsonProperty("response_format")]
    public ChatRequestResponseFormats? ResponseFormat { get; set; }

	/// <summary>
	///     Specifies the response should be streamed. When using abstractions, such as <see cref="Conversation"/>, this is set automatically by the library.
	/// </summary>
	[JsonProperty("stream")]
    public bool? Stream { get; set; }

	[JsonIgnore]
	internal bool StreamResolved => Stream ?? false;
	
	/// <summary>
	/// Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
	/// </summary>
	[JsonProperty("logprobs")]
	public bool? Logprobs { get; set; }
	
	/// <summary>
	/// An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
	/// </summary>
	[JsonProperty("top_logprobs")]
	public int? TopLogprobs { get; set; }
	
	/// <summary>
	/// This tool searches the web for relevant results to use in a response. Learn more about the web search tool.
	/// </summary>
	[JsonProperty("web_search_options")]
	public ChatRequestWebSearchOptions? WebSearchOptions { get; set; }
	
	/// <summary>
	///     The stream configuration.<br/>
	///		Note: by default Tornado includes usage for all providers.
	/// </summary>
	[JsonIgnore]
	public ChatStreamOptions? StreamOptions
	{
		get => StreamOptionsInternal;
		set
		{
			StreamOptionsInternal = value;
			StreamOptionsInternalSerialized = StreamOptionsInternal;
		}
	}

	[JsonIgnore]
	internal ChatStreamOptions? StreamOptionsInternal { get; set; }
	
	[JsonProperty("stream_options")]
	internal object? StreamOptionsInternalSerialized { get; set; }
	
	/// <summary>
	///     This is only used for serializing the request into JSON, do not use it directly.
	/// </summary>
	[JsonProperty("stop")]
    internal object? CompiledStop
    {
        get
        {
            return MultipleStopSequences?.Length switch
            {
                1 => StopSequence,
                > 0 => MultipleStopSequences,
                _ => null
            };
        }
    }

	/// <summary>
	///     One or more sequences where the API will stop generating further tokens. The returned text will not contain the
	///     stop sequence.
	/// </summary>
	[JsonIgnore]
    public string[]? MultipleStopSequences { get; set; }

	/// <summary>
	///     The stop sequence where the API will stop generating further tokens. The returned text will not contain the stop
	///     sequence.  For convenience, if you are only requesting a single stop sequence, set it here
	/// </summary>
	[JsonIgnore]
    public string? StopSequence
    {
        get => MultipleStopSequences?.FirstOrDefault() ?? null;
        set
        {
            if (value != null)
                MultipleStopSequences = [value];
        }
    }

	/// <summary>
	///     How many tokens to complete to. Can return fewer if a stop sequence is hit.
	///		Note Anthropic: Streaming is required when max_tokens is greater than 21,333 for Claude 3.7+ models.
	/// </summary>
	[JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

	/// <summary>
	///		Strategy for serializing <see cref="MaxTokens"/>.
	/// </summary>
	[JsonIgnore] 
	public ChatRequestMaxTokensSerializers MaxTokensSerializer { get; set; } = ChatRequestMaxTokensSerializers.Auto;

	/// <summary>
	///     The scale of the penalty for how often a token is used.  Should generally be between 0 and 1, although negative
	///     numbers are allowed to encourage token reuse.  Defaults to 0.
	/// </summary>
	[JsonProperty("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

	/// <summary>
	///     The scale of the penalty applied if a token is already present at all.  Should generally be between 0 and 1,
	///     although negative numbers are allowed to encourage token reuse.  Defaults to 0.
	/// </summary>
	[JsonProperty("presence_penalty")]
    public double? PresencePenalty { get; set; }

	/// <summary>
	///     Modify the likelihood of specified tokens appearing in the completion.
	///     Accepts a json object that maps tokens(specified by their token ID in the tokenizer) to an associated bias value
	///     from -100 to 100.
	///     Mathematically, the bias is added to the logits generated by the model prior to sampling.
	///     The exact effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of
	///     selection; values like -100 or 100 should result in a ban or exclusive selection of the relevant token.
	/// </summary>
	[JsonProperty("logit_bias")]
    public IReadOnlyDictionary<string, float>? LogitBias { get; set; }

	/// <summary>
	///     A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
	/// </summary>
	[JsonProperty("user")]
    public string? User { get; set; }

	/// <summary>
	///     A list of tools the model may generate JSON inputs for.
	/// </summary>
	[JsonProperty("tools")]
    public List<Tool>? Tools { get; set; }

	/// <summary>
	///     Parallel function calling can be disabled / enabled for vendors supporting the feature.
	///		As of 6/24, the only vendor supporting the feature is OpenAI.
	/// </summary>
	[JsonProperty("parallel_tool_calls")]
	public bool? ParallelToolCalls { get; set; }

	/// <summary>
	///     Represents an optional field when sending tools calling prompt.
	///     This field determines which function to call.
	/// </summary>
	/// <remarks>
	///     If this field is not specified, the default behavior ("auto") allows the model to automatically decide whether to
	///     call tools or not.
	///     Specify the name of the function to call in the "Name" attribute of the FunctionCall object.
	///     If you do not want the model to call any function, pass "None" for the "Name" attribute.
	/// </remarks>
	[JsonProperty("tool_choice")]
    [JsonConverter(typeof(OutboundToolChoice.OutboundToolChoiceConverter))]
    public OutboundToolChoice? ToolChoice { get; set; }

	/// <summary>
	///     If set the functions part of the outbound request encoded as JSON are stored here.
	///     This can be used a cheap heuristic for counting tokens used when streaming.
	///     Note that OpenAI silently transforms the provided JSON-schema into TypeScript and hence the real usage will be
	///     somewhat lower.
	/// </summary>
	[JsonIgnore]
    public Ref<string>? OutboundFunctionsContent { get; internal set; }

	/// <summary>
	///     This can be any API provider specific data. Currently used in KoboldCpp.
	/// </summary>
	[JsonProperty("adapter")]
    public Dictionary<string, object?>? Adapter { get; set; }
	
	/// <summary>
	///		Features supported only by a single/few providers with no shared equivalent.
	/// </summary>
	[JsonIgnore]
	public ChatRequestVendorExtensions? VendorExtensions { get; set; }

	/// <summary>
	///		Trims the leading whitespace and newline characters in the response. Unless you need to work with responses
	///		with leading whitespace it is recommended to keep this switch on. When streaming, some providers/models incorrectly
	///		produce leading whitespace if the text part of the streamed response is preceded with tool blocks.
	/// </summary>
	[JsonIgnore]
	public bool TrimResponseStart { get; set; } = true;

	/// <summary>
	///		Cancellation token to use with the request.
	/// </summary>
	[JsonIgnore]
	public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
	
	[JsonIgnore]
	internal string? UrlOverride { get; set; }
	
	[JsonIgnore]
	internal Conversation? OwnerConversation { get; set; }
	
	internal void OverrideUrl(string url)
	{
		UrlOverride = url;
	}
	
	private static readonly PropertyRenameAndIgnoreSerializerContractResolver MaxTokensRenamer = new PropertyRenameAndIgnoreSerializerContractResolver();
	private static readonly JsonSerializerSettings MaxTokensRenamerSettings = new JsonSerializerSettings
	{
		ContractResolver = MaxTokensRenamer,
		NullValueHandling = NullValueHandling.Ignore
	};
	
	static ChatRequest()
	{
		MaxTokensRenamer.RenameProperty(typeof(ChatRequest), "max_tokens", "max_completion_tokens");	
	}

	private static readonly FrozenDictionary<LLmProviders, Func<ChatRequest, IEndpointProvider, string>> SerializeMap = new Dictionary<LLmProviders, Func<ChatRequest, IEndpointProvider, string>>
	{
		{ 
			LLmProviders.OpenAi, (x, y) =>
			{
				if (x.Model is not null)
				{
					if (ChatModelOpenAi.ReasoningModelsAll.Contains(x.Model))
					{
						// reasoning models do not support temperature
						x.Temperature = null;
					}
				}

				switch (x.MaxTokensSerializer)
				{
					case ChatRequestMaxTokensSerializers.Auto:
					{
						if (x.Model is not null)
						{
							if (ChatModelOpenAi.ReasoningModelsAll.Contains(x.Model))
							{
								return JsonConvert.SerializeObject(x, MaxTokensRenamerSettings);
							}	
						}

						return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
					}
					case ChatRequestMaxTokensSerializers.MaxCompletionTokens:
					{
						return JsonConvert.SerializeObject(x, MaxTokensRenamerSettings);
					}
					default:
					{
						return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
					}
				}
			}
		},
		{ LLmProviders.DeepSeek, (x, y) => JsonConvert.SerializeObject(x, EndpointBase.NullSettings) },
		{ LLmProviders.Anthropic, (x, y) => JsonConvert.SerializeObject(new VendorAnthropicChatRequest(x, y), EndpointBase.NullSettings) },
		{ LLmProviders.Cohere, (x, y) => JsonConvert.SerializeObject(new VendorCohereChatRequest(x, y), EndpointBase.NullSettings) },
		{ LLmProviders.Google, (x, y) => JsonConvert.SerializeObject(new VendorGoogleChatRequest(x, y), EndpointBase.NullSettings) },
		{ 
			LLmProviders.Mistral, (x, y) =>
			{
				VendorMistralChatRequest request = new VendorMistralChatRequest(x, y);
				return request.Serialize();
			}
		},
		{ 
			LLmProviders.Groq, (x, y) =>
			{
				// fields unsupported by groq
				x.LogitBias = null; 
				return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
			} 
		},
		{ 
			LLmProviders.XAi, (x, y) =>
			{
				return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
			} 
		},
		{ 
			LLmProviders.Perplexity, (x, y) =>
			{
				return JsonConvert.SerializeObject(x, EndpointBase.NullSettings);
			} 
		}
	}.ToFrozenDictionary();

	/// <summary>
	///		Serializes the chat request into the request body, based on the conventions used by the LLM provider.
	/// </summary>
	/// <param name="provider"></param>
	/// <returns></returns>
	public TornadoRequestContent Serialize(IEndpointProvider provider)
	{
		if (OwnerConversation is not null)
		{
			Messages = OwnerConversation.Messages.ToList();
		}
		
		if (Messages is not null)
		{
			foreach (ChatMessage msg in Messages)
			{
				msg.Request = this;
			}	
		}

		ChatStreamOptions? storedOptions = null;
		bool restoreStreamOptions = false;
		
		if (!StreamResolved && StreamOptions is not null)
		{
			storedOptions = ChatStreamOptions.Duplicate(StreamOptions);
			StreamOptions = null;
			restoreStreamOptions = true;
		}
		else if (StreamResolved && StreamOptions is null)
		{
			storedOptions = null;
			StreamOptions = ChatStreamOptions.KnownOptionsIncludeUsage;
			restoreStreamOptions = true;
		}
		
		TornadoRequestContent serialized = SerializeMap.TryGetValue(provider.Provider, out Func<ChatRequest, IEndpointProvider, string>? serializerFn) ? new TornadoRequestContent(serializerFn.Invoke(this, provider), UrlOverride, provider, CapabilityEndpoints.Chat) : new TornadoRequestContent(string.Empty, UrlOverride, provider, CapabilityEndpoints.Chat);

		if (restoreStreamOptions)
		{
			StreamOptions = storedOptions;
		}
		
		return serialized;
	}
	
	internal class ModalitiesJsonConverter : JsonConverter<List<ChatModelModalities>>
	{
		public override void WriteJson(JsonWriter writer, List<ChatModelModalities>? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				return;
			}

			if (value.Count is 0)
			{
				return;
			}
			
			writer.WriteStartArray();

			foreach (ChatModelModalities x in value)
			{
				switch (x)
				{
					case ChatModelModalities.Audio:
						writer.WriteValue("audio");
						break;
					case ChatModelModalities.Text:
						writer.WriteValue("text");
						break;
				}
			}

			writer.WriteEndArray();
		}

		public override List<ChatModelModalities>? ReadJson(JsonReader reader, Type objectType, List<ChatModelModalities>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return existingValue;
		}
	}
    
    internal class ChatMessageRequestMessagesJsonConverter : JsonConverter<IList<ChatMessage>?>
    {
        public override void WriteJson(JsonWriter writer, IList<ChatMessage>? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            ChatRequest? request = null;
            bool isExpired = false;

            if (value.Count > 0)
            {
	            request = value[0].Request;
            }

            writer.WriteStartArray();

            foreach (ChatMessage msg in value)
            {
                writer.WriteStartObject();
                
                if (msg.Role is not null)
                {
	                writer.WritePropertyName("role");
	                
	                if (msg.Role is ChatMessageRoles.System)
	                {
		                if (request?.Model is not null && ChatModelOpenAiGpt4.ReasoningModels.Contains(request.Model))
		                {
			                writer.WriteValue("developer");
		                }
		                else
		                {
			                writer.WriteValue("system");
		                }
	                }
	                else
	                {
		                writer.WriteValue(ChatMessageRolesCls.MemberRolesDictInverse[msg.Role.Value]);   
	                }   
                }

                if (msg.Role is not null)
                {
	                switch (msg.Role)
	                {
		                case ChatMessageRoles.Tool:
		                {
			                writer.WritePropertyName("tool_call_id");
			                writer.WriteValue(msg.ToolCallId);
			                break;
		                }
		                case ChatMessageRoles.Assistant:
		                {
			                if (msg.Prefix is not null)
			                {
				                writer.WritePropertyName("prefix");
				                writer.WriteValue(msg.Prefix.Value);
			                }
			                
			                if (msg.ToolCalls is not null)
			                {
				                writer.WritePropertyName("tool_calls");

				                writer.WriteStartArray();

				                foreach (ToolCall call in msg.ToolCalls)
				                {
					                writer.WriteStartObject();

					                writer.WritePropertyName("id");
					                writer.WriteValue(call.Id);

					                writer.WritePropertyName("type");
					                writer.WriteValue(call.Type);

					                writer.WritePropertyName("function");
					                writer.WriteStartObject();

					                writer.WritePropertyName("name");
					                writer.WriteValue(call.FunctionCall.Name);

					                writer.WritePropertyName("arguments");
					                writer.WriteValue(call.FunctionCall.Arguments);

					                writer.WriteEndObject();

					                writer.WriteEndObject();
				                }

				                writer.WriteEndArray();
			                }

			                if (msg.Audio is not null)
			                {
				                if (request?.Audio?.CompressionStrategy is ChatAudioCompressionStrategies.Native or ChatAudioCompressionStrategies.PreferNative)
				                {
					                if (request.Audio.CompressionStrategy is ChatAudioCompressionStrategies.PreferNative)
					                {
						                isExpired = DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= msg.Audio.ExpiresAt;
					                }

					                if (!isExpired)
					                {
						                writer.WritePropertyName("audio");
						                writer.WriteStartObject();
				                
						                writer.WritePropertyName("id");
						                writer.WriteValue(msg.Audio.Id);
								
						                writer.WriteEndObject();    
					                }
				                }
			                }

			                break;
		                }
	                }
                }

                if (!string.IsNullOrWhiteSpace(msg.Name))
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(msg.Name);
                }

                if (msg is { Role: ChatMessageRoles.Tool, Content: null })
                {
	                goto closeMsgObj;
                }

                if (msg is { Role: ChatMessageRoles.Assistant, Content: null, ToolCalls: not null })
                {
	                goto closeMsgObj;
                }
                
                writer.WritePropertyName("content");

                if (msg.Parts?.Count > 0)
                {
                    writer.WriteStartArray();

                    foreach (ChatMessagePart part in msg.Parts)
                    {
                        writer.WriteStartObject();

                        writer.WritePropertyName("type");

                        string type = part.Type switch
                        {
	                        ChatMessageTypes.Text => "text",
	                        ChatMessageTypes.Image => "image_url",
	                        ChatMessageTypes.Audio => "input_audio",
	                        _ => "text"
                        };
                        
	                    writer.WriteValue(type);   
	                    
                        switch (part.Type)
                        {
	                        case ChatMessageTypes.Text:
	                        {
		                        writer.WritePropertyName("text");
		                        writer.WriteValue(part.Text);
		                        break;
	                        }
	                        case ChatMessageTypes.Image:
	                        {
		                        writer.WritePropertyName("image_url");
		                        writer.WriteStartObject();

		                        writer.WritePropertyName("url");
		                        writer.WriteValue(part.Image?.Url);

		                        writer.WriteEndObject();
		                        break;
	                        }
	                        case ChatMessageTypes.Audio:
	                        {
		                        writer.WritePropertyName("input_audio");
		                        writer.WriteStartObject();
								
		                        writer.WritePropertyName("data");
		                        writer.WriteValue(part.Audio?.Data);

		                        writer.WritePropertyName("format");

		                        if (part.Audio is not null)
		                        {
			                        switch (part.Audio.Format)
			                        {
				                        case ChatAudioFormats.Wav:
				                        {
					                        writer.WriteValue("wav");
					                        break;
				                        }
				                        case ChatAudioFormats.Mp3:
				                        {
					                        writer.WriteValue("mp3");
					                        break;
				                        }
			                        }
		                        }
		                        else
		                        {
			                        writer.WriteValue(string.Empty);
		                        }

		                        writer.WriteEndObject();
		                        break;
	                        }
                        }

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }
                else
                {
	                if (msg.Audio is not null)
	                {
		                if (request?.Audio?.CompressionStrategy is ChatAudioCompressionStrategies.OutputAsText or ChatAudioCompressionStrategies.PreferNative)
		                {
			                // only write transcription if the audio cache expired
			                if (request.Audio.CompressionStrategy is ChatAudioCompressionStrategies.PreferNative && !isExpired)
			                {
				                msg.Content = msg.Audio.Id;
				                goto writeNativeContent;
			                }
			                
			                writer.WriteValue(msg.Audio.Transcript);
			                goto closeMsgObj;
		                }
	                }
	                
	                writeNativeContent:
	                writer.WriteValue(msg.Content);
                }

                closeMsgObj:
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override IList<ChatMessage>? ReadJson(JsonReader reader, Type objectType, IList<ChatMessage>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return existingValue;
        }
    }
}